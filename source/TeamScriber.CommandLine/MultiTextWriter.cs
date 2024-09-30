using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamScriber.CommandLine
{
    public class MultiTextWriter : TextWriter
    {
        private List<TextWriter> writers = new List<TextWriter>();
        private readonly string _prefix = string.Empty;

        public override Encoding Encoding => Encoding.Default;

        public MultiTextWriter(TextWriter consoleStream, string prefix = "")
        {
            writers.Add(consoleStream);
            _prefix = prefix;
        }

        public void AddWriter(TextWriter writer)
        {
            writers.Add(writer);
        }

        public override void WriteLine(string value)
        {
            base.WriteLine();

            string message = string.IsNullOrEmpty(_prefix) ? value : _prefix + " " + value;

            foreach (var writer in writers)
            {
                writer.WriteLine(message);
            }
        }

        public override void WriteLine()
        {
            base.WriteLine();

            WriteLine(string.Empty);
        }

    }
}
