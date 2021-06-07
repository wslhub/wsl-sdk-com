using System;
using System.Runtime.InteropServices;
using WslSdk.Models;

namespace WslSdk.Contracts
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
