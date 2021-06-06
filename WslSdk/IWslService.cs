using System;
using System.Runtime.InteropServices;

namespace WslSdk
{
    [ComVisible(true)]
    [Guid("62BD3105-260E-45AF-834B-E6C790F986D0")]
    public interface IWslService
    {
        bool IsDistroRegistered(string distroName);

        DistroRegistryInfo GetDefaultDistro();

        string[] GetDistroList();

        string RunWslCommand(string distroName, string commandLine);
    }
}
