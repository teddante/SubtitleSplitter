namespace SubtitleSplitter
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnImportClicked(object sender, EventArgs e)
        {
            var result = await FilePicker.PickAsync();
            if (result != null)
            {
                var text = await File.ReadAllTextAsync(result.FullPath);
                InputTextBox.Text = text;
            }
        }

        private string[] ConvertTextToSubtitles(string text)
        {
            // This is a placeholder implementation.
            var sentences = text.Split('.');
            return sentences;
        }

        private void SaveSubtitlesToFile(string[] subtitles)
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "subtitles.srt");
            File.WriteAllLines(path, subtitles);
        }
    }

}
