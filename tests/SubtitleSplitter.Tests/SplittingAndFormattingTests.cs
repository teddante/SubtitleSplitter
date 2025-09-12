using System;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using SubtitleSplitter;
using Xunit;

namespace SubtitleSplitterProject.Tests;

public class SplittingAndFormattingTests
{

    [Fact]
    public void DoesNotSplitOnAbbreviations()
    {
        var text = "Dr. Smith went home. He slept.";
        var blocks = SubtitleSplitter.SubtitleSplitter.ConvertTextToSubtitles(text);
        // Expect 2 captions
        blocks.Length.Should().Be(2);
        blocks[0].Should().Contain("Dr. Smith went home.");
        blocks[1].Should().Contain("He slept.");
    }

    [Fact]
    public void DoesNotSplitDecimalNumbers()
    {
        var text = "Version 3.2 is out. It's great.";
        var blocks = SubtitleSplitter.SubtitleSplitter.ConvertTextToSubtitles(text);
        blocks.Length.Should().Be(2);
        blocks[0].Should().Contain("Version 3.2 is out.");
        blocks[1].Should().Contain("It's great.");
    }

    [Fact]
    public void ComputesDurationByAverageCps()
    {
        var text = "Hi."; // 3 characters -> 3 / 15 = 0.2s
        var blocks = SubtitleSplitter.SubtitleSplitter.ConvertTextToSubtitles(text);
        var times = ParseTimes(blocks[0]);
        (times.end - times.start).TotalSeconds.Should().BeApproximately(3.0 / 15.0, 0.01);
    }

    [Fact]
    public void WrapsToMaxTwoLines_NoTruncation()
    {
        var text = "This is a very long sentence that should wrap to two lines based on the configured maximum line length.";
        var blocks = SubtitleSplitter.SubtitleSplitter.ConvertTextToSubtitles(text);
        var body = GetBodyLines(blocks[0]);
        // No more than two lines in the caption
        body.Length.Should().BeLessOrEqualTo(2);
        // Lines except the final one should respect the max line length
        if (body.Length > 1)
        {
            body.Take(body.Length - 1).All(l => l.Length <= 42).Should().BeTrue();
        }

        // Ensure no truncation: joined text should equal normalized input
        string Normalize(string s) => Regex.Replace(s.Replace("\r", " ").Replace("\n", " "), "\\s+", " ").Trim();
        var joined = Normalize(string.Join(" ", body));
        joined.Should().Be(Normalize(text));
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
