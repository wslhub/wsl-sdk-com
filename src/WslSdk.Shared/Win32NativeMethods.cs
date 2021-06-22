using System;
using System.Runtime.InteropServices;
using System.Security;

namespace WslSdk.Shared
{
    internal static class Win32NativeMethods
    {
        [SecurityCritical]
        [DllImport("kernel32.dll",
            CallingConvention = CallingConvention.Winapi,
            SetLastError = true,
            ExactSpelling = true)]
        public static extern IntPtr GetStdHandle(
            [MarshalAs(UnmanagedType.U4)] int nStdHandle);

        [SecurityCritical]
        [DllImport("kernel32.dll",
            CallingConvention = CallingConvention.Winapi,
            SetLastError = true,
            ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CreatePipe(
            out IntPtr hReadPipe,
            out IntPtr hWritePipe,
            ref SECURITY_ATTRIBUTES lpPipeAttributes,
            [MarshalAs(UnmanagedType.U4)] int nSize);

        [SecurityCritical]
        [DllImport("kernel32.dll",
            CallingConvention = CallingConvention.Winapi,
            SetLastError = true,
            ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReadFile(
            IntPtr hFile,
            IntPtr lpBuffer,
            [MarshalAs(UnmanagedType.U4)] int nNumberOfBytesToRead,
            [MarshalAs(UnmanagedType.U4)] out int lpNumberOfBytesRead,
            IntPtr lpOverlapped);

        [SecurityCritical]
        [DllImport("kernel32.dll",
            CallingConvention = CallingConvention.Winapi,
            SetLastError = true,
            ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.U4)]
        public static extern int WaitForSingleObject(
            IntPtr hHandle,
            [MarshalAs(UnmanagedType.U4)] int dwMilliseconds);

        [SecurityCritical]
        [DllImport("kernel32.dll",
            CallingConvention = CallingConvention.Winapi,
            SetLastError = true,
            ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetExitCodeProcess(
            IntPtr hProcess,
            [MarshalAs(UnmanagedType.U4)] out int lpExitCode);

        [SecurityCritical]
        [DllImport("kernel32.dll",
            CallingConvention = CallingConvention.Winapi,
            SetLastError = true,
            ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        public static readonly int
            STD_INPUT_HANDLE = -10,
            STD_OUTPUT_HANDLE = -11,
            STD_ERROR_HANDLE = -12;

        public static readonly int
            INFINITE = unchecked((int)0xFFFFFFFF);

        public static readonly int
            WAIT_ABANDONED = 0x00000080,
            WAIT_OBJECT_0 = 0x00000000,
            WAIT_TIMEOUT = 0x00000102,
            WAIT_FAILED = unchecked((int)0xFFFFFFFF);

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES
        {
            [MarshalAs(UnmanagedType.U4)]
            public int nLength;

            public IntPtr lpSecurityDescriptor;

            [MarshalAs(UnmanagedType.Bool)]
            public bool bInheritHandle;
        }
    }
}
