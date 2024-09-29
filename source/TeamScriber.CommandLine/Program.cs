using CommandLine;
using TeamScriber;
using System;
using System.IO;
using System.Threading.Tasks;

// ffmpeg -i AI-01.m4a -vn -acodec copy Session-AI-01.m4a

namespace TeamScriber.CommandLine
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await Program.Main(args, null);
        }

        public static async Task Main(string[] args, IProgress<ProgressInfo>? progress)
        {
            if(progress == null)
            {
                // just do nothing with the reporting
                progress = new Progress<ProgressInfo>();
            }

            var progressInfo = new ProgressInfo();

            var result = Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsed(options =>
                {
                    if (options.Help)
                    {
                        Console.WriteLine(CommandLineOptions.GetUsage(args));
                        Environment.Exit(0);
                    }

                    // Use options.TeamsVideoPaths, options.OutputPath, and options.Verbose variables here
                    Console.WriteLine("# Starting TeamScriber...");
                    Console.WriteLine();
                    Console.WriteLine($"File path: {options.TeamsVideoPaths}");
                    Console.WriteLine($"Output file path: {options.AudioOutputDirectory}");
                    Console.WriteLine($"Verbose mode: {options.Verbose}");
                    Console.WriteLine($"OpenAI Base Path: {options.OpenAIBasePath}");
                    Console.WriteLine($"Include Timestamps: {options.IncludeTimestamps}");
                    Console.WriteLine();
                })
                .WithNotParsed(errors =>
                {
                    Console.WriteLine("Error parsing command line arguments.");
                    Console.WriteLine(CommandLineOptions.GetUsage(args));
                    Environment.Exit(1);
                });

            Context context = new Context();
            context.Options = result.Value;
            context.ProgressRepporter = progress;
            context.ProgressInfo = progressInfo;
            context.ProgressInfo.MaxValue = 100; // temporary value, will be updated later


            if (context.Options.RecordAudio)
            {
                await AudioRecordingHelper.RecordAudioAsync(context);
                progressInfo.Value++;
                progress.Report(progressInfo);
            }
            else
            {
                // Find all the videos to transcribe
                context.Videos = new List<string>();
                foreach (var video in context.Options.TeamsVideoPaths.Split('|'))
                {
                    // If the specified path is a directory, get all the mp4 files in the directory
                    if (Directory.Exists(video))
                    {
                        var newVideos = Directory.GetFiles(video, "*.mp4").ToList();
                        newVideos.Sort((x, y) => string.Compare(Path.GetFileNameWithoutExtension(x), Path.GetFileNameWithoutExtension(y)));
                        context.Videos.AddRange(newVideos);
                    }
                    else
                    {
                        context.Videos.Add(video);
                    }
                }

                progressInfo.MaxValue = GetMaxProgressValue(context);

                FfmpegHelper.GenerateAudio(context);
            }

            if (context.Options.UseWhisper)
            {
                await WhisperHelper.GenerateTranscription(context);
            }

            if (context.Options.UsePromptsQueries)
            {
                var vendor = context.Options.Model.Split(':')[0].ToLower();
                switch (vendor)
                {
                    case "openai":
                        await OpenAIGptHelper.GenerateAnswers(context);
                        break;
                    case "anthropic":
                        await AnthropicClaudeHelper.GenerateAnswers(context);
                        break;
                    default:
                        Console.WriteLine($"/!\\ Model vendor \"{vendor}\" not supported.");
                        Environment.Exit(1);
                        break;
                }

                await HtmlHelper.GenerateHtml(context);
            }

            if(context.Options.ImportInOneNote)
            {
                await OneNoteHelper.ImportInOneNote(context);
            }

            Console.WriteLine("# Finished!");
            Console.WriteLine();
            Console.WriteLine("Congrats!  You Team session is now imported in OneNote!");
            Console.WriteLine();
            Console.WriteLine("If you liked it, please consider giving us a star on GitHub and share the news!");
            Console.WriteLine();
            Console.WriteLine("              -->    https://github.com/PatBQc/TeamScriber    <--");
            Console.WriteLine();
            Console.WriteLine();
        }

        private static int GetMaxProgressValue(Context context)
        {
            int steps = 0;

            if (context.Options.RecordAudio)
            {
                steps++; // Step to record audio
            }
            else
            {
                // Count the number of videos to transcribe
                foreach (var video in context.Videos)
                {
                    steps++; // Step to transcribe each video to single audio file

                    steps += LogicConsts.ProgressWeightAudioPerVideo; // Step to generate audio files from video
                }
            }

            if (context.Options.UseWhisper)
            {
                steps += LogicConsts.ProgressWeightWhisperConversion; // Step to generate transcription using Whisper
            }

            if (context.Options.UsePromptsQueries)
            {
                steps += LogicConsts.ProgressWeightPromptQueries; // Step to generate answers using prompts
            }

            if(context.Options.ImportInOneNote)
            {
                steps += LogicConsts.ProgressWeightOneNoteImport; // Step to import to OneNote
            }

            return steps;
        }
    }
}