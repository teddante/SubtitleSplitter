using Microsoft.VisualStudio.TestTools.UnitTesting;
using SubtitleSplitter;

namespace Tests
{
    [TestClass()]
    public class SubtitleSplitterTests
    {
        readonly string tempFilePath = Path.GetTempFileName();

        [TestMethod]
        public void ConvertTextToSubtitles_EmptyString_ReturnsEmptyArray()
        {
            var result = SubtitleSplitter.SubtitleSplitter.ConvertTextToSubtitles("");
            Assert.AreEqual(0, result.Length);
        }

        [TestMethod]
        public void ConvertTextToSubtitles_OneSentence_ReturnsOneSubtitle()
        {
            var result = SubtitleSplitter.SubtitleSplitter.ConvertTextToSubtitles("This is a test.");
            Assert.AreEqual(1, result.Length);
        }

        [TestMethod]
        public void ConvertTextToSubtitles_MultipleSentences_ReturnsMultipleSubtitles()
        {
            var result = SubtitleSplitter.SubtitleSplitter.ConvertTextToSubtitles("This is a test. Another test.");
            Assert.AreEqual(2, result.Length);
        }

        [TestMethod]
        public void ConvertTextToSubtitles_MultipleSentencesAndSentencesPerSubtitle_ReturnsCorrectNumberOfSubtitles()
        {
            var result = SubtitleSplitter.SubtitleSplitter.ConvertTextToSubtitles("This is a test. Another test. Yet another test.", 2);
            Assert.AreEqual(2, result.Length);
        }

        [TestMethod]
        public void SaveSubtitlesToFile_EmptyArray_DoesNotThrowException()
        {
            try
            {
                SubtitleSplitter.SubtitleSplitter.SaveSubtitlesToFile([], tempFilePath);
            }
            catch
            {
                Assert.Fail("Expected no exception, but got one.");
            }
        }

        [TestMethod]
        public void SaveSubtitlesToFile_OneSubtitle_DoesNotThrowException()
        {
            try
            {
                SubtitleSplitter.SubtitleSplitter.SaveSubtitlesToFile(["Test subtitle"], tempFilePath);
            }
            catch
            {
                Assert.Fail("Expected no exception, but got one.");
            }
        }

        [TestMethod]
        public void SaveSubtitlesToFile_MultipleSubtitles_DoesNotThrowException()
        {
            try
            {
                SubtitleSplitter.SubtitleSplitter.SaveSubtitlesToFile(["Test subtitle", "Another test subtitle"], tempFilePath);
            }
            catch
            {
                Assert.Fail("Expected no exception, but got one.");
            }
        }
    }
}