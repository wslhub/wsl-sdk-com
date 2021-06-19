using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using WslSdk.Interop;
using WslSdk.Models;

namespace WslSdk
{
    internal static class Wsl
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
            var normalizedPath = Path.GetFullPath(basePath);

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
            distro.IsRegistered = NativeMethods.WslIsDistributionRegistered(registryInfoItem.DistroName);

            if (!distro.IsRegistered)
                return null;

            var hr = NativeMethods.WslGetDistributionConfiguration(
                registryInfoItem.DistroName,
                out int distroVersion,
                out int defaultUserId,
                out DistroFlags flags,
                out IntPtr environmentVariables,
                out int environmentVariableCount);

            if (hr != 0)
                return null;

            distro.WslVersion = distroVersion;
            distro.DefaultUid = defaultUserId;
            distro.DistroFlags = flags;

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

        /// <summary>
        /// Get details of WSL distributions reported as installed on the system by calling the WSL API.
        /// </summary>
        /// <returns>Returns the list of WSL distributions inquired for detailed information with the WSL API.</returns>
        public static IEnumerable<DistroInfo> EnumerateDistroQueryResult()
        {
            foreach (var eachItem in EnumerateDistroFromRegistry())
                yield return QueryDistro(eachItem);
        }

        /// <summary>
        /// Execute the specified command through the default shell of a specific WSL distribution, and get the result as a System.IO.Stream object.
        /// </summary>
        /// <param name="distroName">The name of the WSL distribution on which to run the command.</param>
        /// <param name="commandLine">The command you want to run.</param>
        /// <param name="outputStream">The System.IO.Stream object to receive the results. It must be writable.</param>
        /// <param name="bufferLength">Specifies the size of the buffer array to use when copying from anonymous pipes to the underlying stream. You do not need to specify a value.</param>
        /// <returns>Returns the sum of the number of bytes received.</returns>
        public static unsafe long RunWslCommand(string distroName, string commandLine, Stream outputStream, int bufferLength = 65536)
        {
            var isRegistered = NativeMethods.WslIsDistributionRegistered(distroName);

            if (!isRegistered)
                throw new Exception($"{distroName} is not registered distro.");

            var stdin = NativeMethods.GetStdHandle(NativeMethods.STD_INPUT_HANDLE);
            var stderr = NativeMethods.GetStdHandle(NativeMethods.STD_ERROR_HANDLE);

            var attributes = new NativeMethods.SECURITY_ATTRIBUTES
            {
                lpSecurityDescriptor = IntPtr.Zero,
                bInheritHandle = true,
            };
            attributes.nLength = Marshal.SizeOf(attributes);

            if (!NativeMethods.CreatePipe(out IntPtr readPipe, out IntPtr writePipe, ref attributes, 0))
                throw new Exception("Cannot create pipe for I/O.");

            try
            {
                var hr = NativeMethods.WslLaunch(distroName, commandLine, false, stdin, writePipe, stderr, out IntPtr child);

                if (hr < 0)
                    throw new COMException("Cannot launch WSL process", hr);

                NativeMethods.WaitForSingleObject(child, NativeMethods.INFINITE);

                if (!NativeMethods.GetExitCodeProcess(child, out int exitCode))
                {
                    var lastError = Marshal.GetLastWin32Error();
                    NativeMethods.CloseHandle(child);
                    throw new Win32Exception(lastError, "Cannot query exit code of the process.");
                }

                if (exitCode != 0)
                {
                    NativeMethods.CloseHandle(child);
                    throw new Exception($"Process exit code is non-zero: {exitCode}");
                }

                NativeMethods.CloseHandle(child);
                bufferLength = Math.Min(bufferLength, 1024);

                var bufferPointer = Marshal.AllocHGlobal(bufferLength);
                var pBufferPointer = (byte*)bufferPointer.ToPointer();

                var buffer = new byte[bufferLength];

                var length = 0L;
                var read = 0;

                while (true)
                {
                    if (!NativeMethods.ReadFile(readPipe, bufferPointer, bufferLength, out read, IntPtr.Zero))
                    {
                        var lastError = Marshal.GetLastWin32Error();
                        Marshal.FreeHGlobal(bufferPointer);

                        if (lastError != 0)
                            throw new Win32Exception(lastError, "Cannot read data from pipe.");

                        break;
                    }

                    fixed (byte* pBuffer = buffer)
                    {
                        Buffer.MemoryCopy(pBufferPointer, pBuffer, read, read);
                        length += read;
                    }
                    outputStream.Write(buffer, 0, read);

                    if (read < bufferLength)
                    {
                        Marshal.FreeHGlobal(bufferPointer);
                        break;
                    }
                }

                return length;
            }
            finally
            {
                NativeMethods.CloseHandle(readPipe);
                NativeMethods.CloseHandle(writePipe);
            }
        }

