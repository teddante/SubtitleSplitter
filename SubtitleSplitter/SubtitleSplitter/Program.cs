internal class Program
{
    /// <summary>
    /// The entry point of the application.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    private static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Please drag a text file onto the exe.");
            return;
        }

        var filePath = args[0];
        var text = File.ReadAllText(filePath);
        var subtitles = ConvertTextToSubtitles(text);
        SaveSubtitlesToFile(subtitles);
    }

    /// <summary>
    /// Saves the given array of subtitles to a file.
    /// </summary>
    /// <param name="subtitles">The array of subtitles to be saved.</param>
    private static void SaveSubtitlesToFile(string[] subtitles)
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "subtitles.srt");
        File.WriteAllLines(path, subtitles);
    }

    /// <summary>
    /// Converts a given text into an array of subtitles based on the specified number of sentences per subtitle.
    /// </summary>
    /// <param name="text">The text to be converted into subtitles.</param>
    /// <param name="sentencesPerSubtitle">The number of sentences per subtitle.</param>
    /// <returns>An array of subtitles.</returns>
    private static string[] ConvertTextToSubtitles(string text, int sentencesPerSubtitle = 2)
    {
        var sentences = text.Split('.');
        var subtitles = new List<string>();
        var subtitleNumber = 1;
        var startTime = TimeSpan.Zero;
        var wordsPerMinute = 200;

        for (var i = 0; i < sentences.Length; i += sentencesPerSubtitle)
        {
            var groupOfSentences = sentences.Skip(i).Take(sentencesPerSubtitle).ToArray();
            var sentence = string.Join(". ", groupOfSentences);
            var wordCount = sentence.Split(' ').Length;
            var duration = TimeSpan.FromMinutes((double)wordCount / wordsPerMinute);
            var endTime = startTime + duration;

            var subtitle = $"{subtitleNumber}\n{startTime:hh\\:mm\\:ss\\,fff} --> {endTime:hh\\:mm\\:ss\\,fff}\n{sentence}\n";
            subtitles.Add(subtitle);

            subtitleNumber++;
            startTime = endTime;
        }

        return [.. subtitles];
    }
}