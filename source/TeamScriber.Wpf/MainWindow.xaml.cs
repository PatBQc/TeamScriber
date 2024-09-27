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
using TeamScriber.CommandLine;

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
            Console.SetError(new TextBoxWriter(logTextBox, @"/!\ ERROR"));
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
                Console.WriteLine($"Selected file (Simple): {videoPathTextBoxSimple.Text}\n");
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
                Console.WriteLine($"Selected file (Advanced): {videoPathTextBoxAdvanced.Text}\n");
            }
        }

        private void OnSelectDirectoryAdvanced(object sender, RoutedEventArgs e)
        {
            // Open folder dialog to select a directory
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                videoPathTextBoxAdvanced.Text = dialog.SelectedPath;
                Console.WriteLine($"Selected directory (Advanced): {videoPathTextBoxAdvanced.Text}\n");
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
            string apiKeyOpenAI = apiKeyOpenAITextBox.Text;
            string apiKeyAnthropic = apiKeyAnthropicTextBox.Text;
            string model = ((ComboBoxItem)modelComboBox.SelectedItem).Content.ToString();
            string language = whisperLanguageTextBox.Text;
            bool useWhisperAzureThrottling = useWhisperAzureThrottlingCheckBox.IsChecked ?? false;
            bool includeTimestamps = includeTimestampsCheckBox.IsChecked ?? false;

            string ffmpegPath = ffmpegPathTextBox.Text;
            bool useWhisper = useWhisperCheckBox.IsChecked ?? true;
            string openAIBasePath = openAIBasePathTextBox.Text;
            string transcriptionOutputDirectory = transcriptionOutputDirectoryTextBox.Text;
            bool usePromptsQueries = usePromptsQueriesCheckBox.IsChecked ?? true;
            string promptsPath = promptsTextBox.Text;
            string systemPromptsPath = systemPromptsTextBox.Text;
            string questionsOutputDirectory = questionsOutputDirectoryTextBox.Text;
            bool verbose = verboseCheckBox.IsChecked ?? false;
            bool recordAudio = recordAudioCheckBox.IsChecked ?? false;

            // Build the arguments string for the command-line tool
            string arguments = $"-i \"{videoPathTextBoxAdvanced.Text}\" ";

            arguments += !string.IsNullOrEmpty(audioPath) ? $"-a \"{audioPath}\" " : "";

            arguments += $"-m \"{model}\" ";

            arguments += !string.IsNullOrEmpty(language) ? $"-l {language} " : "";

            arguments += !string.IsNullOrEmpty(apiKeyOpenAI) ? $"-k {apiKeyOpenAI} " : "";

            arguments += !string.IsNullOrEmpty(apiKeyAnthropic) ? $"-c {apiKeyAnthropic} " : "";

            arguments += includeTimestamps ? "--timestamps " : "";

            arguments += !string.IsNullOrEmpty(ffmpegPath) ? $"-f \"{ffmpegPath}\" " : "";

            arguments += $"-w {useWhisper} ";

            arguments += !string.IsNullOrEmpty(openAIBasePath) ? $"-b \"{openAIBasePath}\" " : "";

            arguments += !string.IsNullOrEmpty(transcriptionOutputDirectory) ? $"-t \"{transcriptionOutputDirectory}\" " : "";

            arguments += $"-q {usePromptsQueries} ";

            arguments += !string.IsNullOrEmpty(promptsPath) ? $"-p \"{promptsPath}\" " : "";

            arguments += !string.IsNullOrEmpty(systemPromptsPath) ? $"-s \"{systemPromptsPath}\" " : "";

            arguments += !string.IsNullOrEmpty(questionsOutputDirectory) ? $"-o \"{questionsOutputDirectory}\" " : "";

            arguments += useWhisperAzureThrottling ? "--whisper-azure-throttle " : "";

            arguments += verbose ? "-v " : "";

            arguments += recordAudio ? "-r " : "";

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

                Console.WriteLine($"Running TeamScriber with arguments: {arguments}\n");
                Console.WriteLine();

                var progress = new Progress<ProgressInfo>(info =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if(progressBar.Maximum != info.MaxValue)
                        {
                            progressBar.Maximum = info.MaxValue;
                        }
                        progressBar.Value = info.Value;
                    });
                });

                await TeamScriber.CommandLine.Program.Main(args, progress);

                MessageBox.Show("Transcription completed successfully.", "Finished", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // Update the logTextBox in case of an error
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Console.WriteLine($"Error: {ex.Message}\n");
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
