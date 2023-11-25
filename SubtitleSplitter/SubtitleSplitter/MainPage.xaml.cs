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
    }

}
