using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
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
            CharSet = CharSet.Ansi,
            ExactSpelling = true)]
        public static extern IntPtr CreateNamedPipeA(
            string lpName,
            [MarshalAs(UnmanagedType.U4)] int dwOpenMode,
            [MarshalAs(UnmanagedType.U4)] int dwPipeMode,
            [MarshalAs(UnmanagedType.U4)] int nMaxInstances,
            [MarshalAs(UnmanagedType.U4)] int nOutBufferSize,
            [MarshalAs(UnmanagedType.U4)] int nInBufferSize,
            [MarshalAs(UnmanagedType.U4)] int nDefaultTimeOut,
            SECURITY_ATTRIBUTES lpSecurityAttributes);

        [SecurityCritical]
        [DllImport("kernel32.dll",
            CallingConvention = CallingConvention.Winapi,
            SetLastError = true,
            CharSet = CharSet.Ansi,
            ExactSpelling = true)]
        public static extern IntPtr CreateFileA(
            string lpFileName,
            [MarshalAs(UnmanagedType.U4)] int dwDesiredAccess,
            [MarshalAs(UnmanagedType.U4)] int dwShareMode,
            SECURITY_ATTRIBUTES lpSecurityAttributes,
            [MarshalAs(UnmanagedType.U4)] int dwCreationDisposition,
            [MarshalAs(UnmanagedType.U4)] int dwFlagsAndAttributes,
            IntPtr hTemplateFile);

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

        public static readonly int
            FILE_FLAG_FIRST_PIPE_INSTANCE = 0x00080000,
            FILE_FLAG_WRITE_THROUGH = unchecked((int)0x80000000u),
            FILE_FLAG_OVERLAPPED = 0x40000000;

        public static readonly int
            PIPE_ACCESS_INBOUND = 0x00000001,
            PIPE_ACCESS_OUTBOUND = 0x00000002,
            PIPE_ACCESS_DUPLEX = 0x00000003;

        public static readonly int
            WRITE_DAC = 0x00040000,
            WRITE_OWNER = 0x00080000,
            ACCESS_SYSTEM_SECURITY = 0x01000000;

        public static readonly int
            PIPE_TYPE_BYTE = 0x00000000,
            PIPE_TYPE_MESSAGE = 0x00000004;

        public static readonly int
            PIPE_READMODE_BYTE = 0x00000000,
            PIPE_READMODE_MESSAGE = 0x00000002;

        public static readonly int
            PIPE_WAIT = 0x00000000,
            PIPE_NOWAIT = 0x00000001;

        public static readonly int
            PIPE_ACCEPT_REMOTE_CLIENTS = 0x00000000,
            PIPE_REJECT_REMOTE_CLIENTS = 0x00000008;

        public static readonly int
            PIPE_UNLIMITED_INSTANCES = 255;

        public static readonly int
            GENERIC_READ = unchecked((int)0x80000000),
            GENERIC_WRITE = 0x40000000,
            GENERIC_EXECUTE = 0x20000000,
            GENERIC_ALL = 0x10000000;

        public static readonly int
            CREATE_NEW = 1,
            CREATE_ALWAYS = 2,
            OPEN_EXISTING = 3,
            OPEN_ALWAYS = 4,
            TRUNCATE_EXISTING = 5;

        public static readonly int
            FILE_ATTRIBUTE_READONLY = 1,
            FILE_ATTRIBUTE_HIDDEN = 2,
            FILE_ATTRIBUTE_SYSTEM = 4,
            FILE_ATTRIBUTE_ARCHIVE = 32,
            FILE_ATTRIBUTE_NORMAL = 128,
            FILE_ATTRIBUTE_TEMPORARY = 256,
            FILE_ATTRIBUTE_OFFLINE = 4096,
            FILE_ATTRIBUTE_ENCRYPTED = 16384;

        public static readonly int
            FILE_FLAG_BACKUP_SEMANTICS = 0x02000000,
            FILE_FLAG_DELETE_ON_CLOSE = 0x04000000,
            FILE_FLAG_NO_BUFFERING = 0x20000000,
            FILE_FLAG_OPEN_NO_RECALL = 0x00100000,
            FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000,
            FILE_FLAG_POSIX_SEMANTICS = 0x01000000,
            FILE_FLAG_RANDOM_ACCESS = 0x10000000,
            FILE_FLAG_SESSION_AWARE = 0x00800000,
            FILE_FLAG_SEQUENTIAL_SCAN = 0x08000000;

        public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        public static volatile int PipeSerialNumber = 0;

        /*++
        Excerpted From: https://stackoverflow.com/questions/60645/overlapped-i-o-on-anonymous-pipe

        Routine Description:
            The CreatePipeEx API is used to create an anonymous pipe I/O device.
            Unlike CreatePipe FILE_FLAG_OVERLAPPED may be specified for one or
            both handles.
            Two handles to the device are created.  One handle is opened for
            reading and the other is opened for writing.  These handles may be
            used in subsequent calls to ReadFile and WriteFile to transmit data
            through the pipe.
        Arguments:
            lpReadPipe - Returns a handle to the read side of the pipe.  Data
                may be read from the pipe by specifying this handle value in a
                subsequent call to ReadFile.
            lpWritePipe - Returns a handle to the write side of the pipe.  Data
                may be written to the pipe by specifying this handle value in a
                subsequent call to WriteFile.
            lpPipeAttributes - An optional parameter that may be used to specify
                the attributes of the new pipe.  If the parameter is not
                specified, then the pipe is created without a security
                descriptor, and the resulting handles are not inherited on
                process creation.  Otherwise, the optional security attributes
                are used on the pipe, and the inherit handles flag effects both
                pipe handles.
            nSize - Supplies the requested buffer size for the pipe.  This is
                only a suggestion and is used by the operating system to
                calculate an appropriate buffering mechanism.  A value of zero
                indicates that the system is to choose the default buffering
                scheme.
        Return Value:
            TRUE - The operation was successful.
            FALSE/NULL - The operation failed. Extended error status is available
                using GetLastError.
        --*/
        private static bool CreatePipeEx(
            [Out] out IntPtr readPipeHandle,
            [Out] out IntPtr writePipeHandle,
            [In] SECURITY_ATTRIBUTES pipeAttributes,
            [In] int size,
            int readModeFlag, int writeModeFlag)
        {
            readPipeHandle = IntPtr.Zero;
            writePipeHandle = IntPtr.Zero;

            IntPtr readPipeHandleTemp, writePipeHandleTemp;
            string pipeName;

            //
            // Only one valid OpenMode flag - FILE_FLAG_OVERLAPPED
            //

            if (((readModeFlag | writeModeFlag) & (~FILE_FLAG_OVERLAPPED)) != 0)
                throw new ArgumentException("Only one valid OpenMode flag - FILE_FLAG_OVERLAPPED");

            //
            //  Set the default timeout to 120 seconds
            //

            size = Math.Max(size, 4096);
            pipeName = $"\\\\.\\Pipe\\WslSdk.{(Process.GetCurrentProcess().Id):8x}.{(Interlocked.Increment(ref PipeSerialNumber)):8x}";

            readPipeHandleTemp = CreateNamedPipeA(
                pipeName,
                PIPE_ACCESS_INBOUND | readModeFlag,
                PIPE_TYPE_BYTE | PIPE_WAIT,
                1, /* Number of pipes */
                size, /* Out buffer size */
                size, /* In buffer size */
                120 * 1000, /* Timeout in ms */
                pipeAttributes);

            if (readPipeHandleTemp == IntPtr.Zero)
                return false;

            writePipeHandleTemp = CreateFileA(
                pipeName,
                GENERIC_WRITE,
                0, /* No sharing */
                pipeAttributes,
                OPEN_EXISTING,
                FILE_ATTRIBUTE_NORMAL | writeModeFlag,
                IntPtr.Zero /* Template file */ );

            if (writePipeHandleTemp == INVALID_HANDLE_VALUE)
            {
                CloseHandle(readPipeHandleTemp);
                throw new Win32Exception();
            }

            readPipeHandle = readPipeHandleTemp;
            writePipeHandle = writePipeHandleTemp;
            return true;
        }

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
                if (!CreatePipeEx(out childStdoutReadPipe, out childStdoutWritePipe, attributes, bufferLength, FILE_FLAG_OVERLAPPED, FILE_FLAG_OVERLAPPED))
                    throw new Exception("Cannot create pipe for I/O.");

                bufferPointer = Marshal.AllocHGlobal(bufferLength);
                var pBufferPointer = (byte*)bufferPointer.ToPointer();

                var hr = WslLaunch(distroName, commandLine, true, stdin, childStdoutWritePipe, stderr, out child);

                if (hr < 0)
                    throw new COMException("Cannot launch WSL process", hr);

                if (childStdoutWritePipe != IntPtr.Zero)
                {
                    CloseHandle(childStdoutWritePipe);
                    childStdoutWritePipe = IntPtr.Zero;
                }

                var buffer = new byte[bufferLength];
                var read = 0;

                while (true)
                {
                    RtlZeroMemory(bufferPointer, bufferLength);

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

                Console.Read();
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
