using System;
using System.IO;
using System.Text;
using System.Windows.Controls;

namespace TeamScriber.Wpf
{
    public class TextBoxWriter : TextWriter
    {
        private readonly TextBox _textBox;

        public TextBoxWriter(TextBox textBox)
        {
            _textBox = textBox;
        }

        public override void WriteLine(string value)
        {
            base.WriteLine(value);

            // Make sure updates happen on the UI thread
            _textBox.Dispatcher.Invoke(() =>
            {
                _textBox.AppendText(value + Environment.NewLine);
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
