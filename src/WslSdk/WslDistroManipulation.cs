using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using WslSdk.Models;
using WslSdk.Shared;

namespace WslSdk
{
    internal static class WslDistroManipulation
    {
        private static Guid GetDefaultDistroGuid(RegistryKey lxssKey)
        {
            return Guid.Parse(lxssKey.GetValue("DefaultDistribution", default(string)) as string);
        }

        /// <summary>
        /// Reads WSL-related information from a registry key and returns it as a model object.
        /// </summary>
        /// <param name="distroKey">Registry key from which to read information.</param>
        /// <param name="parsedDefaultGuid">Default distribution's GUID key as recorded in the LXSS registry key.</param>
        /// <returns>Returns the WSL distribution information obtained through registry information.</returns>
        private static DistroRegistryInfo ReadFromRegistryKey(RegistryKey distroKey, Guid parsedDefaultGuid)
        {
            if (!Guid.TryParse(Path.GetFileName(distroKey.Name), out Guid parsedGuid))
                return null;

            var distroName = distroKey.GetValue("DistributionName", default(string)) as string;

            if (string.IsNullOrWhiteSpace(distroName))
                return null;

            var basePath = distroKey.GetValue("BasePath", default(string)) as string;
            var normalizedPath = Path.GetFullPath(basePath.Replace("\\\\?\\", string.Empty));

            var kernelCommandLine = (distroKey.GetValue("KernelCommandLine", default(string)) as string ?? string.Empty);

            return new DistroRegistryInfo()
            {
                DistroId = parsedGuid.ToString(),
                DistroName = distroName,
                BasePath = normalizedPath,
                IsDefault = (parsedDefaultGuid == parsedGuid),
                KernelCommandLine = kernelCommandLine.Split(
                    new char[] { ' ', '\t', },
                    StringSplitOptions.RemoveEmptyEntries),
            };
        }

        private static RegistryKey OpenLxssRegistryKey()
        {
            return Registry.CurrentUser.OpenSubKey(
                Path.Combine("SOFTWARE", "Microsoft", "Windows", "CurrentVersion", "Lxss"),
                false);
        }

        /// <summary>
        /// Returns information about the default WSL distribution from the registry.
        /// </summary>
        /// <returns>
        /// Returns default WSL distribution information obtained through registry information.
        /// Returns null if no WSL distro is installed or no distro is set as the default.
        /// </returns>
        public static DistroRegistryInfo GetDefaultDistroFromRegistry()
        {
            using (var lxssKey = OpenLxssRegistryKey())
            {
                var defaultGuid = GetDefaultDistroGuid(lxssKey);

                foreach (var keyName in lxssKey.GetSubKeyNames())
                {
                    using (var eachDistroKey = lxssKey.OpenSubKey(keyName))
                    {
                        var info = ReadFromRegistryKey(eachDistroKey, defaultGuid);

                        if (info == null)
                            continue;

                        if (info.IsDefault)
                            return info;
                    }
                }
            }

            return null;
        }

