using System;
using System.Runtime.InteropServices;

namespace WslSdk.Shared
{
    internal sealed class WslApiLoader : IDisposable
    {
        [DllImport("kernel32.dll", EntryPoint = "LoadLibraryW", ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string librayName);

        [DllImport("kernel32.dll", EntryPoint = "GetProcAddress", ExactSpelling = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr GetProcAddress(IntPtr libraryHandle, string procedureName);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", EntryPoint = "FreeLibrary", ExactSpelling = true)]
        private static extern bool FreeLibrary(IntPtr libraryHandle);

        public WslApiLoader()
            : base()
        {
            wslModuleHandle = LoadLibrary("wslapi.dll");

            if (wslModuleHandle == IntPtr.Zero)
                throw new NotSupportedException("Cannot load wslapi.dll module from system.");

            IntPtr tempHandle;

            tempHandle = GetProcAddress(wslModuleHandle, "WslIsDistributionRegistered");
            wslIsDistributionRegistered = (WslIsDistributionRegisteredDelegate)Marshal.GetDelegateForFunctionPointer(
                tempHandle, typeof(WslIsDistributionRegisteredDelegate));

            tempHandle = GetProcAddress(wslModuleHandle, "WslGetDistributionConfiguration");
            wslGetDistributionConfiguration = (WslGetDistributionConfigurationDelegate)Marshal.GetDelegateForFunctionPointer(
                tempHandle, typeof(WslGetDistributionConfigurationDelegate));

            tempHandle = GetProcAddress(wslModuleHandle, "WslLaunch");
            wslLaunch = (WslLaunchDelegate)Marshal.GetDelegateForFunctionPointer(
                tempHandle, typeof(WslLaunchDelegate));

            tempHandle = GetProcAddress(wslModuleHandle, "WslLaunchInteractive");
            wslLaunchInteractive = (WslLaunchInteractiveDelegate)Marshal.GetDelegateForFunctionPointer(
                tempHandle, typeof(WslLaunchInteractiveDelegate));

            tempHandle = GetProcAddress(wslModuleHandle, "WslConfigureDistribution");
            wslConfigureDistribution = (WslConfigureDistributionDelegate)Marshal.GetDelegateForFunctionPointer(
                tempHandle, typeof(WslConfigureDistributionDelegate));

            tempHandle = GetProcAddress(wslModuleHandle, "WslRegisterDistribution");
            wslRegisterDistribution = (WslRegisterDistributionDelegate)Marshal.GetDelegateForFunctionPointer(
                tempHandle, typeof(WslRegisterDistributionDelegate));

            tempHandle = GetProcAddress(wslModuleHandle, "WslUnregisterDistribution");
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
                FreeLibrary(wslModuleHandle);

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
