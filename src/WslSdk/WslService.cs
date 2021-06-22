using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using WslSdk.Contracts;
using WslSdk.Interop;
using WslSdk.Models;
using WslSdk.Shared;

namespace WslSdk
{
    [ComVisible(true)]
    [ProgId("WslSdk.WslService")]
    [Guid("1D0D99B6-AF95-4D3F-B55B-BA17CB2D549B")]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    public class WslService : IWslService
    {
        public WslService()
        {
            // Increment the lock count of objects in the COM server.
            WslComServer.Instance.Lock();
        }

        ~WslService()
        {
            // Decrement the lock count of objects in the COM server.
            WslComServer.Instance.Unlock();
        }

        public bool IsDistroRegistered(string distroName)
        {
            return WslNativeMethods.Api.WslIsDistributionRegistered(distroName);
        }

        public DistroRegistryInfo GetDefaultDistro()
        {
            return WslDistroManipulation.GetDefaultDistroFromRegistry();
        }

        public string[] GetDistroList()
        {
            return WslDistroManipulation.EnumerateDistroFromRegistry().Select(x => x.DistroName).ToArray();
        }

        public string RunWslCommand(string distroName, string commandLine)
        {
            return WslInteraction.RunWslCommand(distroName, commandLine);
        }

        public DistroRegistryInfo GetDistroInfo(string distroName)
        {
            return WslDistroManipulation.GetDistroFromRegistry(distroName);
        }

        public string GetDefaultDistroName()
        {
            return WslDistroManipulation.GetDefaultDistroFromRegistry()?.DistroName;
        }

        public DistroInfo QueryDistroInfo(string distroName)
        {
            return WslDistroManipulation.QueryDistro(distroName);
        }

        public void SetDefaultUid(string distroName, int defaultUid)
        {
            WslDistroManipulation.SetDistroConfiguration(distroName, defaultUid, null);
        }

        public void SetDistroFlags(string distroName, DistroFlags distroFlags)
        {
            WslDistroManipulation.SetDistroConfiguration(distroName, null, distroFlags);
        }

        public string GenerateRandomName(bool addNumberPostfix)
        {
            return NamesGenerator.GetRandomName(addNumberPostfix ? 1 : 0);
        }

        public void RegisterDistro(string newDistroName, string tarGzipFilePath, string targetDirectoryPath)
        {
            WslDistroManipulation.RegisterDistro(newDistroName, tarGzipFilePath, targetDirectoryPath);
        }

        public void UnregisterDistro(string existingDistroName)
        {
            WslDistroManipulation.UnregisterDistro(existingDistroName);
        }

        public string GetWslWindowsPath(string distroName)
        {
            var targetDirectoryPath = Path.Combine("\\\\wsl$", distroName);

            if (!Directory.Exists(targetDirectoryPath))
                return null;

            return targetDirectoryPath;
        }

        public string TranslateToWindowsPath(string distroName, string linuxPath)
        {
            return WslInteraction.RunWslCommand(distroName, $"/usr/bin/wslpath -a -w {linuxPath}");
        }

        public string TranslateToLinuxPath(string distroName, string windowsPath)
        {
            return WslInteraction.RunWslCommand(distroName, $"/usr/bin/wslpath -a -u {windowsPath}");
        }

        public string CreateDriveMapping(string distroName, string desiredDriveLetter)
        {
            throw new NotImplementedException();
        }

        public string CreateSymbolicLink(string distroName, string desiredPath, string symbolicLinkName)
        {
            throw new NotImplementedException();
        }

        // These routines perform the additional COM registration needed by 
        // the service.

        [ComRegisterFunction]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Register(Type t)
        {
            try
            {
                HelperMethods.RegasmRegisterLocalServer(t);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message); // Log the error
                throw ex; // Re-throw the exception
            }
        }

        [ComUnregisterFunction]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Unregister(Type t)
        {
            try
            {
                HelperMethods.RegasmUnregisterLocalServer(t);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message); // Log the error
                throw ex; // Re-throw the exception
            }
        }
    }
}
