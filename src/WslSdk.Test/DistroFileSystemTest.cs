using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace WslSdk.Test
{
    [TestClass]
    public class DistroFileSystemTest
    {
        private dynamic ActivateWslService()
        {
            var wslServiceType = Type.GetTypeFromProgID("WslSdk.WslService");
            dynamic wslService = Activator.CreateInstance(wslServiceType);
            return wslService;
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
        public void Test_LinuxToWindowsPath_Recursive()
        {
            dynamic wslService = ActivateWslService();
            var randomName = wslService.GenerateRandomName(true);
            var busyboxRootfsFile = Path.GetFullPath("busybox.tgz");
            var tempDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WslSdkTest", randomName);

            wslService.RegisterDistro(randomName, busyboxRootfsFile, tempDirectory);
            var res = wslService.TranslateToWindowsPath(randomName, "/mnt/c/Windows");

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

        [TestMethod]
        public void Test_PathExistence()
        {
            dynamic wslService = ActivateWslService();
            var randomName = wslService.GenerateRandomName(true);
            var busyboxRootfsFile = Path.GetFullPath("busybox.tgz");
            var tempDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WslSdkTest", randomName);

            wslService.RegisterDistro(randomName, busyboxRootfsFile, tempDirectory);

            var res = wslService.TestLinuxPath(randomName, "/bin");
            Assert.IsTrue(res);

            res = wslService.TestLinuxPath(randomName, "/aaa");
            Assert.IsFalse(res);

            wslService.UnregisterDistro(randomName);
        }
    }
}
