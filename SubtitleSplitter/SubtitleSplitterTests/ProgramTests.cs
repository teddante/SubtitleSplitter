using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    [TestClass()]
    public class ProgramTests
    {
        [TestMethod]
        public void ConvertTextToSubtitles_EmptyString_ReturnsEmptyArray()
        {
            var result = Program.ConvertTextToSubtitles("");
            Assert.AreEqual(0, result.Length);
        }

        [TestMethod]
        public void ConvertTextToSubtitles_OneSentence_ReturnsOneSubtitle()
        {
            var result = Program.ConvertTextToSubtitles("This is a test.");
            Assert.AreEqual(1, result.Length);
        }

        [TestMethod]
        public void ConvertTextToSubtitles_MultipleSentences_ReturnsMultipleSubtitles()
        {
            var result = Program.ConvertTextToSubtitles("This is a test. Another test.");
            Assert.AreEqual(2, result.Length);
        }

        [TestMethod]
        public void ConvertTextToSubtitles_MultipleSentencesAndSentencesPerSubtitle_ReturnsCorrectNumberOfSubtitles()
        {
            var result = Program.ConvertTextToSubtitles("This is a test. Another test. Yet another test.", 2);
            Assert.AreEqual(2, result.Length);
        }

        [TestMethod]
        public void SaveSubtitlesToFile_EmptyArray_DoesNotThrowException()
        {
            try
            {
                Program.SaveSubtitlesToFile(Array.Empty<string>());
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
                Program.SaveSubtitlesToFile(["Test subtitle"]);
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
                Program.SaveSubtitlesToFile(["Test subtitle", "Another test subtitle"]);
            }
            catch
            {
                Assert.Fail("Expected no exception, but got one.");
            }
        }
    }
}