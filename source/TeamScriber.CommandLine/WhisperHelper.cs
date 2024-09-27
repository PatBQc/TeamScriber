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
using System.Text.Json;

namespace TeamScriber
{
    internal class WhisperHelper
    {
        // Required to control Azure throtling async work
        private const int MaxCallsPerMinute = 3;
        private static readonly TimeSpan CallInterval = TimeSpan.FromSeconds(20); // 60 seconds / 3 calls = 20 seconds per call
        private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1); // Only one concurrent call at a time
        private static DateTime lastApiCallTime = DateTime.MinValue;

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

            var openAiOptions = new OpenAiOptions()
            {
                ApiKey = context.Options.OpenAIAPIKey
            };

            // Set the base path if provided
            if (!string.IsNullOrEmpty(context.Options.OpenAIBasePath))
            {
                openAiOptions.BaseDomain = context.Options.OpenAIBasePath;
            }

            var openAiService = new OpenAIService(openAiOptions);

            context.Transcriptions = new List<string>();

            foreach (var audio in context.Audios)
            {
                Console.WriteLine($"Processing audio on file: {audio}");
                Console.WriteLine();

                var outputDirectory = context.Options.TranscriptionOutputDirectory;
                if (string.IsNullOrEmpty(outputDirectory))
                {
                    outputDirectory = Path.GetDirectoryName(audio);
                    if (string.IsNullOrEmpty(outputDirectory))
                    {
                        outputDirectory = Directory.GetCurrentDirectory();
                    }
                    else
                    {
                        Directory.CreateDirectory(outputDirectory);
                    }
                    Console.WriteLine($"Output directory is not set. Defaulting to current directory: {outputDirectory}");
                }
                else
                {
                    Directory.CreateDirectory(outputDirectory);
                    Console.WriteLine($"Using configured output directory: {outputDirectory}");
                }

                var transcription = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(audio) + ".txt");
                context.Transcriptions.Add(transcription);

                var audioFileContent = await System.IO.File.ReadAllBytesAsync(audio);
                var audioChunks = SplitAudioIntoChunks(audio, audioFileContent, TimeSpan.FromMinutes(10), context);

                await Task.WhenAll(audioChunks.Select(async audioChunk =>
                {
                    int retryCount = 5;
                    bool success = false;

                    while (!success && retryCount >= 0)
                    {
                        // Required to control Azure throtling async work
                        if (context.Options.WhisperAzureThrottle)
                            await semaphore.WaitAsync();

                        try
                        {
                            // Required to control Azure throtling async work
                            if (context.Options.WhisperAzureThrottle)
                            {
                                // Respect API rate limit
                                var timeSinceLastCall = DateTime.Now - lastApiCallTime;
                                lastApiCallTime = DateTime.Now;

                                if (timeSinceLastCall < CallInterval)
                                {
                                    var delay = CallInterval - timeSinceLastCall;
                                    Console.WriteLine($"Waiting for {delay.TotalSeconds} seconds due to rate limiting...");

                                    // Let's remove this delay for now, as I am working with OpenAI directly for the moment
                                    await Task.Delay(delay);
                                }
                            }

                            Console.WriteLine($"Transcribing chunk #{audioChunk.ID} of {audioChunks.Count}");

                            var responseFormat = context.Options.IncludeTimestamps ? StaticValues.AudioStatics.ResponseFormat.Srt : StaticValues.AudioStatics.ResponseFormat.VerboseJson;

                            var audioResult = await openAiService.Audio.CreateTranscription(new AudioCreateTranscriptionRequest
                            {
                                FileName = "audio.mp3",
                                File = audioChunk.AudioSegment,
                                Model = Models.WhisperV1,
                                Language = context.Options.WhisperLanguage,
                                ResponseFormat = responseFormat
                            });

                            if (audioResult.Successful)
                            {
                                success = true;
                                audioChunk.Text = responseFormat == StaticValues.AudioStatics.ResponseFormat.Srt ? ExtractPlainText(audioResult.Text) : audioResult.Text;
                                Console.WriteLine($"Transcription for chunk #{audioChunk.ID} of {audioChunks.Count} done.");
                                if (context.Options.Verbose)
                                {
                                    Console.WriteLine($"Transcription for chunk {audioChunk.ID}:" + Environment.NewLine + audioChunk.Text);
                                    Console.WriteLine();
                                }
                            }
                            else
                            {
                                if (audioResult.Error == null)
                                {
                                    Console.WriteLine();
                                    Console.WriteLine($"/!\\ Did not receive a successful response from Whisper API on chunk #{audioChunk.ID} /!\\");
                                    Console.WriteLine();
                                }
                                else
                                {
                                    Console.WriteLine();
                                    Console.WriteLine($"/!\\ Did not receive a successful response from Whisper API on chunk #{audioChunk.ID} /!\\");
                                    Console.WriteLine("Error " + audioResult.Error.Code + ": " + audioResult.Error.Message);
                                    Console.WriteLine();
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine();
                            Console.WriteLine($"/!\\ Did not receive a proper response from Whisper API on chunk #{audioChunk.ID} /!\\");
                            Console.WriteLine(ex.ToString());
                            Console.WriteLine();
                        }
                        finally
                        {
                            // Required to control Azure throtling async work
                            if (context.Options.WhisperAzureThrottle)
                            {
                                semaphore.Release();
                            }
                        }

                        --retryCount;

                        if (!success)
                        {
                            Console.WriteLine();
                            Console.WriteLine($"Retrying chunk #{audioChunk.ID}...");
                            Console.WriteLine();
                        }
                    }
                }));

                Console.WriteLine("Finished transcription of " + audio);
                Console.WriteLine();

                // Adjust timestamps before concatenation
                if (context.Options.IncludeTimestamps)
                {
                    AdjustTimestamps(audioChunks);
                }


                var fullTranscriptionText = string.Join(Environment.NewLine, audioChunks.OrderBy(x => x.ID).Select(x => x.Text));
                File.WriteAllText(transcription, fullTranscriptionText);

            } // foreach audio file
        }

