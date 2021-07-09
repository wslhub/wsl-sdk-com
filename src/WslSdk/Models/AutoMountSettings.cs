using System;
using System.Runtime.InteropServices;

namespace WslSdk.Models
{
    [ComVisible(true)]
    [Guid("626115F6-56D8-4E40-994E-D54D86D768B2")]
    public sealed class AutoMountSettings
    {
        public bool? Enabled { get; internal set; }
        public string Root { get; internal set; }
        public string Options { get; internal set; }
        public bool MountFileSystemTab { get; internal set; }
    }
}
