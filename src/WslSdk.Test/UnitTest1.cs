using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace WslSdk.Test
{
    [TestClass]
    public class DistroEnumTest
    {
        [TestMethod]
        public void Test_GetDefaultDistro()
        {
            var wslServiceType = Type.GetTypeFromProgID("WslSdk.WslService");
            dynamic wslService = Activator.CreateInstance(wslServiceType);
            dynamic distroInfo = wslService.GetDefaultDistro();
            Assert.AreNotEqual(distroInfo.DistroId(), null);
        }
    }
}
