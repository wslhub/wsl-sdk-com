using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace WslSdk
{
    internal static class Wsl
    {
        /// <summary>
        /// Reads WSL-related information from a registry key and returns it as a model object.
        /// </summary>
        /// <param name="lxssKey">Registry key from which to read information.</param>
        /// <param name="keyName">The GUID name under the LXSS registry key.</param>
        /// <param name="parsedDefaultGuid">Default distribution's GUID key as recorded in the LXSS registry key.</param>
        /// <returns>Returns the WSL distribution information obtained through registry information.</returns>
        private static DistroRegistryInfo ReadFromRegistryKey(RegistryKey lxssKey, string keyName, Guid? parsedDefaultGuid)
        {
            if (!Guid.TryParse(keyName, out Guid parsedGuid))
                return null;

            using (var distroKey = lxssKey.OpenSubKey(keyName))
            {
                var distroName = distroKey.GetValue("DistributionName", default(string)) as string;

                if (string.IsNullOrWhiteSpace(distroName))
                    return null;

                var basePath = distroKey.GetValue("BasePath", default(string)) as string;
                var normalizedPath = Path.GetFullPath(basePath);

                var kernelCommandLine = (distroKey.GetValue("KernelCommandLine", default(string)) as string ?? string.Empty);
                var result = new DistroRegistryInfo()
                {
                    DistroId = parsedGuid.ToString(),
                    DistroName = distroName,
                    BasePath = normalizedPath,
                };
                result.KernelCommandLine = kernelCommandLine.Split(
                    new char[] { ' ', '\t', },
                    StringSplitOptions.RemoveEmptyEntries);

                if (parsedDefaultGuid.HasValue && parsedDefaultGuid == parsedGuid)
                {
                    result.IsDefault = true;
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns information about the default WSL distribution from the registry.
        /// </summary>
        /// <returns>
        /// Returns default WSL distribution information obtained through registry information.
        /// Returns null if no WSL distro is installed or no distro is set as the default.
        /// </returns>
        public static DistroRegistryInfo GetDefaultDistro()
        {
            var currentUser = Registry.CurrentUser;
            var lxssPath = Path.Combine("SOFTWARE", "Microsoft", "Windows", "CurrentVersion", "Lxss");

            using (var lxssKey = currentUser.OpenSubKey(lxssPath, false))
            {
                var defaultGuid = Guid.TryParse(
                    lxssKey.GetValue("DefaultDistribution", default(string)) as string,
                    out Guid parsedDefaultGuid) ? parsedDefaultGuid : default(Guid?);

                foreach (var keyName in lxssKey.GetSubKeyNames())
                {
                    var info = ReadFromRegistryKey(lxssKey, keyName, defaultGuid);

                    if (info == null)
                        continue;

                    if (info.IsDefault)
                        return info;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns information about WSL distributions obtained from the registry without calling the WSL API.
        /// </summary>
        /// <returns>Returns a list of information about the searched WSL distributions.</returns>
        public static IEnumerable<DistroRegistryInfo> GetDistroListFromRegistry()
        {
            var currentUser = Registry.CurrentUser;
            var lxssPath = Path.Combine("SOFTWARE", "Microsoft", "Windows", "CurrentVersion", "Lxss");

            using (var lxssKey = currentUser.OpenSubKey(lxssPath, false))
            {
                var defaultGuid = Guid.TryParse(
                    lxssKey.GetValue("DefaultDistribution", default(string)) as string,
                    out Guid parsedDefaultGuid) ? parsedDefaultGuid : default(Guid?);

                foreach (var keyName in lxssKey.GetSubKeyNames())
                {
                    var info = ReadFromRegistryKey(lxssKey, keyName, defaultGuid);

                    if (info == null)
                        continue;

                    if (info.IsDefault)
                        yield return info;
                }
            }
        }

        /// <summary>
        /// Get details of WSL distributions reported as installed on the system by calling the WSL API.
        /// </summary>
        /// <returns>Returns the list of WSL distributions inquired for detailed information with the WSL API.</returns>
        public unsafe static IEnumerable<DistroInfo> GetDistroQueryResult()
        {
            var results = new List<DistroInfo>();

            foreach (var eachItem in GetDistroListFromRegistry())
            {
                var distro = new DistroInfo()
                {
                    DistroId = eachItem.DistroId,
                    DistroName = eachItem.DistroName,
                    BasePath = eachItem.BasePath,
                };
                distro.KernelCommandLine = eachItem.KernelCommandLine;
                results.Add(distro);

                distro.IsRegistered = NativeMethods.WslIsDistributionRegistered(eachItem.DistroName);

                if (!distro.IsRegistered)
                    continue;

                var hr = NativeMethods.WslGetDistributionConfiguration(
                    eachItem.DistroName,
                    out int distroVersion,
                    out int defaultUserId,
                    out DistroFlags flags,
                    out IntPtr environmentVariables,
                    out int environmentVariableCount);

                if (hr != 0)
                    continue;

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
            }

            return results;
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

