using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text.RegularExpressions;

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
        public void Test_GenerateRandomName()
        {
            dynamic wslService = ActivateWslService();

            var withoutPostfix = wslService.GenerateRandomName(false);
            var withPostfix = wslService.GenerateRandomName(true);

            Assert.IsFalse(Regex.IsMatch(withoutPostfix, "[0-9]+$"));
            Assert.IsTrue(Regex.IsMatch(withPostfix, "[0-9]+$"));
        }

        [TestMethod]
        public void Test_DistroRegisterUnregister()
        {
            dynamic wslService = ActivateWslService();
            var randomName = wslService.GenerateRandomName(true);
            var busyboxRootfsFile = Path.GetFullPath("busybox.tgz");
            var tempDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WslSdkTest", randomName);

            wslService.RegisterDistro(randomName, busyboxRootfsFile, tempDirectory);
            var res = wslService.RunWslCommand(randomName, "ls /");
            wslService.UnregisterDistro(randomName);

            Assert.IsNotNull(res);
            Assert.IsTrue(res.Length > 0);
        }

        [TestMethod]
        public void Test_DistroConfigurationChange()
        {
            dynamic wslService = ActivateWslService();
            var randomName = wslService.GenerateRandomName(true);
            var busyboxRootfsFile = Path.GetFullPath("busybox.tgz");
            var tempDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WslSdkTest", randomName);

            wslService.RegisterDistro(randomName, busyboxRootfsFile, tempDirectory);
            dynamic queryResult = wslService.QueryDistroInfo(randomName);
            var res = wslService.RunWslCommand(randomName, "ls /");
            wslService.SetDefaultUid(randomName, queryResult.DefaultUid());
            wslService.SetDistroFlags(randomName, queryResult.DistroFlags());
            wslService.UnregisterDistro(randomName);

            Assert.IsNotNull(res);
            Assert.IsTrue(res.Length > 0);

            Assert.IsNotNull(randomName);
            Assert.AreNotEqual(queryResult.WslVersion(), 0);
            Assert.AreEqual(queryResult.DefaultUid().GetType(), typeof(int));
        }

        [TestMethod]
        public void Test_LinuxToWindowsPath()
        {
            dynamic wslService = ActivateWslService();
            var randomName = wslService.GenerateRandomName(true);
            var busyboxRootfsFile = Path.GetFullPath("busybox.tgz");
            var tempDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WslSdkTest", randomName);

            wslService.RegisterDistro(randomName, busyboxRootfsFile, tempDirectory);
            var res = wslService.TranslateToWindowsPath(randomName, "/bin");

            Assert.IsNotNull(res);
            Assert.IsTrue(res.Length > 0);
            Assert.IsTrue(Directory.Exists(res));

            wslService.UnregisterDistro(randomName);
        }

        [TestMethod]
        public void Test_WindowsToLinuxPath()
        {
            dynamic wslService = ActivateWslService();
            var randomName = wslService.GenerateRandomName(true);
            var busyboxRootfsFile = Path.GetFullPath("busybox.tgz");
            var tempDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WslSdkTest", randomName);

            wslService.RegisterDistro(randomName, busyboxRootfsFile, tempDirectory);
            var res = wslService.TranslateToLinuxPath(randomName, @"C:\\Windows");

            Assert.IsNotNull(res);
            Assert.IsTrue(res.Length > 0);
            Assert.AreEqual("/mnt/c/Windows", res);

            wslService.UnregisterDistro(randomName);
        }

        [TestMethod]
        public void Test_WindowsToLinuxPath_Recursive()
        {
            dynamic wslService = ActivateWslService();
            var randomName = wslService.GenerateRandomName(true);
            var busyboxRootfsFile = Path.GetFullPath("busybox.tgz");
            var tempDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WslSdkTest", randomName);

            wslService.RegisterDistro(randomName, busyboxRootfsFile, tempDirectory);
            var res = wslService.TranslateToLinuxPath(randomName, $@"\\\\wsl$\\{randomName}\\bin");

            Assert.IsNotNull(res);
            Assert.IsTrue(res.Length > 0);
            Assert.AreEqual("/bin", res);

            wslService.UnregisterDistro(randomName);
        }
    }
}
