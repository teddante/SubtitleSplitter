using System.Text;
using System.Text.RegularExpressions;

namespace SubtitleSplitter
{
    public partial class SubtitleSplitter
    {
        // Sensible defaults (tune as needed)
        private const int DefaultWpm = 200;          // words per minute
        private const double DefaultCps = 15.0;      // characters per second (reading speed cap)
        private const double DefaultMinDurationSec = 1.0;   // default minimum on-screen time
        private const double DefaultMaxDurationSec = 7.0;   // default maximum on-screen time
        private const double MinTechnicalGapSec = 0.2;      // minimum gap between cues

        private const int DefaultMaxLineLength = 42; // target characters per line
        private const int DefaultMaxLines = 2;       // target max lines per caption

        public sealed record Options
        {
            public string InputPath { get; init; } = string.Empty;
            public int SentencesPerSubtitle { get; init; } = 1;
            public int WordsPerMinute { get; init; } = DefaultWpm;
            public double Cps { get; init; } = DefaultCps;
            public double GapSeconds { get; init; } = 1.0;
            public double MinDurationSec { get; init; } = DefaultMinDurationSec;
            public double MaxDurationSec { get; init; } = DefaultMaxDurationSec;
            public bool SplitOnNewline { get; init; } = false;
            public int MaxLineLength { get; init; } = DefaultMaxLineLength;
            public int MaxLines { get; init; } = DefaultMaxLines;
        }

        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: SubtitleSplitter <input.txt>");
                Environment.ExitCode = 1;
                return;
            }

            var inputPath = args[0];
            var options = new Options { InputPath = inputPath };

