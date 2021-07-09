﻿using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using WslSdk.Contracts;
using WslSdk.Interop;
using WslSdk.Models;
using WslSdk.Shared;

namespace WslSdk
{
    [ComVisible(true)]
    [ProgId("WslSdk.WslService")]
    [ClassInterface(ClassInterfaceType.None)]
    [Guid("1D0D99B6-AF95-4D3F-B55B-BA17CB2D549B")]
    [ComSourceInterfaces(typeof(IWslServiceEvents))]
    public class WslService : IWslService
    {
        public WslService()
        {
            // Increment the lock count of objects in the COM server.
            SdkApplication.Instance.Lock();
        }

        ~WslService()
        {
            // Decrement the lock count of objects in the COM server.
            SdkApplication.Instance.Unlock();
        }

        public bool IsDistroRegistered(string distroName)
        {
            return WslNativeMethods.Api.WslIsDistributionRegistered(distroName);
        }

        public DistroRegistryInfo GetDefaultDistro()
        {
            return WslDistroManipulation.GetDefaultDistroFromRegistry();
        }

        public string[] GetDistroList()
        {
            return WslDistroManipulation.EnumerateDistroFromRegistry().Select(x => x.DistroName).ToArray();
        }

        public string RunWslCommand(string distroName, string commandLine)
        {
            var buffer = new StringBuilder();

            using (var writer = new StringWriter(buffer))
            {
                WslLauncher.RunCommandAsString(
                    WslNativeMethods.Api,
                    distroName, commandLine, false,
                    stdout: data => {
                        StdoutDataReceived?.Invoke(data);
                        writer.Write(data);
                    },
                    stderr: data => StderrDataReceived?.Invoke(data));
                writer.Flush();
            }

            return buffer.ToString();
        }

        public string RunWslCommandWithInput(string distroName, string commandLine, string inputFilePath)
        {
            Stream inputStream = null;
            var buffer = new StringBuilder();

            if (inputFilePath != null && File.Exists(inputFilePath))
                inputStream = File.OpenRead(inputFilePath);

            using (inputStream)
            using (var writer = new StringWriter(buffer))
            {
                WslLauncher.RunCommandAsString(
                    WslNativeMethods.Api,
                    distroName, commandLine, false,
                    stdin: inputStream,
                    stdout: data => {
                        StdoutDataReceived?.Invoke(data);
                        writer.Write(data);
                    },
                    stderr: data => StderrDataReceived?.Invoke(data));
                writer.Flush();
            }

            return buffer.ToString();
        }

        public DistroRegistryInfo GetDistroInfo(string distroName)
        {
            return WslDistroManipulation.GetDistroFromRegistry(distroName);
        }

        public string GetDefaultDistroName()
        {
            return WslDistroManipulation.GetDefaultDistroFromRegistry()?.DistroName;
        }

        public DistroInfo QueryDistroInfo(string distroName)
        {
            return WslDistroManipulation.QueryDistro(distroName);
        }

        public void SetDefaultUid(string distroName, int defaultUid)
        {
            WslDistroManipulation.SetDistroConfiguration(distroName, defaultUid, null);
        }

        public void SetDistroFlags(string distroName, DistroFlags distroFlags)
        {
            WslDistroManipulation.SetDistroConfiguration(distroName, null, distroFlags);
        }

        public string GenerateRandomName(bool addNumberPostfix)
        {
            return NamesGenerator.GetRandomName(addNumberPostfix ? 1 : 0);
        }

        public void RegisterDistro(string newDistroName, string tarGzipFilePath, string targetDirectoryPath)
        {
            WslDistroManipulation.RegisterDistro(newDistroName, tarGzipFilePath, targetDirectoryPath);
        }

        public void UnregisterDistro(string existingDistroName)
        {
            WslDistroManipulation.UnregisterDistro(existingDistroName);
        }

        public string GetWslWindowsPath(string distroName)
        {
            var targetDirectoryPath = Path.Combine("\\\\wsl$", distroName);

            if (!Directory.Exists(targetDirectoryPath))
                return null;

            return targetDirectoryPath;
        }

        [ComVisible(false)]
        public delegate void StdoutDataReceivedDelegate(string data);

        public event StdoutDataReceivedDelegate StdoutDataReceived;

        [ComVisible(false)]
        public delegate void StderrDataReceivedDelegate(string data);

        public event StderrDataReceivedDelegate StderrDataReceived;

        // These routines perform the additional COM registration needed by 
        // the service.

        [ComRegisterFunction]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Register(Type t)
        {
            try
            {
                HelperMethods.RegasmRegisterLocalServer(t);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message); // Log the error
                throw ex; // Re-throw the exception
            }
        }

        [ComUnregisterFunction]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Unregister(Type t)
        {
            try
            {
                HelperMethods.RegasmUnregisterLocalServer(t);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message); // Log the error
                throw ex; // Re-throw the exception
            }
        }
    }
}