        public static DistroRegistryInfo GetDistroFromRegistry(string distroName)
        {
            using (var lxssKey = OpenLxssRegistryKey())
            {
                var defaultGuid = GetDefaultDistroGuid(lxssKey);

                foreach (var keyName in lxssKey.GetSubKeyNames())
                {
                    using (var eachDistroKey = lxssKey.OpenSubKey(keyName))
                    {
                        var info = ReadFromRegistryKey(eachDistroKey, defaultGuid);

                        if (info == null)
                            continue;

                        if (string.Equals(info.DistroName, distroName, StringComparison.Ordinal))
                            return info;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Returns information about WSL distributions obtained from the registry without calling the WSL API.
        /// </summary>
        /// <returns>Returns a list of information about the searched WSL distributions.</returns>
        public static IEnumerable<DistroRegistryInfo> EnumerateDistroFromRegistry()
        {
            using (var lxssKey = OpenLxssRegistryKey())
            {
                var defaultGuid = GetDefaultDistroGuid(lxssKey);

                foreach (var keyName in lxssKey.GetSubKeyNames())
                {
                    using (var eachDistroKey = lxssKey.OpenSubKey(keyName))
                    {
                        var info = ReadFromRegistryKey(eachDistroKey, defaultGuid);

                        if (info == null)
                            continue;

                        if (info.IsDefault)
                            yield return info;
                    }
                }
            }
        }

        public static DistroInfo QueryDefaultDistro()
        {
            return QueryDistro(GetDefaultDistroFromRegistry());
        }

        public static DistroInfo QueryDistro(string distroName)
        {
            return QueryDistro(GetDistroFromRegistry(distroName));
        }

        public unsafe static DistroInfo QueryDistro(DistroRegistryInfo registryInfoItem)
        {
            if (registryInfoItem == null)
                return null;

            var distro = new DistroInfo()
            {
                DistroId = registryInfoItem.DistroId,
                DistroName = registryInfoItem.DistroName,
                BasePath = registryInfoItem.BasePath,
            };

            distro.KernelCommandLine = registryInfoItem.KernelCommandLine;
            distro.IsRegistered = WslNativeMethods.Api.WslIsDistributionRegistered(registryInfoItem.DistroName);

            if (!distro.IsRegistered)
                return null;

            var hr = WslNativeMethods.Api.WslGetDistributionConfiguration(
                registryInfoItem.DistroName,
                out int distroVersion,
                out int defaultUserId,
                out int flags,
                out IntPtr environmentVariables,
                out int environmentVariableCount);

            if (hr != 0)
                return null;

            distro.WslVersion = distroVersion;
            distro.DefaultUid = defaultUserId;
            distro.DistroFlags = (DistroFlags)flags;

            var lpEnvironmentVariables = (byte***)environmentVariables.ToPointer();

            for (int i = 0; i < environmentVariableCount; i++)
            {
                byte** lpArray = lpEnvironmentVariables[i];
                var content = Marshal.PtrToStringAnsi(new IntPtr(lpArray));
                distro.DefaultEnvironmentVariables.Add(content);
                Marshal.FreeCoTaskMem(new IntPtr(lpArray));
            }

            Marshal.FreeCoTaskMem(new IntPtr(lpEnvironmentVariables));
            return distro;
        }

        public static void SetDistroConfiguration(string distroName, int? defaultUid, DistroFlags? distroFlags)
        {
            var currentConfig = QueryDistro(distroName);
            var newDefaultUid = currentConfig.DefaultUid;
            var newFlags = currentConfig.DistroFlags;

            if (defaultUid.HasValue)
                newDefaultUid = defaultUid.Value;

            if (distroFlags.HasValue)
                newFlags = distroFlags.Value;

            var hr = WslNativeMethods.Api.WslConfigureDistribution(distroName, newDefaultUid, (int)newFlags);

            if (hr != 0)
                throw new COMException($"Unexpected error occurred. ({hr:X8})", hr);
        }

        public static void RegisterDistro(string distroName, string tarGzipFilePath, string targetDirectoryPath)
        {
            if (string.IsNullOrWhiteSpace(distroName))
                throw new ArgumentException("Distro name cannot be null reference or empty string.", nameof(distroName));

            var distrorunPath = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "distrorun.exe");

            if (!File.Exists(distrorunPath))
                throw new FileNotFoundException("distrorun.exe required to run the distro registration.", distrorunPath);

            if (!File.Exists(tarGzipFilePath))
                throw new ArgumentException("Selected tar.gz file does not exists.", nameof(tarGzipFilePath));

            if (!Directory.Exists(targetDirectoryPath))
                Directory.CreateDirectory(targetDirectoryPath);

            var newLauncherPath = Path.Combine(targetDirectoryPath, distroName.TrimEnd('.') + ".exe");
            var newRootfsPath = Path.Combine(targetDirectoryPath, "install.tar.gz");

            File.Copy(distrorunPath, newLauncherPath, true);
            File.Copy(tarGzipFilePath, newRootfsPath, true);

            var launcherPsi = new ProcessStartInfo(newLauncherPath, "install")
            {
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
            };

            var process = new Process()
            {
                StartInfo = launcherPsi,
                EnableRaisingEvents = true,
            };

            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new Exception($"Process exit code is non-zero: {process.ExitCode} - {process.StandardError.ReadToEnd()}");
        }

        public static void UnregisterDistro(string distroName)
        {
            var hr = WslNativeMethods.Api.WslUnregisterDistribution(distroName);

            if (hr != 0)
                throw new COMException($"Unexpected error occurred. ({hr:X8})", hr);
        }

        /// <summary>
        /// Get details of WSL distributions reported as installed on the system by calling the WSL API.
        /// </summary>
        /// <returns>Returns the list of WSL distributions inquired for detailed information with the WSL API.</returns>
        public static IEnumerable<DistroInfo> EnumerateDistroQueryResult()
        {
            foreach (var eachItem in EnumerateDistroFromRegistry())
                yield return QueryDistro(eachItem);
        }
    }
}

