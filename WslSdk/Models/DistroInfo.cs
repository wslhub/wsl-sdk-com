using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace WslSdk.Models
{
    /// <summary>
    /// A model class that contains information obtained by calling the WSL API in addition to information read from the registry.
    /// </summary>
    [ComVisible(true)]
    [Guid("61C39009-D5E4-46D4-A306-113A5D459803")]
    public sealed class DistroInfo : DistroRegistryInfo
    {
        /// <summary>
        /// Default environment variables set in the distribution.
        /// </summary>
        public List<string> DefaultEnvironmentVariables { get; internal set; } = new List<string>();

        /// <summary>
        /// The UID of the user to use when running the distribution.
        /// </summary>
        public int DefaultUid { get; internal set; }

        /// <summary>
        /// Represents the default settings of the distribution.
        /// </summary>
        public DistroFlags DistroFlags { get; internal set; }

        /// <summary>
        /// Whether the distribution was successfully registered with the WSL.
        /// </summary>
        public bool IsRegistered { get; internal set; }

        /// <summary>
        /// Whether or not it is set as the default distribution.
        /// </summary>
        public bool IsDefaultDistro { get; internal set; }

        /// <summary>
        /// Determine which version of the WSL runtime is configured to use.
        /// </summary>
        public int WslVersion { get; internal set; }

        /// <summary>
        /// Whether the WSL distribution has been set up to allow interaction with Windows applications.
        /// </summary>
        public bool EnableInterop => DistroFlags.HasFlag(DistroFlags.EnableInterop);

        /// <summary>
        /// Whether the Windows file system can be mounted on the WSL distribution.
        /// </summary>
        public bool EnableDriveMounting => DistroFlags.HasFlag(DistroFlags.EnableDriveMouting);

        /// <summary>
        /// Whether to also add the Windows PATH environment variable to the WSL distribution's PATH environment variable.
        /// </summary>
        public bool AppendNtPath => DistroFlags.HasFlag(DistroFlags.AppendNtPath);
    }
}
