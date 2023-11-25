namespace SubtitleSplitter
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Event handler for the import button click event.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        private async void OnImportClicked(object sender, EventArgs e)
        {
            var result = await FilePicker.PickAsync();
            if (result != null)
            {
                var text = await File.ReadAllTextAsync(result.FullPath);
                InputTextBox.Text = text;
            }
        }

        /// <summary>
        /// Converts a text into an array of subtitles.
        /// </summary>
        /// <param name="text">The input text.</param>
        /// <returns>An array of subtitles.</returns>
        private string[] ConvertTextToSubtitles(string text)
        {
            // This is a placeholder implementation.
            var sentences = text.Split('.');
            return sentences;
        }

        /// <summary>
        /// Saves the given subtitles to a file.
        /// </summary>
        /// <param name="subtitles">The array of subtitles to be saved.</param>
        private void SaveSubtitlesToFile(string[] subtitles)
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "subtitles.srt");
            File.WriteAllLines(path, subtitles);
        }
    }

}
