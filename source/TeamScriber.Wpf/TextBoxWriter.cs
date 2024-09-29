using System;
using System.IO;
using System.Text;
using System.Windows.Controls;

namespace TeamScriber.Wpf
{
    public class TextBoxWriter : TextWriter
    {
        private readonly TextBox _textBox;
        private readonly string _prefix = string.Empty;

        public TextBoxWriter(TextBox textBox, string prefix = "")
        {
            _textBox = textBox;
            _prefix = prefix;
        }

        public override void WriteLine(string value)
        {
            string message = string.IsNullOrEmpty(_prefix) ? value : _prefix + " " + value;
            base.WriteLine(message);

            // Make sure updates happen on the UI thread
            _textBox.Dispatcher.Invoke(() =>
            {
                _textBox.AppendText(message + Environment.NewLine);
                _textBox.ScrollToEnd();  // Scrolls to the end after writing
            });
        }

        public override void WriteLine()
        {
            this.WriteLine(string.Empty);
        }

        public override Encoding Encoding => Encoding.UTF8;
    }
}
