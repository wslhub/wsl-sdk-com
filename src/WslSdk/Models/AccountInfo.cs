using System;
using System.Runtime.InteropServices;

namespace WslSdk.Models
{
    [ComVisible(true)]
    [Guid("498CEFF9-0B98-4AD4-9FE9-68F6A0160A0F")]
    public sealed class AccountInfo
    {
        public string RawData { get; internal set; }

        public string Username { get; internal set; }
        public string Password { get; internal set; }
        public int UserId { get; internal set; }
        public int GroupId { get; internal set; }
        public string UserIdInfo { get; internal set; }
        public string HomeDirectoryPath { get; internal set; }
        public string AssignedShellPath { get; internal set; }

        public string UserFullName { get; internal set; }
        public string ContactInfo { get; internal set; }
        public string OfficePhoneNo { get; internal set; }
        public string HomePhoneNo { get; internal set; }
        public string[] MiscInfo { get; internal set; }

        public bool IsSuperUser => UserId == 0;

        public override string ToString() => RawData;
    }
}
