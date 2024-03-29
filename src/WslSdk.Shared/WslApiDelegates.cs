﻿using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

namespace WslSdk.Shared
{
    [return: MarshalAs(UnmanagedType.Bool)]
    [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Unicode)]
    internal delegate bool WslIsDistributionRegisteredDelegate(
        string distributionName);

    [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Unicode)]
    internal delegate int WslGetDistributionConfigurationDelegate(
        string distributionName,
        [Out, MarshalAs(UnmanagedType.I4)] out int distributionVersion,
        [Out, MarshalAs(UnmanagedType.I4)] out int defaultUID,
        [Out, MarshalAs(UnmanagedType.I4)] out int wslDistributionFlags,
        out IntPtr defaultEnvironmentVariables,
        [MarshalAs(UnmanagedType.I4)] out int defaultEnvironmentVariableCount);

    [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Unicode)]
    internal delegate int WslLaunchDelegate(
        string distributionName,
        string command,
        [MarshalAs(UnmanagedType.Bool)] bool useCurrentWorkingDirectory,
        SafeFileHandle stdIn,
        SafeFileHandle stdOut,
        SafeFileHandle stdErr,
        out IntPtr process);

    [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Unicode)]
    internal delegate int WslLaunchInteractiveDelegate(
        string distributionName,
        string command,
        [MarshalAs(UnmanagedType.Bool)] bool useCurrentWorkingDirectory,
        [Out, MarshalAs(UnmanagedType.U4)] out int exitCode);

    [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Unicode)]
    internal delegate int WslConfigureDistributionDelegate(
        string distributionName,
        [MarshalAs(UnmanagedType.I4)] int defaultUID,
        [MarshalAs(UnmanagedType.I4)] int wslDistributionFlags);

    [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Unicode)]
    internal delegate int WslRegisterDistributionDelegate(
        string distributionName,
        string tarGzFilename);

    [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Unicode)]
    internal delegate int WslUnregisterDistributionDelegate(
        string distributionName);
}
