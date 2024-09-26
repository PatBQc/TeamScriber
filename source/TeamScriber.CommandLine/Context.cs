using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamScriber
{
    internal class Context
    {
        public CommandLineOptions Options { get; set; }

        public List<string> Videos { get; set; }

        public List<string> Audios { get; set; }

        public List<string> Transcriptions { get; set; }
    }
}
