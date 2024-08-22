using System;
using System.Diagnostics;
using CommandLine;
using CommandLine.Text;
using TeamScriber;


// ffmpeg -i AI-01.m4a -vn -acodec copy Session-AI-01.m4a

var result = Parser.Default.ParseArguments<CommandLineOptions>(args)
    .WithParsed(options =>
    {
        // Use options.TeamsVideoPaths, options.OutputPath, and options.Verbose variables here
        Console.WriteLine($"File path: {options.TeamsVideoPaths}");
        Console.WriteLine($"Output file path: {options.AudioOutputDirectory}");
        Console.WriteLine($"Verbose mode: {options.Verbose}");
        Console.WriteLine();
    })
    .WithNotParsed(errors =>
    {
        Console.WriteLine("Error parsing command line arguments.");
        Console.WriteLine(CommandLineOptions.GetUsage());
        Environment.Exit(0);
    });

Context context = new Context();
context.Options = result.Value;

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

if (context.Options.UseWhisper)
{
    await WhisperHelper.GenerateTranscription(context);
}

