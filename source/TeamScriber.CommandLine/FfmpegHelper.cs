using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeamScriber.CommandLine;

namespace TeamScriber
{
    internal class FfmpegHelper
    {
        public static void GenerateAudio(Context context)
        {
            // Find ffmpeg executable path.  If it's in current directory: take it.  Otherwise, search in PATH
            var ffmpegPath = context.Options.FfmpegPath;
            context.Audios = new List<string>();

            if (string.IsNullOrEmpty(ffmpegPath))
            {
                ffmpegPath = Path.Combine(Directory.GetCurrentDirectory(), "ffmpeg.exe");
            }

            if (!File.Exists(ffmpegPath))
            {
                var enviromentPath = System.Environment.GetEnvironmentVariable("PATH");

                var paths = enviromentPath.Split(';');
                ffmpegPath = paths.Select(x => Path.Combine(x, "ffmpeg.exe"))
                                   .Where(x => File.Exists(x))
                                   .FirstOrDefault();
            }

            foreach (var video in context.Videos)
            {
                Console.WriteLine();
                Console.WriteLine("# Video processing to extract the audio");
                Console.WriteLine();
                Console.WriteLine($"Processing video: {video}");
                Console.WriteLine();

                var outputDirectory = context.Options.AudioOutputDirectory;
                if (string.IsNullOrEmpty(outputDirectory))
                {
                    outputDirectory = Path.GetDirectoryName(video);
                }
                else
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                var audio = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(video) + ".m4a");

                // Execute ffmpeg command to split the audio from the video
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = $"-i \"{video}\" -vn -acodec copy \"{audio}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                Console.WriteLine("Launching: " + processStartInfo.FileName + " " + processStartInfo.Arguments);
                Console.WriteLine();
                Console.WriteLine();

                Console.WriteLine(LogicConsts.LineSeparator);
                Console.WriteLine("## FFMPEG log: ");
                using var process = new Process();
                if (context.Options.Verbose)
                {
                    process.OutputDataReceived += (sender, args) => Console.WriteLine("    " + args.Data);
                    process.ErrorDataReceived += (sender, args) => Console.WriteLine("    " + args.Data);
                }
                process.StartInfo = processStartInfo;

                if (process.Start())
                {
                    if (context.Options.Verbose)
                    {
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                    }
                    process.WaitForExit();
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine(@"/!\ Error starting ffmpeg process.");
                    Console.WriteLine();
                    Environment.Exit(0);
                }

                context.ProgressInfo.Value++;
                context.ProgressRepporter?.Report(context.ProgressInfo);

                Console.WriteLine(LogicConsts.LineSeparator);
                Console.WriteLine();
                Console.WriteLine();

                Console.WriteLine($"--> Audio file generated: {audio}");
                Console.WriteLine();
                Console.WriteLine();

                context.Audios.Add(audio);
            }
        }
    }
}
