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

        DistroRegistryInfo GetDistroInfo(string distroName);

        string GetDefaultDistroName();

        DistroInfo QueryDistroInfo(string distroName);

        bool SetDefaultUid(string distroName, int defaultUid);

        bool SetDistroFlags(string distroName, DistroFlags distroFlags);

        string GenerateRandomName(bool addNumberPostfix);

        bool RegisterDistro(string newDistroName, string tarGzipFilePath);

        bool UnregisterDistro(string existingDistroName);

        string GetWslWindowsPath(string distroName);

        string TranslateToWindowsPath(string distroName, string linuxPath);

        string TranslateToLinuxPath(string distroName, string windowsPath);

        string CreateDriveMapping(string distroName, string desiredDriveLetter);

        string CreateSymbolicLink(string distroName, string desiredPath, string symbolicLinkName);
    }
}
