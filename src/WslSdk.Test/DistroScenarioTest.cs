using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

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
            var res = wslService.RunWslCommand(defaultDistroName, "curl https://www.google.com");

            Assert.IsNotNull(res);
            Assert.IsTrue(res.Length > 0);
            Assert.IsTrue(res.Contains("</html>"));
        }

    }
}
