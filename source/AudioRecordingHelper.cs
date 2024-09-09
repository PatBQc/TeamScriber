using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudio.Lame;
using NAudio.CoreAudioApi;
using System;
using System.IO;
using System.Threading.Tasks;
using TeamScriber;

namespace TeamScriber
{
    internal static class AudioRecordingHelper
    {
        internal static async Task RecordAudioAsync(Context context)
        {
            Console.WriteLine("Starting audio recording. Press Enter to stop recording.");

            context.Audios = new List<string>();
            var outputDirectory = context.Options.AudioOutputDirectory;
            var safeOutputDirectory = outputDirectory ?? Directory.GetCurrentDirectory();
            var outputFileName = $"RecordedAudio_{DateTime.Now:yyyyMMdd_HHmmss}";

            using var microphoneCapture = new WaveInEvent();
            using var speakerCapture = new WasapiLoopbackCapture();

            // Display the names of the audio capture devices
            Console.WriteLine($"Microphone Device: {WaveInEvent.GetCapabilities(microphoneCapture.DeviceNumber).ProductName}");
            var enumerator = new MMDeviceEnumerator();
            var speakerDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
            Console.WriteLine($"Speaker Device: {speakerDevice.FriendlyName}");

            string microphoneFile = Path.Combine(safeOutputDirectory, $"{outputFileName}_Microphone.wav");
            string speakerFile = Path.Combine(safeOutputDirectory, $"{outputFileName}_Speaker.wav");
            string combinedFile = Path.Combine(safeOutputDirectory, $"{outputFileName}_Combined.wav");

            using (var micWriter = new WaveFileWriter(microphoneFile, microphoneCapture.WaveFormat))
            using (var speakerWriter = new WaveFileWriter(speakerFile, speakerCapture.WaveFormat))
            {
                microphoneCapture.DataAvailable += (s, e) =>
                {
                    micWriter.Write(e.Buffer, 0, e.BytesRecorded);
                };

                speakerCapture.DataAvailable += (s, e) =>
                {
                    speakerWriter.Write(e.Buffer, 0, e.BytesRecorded);
                };

                microphoneCapture.StartRecording();
                speakerCapture.StartRecording();

                await Task.Run(() => Console.ReadLine());

                microphoneCapture.StopRecording();
                speakerCapture.StopRecording();
            }

            Console.WriteLine("Recording stopped. Combining audio...");

            // Combine the microphone and speaker WAV files
            using (var micReader = new AudioFileReader(microphoneFile))
            using (var speakerReader = new AudioFileReader(speakerFile))
            {
                // Convert microphone to 16 bit PCM, 44.1 kHz, and ensure mono output
                var micResampler = new WdlResamplingSampleProvider(micReader.ToSampleProvider(), 44100);
                ISampleProvider micConverted;
                if (micReader.WaveFormat.Channels == 1)
                {
                    micConverted = micResampler; // already mono
                }
                else
                {
                    micConverted = new StereoToMonoSampleProvider(micResampler); // convert to mono
                }

                // Convert speaker to 16 bit PCM, 44.1 kHz, and ensure mono output
                var speakerResampler = new WdlResamplingSampleProvider(speakerReader.ToSampleProvider(), 44100);
                ISampleProvider speakerConverted;
                if (speakerReader.WaveFormat.Channels == 1)
                {
                    speakerConverted = speakerResampler; // already mono
                }
                else
                {
                    speakerConverted = new StereoToMonoSampleProvider(speakerResampler); // convert to mono
                }

                // Mix the two mono streams together
                var mixer = new MixingSampleProvider(new[] { micConverted, speakerConverted });
                WaveFileWriter.CreateWaveFile16(combinedFile, mixer);
            }

            Console.WriteLine("Converting to MP3...");

            // Convert WAV to MP3
            var mp3Path = Path.Combine(safeOutputDirectory, $"{outputFileName}_Combined.mp3");
            using (var reader = new AudioFileReader(combinedFile))
            using (var writer = new LameMP3FileWriter(mp3Path, reader.WaveFormat, LAMEPreset.STANDARD))
            {
                reader.CopyTo(writer);
            }

            Console.WriteLine("Recording and conversion complete.");
            context.Audios.Add(mp3Path);
            Console.WriteLine("Conversion complete. Proceeding with transcription...");
        }
    }
}