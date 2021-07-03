using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;

namespace WslSdk.Test
{
    [TestClass]
    public class DistroScenarioTest
    {
        private dynamic ActivateWslService()
        {
            var wslServiceType = Type.GetTypeFromProgID("WslSdk.WslService");
            dynamic wslService = Activator.CreateInstance(wslServiceType);
            return wslService;
        }

        private string GenerateRandomLatinText(int minWords = 10, int maxWords = 30,
            int minSentences = 2, int maxSentences = 5,
            int numParagraphs = 3)
        {
            // https://stackoverflow.com/questions/4286487/is-there-any-lorem-ipsum-generator-in-c

            var words = new string[] {
                "lorem", "ipsum", "dolor", "sit", "amet", "consectetuer",
                "adipiscing", "elit", "sed", "diam", "nonummy", "nibh", "euismod",
                "tincidunt", "ut", "laoreet", "dolore", "magna", "aliquam", "erat"
            };

            var rand = new Random();
            int numSentences = rand.Next(maxSentences - minSentences)
                + minSentences + 1;
            int numWords = rand.Next(maxWords - minWords) + minWords + 1;

            var result = new StringBuilder();

            for (int p = 0; p < numParagraphs; p++)
            {
                for (int s = 0; s < numSentences; s++)
                {
                    for (int w = 0; w < numWords; w++)
                    {
                        if (w > 0) { result.Append(" "); }
                        result.Append(words[rand.Next(words.Length)]);
                    }
                    result.Append(". ");
                }
            }

            return result.ToString();
        }

        [TestMethod]
        public void Test_EmptyCommand_0()
        {
            dynamic wslService = ActivateWslService();
            var defaultDistroName = wslService.GetDefaultDistroName();
            try
            {
                wslService.RunWslCommand(defaultDistroName, null);
                Assert.Fail();
            }
            catch { }
        }

        [TestMethod]
        public void Test_EmptyCommand_1()
        {
            dynamic wslService = ActivateWslService();
            var defaultDistroName = wslService.GetDefaultDistroName();
            try
            {
                wslService.RunWslCommand(defaultDistroName, string.Empty);
                Assert.Fail();
            }
            catch { }
        }

        [TestMethod]
        public void Test_EmptyCommand_2()
        {
            dynamic wslService = ActivateWslService();
            var defaultDistroName = wslService.GetDefaultDistroName();
            try
            {
                wslService.RunWslCommand(defaultDistroName, new string(' ', 128));
                Assert.Fail();
            }
            catch { }
        }

        [TestMethod]
        public void Test_CurlSimple()
        {
            dynamic wslService = ActivateWslService();
            var defaultDistroName = wslService.GetDefaultDistroName();
            var res = wslService.RunWslCommand(defaultDistroName, "curl https://api.ipify.org");

            Assert.IsNotNull(res);
            Assert.IsTrue(res.Length > 0);
        }

        [TestMethod]
        public void Test_CurlLargeContent()
        {
            dynamic wslService = ActivateWslService();
            var defaultDistroName = wslService.GetDefaultDistroName();
            var res = wslService.RunWslCommand(defaultDistroName, "curl https://www.naver.com");

            Assert.IsNotNull(res);
            Assert.IsTrue(res.Length > 0);
            Assert.IsTrue(res.Contains("</html>"));
        }

        [TestMethod]
        public void Test_CatTest()
        {
            dynamic wslService = ActivateWslService();
            var defaultDistroName = wslService.GetDefaultDistroName();

            var tempFilePath = Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString("n") + ".txt");
            var content = GenerateRandomLatinText();
            File.WriteAllText(tempFilePath, content, new UTF8Encoding(false));

            try
            {
                var res = wslService.RunWslCommandWithInput(defaultDistroName, "cat", tempFilePath);

                Assert.IsNotNull(res);
                Assert.IsTrue(res.Length > 0);
                Assert.IsTrue(res.Equals(content, StringComparison.Ordinal));
            }
            finally
            {
                if (tempFilePath != null && File.Exists(tempFilePath))
                {
                    try { File.Delete(tempFilePath); }
                    catch { }
                }
            }
        }
    }
}
