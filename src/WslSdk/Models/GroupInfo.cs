using System;
using System.Runtime.InteropServices;

namespace WslSdk.Models
{
    [ComVisible(true)]
    [Guid("DA093675-A5F7-4C1E-94F3-DD2BD958723A")]
    public sealed class GroupInfo
    {
        public string RawData { get; internal set; }

        public string GroupName { get; internal set; }
        public string Password { get; internal set; }
        public int GroupId { get; internal set; }
        public string GroupUserList { get; internal set; }

        public string[] GroupUserNames { get; internal set; }

        public override string ToString() => RawData;
    }
}
