using System;
using System.Runtime.InteropServices;

namespace WslSdk.Models
{
    [ComVisible(true)]
    [Guid("7EED5FAD-60E5-4290-92CC-352B09A1BF14")]
    public sealed class NetworkSettings
    {
        public bool? GenerateHosts { get; internal set; }
        public bool? GenerateResolvConf { get; internal set; }
    }
}
