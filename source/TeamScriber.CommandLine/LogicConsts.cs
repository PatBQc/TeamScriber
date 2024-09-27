using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamScriber.CommandLine
{
    internal class LogicConsts
    {
        public static readonly TimeSpan AudioSegmentTime = new TimeSpan(0, 5, 0);

        public const int ProgressWeightAudioPerVideo = 4;
        public const int ProgressWeightWhisperConversion = 10;
        public const int ProgressWeightPromptQueries = 10;
    }
}
