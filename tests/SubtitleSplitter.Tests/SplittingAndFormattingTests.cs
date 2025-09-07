using System;
using System.Linq;
using FluentAssertions;
using SubtitleSplitter;
using Xunit;

namespace SubtitleSplitterProject.Tests;

public class SplittingAndFormattingTests
{
    private static SubtitleSplitter.SubtitleSplitter.Options DefaultOptions => new()
    {
        SentencesPerSubtitle = 1,
        WordsPerMinute = 200,
        Cps = 15,
        GapSeconds = 1,
        MinDurationSec = 1,
        MaxDurationSec = 7,
        SplitOnNewline = false,
        MaxLineLength = 30,
        MaxLines = 2,
        InputPath = "dummy.txt"
    };

    [Fact]
    public void DoesNotSplitOnAbbreviations()
    {
        var text = "Dr. Smith went home. He slept.";
        var opts = DefaultOptions with { SentencesPerSubtitle = 1 };
        var blocks = SubtitleSplitter.SubtitleSplitter.ConvertTextToSubtitles(text, opts);
        // Expect 2 captions
        blocks.Length.Should().Be(2);
        blocks[0].Should().Contain("Dr. Smith went home.");
        blocks[1].Should().Contain("He slept.");
    }

    [Fact]
    public void DoesNotSplitDecimalNumbers()
    {
        var text = "Version 3.2 is out. It's great.";
        var opts = DefaultOptions;
        var blocks = SubtitleSplitter.SubtitleSplitter.ConvertTextToSubtitles(text, opts);
        blocks.Length.Should().Be(2);
        blocks[0].Should().Contain("Version 3.2 is out.");
        blocks[1].Should().Contain("It's great.");
    }

    [Fact]
    public void EnforcesDurationClamp()
    {
        var text = "Hi."; // short -> ensure min duration
        var opts = DefaultOptions with { WordsPerMinute = 1000, Cps = 1000, MinDurationSec = 2, MaxDurationSec = 3 };
        var blocks = SubtitleSplitter.SubtitleSplitter.ConvertTextToSubtitles(text, opts);
        var times = ParseTimes(blocks[0]);
        (times.end - times.start).TotalSeconds.Should().BeApproximately(2.0, 0.01);

        var longText = string.Join(" ", Enumerable.Repeat("word", 100)); // long -> ensure max duration
        opts = DefaultOptions with { WordsPerMinute = 10, Cps = 1, MinDurationSec = 1, MaxDurationSec = 3 };
        blocks = SubtitleSplitter.SubtitleSplitter.ConvertTextToSubtitles(longText, opts);
        times = ParseTimes(blocks[0]);
        (times.end - times.start).TotalSeconds.Should().BeApproximately(3.0, 0.01);
    }

    [Fact]
    public void WrapsToMaxTwoLines()
    {
        var text = "This is a very long sentence that should wrap to two lines based on the configured maximum line length.";
        var opts = DefaultOptions with { MaxLineLength = 28, MaxLines = 2 };
        var blocks = SubtitleSplitter.SubtitleSplitter.ConvertTextToSubtitles(text, opts);
        var body = GetBodyLines(blocks[0]);
        body.Length.Should().BeLessOrEqualTo(2);
        body.All(l => l.Length <= 28).Should().BeTrue();
    }

    private static (TimeSpan start, TimeSpan end) ParseTimes(string block)
    {
        // block format:
        // number\n
        // 00:00:00,000 --> 00:00:03,733\n
        // text\n
        var lines = block.Split('\n');
        var timeLine = lines[1].Trim();
        var parts = timeLine.Split("-->", StringSplitOptions.TrimEntries);
        return (ParseSrt(parts[0]), ParseSrt(parts[1]));
    }

    private static TimeSpan ParseSrt(string s)
    {
        // HH:MM:SS,mmm
        var t = s.Trim();
        var h = int.Parse(t.Substring(0, 2));
        var m = int.Parse(t.Substring(3, 2));
        var sec = int.Parse(t.Substring(6, 2));
        var ms = int.Parse(t.Substring(9, 3));
        return new TimeSpan(0, h, m, sec, ms);
    }

    private static string[] GetBodyLines(string block)
    {
        var lines = block.Split('\n');
        // lines[0] = number, lines[1] = time range, remainder until blank is body
        return lines.Skip(2).TakeWhile(l => !string.IsNullOrWhiteSpace(l)).ToArray();
    }
}
