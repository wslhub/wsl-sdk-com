using System;
using System.Runtime.InteropServices;
using WslSdk.Shared;

namespace WslSdk
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static void Main(string[] args)
        {
            var result = ComNativeMethods.CoInitializeSecurity(
                IntPtr.Zero,
                (-1),
                IntPtr.Zero,
                IntPtr.Zero,
                ComNativeMethods.RpcAuthnLevel.Default,
                ComNativeMethods.RpcImpLevel.Impersonate,
                IntPtr.Zero,
                ComNativeMethods.EoAuthnCap.StaticCloaking,
                IntPtr.Zero);

            if (result != 0)
                throw new COMException("Cannot complete CoInitializeSecurity.", result);

            Console.WriteLine(string.Join(", ", args));

            // Run the out-of-process COM server
            WslComServer.Instance.Run();
        }
    }
}
