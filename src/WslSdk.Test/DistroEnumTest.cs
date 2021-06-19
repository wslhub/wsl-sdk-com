using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace WslSdk.Test
{
    [TestClass]
    public class DistroEnumTest
    {
        private dynamic ActivateWslService()
        {
            var wslServiceType = Type.GetTypeFromProgID("WslSdk.WslService");
            dynamic wslService = Activator.CreateInstance(wslServiceType);
            return wslService;
        }

        [TestMethod]
        public void Test_GetDefaultDistro()
        {
            dynamic wslService = ActivateWslService();
            dynamic distroInfo = wslService.GetDefaultDistro();

            Assert.IsNotNull(distroInfo);
            Assert.AreNotEqual(distroInfo.DistroId(), null);
            Assert.IsTrue(distroInfo.IsDefault());
        }

        [TestMethod]
        public void Test_GetDefaultDistroName()
        {
            dynamic wslService = ActivateWslService();
            dynamic defaultDistroName = wslService.GetDefaultDistroName();

            Assert.IsNotNull(defaultDistroName);
        }

        [TestMethod]
        public void Test_IsDistroRegistered()
        {
            dynamic wslService = ActivateWslService();
            dynamic defaultDistroName = wslService.GetDefaultDistroName();

            bool shouldRegistered = (bool)wslService.IsDistroRegistered(defaultDistroName);
            Assert.IsTrue(shouldRegistered);

            bool shouldNotRegistered = (bool)wslService.IsDistroRegistered(Guid.NewGuid().ToString("n"));
            Assert.IsFalse(shouldNotRegistered);
        }

        [TestMethod]
        public void Test_GetDistroList()
        {
            dynamic wslService = ActivateWslService();
            string[] distroList = (string[])wslService.GetDistroList();
            Assert.IsNotNull(distroList);
            Assert.IsTrue(distroList.Length > 0);
        }

        [TestMethod]
        public void Test_QueryDistroInfo()
        {
            dynamic wslService = ActivateWslService();
            var defaultDistroName = wslService.GetDefaultDistroName();
            dynamic queryResult = wslService.QueryDistroInfo(defaultDistroName);

            Assert.IsNotNull(defaultDistroName);
            Assert.AreNotEqual(queryResult.WslVersion(), 0);
            Assert.AreEqual(queryResult.DefaultUid().GetType(), typeof(int));
        }

        [TestMethod]
        public void Test_RunWslCommand()
        {
            dynamic wslService = ActivateWslService();
            var defaultDistroName = wslService.GetDefaultDistroName();
            var res = wslService.RunWslCommand(defaultDistroName, "cat /etc/os-release");

            Assert.IsNotNull(res);
            Assert.IsTrue(res.Length > 0);
        }
    }
}
