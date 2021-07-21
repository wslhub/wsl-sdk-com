using Microsoft.Win32.SafeHandles;
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
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CreatePipe(
            out SafeFileHandle hReadPipe,
            out SafeFileHandle hWritePipe,
            [In] ref SECURITY_ATTRIBUTES lpPipeAttributes,
            [MarshalAs(UnmanagedType.U4)] int nSize);

        [SecurityCritical]
        [DllImport("kernel32.dll",
            CallingConvention = CallingConvention.Winapi,
            SetLastError = true,
            ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetHandleInformation(
            SafeHandle hObject,
            [MarshalAs(UnmanagedType.U4)] int dwMask,
            [MarshalAs(UnmanagedType.U4)] int dwFlags);

        [SecurityCritical]
        [DllImport("kernel32.dll",
            CallingConvention = CallingConvention.Winapi,
            SetLastError = true,
            ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReadFile(
            SafeFileHandle hFile,
            IntPtr lpBuffer,
            [MarshalAs(UnmanagedType.U4)] int nNumberOfBytesToRead,
            [MarshalAs(UnmanagedType.U4)] out int lpNumberOfBytesRead,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll",
            CallingConvention = CallingConvention.Winapi,
            EntryPoint = "WriteFile",
            SetLastError = true,
            ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WriteFile(
            SafeFileHandle hFile,
            IntPtr lpBuffer,
            [MarshalAs(UnmanagedType.U4)] int nNumberOfBytesToWrite,
            [MarshalAs(UnmanagedType.U4)] out int lpNumberOfBytesWritten,
            IntPtr lpOverlapped);

        [SecurityCritical]
        [DllImport("kernel32.dll",
            CallingConvention = CallingConvention.Winapi,
            SetLastError = true,
            ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetExitCodeProcess(
            IntPtr hProcess,
            [MarshalAs(UnmanagedType.U4)] out int lpExitCode);

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AllocConsole();

        [DllImport("kernel32.dll", EntryPoint = "LoadLibraryW", ExactSpelling = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr LoadLibrary(string librayName);

        [DllImport("kernel32.dll", EntryPoint = "GetProcAddress", ExactSpelling = true, CharSet = CharSet.Ansi)]
        public static extern IntPtr GetProcAddress(IntPtr libraryHandle, string procedureName);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", EntryPoint = "FreeLibrary", ExactSpelling = true)]
        public static extern bool FreeLibrary(IntPtr libraryHandle);

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        public static extern IntPtr GetConsoleWindow();

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("user32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public static readonly int SW_HIDE = 0;

        public static readonly int HANDLE_FLAG_INHERIT = 0x00000001;

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
