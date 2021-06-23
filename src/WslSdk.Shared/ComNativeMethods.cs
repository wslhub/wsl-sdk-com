using System;
using System.Runtime.InteropServices;
using System.Security;

namespace WslSdk.Shared
{
    internal static class ComNativeMethods
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
    }
}
