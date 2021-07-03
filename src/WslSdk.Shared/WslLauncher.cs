﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace WslSdk.Shared
{
    internal sealed class WslLauncher : IDisposable
    {
        public const int DefaultBufferSize = 65536;

        public static int? RunCommand(
            string distroName,
            string command,
            bool useCurrentWorkingDirectory,
            out long stdinWrittenByte,
            Stream stdin = null,
            Action<byte[]> stdout = null,
            Action<byte[]> stderr = null,
            int bufferSize = DefaultBufferSize)
        {
            using (var wsl = new WslLauncher(distroName, command, bufferSize))
            {
                return wsl.Start(useCurrentWorkingDirectory, out stdinWrittenByte, stdin, stdout, stderr);
            }
        }

        public static int? RunCommand(
            string distroName,
            string command,
            bool useCurrentWorkingDirectory,
            Stream stdin = null,
            Action<byte[]> stdout = null,
            Action<byte[]> stderr = null,
            int bufferSIze = DefaultBufferSize)
        {
            return RunCommand(distroName, command, useCurrentWorkingDirectory, out _, stdin, stdout, stderr, bufferSIze);
        }

        public static string RunCommand(
            string distroName,
            string command,
            bool useCurrentWorkingDirectory)
        {
            var encoding = new UTF8Encoding(false);
            var buffer = new StringBuilder();

            RunCommand(distroName, command, useCurrentWorkingDirectory,
                stdout: data => buffer.Append(encoding.GetString(data, 0, data.Length)));

            return buffer.ToString();
        }

        private WslLauncher(string distroName, string command, int bufferSize = DefaultBufferSize)
            : base()
        {
            _distroName = distroName;
            _command = command;
            _bufferSize = Math.Max(1024, bufferSize);

            _disposed = false;

            _hChildStd_IN_Rd = IntPtr.Zero;
            _hChildStd_IN_Wr = IntPtr.Zero;
            _hChildStd_OUT_Rd = IntPtr.Zero;
            _hChildStd_OUT_Wr = IntPtr.Zero;
            _hChildStd_ERR_Rd = IntPtr.Zero;
            _hChildStd_ERR_Wr = IntPtr.Zero;

            _securityAttributes = new Win32NativeMethods.SECURITY_ATTRIBUTES()
            {
                lpSecurityDescriptor = IntPtr.Zero,
                bInheritHandle = true,
            };
            _securityAttributes.nLength = Marshal.SizeOf(_securityAttributes);

            // Create a pipe for the child process's STDOUT. 
            if (!Win32NativeMethods.CreatePipe(out _hChildStd_OUT_Rd, out _hChildStd_OUT_Wr, ref _securityAttributes, 0))
                throw new Exception("StdoutRd CreatePipe");

            // Ensure the read handle to the pipe for STDOUT is not inherited.
            if (!Win32NativeMethods.SetHandleInformation(_hChildStd_OUT_Rd, Win32NativeMethods.HANDLE_FLAG_INHERIT, 0))
                throw new Exception("Stdout SetHandleInformation");

            // Create a pipe for the child process's STDERR. 
            if (!Win32NativeMethods.CreatePipe(out _hChildStd_ERR_Rd, out _hChildStd_ERR_Wr, ref _securityAttributes, 0))
                throw new Exception("StderrRd CreatePipe");

            // Ensure the read handle to the pipe for STDERR is not inherited.
            if (!Win32NativeMethods.SetHandleInformation(_hChildStd_ERR_Rd, Win32NativeMethods.HANDLE_FLAG_INHERIT, 0))
                throw new Exception("Stderr SetHandleInformation");

            // Create a pipe for the child process's STDIN. 
            if (!Win32NativeMethods.CreatePipe(out _hChildStd_IN_Rd, out _hChildStd_IN_Wr, ref _securityAttributes, 0))
                throw new Exception("Stdin CreatePipe");

            // Ensure the write handle to the pipe for STDIN is not inherited. 
            if (!Win32NativeMethods.SetHandleInformation(_hChildStd_IN_Wr, Win32NativeMethods.HANDLE_FLAG_INHERIT, 0))
                throw new Exception("Stdin SetHandleInformation");
        }

        private int? Start(bool useCurrentWorkingDirectory, out long stdinWrittenByte, Stream stdin = null, Action<byte[]> stdout = null, Action<byte[]> stderr = null)
        {
            IntPtr hProcess = CreateWslProcess(useCurrentWorkingDirectory);
            stdinWrittenByte = 0L;

            if (stdin != null)
                stdinWrittenByte = WriteToStdinPipe(stdin);

            ReadFromStdoutPipe(stdout);
            ReadFromStderrPipe(stderr);

            int? result = null;
            if (Win32NativeMethods.GetExitCodeProcess(hProcess, out int exitCode))
                result = exitCode;

            Win32NativeMethods.CloseHandle(hProcess);
            return result;
        }

        private bool _disposed;

        private int _bufferSize;
        private IntPtr _hChildStd_IN_Rd;
        private IntPtr _hChildStd_IN_Wr;
        private IntPtr _hChildStd_OUT_Rd;
        private IntPtr _hChildStd_OUT_Wr;
        private IntPtr _hChildStd_ERR_Rd;
        private IntPtr _hChildStd_ERR_Wr;
        private Win32NativeMethods.SECURITY_ATTRIBUTES _securityAttributes;

        private readonly string _distroName;
        private readonly string _command;

        // Create a child process that uses the previously created pipes for STDIN and STDOUT.
        private IntPtr CreateWslProcess(bool useCurrentWorkingDirectory)
        {
            int hr = WslNativeMethods.Api.WslLaunch(_distroName, _command, useCurrentWorkingDirectory,
                stdIn: _hChildStd_IN_Rd,
                stdOut: _hChildStd_OUT_Wr,
                stdErr: _hChildStd_ERR_Wr,
                out IntPtr hProcess);

            if (hr != 0)
                throw new COMException("Unexpected error occurred.", hr);

            Win32NativeMethods.CloseHandle(_hChildStd_OUT_Wr);
            _hChildStd_OUT_Wr = IntPtr.Zero;

            Win32NativeMethods.CloseHandle(_hChildStd_ERR_Wr);
            _hChildStd_ERR_Wr = IntPtr.Zero;

            Win32NativeMethods.CloseHandle(_hChildStd_IN_Rd);
            _hChildStd_IN_Rd = IntPtr.Zero;

            return hProcess;
        }

        // Read from a file and write its contents to the pipe for the child's STDIN. Stop when there is no more data. 
        private long WriteToStdinPipe(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (!stream.CanRead)
                throw new ArgumentException("Selected stream does not support read function.", nameof(stream));

            long lTotal = 0L;
            int dwRead, dwWritten;
            bool bSuccess;
            IntPtr chBuf = IntPtr.Zero;

            try
            {
                chBuf = Marshal.AllocHGlobal(_bufferSize);

                for (; ; )
                {
                    byte[] buffer = new byte[_bufferSize];
                    dwRead = stream.Read(buffer, 0, buffer.Length);
                    if (dwRead == 0) break;

                    Marshal.Copy(buffer, 0, chBuf, dwRead);
                    bSuccess = Win32NativeMethods.WriteFile(_hChildStd_IN_Wr, chBuf, dwRead, out dwWritten, IntPtr.Zero);
                    Interlocked.Add(ref lTotal, dwWritten);
                    if (!bSuccess) break;
                }

                return lTotal;
            }
            finally
            {
                if (chBuf != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(chBuf);
                    chBuf = IntPtr.Zero;
                }

                if (!Win32NativeMethods.CloseHandle(_hChildStd_IN_Wr))
                    throw new Exception("StdInWr CloseHandle");

                _hChildStd_IN_Wr = IntPtr.Zero;
            }
        }

        // Read output from the child process's pipe for STDOUT and write to the parent process's pipe for STDOUT. Stop when there is no more data. 
        private void ReadFromStdoutPipe(Action<byte[]> stdoutReceiver)
        {
            int dwRead;
            bool bSuccess;
            IntPtr chBuf = IntPtr.Zero;

            try
            {
                chBuf = Marshal.AllocHGlobal(_bufferSize);

                for (; ; )
                {
                    bSuccess = Win32NativeMethods.ReadFile(_hChildStd_OUT_Rd, chBuf, _bufferSize, out dwRead, IntPtr.Zero);
                    if (!bSuccess || dwRead == 0) break;

                    if (stdoutReceiver != null)
                    {
                        byte[] temp = new byte[dwRead];
                        Marshal.Copy(chBuf, temp, 0, dwRead);
                        stdoutReceiver.Invoke(temp);
                    }
                }
            }
            finally
            {
                if (chBuf != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(chBuf);
                    chBuf = IntPtr.Zero;
                }
            }
        }

        // Read output from the child process's pipe for STDERR and write to the parent process's pipe for STDER. Stop when there is no more data. 
        private void ReadFromStderrPipe(Action<byte[]> stderrReceiver)
        {
            int dwRead;
            bool bSuccess;
            IntPtr chBuf = IntPtr.Zero;

            try
            {
                chBuf = Marshal.AllocHGlobal(_bufferSize);

                for (; ; )
                {
                    bSuccess = Win32NativeMethods.ReadFile(_hChildStd_ERR_Rd, chBuf, _bufferSize, out dwRead, IntPtr.Zero);
                    if (!bSuccess || dwRead == 0) break;

                    if (stderrReceiver != null)
                    {
                        byte[] temp = new byte[dwRead];
                        Marshal.Copy(chBuf, temp, 0, dwRead);
                        stderrReceiver.Invoke(temp);
                    }
                }
            }
            finally
            {
                if (chBuf != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(chBuf);
                    chBuf = IntPtr.Zero;
                }
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects)
                }

                if (_hChildStd_IN_Rd != IntPtr.Zero)
                {
                    Win32NativeMethods.CloseHandle(_hChildStd_IN_Rd);
                    _hChildStd_IN_Rd = IntPtr.Zero;
                }

                if (_hChildStd_IN_Wr != IntPtr.Zero)
                {
                    Win32NativeMethods.CloseHandle(_hChildStd_IN_Wr);
                    _hChildStd_IN_Wr = IntPtr.Zero;
                }

                if (_hChildStd_OUT_Rd != IntPtr.Zero)
                {
                    Win32NativeMethods.CloseHandle(_hChildStd_OUT_Rd);
                    _hChildStd_OUT_Rd = IntPtr.Zero;
                }

                if (_hChildStd_OUT_Wr != IntPtr.Zero)
                {
                    Win32NativeMethods.CloseHandle(_hChildStd_OUT_Wr);
                    _hChildStd_OUT_Wr = IntPtr.Zero;
                }

                if (_hChildStd_ERR_Rd != IntPtr.Zero)
                {
                    Win32NativeMethods.CloseHandle(_hChildStd_ERR_Rd);
                    _hChildStd_ERR_Rd = IntPtr.Zero;
                }

                if (_hChildStd_ERR_Wr != IntPtr.Zero)
                {
                    Win32NativeMethods.CloseHandle(_hChildStd_ERR_Wr);
                    _hChildStd_ERR_Wr = IntPtr.Zero;
                }

                _disposed = true;
            }
        }

        ~WslLauncher()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
