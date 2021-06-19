using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;

namespace WslSdk.Test
{
    [TestClass]
    public class DistroManipTest
    {
        private dynamic ActivateWslService()
        {
            var wslServiceType = Type.GetTypeFromProgID("WslSdk.WslService");
            dynamic wslService = Activator.CreateInstance(wslServiceType);
            return wslService;
        }

        [TestMethod]
        public void Test_RandomNameGenerator()
        {
            var candidates = new List<string>(100);
            for (int i = 0; i < candidates.Capacity; i++)
                candidates.Add(NamesGenerator.GetRandomName(1));

            Assert.IsTrue(candidates.Count > 0);
        }

        [TestMethod]
        public void Test_DistroRegisterUnregister()
        {
            dynamic wslService = ActivateWslService();
            var randomName = NamesGenerator.GetRandomName(1);
            var busyboxRootfsFile = Path.GetFullPath("busybox.tgz");

            var registerResult = wslService.RegisterDistro(randomName, busyboxRootfsFile);
            var res = wslService.RunWslCommand(randomName, "ls /");
            var unregisterResult = wslService.UnregisterDistro(randomName);

            Assert.IsNotNull(res);
            Assert.IsTrue(res.Length > 0);
            Assert.IsTrue(registerResult);
            Assert.IsTrue(unregisterResult);
        }

        [TestMethod]
        public void Test_DistroConfigurationChange()
        {
            dynamic wslService = ActivateWslService();
            var randomName = NamesGenerator.GetRandomName(1);
            var busyboxRootfsFile = Path.GetFullPath("busybox.tgz");

            var registerResult = wslService.RegisterDistro(randomName, busyboxRootfsFile);
            dynamic queryResult = wslService.QueryDistroInfo(randomName);
            var res = wslService.RunWslCommand(randomName, "ls /");
            var setDefaultUidResult = wslService.SetDefaultUid(randomName, queryResult.DefaultUid());
            var setFlagResult = wslService.SetDistroFlags(randomName, queryResult.DistroFlags());
            var unregisterResult = wslService.UnregisterDistro(randomName);

            Assert.IsNotNull(res);
            Assert.IsTrue(res.Length > 0);
            Assert.IsTrue(registerResult);
            Assert.IsTrue(setDefaultUidResult);
            Assert.IsTrue(setFlagResult);
            Assert.IsTrue(unregisterResult);

            Assert.IsNotNull(randomName);
            Assert.AreNotEqual(queryResult.WslVersion(), 0);
            Assert.AreEqual(queryResult.DefaultUid().GetType(), typeof(int));
        }
    }
}
