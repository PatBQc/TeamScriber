﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeamScriber.CommandLine;

namespace TeamScriber
{
    internal class Context
    {
        public CommandLineOptions Options { get; set; }

        public List<string> Videos { get; set; }

        public List<string> Audios { get; set; }

        public List<string> Transcriptions { get; set; }

        public List<string> AnswersMarkdown { get; set; }

        public List<string> AnswersHtml01Base { get; set; }

        public List<string> AnswersHtml02Embed { get; set; }

        public ProgressInfo ProgressInfo { get; set; }

        public IProgress<ProgressInfo> ProgressRepporter { get; set; }

        public StringWriter Logger { get; set; }
    }
}
