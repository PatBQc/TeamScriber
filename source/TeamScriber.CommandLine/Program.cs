using CommandLine;
using TeamScriber;
using System;
using System.IO;
using System.Threading.Tasks;

// ffmpeg -i AI-01.m4a -vn -acodec copy Session-AI-01.m4a

class Program
{
    static async Task Main(string[] args)
    {
        var result = Parser.Default.ParseArguments<CommandLineOptions>(args)
            .WithParsed(options =>
            {
                if (options.Help)
                {
                    Console.WriteLine(CommandLineOptions.GetUsage(args));
                    Environment.Exit(0);
                }

                // Use options.TeamsVideoPaths, options.OutputPath, and options.Verbose variables here
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

        if (context.Options.RecordAudio)
        {
            await AudioRecordingHelper.RecordAudioAsync(context);
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
        }
    }
}