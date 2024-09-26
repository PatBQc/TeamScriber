using Microsoft.Win32;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TeamScriber.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Console.SetOut(new TextBoxWriter(logTextBox));
        }

        // Simple file selection
        private void OnSelectFileSimple(object sender, RoutedEventArgs e)
        {
            // Open file dialog to select a Teams video file
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Video Files (*.mp4;*.avi)|*.mp4;*.avi";
            if (openFileDialog.ShowDialog() == true)
            {
                videoPathTextBoxSimple.Text = openFileDialog.FileName;
                logTextBox.Text += $"Selected file (Simple): {videoPathTextBoxSimple.Text}\n";
            }
        }

        // Advanced file or directory selection
        private void OnSelectFileAdvanced(object sender, RoutedEventArgs e)
        {
            // Open file dialog to select either a file or directory
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Video Files (*.mp4;*.avi)|*.mp4;*.avi";
            if (openFileDialog.ShowDialog() == true)
            {
                videoPathTextBoxAdvanced.Text = openFileDialog.FileName;
                logTextBox.Text += $"Selected file (Advanced): {videoPathTextBoxAdvanced.Text}\n";
            }
        }

        // Start transcription for Simple mode
        private async void OnTranscribeSimple(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(videoPathTextBoxSimple.Text))
            {
                MessageBox.Show("Please select a video file.");
                return;
            }

            // Get the selected language
            string language = GetLanguageCode(((ComboBoxItem)languageComboBoxSimple.SelectedItem).Content.ToString());

            // Build the arguments string for the command-line tool
            string arguments = $"-i \"{videoPathTextBoxSimple.Text}\" -v ";

            if(language != null)
            {
                arguments += $"-l {language} ";
            }

            // Run TeamScriber with the generated arguments
            await Task.Run(async () =>
            {
                await RunTeamScriberAsync(arguments);
            });
        }

        // Start transcription for Advanced mode
        private async void OnTranscribeAdvanced(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(videoPathTextBoxAdvanced.Text))
            {
                MessageBox.Show("Please select a file or directory.");
                return;
            }

            // Get input values from the advanced mode UI
            string audioPath = audioPathTextBox.Text;
            string apiKey = apiKeyTextBox.Text;
            string model = ((ComboBoxItem)modelComboBox.SelectedItem).Content.ToString();
            string language = GetLanguageCode(((ComboBoxItem)languageComboBoxAdvanced.SelectedItem).Content.ToString());
            bool includeTimestamps = timestampsCheckbox.IsChecked ?? false;

            // Build the arguments string for the command-line tool
            string arguments = $"-i \"{videoPathTextBoxAdvanced.Text}\" -a \"{audioPath}\" -k \"{apiKey}\" -m \"{model}\" ";

            if (language != null)
            {
                arguments += $"-l {language} ";
            }

            if (includeTimestamps) arguments += " --timestamps";

            // Run TeamScriber with the generated arguments
            await Task.Run(async () =>
            {
                await RunTeamScriberAsync(arguments);
            });
        }

        // Method to run TeamScriber with given arguments
        private async Task RunTeamScriberAsync(string arguments)
        {
            try
            {
                var args = ParseArguments(arguments);
                await TeamScriber.CommandLine.Program.Main(args);
            }
            catch (Exception ex)
            {
                // Update the logTextBox in case of an error
                Application.Current.Dispatcher.Invoke(() =>
                {
                    logTextBox.Text += $"Error: {ex.Message}\n";
                });
            }
        }

        private static string[] ParseArguments(string commandLine)
        {
            var parser = new Parser();
            var result = parser.Parse(commandLine);
            return result.Tokens.Select(t => t.Value).ToArray();
        }

        private static string GetLanguageCode(string language) => language.ToLower() switch
        {
            "french" => "fr",
            "english" => "en",

            _ => null
        };
    }
}
