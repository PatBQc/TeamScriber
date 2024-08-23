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

        [Option('l', "language", Required = false, Default = "en",
            HelpText =
            """
            OpenAI Whisper language to use. Must be in ISO-639-1 format (ex: en for English, fr for French, ...).
            Default is en.
            """)]
        public string WhisperLanguage { get; set; }

        [Option('k', "openai-api-key", Required = false, Default = "",
            HelpText =
            """
            OpenAI Whisper API model key. If not specified, it will take what is in the OPENAI_API_KEY os environment variable.
            """)]
        public string OpenAIAPIKey { get; set; }

        [Option('t', "transcription", Required = false, Default = "",
            HelpText =
            """
            Transcription output directory.  
            The transcription files generated will have the same filename as the audio, but extention will become .txt.
            If not specified, the transcription files will be generated in the same directory as the audio files.
            """)]
        public string TranscriptionOutputDirectory { get; set; }

        [Option('v', "verbose", Required = false, Default = false,
            HelpText =
            """
            Display execution details.
            Default is false.
            """)]
        public bool Verbose { get; set; }

        public static string GetUsage()
        {
            var help = new HelpText
            {
                Heading = new HeadingInfo("TeamScriber", "1.0"),
                AdditionalNewLineAfterOption = true,
                AddDashesToOption = true
            };

            return help;
        }
    }
}
