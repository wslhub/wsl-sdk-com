using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace WslLaunchTest
{
    internal static class Program
    {
        [SecurityCritical]
        [DllImport("ole32.dll",
            ExactSpelling = true,
            CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.U4)]
        public static extern int CoInitializeSecurity(
            IntPtr pSecDesc,
            int cAuthSvc,
            IntPtr asAuthSvc,
            IntPtr pReserved1,
            [MarshalAs(UnmanagedType.U4)] RpcAuthnLevel dwAuthnLevel,
            [MarshalAs(UnmanagedType.U4)] RpcImpLevel dwImpLevel,
            IntPtr pAuthList,
            [MarshalAs(UnmanagedType.U4)] EoAuthnCap dwCapabilities,
            IntPtr pReserved3);

        public enum RpcAuthnLevel
        {
            Default = 0,
            None = 1,
            Connect = 2,
            Call = 3,
            Pkt = 4,
            PktIntegrity = 5,
            PktPrivacy = 6
        }

        public enum RpcImpLevel
        {
            Default = 0,
            Anonymous = 1,
            Identify = 2,
            Impersonate = 3,
            Delegate = 4
        }

        public enum EoAuthnCap
        {
            None = 0x00,
            MutualAuth = 0x01,
            StaticCloaking = 0x20,
            DynamicCloaking = 0x40,
            AnyAuthority = 0x80,
            MakeFullSIC = 0x100,
            Default = 0x800,
            SecureRefs = 0x02,
            AccessControl = 0x04,
            AppID = 0x08,
            Dynamic = 0x10,
            RequireFullSIC = 0x200,
            AutoImpersonate = 0x400,
            NoCustomMarshal = 0x2000,
            DisableAAA = 0x1000
        }

        [SecurityCritical]
        [DllImport("wslapi.dll",
            ExactSpelling = true,
            CharSet = CharSet.Unicode,
            CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.U4)]
        internal static extern int WslLaunch(
            string distributionName,
            string command,
            [MarshalAs(UnmanagedType.Bool)] bool useCurrentWorkingDirectory,
            IntPtr stdIn,
            IntPtr stdOut,
            IntPtr stdErr,
            out IntPtr process);

        [SecurityCritical]
        [DllImport("kernel32.dll",
            CallingConvention = CallingConvention.Winapi,
            SetLastError = true,
            ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CreatePipe(
            out IntPtr hReadPipe,
            out IntPtr hWritePipe,
            SECURITY_ATTRIBUTES lpPipeAttributes,
            [MarshalAs(UnmanagedType.U4)] int nSize);

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

        [SecurityCritical]
        [DllImport("kernel32.dll",
            CallingConvention = CallingConvention.Winapi,
            SetLastError = false,
            ExactSpelling = true)]
        public static extern void RtlZeroMemory(
            IntPtr Destination,
            [MarshalAs(UnmanagedType.U4)] int Length);

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

        [StructLayout(LayoutKind.Sequential)]
        public class SECURITY_ATTRIBUTES
        {
            [MarshalAs(UnmanagedType.U4)]
            public int nLength;

            public IntPtr lpSecurityDescriptor;

            [MarshalAs(UnmanagedType.Bool)]
            public bool bInheritHandle;
        }

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

        private static unsafe void Main(string[] args)
        {
            var result = CoInitializeSecurity(
                IntPtr.Zero,
                (-1),
                IntPtr.Zero,
                IntPtr.Zero,
                RpcAuthnLevel.Default,
                RpcImpLevel.Impersonate,
                IntPtr.Zero,
                EoAuthnCap.StaticCloaking,
                IntPtr.Zero);

            if (result != 0)
                throw new COMException("Cannot complete CoInitializeSecurity.", result);

            // Test Arguments
            int bufferLength = 1024;
            string distroName = "Ubuntu-20.04";
            string commandLine = "curl https://www.google.com";

            var attributes = new SECURITY_ATTRIBUTES
            {
                lpSecurityDescriptor = IntPtr.Zero,
                bInheritHandle = true,
            };
            attributes.nLength = Marshal.SizeOf(attributes);

            IntPtr
                bufferPointer = IntPtr.Zero,
                childStdoutReadPipe = IntPtr.Zero,
                childStdoutWritePipe = IntPtr.Zero,
                child = IntPtr.Zero;

            var stdin = GetStdHandle(STD_INPUT_HANDLE);
            //var stdout = GetStdHandle(STD_OUTPUT_HANDLE);
            var stderr = GetStdHandle(STD_ERROR_HANDLE);

            Action<ArraySegment<byte>> dataReceivedCallback = OnDataReceived;

            try
            {
                if (!CreatePipe(out childStdoutReadPipe, out childStdoutWritePipe, attributes, bufferLength))
                    throw new Exception("Cannot create pipe for I/O.");

                bufferPointer = Marshal.AllocHGlobal(bufferLength);
                var pBufferPointer = (byte*)bufferPointer.ToPointer();

                var hr = WslLaunch(distroName, commandLine, true, stdin, childStdoutWritePipe, stderr, out child);

                if (hr < 0)
                    throw new COMException("Cannot launch WSL process", hr);

                for (int i = 0; i < 10; i++)
                {
                    var waitResult = WaitForSingleObject(child, 1000);

                    if (waitResult != WAIT_TIMEOUT)
                        break;
                }

                if (!GetExitCodeProcess(child, out int exitCode))
                {
                    var lastError = Marshal.GetLastWin32Error();
                    throw new Win32Exception(lastError, "Cannot query exit code of the process.");
                }

                if (exitCode == 259) // STILL_ACTIVE
                    throw new TimeoutException($"Process still running.");

                if (exitCode != 0)
                    throw new Exception($"Process exit code is non-zero: {exitCode}");

                var buffer = new byte[bufferLength];
                var read = 0;

                while (true)
                {
                    RtlZeroMemory(bufferPointer, bufferLength);

                    // Peek pipe data length

                    if (!ReadFile(childStdoutReadPipe, bufferPointer, bufferLength - 1, out read, IntPtr.Zero))
                    {
                        var lastError = Marshal.GetLastWin32Error();

                        if (lastError != 0)
                            throw new Win32Exception(lastError, "Cannot read data from pipe.");

                        break;
                    }

                    fixed (byte* pBuffer = buffer)
                    {
                        Buffer.MemoryCopy(pBufferPointer, pBuffer, read, read);
                    }
                    dataReceivedCallback.Invoke(new ArraySegment<byte>(buffer, 0, read));

                    if (read < bufferLength - 1)
                        break;
                }
            }
            finally
            {
                if (bufferPointer != IntPtr.Zero)
                    Marshal.FreeHGlobal(bufferPointer);

                if (childStdoutWritePipe != IntPtr.Zero)
                    CloseHandle(childStdoutWritePipe);

                if (childStdoutReadPipe != IntPtr.Zero)
                    CloseHandle(childStdoutReadPipe);

                if (child != IntPtr.Zero)
                    CloseHandle(child);
            }
        }

        private static void OnDataReceived(ArraySegment<byte> obj)
        {
            var enc = new UTF8Encoding(false);
            var str = enc.GetString(obj.Array, obj.Offset, obj.Count);
            Console.Write(str);
        }
    }
}
