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
            public string? OutputPath { get; init; }
            public int SentencesPerSubtitle { get; init; } = 2;
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
            var parse = TryParseOptions(args, out var options, out var parseErrorOrHelp);
            if (!parse)
            {
                if (!string.IsNullOrEmpty(parseErrorOrHelp))
                    Console.WriteLine(parseErrorOrHelp);
                Environment.ExitCode = 1;
                return;
            }

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

                SaveSubtitlesToFile(subtitles, options!.InputPath, options!.OutputPath);
                Console.WriteLine("Subtitle file created successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }
        
        private static bool TryParseOptions(string[] args, out Options? options, out string message)
        {
            options = null;
            message = string.Empty;
            if (args.Length == 0)
            {
                message = GetUsage();
                return false;
            }

            string? inputPath = null;
            string? outputPath = null;
            int? sps = null;
            int? wpm = null;
            double? cps = null;
            double? gap = null;
            double? minDur = null;
            double? maxDur = null;
            bool splitOnNewline = false;
            int? maxLineLen = null;
            int? maxLines = null;

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg is "-h" or "--help" or "/?")
                {
                    message = GetUsage();
                    return false;
                }

                if (arg.StartsWith("--"))
                {
                    string key;
                    string? value = null;
                    var idx = arg.IndexOf('=');
                    if (idx > 0)
                    {
                        key = arg.Substring(2, idx - 2);
                        value = arg[(idx + 1)..];
                    }
                    else
                    {
                        key = arg[2..];
                        // If next token is not another switch, treat as value
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                        {
                            value = args[i + 1];
                            i++;
                        }
                    }

                    switch (key)
                    {
                        case "output":
                            if (string.IsNullOrEmpty(value)) return Fail("--output requires a value", out options, out message);
                            outputPath = value;
                            break;
                        case "sentences-per-subtitle":
                            if (!int.TryParse(value, out var spsVal) || spsVal <= 0) return Fail("--sentences-per-subtitle must be a positive integer", out options, out message);
                            sps = spsVal;
                            break;
                        case "wpm":
                            if (!int.TryParse(value, out var wpmVal) || wpmVal <= 0) return Fail("--wpm must be a positive integer", out options, out message);
                            wpm = wpmVal;
                            break;
                        case "cps":
                            if (!double.TryParse(value, out var cpsVal) || cpsVal <= 0) return Fail("--cps must be a positive number", out options, out message);
                            cps = cpsVal;
                            break;
                        case "gap":
                            if (!double.TryParse(value, out var gapVal) || gapVal < 0) return Fail("--gap must be a non-negative number", out options, out message);
                            gap = gapVal;
                            break;
                        case "min-duration":
                            if (!double.TryParse(value, out var minVal) || minVal <= 0) return Fail("--min-duration must be a positive number", out options, out message);
                            minDur = minVal;
                            break;
                        case "max-duration":
                            if (!double.TryParse(value, out var maxVal) || maxVal <= 0) return Fail("--max-duration must be a positive number", out options, out message);
                            maxDur = maxVal;
                            break;
                        case "split-on-newline":
                            // boolean flag, no value expected
                            splitOnNewline = true;
                            break;
                        case "max-line-length":
                            if (!int.TryParse(value, out var mll) || mll <= 10) return Fail("--max-line-length must be an integer > 10", out options, out message);
                            maxLineLen = mll;
                            break;
                        case "max-lines":
                            if (!int.TryParse(value, out var ml) || ml <= 0) return Fail("--max-lines must be a positive integer", out options, out message);
                            maxLines = ml;
                            break;
                        default:
                            return Fail($"Unknown option --{key}", out options, out message);
                    }
                }
                else if (arg.StartsWith("-"))
                {
                    // short options
                    if (arg is "-o")
                    {
                        if (i + 1 >= args.Length || args[i + 1].StartsWith("-")) return Fail("-o requires a value", out options, out message);
                        outputPath = args[i + 1];
                        i++;
                    }
                    else
                    {
                        return Fail($"Unknown option {arg}", out options, out message);
                    }
                }
                else
                {
                    if (inputPath == null) inputPath = arg; // first non-switch is input path
                    else return Fail("Only one input file can be specified", out options, out message);
                }
            }

            if (string.IsNullOrWhiteSpace(inputPath)) return Fail("Input file path is required.\n" + GetUsage(), out options, out message);
            if (!File.Exists(inputPath)) return Fail($"Input file not found: {inputPath}", out options, out message);

            // Extension is not restricted; warn if not .txt (non-fatal)

            if (minDur.HasValue && maxDur.HasValue && minDur.Value > maxDur.Value)
                return Fail("--min-duration cannot be greater than --max-duration", out options, out message);

            options = new Options
            {
                InputPath = inputPath!,
                OutputPath = outputPath,
                SentencesPerSubtitle = sps ?? 2,
                WordsPerMinute = wpm ?? DefaultWpm,
                Cps = cps ?? DefaultCps,
                GapSeconds = gap ?? 1.0,
                MinDurationSec = minDur ?? DefaultMinDurationSec,
                MaxDurationSec = maxDur ?? DefaultMaxDurationSec,
                SplitOnNewline = splitOnNewline,
                MaxLineLength = maxLineLen ?? DefaultMaxLineLength,
                MaxLines = maxLines ?? DefaultMaxLines
            };
            return true;

            static bool Fail(string msg, out Options? o, out string m)
            {
                o = null; m = msg; return false;
            }
        }

        private static string GetUsage() =>
            "Usage:\n" +
            "  SubtitleSplitter <input.txt> [options]\n\n" +
            "Options:\n" +
            "  --sentences-per-subtitle <int>   Number of sentences per caption (default 2)\n" +
            "  --wpm <int>                      Words per minute (default 200)\n" +
            "  --cps <number>                   Characters per second cap (default 15)\n" +
            "  --gap <seconds>                  Gap between captions (default 1.0)\n" +
            "  --min-duration <seconds>         Minimum caption duration (default 1.0)\n" +
            "  --max-duration <seconds>         Maximum caption duration (default 7.0)\n" +
            "  --split-on-newline               Split by newline instead of sentence detection\n" +
            "  --max-line-length <int>          Target max characters per line (default 42)\n" +
            "  --max-lines <int>                Max lines per caption (default 2)\n" +
            "  --output <path|dir>              Output file path or directory\n" +
            "  -o <path|dir>                    Short form for --output\n" +
            "  --help, -h                       Show this help\n";

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

        public static void SaveSubtitlesToFile(string[] subtitles, string inputFilePath, string? output)
        {
            if (string.IsNullOrEmpty(inputFilePath))
                throw new ArgumentException("File path cannot be null or empty.");

            string fullInputPath = Path.GetFullPath(inputFilePath);
            var directory = Path.GetDirectoryName(fullInputPath);
            if (string.IsNullOrEmpty(directory)) directory = Environment.CurrentDirectory;
            var fileName = Path.GetFileNameWithoutExtension(fullInputPath);

            string outputPath;
            if (!string.IsNullOrWhiteSpace(output))
            {
                var outPath = output!;
                if (Directory.Exists(outPath) || outPath.EndsWith(Path.DirectorySeparatorChar) || outPath.EndsWith(Path.AltDirectorySeparatorChar))
                {
                    // Treat as directory
                    Directory.CreateDirectory(outPath);
                    outputPath = Path.Combine(outPath, $"{fileName}_subtitles.srt");
                }
                else if (Path.GetExtension(outPath).Equals(".srt", StringComparison.OrdinalIgnoreCase))
                {
                    // Treat as file path
                    var outDir = Path.GetDirectoryName(Path.GetFullPath(outPath));
                    if (!string.IsNullOrEmpty(outDir)) Directory.CreateDirectory(outDir);
                    outputPath = Path.GetFullPath(outPath);
                }
                else
                {
                    // Unknown form; if parent exists treat as file, else as directory
                    var parent = Path.GetDirectoryName(outPath);
                    if (!string.IsNullOrEmpty(parent))
                    {
                        Directory.CreateDirectory(parent);
                        outputPath = Path.GetFullPath(outPath);
                    }
                    else
                    {
                        Directory.CreateDirectory(outPath);
                        outputPath = Path.Combine(outPath, $"{fileName}_subtitles.srt");
                    }
                }
            }
            else
            {
                outputPath = Path.Combine(directory!, $"{fileName}_subtitles.srt");
            }

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
            if (maxLines <= 0) return text;
            if (maxLineLength <= 10) maxLineLength = 10;

            var words = Regex.Split(text, @"\s+").Where(w => w.Length > 0).ToArray();
            var lines = new List<string>();
            var line = new StringBuilder();

            foreach (var word in words)
            {
                if (line.Length == 0)
                {
                    if (word.Length > maxLineLength)
                    {
                        int idx = 0;
                        while (idx < word.Length && lines.Count < maxLines)
                        {
                            int take = Math.Min(maxLineLength, word.Length - idx);
                            var segment = word.Substring(idx, take);
                            if (take == maxLineLength || idx + take < word.Length)
                            {
                                // full line segment
                                lines.Add(segment);
                            }
                            else
                            {
                                line.Append(segment);
                            }
                            idx += take;
                        }
                    }
                    else
                    {
                        line.Append(word);
                    }
                }
                else if (line.Length + 1 + word.Length <= maxLineLength)
                {
                    line.Append(' ').Append(word);
                }
                else
                {
                    lines.Add(line.ToString());
                    line.Clear();
                    // If a single word is longer than max, hard-break it
                    if (word.Length > maxLineLength)
                    {
                        int idx = 0;
                        while (idx < word.Length)
                        {
                            int take = Math.Min(maxLineLength, word.Length - idx);
                            if (lines.Count < maxLines - 1)
                            {
                                lines.Add(word.Substring(idx, take));
                            }
                            else
                            {
                                // Put remainder in the last line
                                line.Append(word.Substring(idx, take));
                            }
                            idx += take;
                            if (lines.Count >= maxLines) break;
                        }
                    }
                    else
                    {
                        line.Append(word);
                    }
                }

                if (lines.Count >= maxLines) break;
            }

            if (lines.Count < maxLines && line.Length > 0)
                lines.Add(line.ToString());

            // If still have words left and exceeded lines, append remainder to last line
            return string.Join("\n", lines.Take(maxLines));
        }

        [GeneratedRegex(@"(?<=[\.!\?\n])\s+")]
        private static partial Regex NaiveSentenceSplitRegex();

        [GeneratedRegex(@"(?i)\b(?:Mr|Mrs|Ms|Dr|Prof|Sr|Jr|St|Mt|Msgr|Messrs|vs|etc|e\.g|i\.e|cf|al)\.$")]
        private static partial Regex AbbrevEndRegex();
    }
}
