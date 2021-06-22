using System.IO;
using System.Reflection;
using WslSdk.Shared;

namespace WslSdk.DistroLauncher
{
    public static class DistributionInfo
    {
        public static string SuggestedDistroName
        {
            get { return Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location); }
        }

        public static bool CreateUser(string userName)
        {
            // Create the user account
            int exitCode;
            string commandLine = $"/usr/sbin/adduser --quiet --gecos '' {userName}";
            int hr = WslNativeMethods.Api.WslLaunchInteractive(SuggestedDistroName, commandLine, true, out exitCode);

            if (hr != 0 || exitCode != 0)
                return false;

            // Add the user account to any relevant groups.
            commandLine = $"/usr/sbin/usermod -aG adm,cdrom,sudo,dip,plugdev {userName}";
            hr = WslNativeMethods.Api.WslLaunchInteractive(SuggestedDistroName, commandLine, true, out exitCode);

            if (hr != 0 || exitCode != 0)
            {
                commandLine = $"/user/sbin/deluser {userName}";
                WslNativeMethods.Api.WslLaunchInteractive(SuggestedDistroName, commandLine, true, out exitCode);
                return false;
            }

            return true;
        }

        public static int QueryUid(string userName)
        {
            string command = $"/usr/bin/id -u {userName}";
            string content = WslInteraction.RunWslCommand(SuggestedDistroName, command);

            if (!int.TryParse(content, out int uid))
                return (-1); // UID_INVALID

            return uid;
        }
    }
}
