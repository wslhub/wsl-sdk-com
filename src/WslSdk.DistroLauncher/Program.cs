using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using WslSdk.Shared;

namespace WslSdk.DistroLauncher
{
    internal static class Program
    {
        internal const string ARG_CONFIG = "config";
        internal const string ARG_CONFIG_DEFAULT_USER = "--default-user";
        internal const string ARG_INSTALL = "install";
        internal const string ARG_INSTALL_ROOT = "--root";
        internal const string ARG_RUN = "run";
        internal const string ARG_RUN_C = "-c";

        internal static string SuggestedDistroName
        {
            get { return Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location); }
        }

        internal static string DistroBasePath
        {
            get { return Path.Combine($@"\\wsl$\{SuggestedDistroName}\");  }
        }

        internal static bool HasUserManipulationTool
        {
            get
            {
                return File.Exists(Path.Combine(DistroBasePath, @"usr\sbin\adduser")) &&
                    File.Exists(Path.Combine(DistroBasePath, @"usr\sbin\usermod")) &&
                    File.Exists(Path.Combine(DistroBasePath, @"usr\sbin\deluser"));
            }
        }

        internal static bool HasFileRemovalTool
        {
            get { return File.Exists(Path.Combine(DistroBasePath, @"bin\rm")); }
        }

        internal static bool CreateUser(string distroName, string userName)
        {
            // Create the user account
            string commandLine = $"/usr/sbin/adduser --quiet --gecos '' {userName}";
            int hr = WslNativeMethods.Api.WslLaunchInteractive(distroName, commandLine, true, out int exitCode);

            if (hr != 0 || exitCode != 0)
                return false;

            // Add the user account to any relevant groups.
            commandLine = $"/usr/sbin/usermod -aG adm,cdrom,sudo,dip,plugdev {userName}";
            hr = WslNativeMethods.Api.WslLaunchInteractive(distroName, commandLine, true, out exitCode);

            if (hr != 0 || exitCode != 0)
            {
                commandLine = $"/user/sbin/deluser {userName}";
                WslNativeMethods.Api.WslLaunchInteractive(distroName, commandLine, true, out exitCode);
                return false;
            }

            return true;
        }

        internal static int QueryUid(string distroName, string userName)
        {
            string command = $"/usr/bin/id -u {userName}";
            string content = WslLauncher.GetCommandStdoutAsString(WslNativeMethods.Api, distroName, command, false);

            if (!int.TryParse(content, out int uid))
                return (-1); // UID_INVALID

            return uid;
        }

        internal static int InstallDistribution(string distroName, bool createUser)
        {
            // Register the distribution.
            Console.Out.WriteLine($"Installing WSL Distro {distroName}...");
            int hr = WslNativeMethods.Api.WslRegisterDistribution(distroName, "install.tar.gz");
            if (hr != 0)
                return hr;

            // Delete /etc/resolv.conf to allow WSL to generate a version based on Windows networking information.
            if (HasFileRemovalTool)
            {
                hr = WslNativeMethods.Api.WslLaunchInteractive(distroName, "/bin/rm /etc/resolv.conf", true, out int exitCode);
                if (hr != 0)
                    return hr;
            }
            else
            {
                hr = WslNativeMethods.Api.WslLaunchInteractive(distroName, "rm /etc/resolv.conf", true, out int exitCode);
                if (hr != 0)
                    return hr;
            }

            // Create a user account.
            if (createUser && HasUserManipulationTool)
            {
                string newUserName;
                do
                {
                    Console.Out.Write("Type a user name to create: ");
                    newUserName = Console.In.ReadLine();
                }
                while (!CreateUser(distroName, newUserName));

                // Set this user account as the default.
                hr = SetDefaultUser(distroName, newUserName);
                if (hr != 0)
                    return hr;
            }

            return hr;
        }

        internal static int SetDefaultUser(string distroName, string userName)
        {
            int uid = QueryUid(distroName, userName);
            if (uid == (-1)) // UID_INVALID
                return unchecked((int)0x80070057); // E_INVALIDARG

            int hr = WslNativeMethods.Api.WslConfigureDistribution(
                distroName,
                uid,
                /* (WSL_DISTRIBUTION_FLAGS_ENABLE_INTEROP | WSL_DISTRIBUTION_FLAGS_APPEND_NT_PATH | WSL_DISTRIBUTION_FLAGS_ENABLE_DRIVE_MOUNTING) */ 0x7);

            if (hr != 0)
                return hr;

            return hr;
        }

        private static int HRESULT_FROM_WIN32(int x)
            => unchecked((int)(x <= 0 ? x : ((x & 0x0000FFFF) | (7 << 16) | 0x80000000)));

        [STAThread]
        private static void Main(string[] args)
        {
            // Run CoInitializeSecurity first
            int hr = ComNativeMethods.CoInitializeSecurity(
                IntPtr.Zero,
                (-1),
                IntPtr.Zero,
                IntPtr.Zero,
                ComNativeMethods.RpcAuthnLevel.Default,
                ComNativeMethods.RpcImpLevel.Impersonate,
                IntPtr.Zero,
                ComNativeMethods.EoAuthnCap.StaticCloaking,
                IntPtr.Zero);

            if (hr != 0)
            {
                Console.Error.WriteLine($"Cannot complete CoInitializeSecurity. {hr:X8}");
                Environment.Exit(1);
                return;
            }

            string distroName = SuggestedDistroName;

            // Update the title bar of the console window.
            Console.Title = $"WSL Distro {distroName}";

            // Initialize a vector of arguments.
            List<string> arguments = new List<string>(args);

            // Ensure that the Windows Subsystem for Linux optional component is installed.
            int exitCode = 1;
            if (!WslNativeMethods.Api.IsOptionalComponentInstalled())
            {
                Console.Error.WriteLine("WSL does not installed on this system.");
                if (arguments.Count < 1)
                    Console.In.Read();

                Environment.Exit(exitCode);
                return;
            }

            // Install the distribution if it is not already.
            bool installOnly = (arguments.Count > 0 && string.Equals(arguments[0], ARG_INSTALL, StringComparison.Ordinal));
            if (!WslNativeMethods.Api.WslIsDistributionRegistered(distroName))
            {
                // If the '--root' option is specified, do not create a user account.
                bool useRoot = (installOnly && arguments.Count > 1 && string.Equals(arguments[1], ARG_INSTALL_ROOT, StringComparison.Ordinal));
                hr = InstallDistribution(distroName, !useRoot);
                if (hr != 0)
                {
                    if (hr == HRESULT_FROM_WIN32(183)/* ERROR_ALREADY_EXISTS */)
                        Console.Out.WriteLine($"Distro {distroName} already installed.");
                }
                else
                {
                    Console.Out.WriteLine($"Distro {distroName} successfully installed.");
                }

                exitCode = (hr == 0) ? 0 : 1;
            }

            // Parse the command line arguments.
            if (hr == 0 && !installOnly)
            {
                if (arguments.Count < 1)
                {
                    hr = WslNativeMethods.Api.WslLaunchInteractive(
                        distroName, string.Empty, false, out exitCode);

                    // Check exitCode to see if wsl.exe returned that it could not start the Linux process
                    // then prompt users for input so they can view the error message.
                    if (hr == 0 && exitCode == unchecked((int)uint.MaxValue))
                        Console.In.Read();
                }
                else if (string.Equals(arguments[0], ARG_RUN, StringComparison.Ordinal) ||
                    string.Equals(arguments[0], ARG_RUN_C, StringComparison.Ordinal))
                {
                    StringBuilder command = new StringBuilder();
                    for (int i = 1; i < arguments.Count; i++)
                    {
                        command.Append(" ");
                        command.Append(arguments[i]);
                    }

                    hr = WslNativeMethods.Api.WslLaunchInteractive(
                        distroName, command.ToString(), true, out exitCode);
                }
                else if (string.Equals(arguments[0], ARG_CONFIG, StringComparison.Ordinal))
                {
                    hr = unchecked((int)0x80070057); // E_INVALIDARG

                    if (arguments.Count == 3)
                    {
                        if (string.Equals(arguments[1], ARG_CONFIG_DEFAULT_USER, StringComparison.Ordinal))
                            hr = SetDefaultUser(distroName, arguments[2]);
                    }

                    if (hr == 0)
                        exitCode = 0;
                }
                else
                {
                    Console.Out.WriteLine("To Do: Usage Print");
                    Environment.Exit(exitCode);
                    return;
                }
            }

            // If an error was encountered, print an error message.
            if (hr != 0)
            {
                if (hr == HRESULT_FROM_WIN32(414 /*ERROR_LINUX_SUBSYSTEM_NOT_PRESENT*/))
                    Console.Error.WriteLine("WSL does not installed on this system.");
                else
                    Console.Error.WriteLine($"Unexpected error occurred, HRESULT: {hr:X8}");

                if (arguments.Count < 1)
                    Console.In.Read();
            }

            Environment.Exit(hr == 0 ? exitCode : 1);
        }
    }
}
