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

        //public override void Write(char value)
        //{
        //    base.Write(value);
        //    // Make sure updates happen on the UI thread
        //    _textBox.Dispatcher.Invoke(() =>
        //    {
        //        _textBox.AppendText(value.ToString());
        //        _textBox.ScrollToEnd();  // Scrolls to the end after writing
        //    });
        //}

        public override void Write(string value)
        {
            base.Write(value);
            // Make sure updates happen on the UI thread
            _textBox.Dispatcher.Invoke(() =>
            {
                _textBox.AppendText(value);
                _textBox.ScrollToEnd();  // Scrolls to the end after writing
            });
        }

        public override Encoding Encoding => Encoding.UTF8;
    }
}