        private static void AdjustTimestamps(List<AudioChunk> audioChunks)
        {
            TimeSpan cumulativeDuration = TimeSpan.Zero;

            foreach (var chunk in audioChunks.OrderBy(x => x.ID))
            {
                if (!string.IsNullOrEmpty(chunk.Text))
                {
                    var lines = chunk.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var parts = lines[i].Split(' ');
                        if (parts.Length > 0 && TimeSpan.TryParse(parts[0], out TimeSpan timestamp))
                        {
                            var newTimestamp = timestamp + cumulativeDuration;
                            lines[i] = newTimestamp.ToString(@"hh\:mm\:ss");
                        }
                    }
                    chunk.Text = string.Join(Environment.NewLine, lines);
                }
                cumulativeDuration += TimeSpan.FromMinutes(10); // Assuming each chunk is 10 minutes for simplicity
            }
        }

        private static List<AudioChunk> SplitAudioIntoChunks(string audiofilename, byte[] audioFileContent, TimeSpan chunkSize, Context context)
        {
            var audioChunks = new List<AudioChunk>();
            int segmentIndex = 0;
            TimeSpan currentPosition = TimeSpan.Zero;

            // Utilisation de MediaFoundationReader pour tous les types de fichiers, y compris MP3 et M4A
            using (var reader = new MediaFoundationReader(audiofilename))
            {
                var estimatedChunks = (int)Math.Ceiling(reader.TotalTime.TotalSeconds / chunkSize.TotalSeconds);
                long targetBytes = (long)(chunkSize.TotalSeconds * reader.WaveFormat.AverageBytesPerSecond);

                while (currentPosition < reader.TotalTime)
                {
                    Console.WriteLine($"Splitting and converting audio to chunk #{segmentIndex + 1} of {estimatedChunks} to send to Whisper");

                    using (var segmentStream = new MemoryStream())
                    {
                        byte[] buffer = new byte[1024];
                        long bytesWritten = 0;

                        while (bytesWritten < targetBytes)
                        {
                            int bytesRead = reader.Read(buffer, 0, buffer.Length);
                            if (bytesRead == 0)
                                break;

                            segmentStream.Write(buffer, 0, bytesRead);
                            bytesWritten += bytesRead;
                        }

                        segmentStream.Flush();
                        byte[] segmentBytes;

                        // Réinitialise la position du MemoryStream pour bien encoder en MP3
                        segmentStream.Position = 0;

                        using (var mp3Stream = new MemoryStream())
                        {
                            MediaFoundationEncoder.EncodeToMp3(new RawSourceWaveStream(segmentStream, reader.WaveFormat), mp3Stream);
                            mp3Stream.Flush();
                            segmentBytes = mp3Stream.ToArray();
                        }

                        audioChunks.Add(new AudioChunk() { ID = ++segmentIndex, AudioSegment = segmentBytes, Text = string.Empty });

                        // Enregistrer le segment dans le répertoire de sortie
                        var outputDirectory = context.Options.AudioOutputDirectory ?? Directory.GetCurrentDirectory();
                        var segmentFilename = Path.Combine(outputDirectory, $"{Path.GetFileNameWithoutExtension(audiofilename)}-part-{segmentIndex}.mp3");
                        File.WriteAllBytes(segmentFilename, segmentBytes);

                        currentPosition += chunkSize;
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Audio split (10 minutes max) and conversion (mp3) done.");
            Console.WriteLine();

            return audioChunks;
        }

        private static string ExtractPlainText(string srtJsonResponse)
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(srtJsonResponse);
                if (jsonDoc.RootElement.TryGetProperty("text", out var textElement))
                {
                    var srtLines = textElement.GetString().Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
                    var plainTextBuilder = new StringBuilder();
                    foreach (var srtLine in srtLines)
                    {
                        var srtParts = srtLine.Split('\n');
                        if (srtParts.Length >= 3)
                        {
                            var timestamp = srtParts[1]; // Second line is the timestamp
                            var text = string.Join(" ", srtParts.Skip(2)); // Skip ID and get only the text
                            plainTextBuilder.AppendLine(timestamp);
                            plainTextBuilder.AppendLine(text);
                            plainTextBuilder.AppendLine(); // Add an extra new line for readability
                        }
                    }
                    return plainTextBuilder.ToString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing SRT JSON response: {ex.Message}");
            }
            return string.Empty;
        }

    } // end of class
}

