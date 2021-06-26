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
        [STAThread]
        private static void Main()
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

            //Win32NativeMethods.AllocConsole();
            //var consoleWindowHandle = Win32NativeMethods.GetConsoleWindow();
            //Win32NativeMethods.ShowWindow(consoleWindowHandle, Win32NativeMethods.SW_HIDE);
            
            // Run the out-of-process COM server
            SdkApplication.Instance.Run();
        }
    }
}
