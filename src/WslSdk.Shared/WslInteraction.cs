﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace WslSdk.Shared
{
    internal static class WslInteraction
    {
        /// <summary>
        /// Execute the specified command through the default shell of a specific WSL distribution, and get the result as a System.IO.Stream object.
        /// </summary>
        /// <param name="distroName">The name of the WSL distribution on which to run the command.</param>
        /// <param name="commandLine">The command you want to run.</param>
        /// <param name="dataReceivedCallback">The callback receiving data segment from the child process.</param>
        /// <param name="bufferLength">Specifies the size of the buffer array to use when copying from anonymous pipes to the underlying stream. You do not need to specify a value.</param>
        /// <returns>Returns the sum of the number of bytes received.</returns>
        public static unsafe long RunWslCommand(string distroName, string commandLine, Action<ArraySegment<byte>> dataReceivedCallback, int bufferLength = 65536)
        {
            if (dataReceivedCallback == null)
                throw new ArgumentNullException(nameof(dataReceivedCallback));

            var isRegistered = WslNativeMethods.Api.WslIsDistributionRegistered(distroName);

            if (!isRegistered)
                throw new Exception($"{distroName} is not registered distro.");

            var stdin = Win32NativeMethods.GetStdHandle(Win32NativeMethods.STD_INPUT_HANDLE);
            var stderr = Win32NativeMethods.GetStdHandle(Win32NativeMethods.STD_ERROR_HANDLE);

            var attributes = new Win32NativeMethods.SECURITY_ATTRIBUTES
            {
                lpSecurityDescriptor = IntPtr.Zero,
                bInheritHandle = true,
            };
            attributes.nLength = Marshal.SizeOf(attributes);

            if (!Win32NativeMethods.CreatePipe(out IntPtr readPipe, out IntPtr writePipe, ref attributes, 0))
                throw new Exception("Cannot create pipe for I/O.");

            bufferLength = Math.Max(bufferLength, 1024);
            var bufferPointer = Marshal.AllocHGlobal(bufferLength);
            var pBufferPointer = (byte*)bufferPointer.ToPointer();

            try
            {
                var hr = WslNativeMethods.Api.WslLaunch(distroName, commandLine, false, stdin, writePipe, stderr, out IntPtr child);

                if (hr < 0)
                    throw new COMException("Cannot launch WSL process", hr);

                Win32NativeMethods.WaitForSingleObject(child, Win32NativeMethods.INFINITE);

                if (!Win32NativeMethods.GetExitCodeProcess(child, out int exitCode))
                {
                    var lastError = Marshal.GetLastWin32Error();
                    Win32NativeMethods.CloseHandle(child);
                    throw new Win32Exception(lastError, "Cannot query exit code of the process.");
                }

                if (exitCode != 0)
                {
                    Win32NativeMethods.CloseHandle(child);
                    throw new Exception($"Process exit code is non-zero: {exitCode}");
                }

                Win32NativeMethods.CloseHandle(child);
                var buffer = new byte[bufferLength];
                var length = 0L;
                var read = 0;

                while (true)
                {
                    Win32NativeMethods.RtlZeroMemory(bufferPointer, bufferLength);

                    if (!Win32NativeMethods.ReadFile(readPipe, bufferPointer, bufferLength - 1, out read, IntPtr.Zero))
                    {
                        var lastError = Marshal.GetLastWin32Error();

                        if (lastError != 0)
                            throw new Win32Exception(lastError, "Cannot read data from pipe.");

                        break;
                    }

                    fixed (byte* pBuffer = buffer)
                    {
                        Buffer.MemoryCopy(pBufferPointer, pBuffer, read, read);
                        length += read;
                    }
                    dataReceivedCallback.Invoke(new ArraySegment<byte>(buffer, 0, read));

                    if (read < bufferLength - 1)
                        break;
                }

                return length;
            }
            finally
            {
                Marshal.FreeHGlobal(bufferPointer);
                Win32NativeMethods.CloseHandle(readPipe);
                Win32NativeMethods.CloseHandle(writePipe);
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
        public static string RunWslCommand(string distroName, string commandLine, int bufferLength = 65536)
        {
            var content = new StringBuilder();
            var utf8Encoding = new UTF8Encoding(false);
            RunWslCommand(distroName, commandLine,
                x => content.Append(utf8Encoding.GetString(x.Array, x.Offset, x.Count)),
                bufferLength);
            return content.ToString();
        }

        public static string FindExistingPath(string distroName, params string[] unixPathCandidates)
        {
            var basePath = Path.Combine($@"\\wsl$\{distroName}");

            if (!Directory.Exists(basePath))
                return null;

            var baseUri = new Uri(basePath.Replace("wsl$", "wsl.localhost"), UriKind.Absolute);

            foreach (var eachUnixPathCandidate in unixPathCandidates)
            {
                if (!Uri.TryCreate(eachUnixPathCandidate, UriKind.Relative, out Uri unixPath))
                    continue;

                var combinedPath = new Uri(baseUri, unixPath);
                var eachFullPath = combinedPath.LocalPath.Replace("wsl.localhost", "wsl$");

                if (Directory.Exists(eachFullPath) || File.Exists(eachFullPath))
                    return eachUnixPathCandidate;
            }

            return null;
        }
    }
}
