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
        public void Test_GetDistroNames()
        {
            dynamic wslService = ActivateWslService();
            string[] distroNames = (string[])wslService.GetDistroNames();
            Assert.IsNotNull(distroNames);
            Assert.IsTrue(distroNames.Length > 0);
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

        [TestMethod]
        public void Test_QueryAccountInfoList()
        {
            dynamic wslService = ActivateWslService();
            var defaultDistroName = wslService.GetDefaultDistroName();
            var res = wslService.GetAccountInfoList(defaultDistroName);

            for (int i = 0; i < res.Length; i++)
            {
                dynamic eachUserInfo = res[i];
                Assert.IsNotNull(eachUserInfo.RawData);
                Assert.IsTrue(eachUserInfo.RawData.Length > 0);

                Assert.IsNotNull(eachUserInfo.Username);
                Assert.IsTrue(eachUserInfo.Username.Length > 0);
            }

            Assert.IsNotNull(res);
            Assert.IsTrue(res.Length > 0);
        }

        [TestMethod]
        public void Test_QueryGroupInfoList()
        {
            dynamic wslService = ActivateWslService();
            var defaultDistroName = wslService.GetDefaultDistroName();
            var res = wslService.GetGroupInfoList(defaultDistroName);

            for (int i = 0; i < res.Length; i++)
            {
                dynamic eachUserInfo = res[i];
                Assert.IsNotNull(eachUserInfo.RawData);
                Assert.IsTrue(eachUserInfo.RawData.Length > 0);

                Assert.IsNotNull(eachUserInfo.GroupName);
                Assert.IsTrue(eachUserInfo.GroupName.Length > 0);
            }

            Assert.IsNotNull(res);
            Assert.IsTrue(res.Length > 0);
        }

        [TestMethod]
        public void Test_AutoMountConfig()
        {
            dynamic wslService = ActivateWslService();
            var defaultDistroName = wslService.GetDefaultDistroName();
            wslService.GetAutoMountSettings(defaultDistroName);
        }

        [TestMethod]
        public void Test_NetworkConfig()
        {
            dynamic wslService = ActivateWslService();
            var defaultDistroName = wslService.GetDefaultDistroName();
            wslService.GetNetworkSettings(defaultDistroName);
        }
    }
}
