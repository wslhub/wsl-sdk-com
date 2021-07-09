using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using WslSdk.Contracts;
using WslSdk.Interop;
using WslSdk.Models;
using WslSdk.Shared;

namespace WslSdk
{
    [ComVisible(true)]
    [ProgId("WslSdk.WslService")]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid("1D0D99B6-AF95-4D3F-B55B-BA17CB2D549B")]
    [ComSourceInterfaces(typeof(IWslServiceEvents))]
    public class WslService : IWslService
    {
        public WslService()
        {
            // Increment the lock count of objects in the COM server.
            SdkApplication.Instance.Lock();
        }

        ~WslService()
        {
            // Decrement the lock count of objects in the COM server.
            SdkApplication.Instance.Unlock();
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
            var buffer = new StringBuilder();

            using (var writer = new StringWriter(buffer))
            {
                WslLauncher.RunCommandAsString(
                    WslNativeMethods.Api,
                    distroName, commandLine, false,
                    stdout: data => {
                        StdoutDataReceived?.Invoke(data);
                        writer.Write(data);
                    },
                    stderr: data => StderrDataReceived?.Invoke(data));
                writer.Flush();
            }

            return buffer.ToString();
        }

        public string RunWslCommandWithInput(string distroName, string commandLine, string inputFilePath)
        {
            Stream inputStream = null;
            var buffer = new StringBuilder();

            if (inputFilePath != null && File.Exists(inputFilePath))
                inputStream = File.OpenRead(inputFilePath);

            using (inputStream)
            using (var writer = new StringWriter(buffer))
            {
                WslLauncher.RunCommandAsString(
                    WslNativeMethods.Api,
                    distroName, commandLine, false,
                    stdin: inputStream,
                    stdout: data => {
                        StdoutDataReceived?.Invoke(data);
                        writer.Write(data);
                    },
                    stderr: data => StderrDataReceived?.Invoke(data));
                writer.Flush();
            }

            return buffer.ToString();
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

        public AccountInfo[] GetAccountInfoList(string distroName)
        {
            var etcPasswdFilePath = Path.Combine("\\\\wsl$", distroName, "etc", "passwd");
            var results = new List<AccountInfo>();

            if (!File.Exists(etcPasswdFilePath))
                return results.ToArray();

            var list = File.ReadAllLines(etcPasswdFilePath, Encoding.ASCII)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => new
                {
                    Raw = x,
                    Parts = x.Split(new char[] { ':', }, StringSplitOptions.None),
                })
                .Where(x => x.Parts.Length == 7)
                .Select(x =>
                {
                    var parts = x.Parts;
                    var userName = parts.ElementAtOrDefault(0);
                    var password = parts.ElementAtOrDefault(1);
                    var userIdRaw = parts.ElementAtOrDefault(2);
                    var groupIdRaw = parts.ElementAtOrDefault(3);
                    var userIdInfo = parts.ElementAtOrDefault(4);
                    var homeDirectoryPath = parts.ElementAtOrDefault(5);
                    var assignedShellPath = parts.ElementAtOrDefault(6);

                    var gecosParts = userIdInfo.Split(new char[] { ',', }, StringSplitOptions.None);
                    var userFullName = gecosParts.ElementAtOrDefault(0) ?? string.Empty;
                    var contactInfo = gecosParts.ElementAtOrDefault(1) ?? string.Empty;
                    var officePhoneNo = gecosParts.ElementAtOrDefault(2) ?? string.Empty;
                    var homePhoneNo = gecosParts.ElementAtOrDefault(3) ?? string.Empty;
                    var miscInfo = gecosParts.Skip(4).Select(y => y ?? string.Empty).ToArray();

                    var userId = int.Parse(userIdRaw);
                    var groupId = int.Parse(groupIdRaw);

                    return new AccountInfo
                    {
                        RawData = x.Raw,

                        Username = userName,
                        Password = password,
                        UserId = userId,
                        GroupId = groupId,
                        UserIdInfo = userIdInfo,
                        HomeDirectoryPath = homeDirectoryPath,
                        AssignedShellPath = assignedShellPath,

                        UserFullName = userFullName,
                        ContactInfo = contactInfo,
                        OfficePhoneNo = officePhoneNo,
                        HomePhoneNo = homePhoneNo,
                        MiscInfo = miscInfo,
                    };
                });

            results.AddRange(list);
            return results.ToArray();
        }

        public GroupInfo[] GetGroupInfoList(string distroName)
        {
            var etcGroupFilePath = Path.Combine("\\\\wsl$", distroName, "etc", "group");
            var results = new List<GroupInfo>();

            if (!File.Exists(etcGroupFilePath))
                return results.ToArray();

            var list = File.ReadAllLines(etcGroupFilePath, Encoding.ASCII)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => new
                {
                    Raw = x,
                    Parts = x.Split(new char[] { ':', }, StringSplitOptions.None),
                })
                .Where(x => x.Parts.Length == 4)
                .Select(x =>
                {
                    var parts = x.Parts;

                    var groupName = parts.ElementAtOrDefault(0);
                    var password = parts.ElementAtOrDefault(1);
                    var groupIdRaw = parts.ElementAtOrDefault(2);
                    var groupListRaw = parts.ElementAtOrDefault(3);

                    var groupId = int.Parse(groupIdRaw);

                    return new GroupInfo
                    {
                        RawData = x.Raw,

                        GroupName = groupName,
                        Password = password,
                        GroupId = groupId,
                        GroupUserList = groupListRaw,

                        GroupUserNames = groupListRaw.Split(new char[] { ',', }, StringSplitOptions.RemoveEmptyEntries).ToArray(),
                    };
                });

            results.AddRange(list);
            return results.ToArray();
        }

        [ComVisible(false)]
        public delegate void StdoutDataReceivedDelegate(string data);

        public event StdoutDataReceivedDelegate StdoutDataReceived;

        [ComVisible(false)]
        public delegate void StderrDataReceivedDelegate(string data);

        public event StderrDataReceivedDelegate StderrDataReceived;

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
