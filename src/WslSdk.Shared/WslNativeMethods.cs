using System;

namespace WslSdk.Shared
{
    internal static class WslNativeMethods
    {
        private static readonly Lazy<WslApiLoader> _loader =
            new Lazy<WslApiLoader>(false);

        public static WslApiLoader Api
            => _loader.Value;
    }
}
