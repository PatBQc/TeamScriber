using Microsoft.VisualBasic;
using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.MediaFoundation;
using System.IO;

namespace TeamScriber
{
    internal class WhisperHelper
    {
        public async static Task GenerateTranscription(Context context)
        {
            if (!context.Options.UseWhisper)
            {
                return;
            }

            // Configure your OpenAI API key
            if (string.IsNullOrEmpty(context.Options.OpenAIAPIKey))
            {
                context.Options.OpenAIAPIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            }

            var openAiService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = context.Options.OpenAIAPIKey
            });

            foreach (var audio in context.Audios)
            {
                Console.WriteLine($"Processing audio: {audio}");
                Console.WriteLine();

                var outputDirectory = context.Options.TranscriptionOutputDirectory;
                if (string.IsNullOrEmpty(outputDirectory))
                {
                    outputDirectory = Path.GetDirectoryName(audio);
                }

                var transcription = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(audio) + ".txt");

                var audioFileContent = await System.IO.File.ReadAllBytesAsync(audio);
                var audioChunks = SplitAudioIntoChunks(audio, audioFileContent, TimeSpan.FromMinutes(10));

                //foreach (var audioChunk in audioChunks)
                await Task.WhenAll(audioChunks.Select(async audioChunk =>
                {
                    int retryCount = 5;
                    bool success = false;

                    while (!success && retryCount >= 0)
                    {
                        try
                        {
                            Console.WriteLine("Transcription of " + audio);
                            Console.WriteLine("Segment size: " + audioChunk.AudioSegment.Length + " bytes");


                            var audioResult = await openAiService.Audio.CreateTranscription(new AudioCreateTranscriptionRequest
                            {
                                FileName = "audio.mp3",
                                File = audioChunk.AudioSegment,
                                Model = Models.WhisperV1,
                                Language = context.Options.WhisperLanguage,
                                ResponseFormat = StaticValues.AudioStatics.ResponseFormat.VerboseJson
                            });

                            if (audioResult.Successful)
                            {
                                success = true;
                                audioChunk.Text = audioResult.Text;
                                Console.WriteLine($"Transcription for chunk {audioChunk.ID}:" + Environment.NewLine + audioChunk.Text);
                            }
                            else
                            {
                                if (audioResult.Error == null)
                                {
                                    Console.WriteLine("/!\\ Did not receive a successful response from Whisper API /!\\");
                                }
                                else
                                {
                                    Console.WriteLine("/!\\ Did not receive a successful response from Whisper API /!\\"
                                            + Environment.NewLine + "Error " + audioResult.Error.Code + ": " + audioResult.Error.Message);
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("/!\\ Did not receive a proper response from Whisper API /!\\" + Environment.NewLine + ex.ToString()); ;
                        }
                        --retryCount;
                        if(!success)
                        {
                            Console.WriteLine("Retrying...");
                        }
                    }
                    Console.WriteLine();
                }));

                Console.WriteLine("Finished transcription of " + audio);
                Console.WriteLine();

                var fullTranscriptionText = string.Join(Environment.NewLine, audioChunks.OrderBy(x => x.ID).Select(x => x.Text));
                File.WriteAllText(transcription, fullTranscriptionText);

            } // foreach audio file
        }

        private static List<AudioChunk> SplitAudioIntoChunks(string audiofilename, byte[] audioFileContent, TimeSpan chunkSize)
        {
            var audioChunks = new List<AudioChunk>();

            using var bigFileReader = new MediaFoundationReader(audiofilename);

            int segmentIndex = 0;
            TimeSpan currentPosition = TimeSpan.Zero;
            int bytesPerSecond = bigFileReader.WaveFormat.AverageBytesPerSecond;
            int bytesPerSegment = (int)(bytesPerSecond * chunkSize.TotalSeconds);

            int estimatedChunks = (int)Math.Ceiling(bigFileReader.TotalTime.TotalSeconds / chunkSize.TotalSeconds);

            while (currentPosition < bigFileReader.TotalTime)
            {
                Console.WriteLine($"Spliting and converting audio to chunk #{segmentIndex} of {estimatedChunks} to send to Whisper");

                // First, read the M4A from the big file and convert it to WAV data
                using var wavStream = new MemoryStream();
                using var wavWriter = new WaveFileWriter(wavStream, bigFileReader.WaveFormat);

                byte[] wavBuffer = new byte[bytesPerSegment];
                int bytesRead = bigFileReader.Read(wavBuffer, 0, bytesPerSegment);
                wavWriter.Write(wavBuffer, 0, bytesRead);
                wavWriter.Flush();
                wavWriter.Close();


                // Second, convert the WAV data in the MemoryStream to MP3
                using var secondWavStream = new MemoryStream(wavStream.ToArray());
                using var wavReader = new WaveFileReader(secondWavStream);
                using var mp3Stream = new MemoryStream();

                MediaFoundationEncoder.EncodeToMp3(wavReader, mp3Stream, 192000);
                mp3Stream.Flush();
                wavReader.Flush();
                byte[] mp3Bytes = mp3Stream.ToArray();

                audioChunks.Add(new AudioChunk() { ID = segmentIndex++, AudioSegment = mp3Bytes, Text = string.Empty });

                string outputFilePath = $"{audiofilename}-{segmentIndex.ToString("0000")}.mp3";

                currentPosition += chunkSize;
            }

            Console.WriteLine();
            Console.WriteLine($"Audio split and conversion done");

            return audioChunks;
        }


    } // end of class
}