            try
            {
                var text = ReadFile(options!.InputPath);
                if (text == null)
                {
                    // ReadFile already printed an error
                    Environment.ExitCode = 1;
                    return;
                }
                if (string.IsNullOrWhiteSpace(text))
                {
                    Console.WriteLine("File is empty.");
                    Environment.ExitCode = 1;
                    return;
                }

                var subtitles = ConvertTextToSubtitles(text, options!);

                SaveSubtitlesToFile(subtitles, options!.InputPath);
                Console.WriteLine("Subtitle file created successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }
        

        private static string? ReadFile(string filePath)
        {
            try
            {
                var raw = File.ReadAllText(filePath, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                // Normalize line endings and strip BOM if present
                raw = raw.Replace("\r\n", "\n");
                if (raw.Length > 0 && raw[0] == '\uFEFF') raw = raw.Substring(1);
                return raw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read file: {ex.Message}");
                return null;
            }
        }

        public static void SaveSubtitlesToFile(string[] subtitles, string inputFilePath)
        {
            if (string.IsNullOrEmpty(inputFilePath))
                throw new ArgumentException("File path cannot be null or empty.");

            string fullInputPath = Path.GetFullPath(inputFilePath);
            var directory = Path.GetDirectoryName(fullInputPath);
            if (string.IsNullOrEmpty(directory)) directory = Environment.CurrentDirectory;
            var fileName = Path.GetFileNameWithoutExtension(fullInputPath);
            
            var outputPath = Path.Combine(directory!, $"{fileName}_subtitles.srt");

            // Join with Windows line endings to conform with SRT expectations
            var srt = string.Join("\r\n", subtitles.Select(s => s.Replace("\n", "\r\n")));
            File.WriteAllText(outputPath, srt, new UTF8Encoding(false));
        }

        public static string[] ConvertTextToSubtitles(string text, Options options)
        {
            if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
            if (options.SentencesPerSubtitle <= 0) throw new ArgumentOutOfRangeException(nameof(options.SentencesPerSubtitle));
            if (options.WordsPerMinute <= 0) throw new ArgumentOutOfRangeException(nameof(options.WordsPerMinute));
            if (options.Cps <= 0) throw new ArgumentOutOfRangeException(nameof(options.Cps));
            if (options.GapSeconds < 0) throw new ArgumentOutOfRangeException(nameof(options.GapSeconds));
            if (options.MinDurationSec <= 0) throw new ArgumentOutOfRangeException(nameof(options.MinDurationSec));
            if (options.MaxDurationSec <= 0) throw new ArgumentOutOfRangeException(nameof(options.MaxDurationSec));

            // Split sentences; keep only non-empty, trimmed items
            var sentences = SplitIntoSentences(text, options.SplitOnNewline)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();

            var blocks = new List<string>();
            int number = 1;
            var start = TimeSpan.Zero;

            for (int i = 0; i < sentences.Length; i += options.SentencesPerSubtitle)
            {
                var group = sentences.Skip(i).Take(options.SentencesPerSubtitle);
                var blockText = NormalizeCaptionText(string.Join(" ", group));
                if (blockText.Length == 0) continue;

                // Reading time estimates
                int wordCount = blockText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                int charCount = blockText.Length;

                double secByWpm = (wordCount / (double)options.WordsPerMinute) * 60.0;
                double secByCps = charCount / options.Cps;

                // Choose conservative (larger) time, then clamp
                double durationSec = Math.Max(secByWpm, secByCps);
                durationSec = Math.Clamp(durationSec, options.MinDurationSec, options.MaxDurationSec);

                var duration = TimeSpan.FromSeconds(durationSec);
                var end = start + duration;

                var wrapped = WrapText(blockText, options.MaxLineLength, options.MaxLines);
                blocks.Add(FormatSubtitle(number, start, end, wrapped));
                number++;

                // Apply max of requested gap and minimum technical gap
                var gap = TimeSpan.FromSeconds(Math.Max(options.GapSeconds, MinTechnicalGapSec));
                start = end + gap;
            }

            return blocks.ToArray();
        }

        private static string NormalizeCaptionText(string s)
        {
            // Replace hard newlines with spaces, collapse whitespace, trim
            s = s.Replace("\r", "\n").Replace("\n", " ");
            s = Regex.Replace(s, @"\s+", " ");
            return s.Trim();
        }

        private static string FormatSubtitle(int number, TimeSpan start, TimeSpan end, string text) =>
            $"{number}\n{FormatTime(start)} --> {FormatTime(end)}\n{text}\n";

        private static string FormatTime(TimeSpan t)
        {
            // Use TotalHours to avoid 24h wrap
            int hours = (int)Math.Floor(t.TotalHours);
            int minutes = t.Minutes;
            int seconds = t.Seconds;
            int millis = t.Milliseconds;
            return $"{hours:00}:{minutes:00}:{seconds:00},{millis:000}";
        }

        // Sentence splitting with basic abbreviation and number handling
        private static IEnumerable<string> SplitIntoSentences(string text, bool splitOnNewline)
        {
            text = text.Replace("\r\n", "\n");
            if (splitOnNewline)
                return text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            // First pass: naive split on end punctuation
            var parts = NaiveSentenceSplitRegex().Split(text).Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
            if (parts.Count <= 1) return parts;

            // Merge parts that were split after abbreviations, initials, decimals, or ellipses
            var result = new List<string>();
            string current = parts[0];

            for (int i = 1; i < parts.Count; i++)
            {
                var prev = current;
                var next = parts[i];

                if (ShouldMerge(prev, next))
                {
                    current = prev + " " + next;
                }
                else
                {
                    result.Add(prev);
                    current = next;
                }
            }

            result.Add(current);
            return result;

            static bool ShouldMerge(string prev, string next)
            {
                // Abbreviation list and initials
                if (AbbrevEndRegex().IsMatch(prev)) return true;
                // Decimal numbers like 3.14 | 10.5
                if (Regex.IsMatch(prev, @"\d\.$") && Regex.IsMatch(next, @"^\d")) return true;
                // Ellipses ...
                if (prev.EndsWith("..", StringComparison.Ordinal)) return true;
                // Common ordinal abbreviations (No., etc.)
                if (Regex.IsMatch(prev, @"(?i)\bNo\.$")) return true;
                return false;
            }
        }

        private static string WrapText(string text, int maxLineLength, int maxLines)
        {
            // Goal: wrap up to maxLines, but NEVER truncate content.
            // If content would exceed maxLines, overflow the final line (ignore maxLineLength for the last line).
            if (maxLines <= 0) return text;
            if (maxLineLength <= 10) maxLineLength = 10;

            var words = Regex.Split(text, @"\s+").Where(w => w.Length > 0).ToArray();
            var lines = new List<string>();
            var line = new StringBuilder();

            int i = 0;
            while (i < words.Length)
            {
                // For all lines except the final allowed line, respect maxLineLength and wrap normally.
                if (lines.Count < maxLines - 1)
                {
                    var word = words[i];

                    if (line.Length == 0)
                    {
                        if (word.Length <= maxLineLength)
                        {
                            line.Append(word);
                            i++;
                        }
                        else
                        {
                            // Hard break long word across multiple lines (still respecting maxLines - 1 limit)
                            int idx = 0;
                            while (idx < word.Length && lines.Count < maxLines - 1)
                            {
                                int take = Math.Min(maxLineLength, word.Length - idx);
                                lines.Add(word.Substring(idx, take));
                                idx += take;
                            }

                            // Put any remainder of the long word onto the current (last) line buffer
                            if (idx < word.Length)
                            {
                                line.Append(word.Substring(idx));
                                i++;
                            }
                            else
                            {
                                // Exactly consumed; keep line empty for next word
                                i++;
                            }
                        }
                    }
                    else if (line.Length + 1 + word.Length <= maxLineLength)
                    {
                        line.Append(' ').Append(word);
                        i++;
                    }
                    else
                    {
                        // Current line full, commit and start a new one
                        lines.Add(line.ToString());
                        line.Clear();
                    }
                }
                else
                {
                    // Final allowed line: append everything regardless of maxLineLength to avoid truncation
                    if (line.Length == 0)
                        line.Append(words[i]);
                    else
                        line.Append(' ').Append(words[i]);
                    i++;
                }
            }

            if (line.Length > 0)
                lines.Add(line.ToString());

            // Ensure we do not exceed maxLines in the return value
            if (lines.Count > maxLines)
            {
                // Merge any overflow lines into the last allowed line
                var tail = string.Join(" ", lines.Skip(maxLines - 1));
                return string.Join("\n", lines.Take(maxLines - 1).Concat(new[] { tail }));
            }

            return string.Join("\n", lines);
        }

        [GeneratedRegex(@"(?<=[\.!\?\n])\s+")]
        private static partial Regex NaiveSentenceSplitRegex();

        [GeneratedRegex(@"(?i)\b(?:Mr|Mrs|Ms|Dr|Prof|Sr|Jr|St|Mt|Msgr|Messrs|vs|etc|e\.g|i\.e|cf|al)\.$")]
        private static partial Regex AbbrevEndRegex();
    }
}
