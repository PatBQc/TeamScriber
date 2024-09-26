using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamScriber
{
    internal class AudioChunk
    {
        public int ID { get; set; }

        public byte[] AudioSegment { get; set; }

        public string Text { get; set; }
    }
}
