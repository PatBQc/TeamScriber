using CommandLine.Text;
using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TeamScriber
{
    internal class CommandLineOptions
    {
        [Option('h', "help", Required = false, HelpText = "Display help information about how to use TeamScriber.")]
        public bool Help { get; set; }

        [Option('i', "input", Required = true,
            HelpText =
            """
            Teams video file. 
            Can be one video or a collection of video files separated by a pipe | char.
            Can also be a directory containing video files of a collection of directories seperated by a pipe | char.
            """)]
        public string TeamsVideoPaths { get; set; }

        [Option('a', "audio", Required = false, Default = "",
            HelpText =
            """
            Audio output directory.  
            The audio files generated will have the same filename as the video, but extention will become .m4a.
            If not specified, the audio files will be generated in the same directory as the video files.
            """)]
        public string AudioOutputDirectory { get; set; }

        [Option('f', "ffmpeg", Required = false, Default = "",
            HelpText =
            """
            Path to ffmpeg executable.  
            If not specified, the application will look for ffmpeg in the current directory and in the PATH.
            """)]
        public string FfmpegPath { get; set; }

        [Option('w', "whisper", Required = false, Default = true,
            HelpText =
            """
            Use OpenAI Whisper for audio transcription.
            Default is true.
            """)]
        public bool UseWhisper { get; set; }

        [Option('l', "language", Required = false, Default = "",
            HelpText =
            """
            OpenAI Whisper language to use. Must be in ISO-639-1 format (ex: en for English, fr for French, ...).
            Default is empty ("") letting Whisper figure out the language.
            """)]
        public string WhisperLanguage { get; set; }


        [Option("whisper-azure-throttle", Required = false, Default = false,
            HelpText =
            """
            Use this switch to throttle the number of requests to Azure Whisper API.  
            It will respect the rate limit of no more then 3 calls per minute.
            """)]
        public bool WhisperAzureThrottle { get; set; }

        [Option('k', "openai-api-key", Required = false, Default = "",
            HelpText =
            """
            OpenAI Whisper API model key. If not specified, it will take what is in the OPENAI_API_KEY os environment variable.
            """)]
        public string OpenAIAPIKey { get; set; }

        [Option('b', "openai-base-path", Required = false, Default = "",
            HelpText =
            """
            OpenAI API base path. If specified, this will be used as the base URL for OpenAI API calls (Whisper and ChatCompletion).
            This is useful when a proxy is used for OpenAI API calls.
            """)]
        public string OpenAIBasePath { get; set; }

        [Option('t', "transcription", Required = false, Default = "",
            HelpText =
            """
            Transcription output directory.  
            The transcription files generated will have the same filename as the audio, but extention will become .txt.
            If not specified, the transcription files will be generated in the same directory as the audio files.
            """)]
        public string TranscriptionOutputDirectory { get; set; }

        [Option('q', "prompts-queries", Required = false, Default = true,
            HelpText =
            """
            Do you want to perform as analysis on your transcription using prompts.
            Default is true.
            """)]
        public bool UsePromptsQueries { get; set; }

        [Option('p', "prompts", Required = false, Default = "https://raw.githubusercontent.com/PatBQc/TeamScriber/main/prompts/prompts.txt",
            HelpText =
            """
            The prompts file containing the list of questions to ask the model on the generated transcriptions.
            This can point to a local file or a URL.
            Default is https://raw.githubusercontent.com/PatBQc/TeamScriber/main/prompts/prompts.txt
            """)]
        public string Prompts { get; set; }

        [Option('s', "prompts-system", Required = false, Default = "https://raw.githubusercontent.com/PatBQc/TeamScriber/main/prompts/promptSystem.txt",
            HelpText =
            """
            The prompts file containing the system prompts used to start the conversation with the model.
            This can point to a local file or a URL.
            Default is https://raw.githubusercontent.com/PatBQc/TeamScriber/main/prompts/promptSystem.txt
            """)]
        public string SystemPrompts { get; set; }

        [Option('m', "model", Required = false, Default = "Anthropic:claude-3-5-sonnet-20240620",
            HelpText =
            """
            The model to use to perform the analysis.
            The format is model-company:model-name.  The tooling also supports OpenAI models (ex: OpenAI:gpt-4o)
            Default is "Anthropic:claude-3-5-sonnet-20240620", 
            """)]
        public string Model { get; set; }

        [Option('o', "questions", Required = false, Default = "",
            HelpText =
            """
            Questions output directory.  That's where the questions ans answers files generated by prompting the model with the transcript will be. 
            The files will have the same filename as the transcripts, but extention will become .md.
            If not specified, the files will be generated in the same directory as the transcription files.
            """)]
        public string QuestionsOutputDirectory { get; set; }

        [Option('c', "anthropic-api-key", Required = false, Default = "",
            HelpText =
            """
            Anthropic API model key. If not specified, it will take what is in the ANTHROPIC_API_KEY os environment variable.
            """)]
        public string AnthropicAPIKey { get; set; }

        [Option('v', "verbose", Required = false, Default = false,
            HelpText =
            """
            Display execution details.
            Default is false.
            """)]
        public bool Verbose { get; set; }

        [Option('r', "record", Required = false, HelpText = "Start by recording audio from default microphone and speaker")]
        public bool RecordAudio { get; set; }

        [Option("timestamps", Required = false, Default = false,
            HelpText = "Include timestamps in the Whisper transcription output")]
        public bool IncludeTimestamps { get; set; }

        public static string GetUsage(string[] args)
        {
            var help = new HelpText
            {
                Heading = new HeadingInfo("TeamScriber", "1.0"),
                AdditionalNewLineAfterOption = true,
                AddDashesToOption = true
            };

            help.AddPreOptionsLine("Usage: TeamScriber.exe [options]");
            help.AddPreOptionsLine("");
            help.AddPreOptionsLine("Options:");

            var result = Parser.Default.ParseArguments<CommandLineOptions>(args);
            help.AddOptions(result);

            help.AddPostOptionsLine("");
            help.AddPostOptionsLine("Examples:");
            help.AddPostOptionsLine("  TeamScriber.exe -i \"C:\\Meetings\\ImportantMeeting.mp4\"");
            help.AddPostOptionsLine("  TeamScriber.exe -i \"C:\\Meetings\\Video1.mp4|C:\\Meetings\\Video2.mp4\" -a \"C:\\Output\\Audio\" -t \"C:\\Output\\Transcripts\"");
            help.AddPostOptionsLine("  TeamScriber.exe -i \"C:\\Meetings\" -l \"fr\" -v");
            help.AddPostOptionsLine("  TeamScriber.exe -i \"C:\\Meetings\\Video1.mp4\" -b \"https://api.openai.com/v1\"");
            help.AddPostOptionsLine("  TeamScriber.exe -i \"C:\\Meetings\\Video1.mp4\" -m \"OpenAI:gpt-4\" -p \"C:\\Prompts\\custom_prompts.txt\"");
            help.AddPostOptionsLine("  TeamScriber.exe -i \"C:\\Meetings\\Video1.mp4\" --timestamps");

            return help;
        }
    }
}
