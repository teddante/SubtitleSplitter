using System.Text.RegularExpressions;

namespace SubtitleSplitter
{
    public partial class SubtitleSplitter
    {
        /// <summary>
        /// The entry point of the application.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        public static void Main(string[] args)
        {
            var filePath = ValidateAndParseArgs(args);
            if (filePath == null)
            {
                return;
            }

            try
            {
                var text = ReadFile(filePath);
                if (text != null)
                {
                    var subtitles = ConvertTextToSubtitles(text);
                    SaveSubtitlesToFile(subtitles, filePath);
                }
                else
                {
                    Console.WriteLine("Failed to read file or file is empty.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read file: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates and parses the command line arguments.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        /// <returns>The validated and parsed file path, or null if validation fails.</returns>
        private static string? ValidateAndParseArgs(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Please drag a text file onto the exe.");
                return null;
            }

            var filePath = args[0];
            if (!File.Exists(filePath) || Path.GetExtension(filePath) != ".txt")
            {
                Console.WriteLine("Invalid file path. Please provide a valid text file.");
                return null;
            }

            return filePath;
        }

        /// <summary>
        /// Reads the contents of a file.
        /// </summary>
        /// <param name="filePath">The path of the file to read.</param>
        /// <returns>The contents of the file as a string, or null if an error occurs.</returns>
        private static string? ReadFile(string filePath)
        {
            try
            {
                return File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read file: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Saves the subtitles to a file.
        /// </summary>
        /// <param name="subtitles">The array of subtitles to save.</param>
        /// <param name="inputFilePath">The input file path.</param>
        /// <exception cref="ArgumentException">Thrown when the input file path is null or empty.</exception>
        public static void SaveSubtitlesToFile(string[] subtitles, string inputFilePath)
        {
            if (string.IsNullOrEmpty(inputFilePath))
            {
                throw new ArgumentException("Input file path cannot be null or empty.", nameof(inputFilePath));
            }

            var directory = Path.GetDirectoryName(inputFilePath);
            if (directory != null)
            {
                var fileName = Path.GetFileNameWithoutExtension(inputFilePath);
                var outputPath = Path.Combine(directory, $"{fileName}_subtitles.srt");
                File.WriteAllLines(outputPath, subtitles);
            }
        }

        /// <summary>
        /// Converts a given text into an array of subtitles based on the specified number of sentences per subtitle.
        /// </summary>
        /// <param name="text">The text to be converted into subtitles.</param>
        /// <param name="sentencesPerSubtitle">The number of sentences per subtitle.</param>
        /// <returns>An array of subtitles.</returns>
        public static string[] ConvertTextToSubtitles(string text, int sentencesPerSubtitle = 1, int gapDuration = 1)
        {
            if (string.IsNullOrEmpty(text) || sentencesPerSubtitle <= 0)
            {
                return [];
            }

            // Split text into sentences using regular expression
            var sentences = MyRegex().Split(text);
            var subtitles = new List<string>();
            var subtitleNumber = 1;
            var startTime = TimeSpan.Zero;
            var wordsPerMinute = 200;

            for (var i = 0; i < sentences.Length; i += sentencesPerSubtitle)
            {
                var groupOfSentences = sentences.Skip(i).Take(sentencesPerSubtitle).ToArray();
                var sentence = string.Join(" ", groupOfSentences);
                var wordCount = sentence.Split(' ').Length;
                var duration = TimeSpan.FromMinutes((double)wordCount / wordsPerMinute);
                var endTime = startTime + duration;

                var subtitle = $"{subtitleNumber}\n{startTime:hh\\:mm\\:ss\\,fff} --> {endTime:hh\\:mm\\:ss\\,fff}\n{sentence}\n";
                subtitles.Add(subtitle);

                subtitleNumber++;
                startTime = endTime.Add(TimeSpan.FromSeconds(gapDuration));
            }

            return [.. subtitles];
        }

        [GeneratedRegex(@"(?<=[\.!\?\r\n])\s+")]
        private static partial Regex MyRegex();
    }
}