        /// <summary>
        /// Execute the specified command through the default shell of a specific WSL distribution, and get the result as a string.
        /// </summary>
        /// <remarks>
        /// When receiving data from WSL, it is encoded as UTF-8 data without the byte order mark.
        /// </remarks>
        /// <param name="distroName">The name of the WSL distribution on which to run the command.</param>
        /// <param name="commandLine">The command you want to run.</param>
        /// <param name="bufferLength">Specifies the size of the buffer array to use when copying from anonymous pipes to the underlying stream. You do not need to specify a value.</param>
        /// <returns>Returns the collected output string.</returns>
        public static unsafe string RunWslCommand(string distroName, string commandLine, int bufferLength = 65536)
        {
            var isRegistered = NativeMethods.WslIsDistributionRegistered(distroName);

            if (!isRegistered)
                throw new Exception($"{distroName} is not registered distro.");

            var stdin = NativeMethods.GetStdHandle(NativeMethods.STD_INPUT_HANDLE);
            var stderr = NativeMethods.GetStdHandle(NativeMethods.STD_ERROR_HANDLE);

            var attributes = new NativeMethods.SECURITY_ATTRIBUTES
            {
                lpSecurityDescriptor = IntPtr.Zero,
                bInheritHandle = true,
            };
            attributes.nLength = Marshal.SizeOf(attributes);

            if (!NativeMethods.CreatePipe(out IntPtr readPipe, out IntPtr writePipe, ref attributes, 0))
                throw new Exception("Cannot create pipe for I/O.");

            try
            {
                var hr = NativeMethods.WslLaunch(distroName, commandLine, false, stdin, writePipe, stderr, out IntPtr child);

                if (hr < 0)
                    throw new COMException("Cannot launch WSL process", hr);

                NativeMethods.WaitForSingleObject(child, NativeMethods.INFINITE);

                if (!NativeMethods.GetExitCodeProcess(child, out int exitCode))
                {
                    var lastError = Marshal.GetLastWin32Error();
                    NativeMethods.CloseHandle(child);
                    throw new Win32Exception(lastError, "Cannot query exit code of the process.");
                }

                if (exitCode != 0)
                {
                    NativeMethods.CloseHandle(child);
                    throw new Exception($"Process exit code is non-zero: {exitCode}");
                }

                NativeMethods.CloseHandle(child);

                bufferLength = Math.Min(bufferLength, 1024);
                var bufferPointer = Marshal.AllocHGlobal(bufferLength);
                var outputContents = new StringBuilder();
                var encoding = new UTF8Encoding(false);
                var read = 0;

                while (true)
                {
                    if (!NativeMethods.ReadFile(readPipe, bufferPointer, bufferLength, out read, IntPtr.Zero))
                    {
                        var lastError = Marshal.GetLastWin32Error();
                        Marshal.FreeHGlobal(bufferPointer);

                        if (lastError != 0)
                            throw new Win32Exception(lastError, "Cannot read data from pipe.");

                        break;
                    }

                    outputContents.Append(encoding.GetString((byte*)bufferPointer.ToPointer(), read));

                    if (read < bufferLength)
                    {
                        Marshal.FreeHGlobal(bufferPointer);
                        break;
                    }
                }

                return outputContents.ToString();
            }
            finally
            {
                NativeMethods.CloseHandle(readPipe);
                NativeMethods.CloseHandle(writePipe);
            }
        }
    }
}

