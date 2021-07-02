using System;
using System.Runtime.InteropServices;

namespace WslSdk.Shared
{
    internal sealed class WslApiLoader : IDisposable
    {
        public WslApiLoader()
            : base()
        {
            wslModuleHandle = Win32NativeMethods.LoadLibrary("wslapi.dll");

            if (wslModuleHandle == IntPtr.Zero)
                throw new NotSupportedException("Cannot load wslapi.dll module from system.");

            IntPtr tempHandle;

            tempHandle = Win32NativeMethods.GetProcAddress(wslModuleHandle, "WslIsDistributionRegistered");
            wslIsDistributionRegistered = (WslIsDistributionRegisteredDelegate)Marshal.GetDelegateForFunctionPointer(
                tempHandle, typeof(WslIsDistributionRegisteredDelegate));

            tempHandle = Win32NativeMethods.GetProcAddress(wslModuleHandle, "WslGetDistributionConfiguration");
            wslGetDistributionConfiguration = (WslGetDistributionConfigurationDelegate)Marshal.GetDelegateForFunctionPointer(
                tempHandle, typeof(WslGetDistributionConfigurationDelegate));

            tempHandle = Win32NativeMethods.GetProcAddress(wslModuleHandle, "WslLaunch");
            wslLaunch = (WslLaunchDelegate)Marshal.GetDelegateForFunctionPointer(
                tempHandle, typeof(WslLaunchDelegate));

            tempHandle = Win32NativeMethods.GetProcAddress(wslModuleHandle, "WslLaunchInteractive");
            wslLaunchInteractive = (WslLaunchInteractiveDelegate)Marshal.GetDelegateForFunctionPointer(
                tempHandle, typeof(WslLaunchInteractiveDelegate));

            tempHandle = Win32NativeMethods.GetProcAddress(wslModuleHandle, "WslConfigureDistribution");
            wslConfigureDistribution = (WslConfigureDistributionDelegate)Marshal.GetDelegateForFunctionPointer(
                tempHandle, typeof(WslConfigureDistributionDelegate));

            tempHandle = Win32NativeMethods.GetProcAddress(wslModuleHandle, "WslRegisterDistribution");
            wslRegisterDistribution = (WslRegisterDistributionDelegate)Marshal.GetDelegateForFunctionPointer(
                tempHandle, typeof(WslRegisterDistributionDelegate));

            tempHandle = Win32NativeMethods.GetProcAddress(wslModuleHandle, "WslUnregisterDistribution");
            wslUnregisterDistribution = (WslUnregisterDistributionDelegate)Marshal.GetDelegateForFunctionPointer(
                tempHandle, typeof(WslUnregisterDistributionDelegate));
        }

        ~WslApiLoader()
        {
            Dispose(false);
        }

        private bool disposed;
        private IntPtr wslModuleHandle;
        private WslIsDistributionRegisteredDelegate wslIsDistributionRegistered;
        private WslGetDistributionConfigurationDelegate wslGetDistributionConfiguration;
        private WslLaunchDelegate wslLaunch;
        private WslLaunchInteractiveDelegate wslLaunchInteractive;
        private WslConfigureDistributionDelegate wslConfigureDistribution;
        private WslRegisterDistributionDelegate wslRegisterDistribution;
        private WslUnregisterDistributionDelegate wslUnregisterDistribution;

        public bool IsOptionalComponentInstalled()
            => ((wslModuleHandle != IntPtr.Zero) &&
            (wslIsDistributionRegistered != null) &&
            (wslGetDistributionConfiguration != null) &&
            (wslLaunch != null) &&
            (wslLaunchInteractive != null) &&
            (wslConfigureDistribution != null) &&
            (wslRegisterDistribution != null) &&
            (wslUnregisterDistribution != null));

        public WslIsDistributionRegisteredDelegate WslIsDistributionRegistered
            => wslIsDistributionRegistered;

        public WslGetDistributionConfigurationDelegate WslGetDistributionConfiguration
            => wslGetDistributionConfiguration;

        public WslLaunchDelegate WslLaunch
            => wslLaunch;

        public WslLaunchInteractiveDelegate WslLaunchInteractive
            => wslLaunchInteractive;

        public WslConfigureDistributionDelegate WslConfigureDistribution
            => wslConfigureDistribution;

        public WslRegisterDistributionDelegate WslRegisterDistribution
            => wslRegisterDistribution;

        public WslUnregisterDistributionDelegate WslUnregisterDistribution
            => wslUnregisterDistribution;

        private void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (wslModuleHandle != IntPtr.Zero)
                Win32NativeMethods.FreeLibrary(wslModuleHandle);

            wslModuleHandle = IntPtr.Zero;
            wslIsDistributionRegistered = null;
            wslGetDistributionConfiguration = null;
            wslLaunch = null;
            wslLaunchInteractive = null;
            wslConfigureDistribution = null;
            wslRegisterDistribution = null;
            wslUnregisterDistribution = null;
            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
