namespace SubtitleSplitter
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

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
