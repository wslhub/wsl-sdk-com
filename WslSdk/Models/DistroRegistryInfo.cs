using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace WslSdk.Models
{
    /// <summary>
    /// A model class that represents information about the WSL distribution registered in the registry.
    /// </summary>
    [ComVisible(true)]
    [Guid("5F695373-2BAB-4B0C-A53D-2AE6F5024E44")]
    public class DistroRegistryInfo
    {
        /// <summary>
        /// Unique ID identifying the WSL distribution
        /// </summary>
        public string DistroId { get; internal set; }

        /// <summary>
        /// Name of the WSL distribution
        /// </summary>
        public string DistroName { get; internal set; }

        /// <summary>
        /// List of kernel parameters to be passed on cold boot
        /// </summary>
        public string[] KernelCommandLine { get; internal set; } = new string[] { };

        /// <summary>
        /// The path to the local directory where the WSL distribution is installed.
        /// </summary>
        public string BasePath { get; internal set; }

        /// <summary>
        /// Whether or not registered as the default WSL distribution
        /// </summary>
        public bool IsDefault { get; internal set; }

        /// <summary>
        /// Returns a description of this model object.
        /// </summary>
        /// <returns>Returns a description of this model object.</returns>
        public override string ToString() => $"{DistroName} [{DistroId}]";
    }
}
