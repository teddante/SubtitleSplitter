using System.Text;
using System.Text.RegularExpressions;

namespace SubtitleSplitter
{
    public partial class SubtitleSplitter
    {
        // Simplified behavior: one sentence per caption,
        // duration = characters / AverageCps, no inter-caption gap.
        private const double AverageCps = 15.0;      // average reading speed (characters per second)
        private const int SentencesPerSubtitle = 1;  // fixed grouping
        private const int MaxLineLength = 42;        // target characters per line
        private const int MaxLines = 2;              // target max lines per caption

        public static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: SubtitleSplitter <input.txt>");
                Environment.ExitCode = 1;
                return;
            }

            var inputPath = args[0];

            try
            {
                var text = ReadFile(inputPath);
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

                var entries = ConvertTextToEntries(text);

                // Always export both SRT and FCPXML by default
                var subtitles = EntriesToSrtBlocks(entries);
                SaveSubtitlesToFile(subtitles, inputPath);
                Console.WriteLine("SRT file created successfully.");

                SaveFcpXmlToFile(entries, inputPath);
                Console.WriteLine("FCPXML file created successfully.");
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

        public static string[] ConvertTextToSubtitles(string text)
        {
            var entries = ConvertTextToEntries(text);
            return EntriesToSrtBlocks(entries);
        }

        private static string[] EntriesToSrtBlocks(IReadOnlyList<SubtitleEntry> entries)
        {
            var blocks = new List<string>(entries.Count);
            foreach (var e in entries)
            {
                var wrapped = string.Join("\n", e.Lines);
                blocks.Add(FormatSubtitle(e.Index, e.Start, e.End, wrapped));
            }
            return blocks.ToArray();
        }

        public static List<SubtitleEntry> ConvertTextToEntries(string text)
        {
            var result = new List<SubtitleEntry>();
            if (string.IsNullOrWhiteSpace(text)) return result;

            // Split sentences; keep only non-empty, trimmed items
            var sentences = SplitIntoSentences(text)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();

            int number = 1;
            var start = TimeSpan.Zero;

            for (int i = 0; i < sentences.Length; i += SentencesPerSubtitle)
            {
                var group = sentences.Skip(i).Take(SentencesPerSubtitle);
                var blockText = NormalizeCaptionText(string.Join(" ", group));
                if (blockText.Length == 0) continue;

                // Reading time estimates
                int charCount = blockText.Length;

                // Duration based solely on average reading rate
                double durationSec = charCount / AverageCps;

                var duration = TimeSpan.FromSeconds(durationSec);
                var end = start + duration;

                var wrapped = WrapText(blockText, MaxLineLength, MaxLines).Split('\n');

                result.Add(new SubtitleEntry
                {
                    Index = number,
                    Start = start,
                    End = end,
                    Lines = wrapped
                });
                number++;
                start = end; // No gap between captions
            }

            return result;
        }

        private static void SaveFcpXmlToFile(IReadOnlyList<SubtitleEntry> entries, string inputFilePath)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            if (string.IsNullOrEmpty(inputFilePath))
                throw new ArgumentException("File path cannot be null or empty.");

            string fullInputPath = Path.GetFullPath(inputFilePath);
            var directory = Path.GetDirectoryName(fullInputPath);
            if (string.IsNullOrEmpty(directory)) directory = Environment.CurrentDirectory;
            var fileName = Path.GetFileNameWithoutExtension(fullInputPath);

            var outputPath = Path.Combine(directory!, $"{fileName}_subtitles.fcpxml");

            var xml = BuildFcpXml(entries, projectName: fileName);
            File.WriteAllText(outputPath, xml, new UTF8Encoding(false));
        }

        private static string BuildFcpXml(IReadOnlyList<SubtitleEntry> entries, string projectName)
        {
            // Build a minimal FCPXML 1.8 project with a 1080p30 sequence and a series of Basic Title clips
            // positioned by offset/duration. This form imports into DaVinci Resolve and other NLEs.
            const int fps = 30;
            string Encode(string s)
            {
                return s
                    .Replace("&", "&amp;")
                    .Replace("<", "&lt;")
                    .Replace(">", "&gt;")
                    .Replace("\"", "&quot;");
            }

            string FormatRational(TimeSpan t)
            {
                // Represent as integer frames over fps, e.g. 45/30s
                var frames = (long)Math.Round(t.TotalSeconds * fps);
                return $"{frames}/{fps}s";
            }

            string JoinLinesForText(IEnumerable<string> lines)
            {
                // Use &#10; for line breaks; encode lines individually to preserve the entity
                return string.Join("&#10;", lines.Select(Encode));
            }

            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<fcpxml version=\"1.8\">");
            sb.AppendLine("  <resources>");
            sb.AppendLine("    <format id=\"r1\" frameDuration=\"1/30s\" width=\"1920\" height=\"1080\" colorSpace=\"1-1-1 (Rec. 709)\"/>");
            // Basic Title effect reference; many NLEs (including Resolve) map this identifier
            sb.AppendLine("    <effect id=\"r2\" name=\"Basic Title\" uid=\"/Titles/Basic/Basic Title\"/>");
            // Global text style definition (referenced by titles)
            sb.AppendLine("    <text-style-def id=\"ts1\">");
            sb.AppendLine("      <text-style font=\"Helvetica\" fontSize=\"48\" alignment=\"center\"/>");
            sb.AppendLine("    </text-style-def>");
            sb.AppendLine("  </resources>");
            sb.AppendLine("  <library>");
            sb.AppendLine($"    <event name=\"{Encode(projectName)}\">");
            sb.AppendLine($"      <project name=\"{Encode(projectName)}\">");
            sb.AppendLine("        <sequence format=\"r1\" tcStart=\"0s\" tcFormat=\"NDF\">");
            sb.AppendLine("          <spine>");

            // Emit titles
            foreach (var e in entries)
            {
                var offset = FormatRational(e.Start);
                var duration = FormatRational(e.End - e.Start);
                var name = e.Lines.Length > 0 ? e.Lines[0] : $"Subtitle {e.Index}";
                var text = JoinLinesForText(e.Lines);

                sb.AppendLine($"            <title name=\"{Encode(name)}\" ref=\"r2\" offset=\"{offset}\" duration=\"{duration}\">");
                sb.AppendLine("              <text>");
                sb.AppendLine($"                <text-style ref=\"ts1\">{text}</text-style>");
                sb.AppendLine("              </text>");
                sb.AppendLine("            </title>");
            }

            sb.AppendLine("          </spine>");
            sb.AppendLine("        </sequence>");
            sb.AppendLine("      </project>");
            sb.AppendLine("    </event>");
            sb.AppendLine("  </library>");
            sb.AppendLine("</fcpxml>");
            return sb.ToString();
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
        private static IEnumerable<string> SplitIntoSentences(string text)
        {
            text = text.Replace("\r\n", "\n");
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

        public class SubtitleEntry
        {
            public int Index { get; set; }
            public TimeSpan Start { get; set; }
            public TimeSpan End { get; set; }
            public string[] Lines { get; set; } = Array.Empty<string>();
        }
    }
}
