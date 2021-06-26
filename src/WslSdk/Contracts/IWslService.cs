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

        void SetDefaultUid(string distroName, int defaultUid);

        void SetDistroFlags(string distroName, DistroFlags distroFlags);

        string GenerateRandomName(bool addNumberPostfix);

        void RegisterDistro(string newDistroName, string tarGzipFilePath, string targetDirectoryPath);

        void UnregisterDistro(string existingDistroName);

        string GetWslWindowsPath(string distroName);

        string TranslateToWindowsPath(string distroName, string linuxAbsolutePath);

        string TranslateToLinuxPath(string distroName, string windowsAbsolutePath);

        string CreateDriveMapping(string distroName, string desiredDriveLetter);

        string CreateSymbolicLink(string distroName, string desiredPath, string symbolicLinkName);
    }
}
