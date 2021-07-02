using System;
using System.IO;

namespace WslSdk
{
    internal static class Helpers
    {
        public static string FindExistingPath(string distroName, params string[] unixPathCandidates)
        {
            var basePath = Path.Combine($@"\\wsl$\{distroName}");

            if (!Directory.Exists(basePath))
                return null;

            var baseUri = new Uri(basePath.Replace("wsl$", "wsl.localhost"), UriKind.Absolute);

            foreach (var eachUnixPathCandidate in unixPathCandidates)
            {
                if (!Uri.TryCreate(eachUnixPathCandidate, UriKind.Relative, out Uri unixPath))
                    continue;

                var combinedPath = new Uri(baseUri, unixPath);
                var eachFullPath = combinedPath.LocalPath.Replace("wsl.localhost", "wsl$");

                if (Directory.Exists(eachFullPath) || File.Exists(eachFullPath))
                    return eachUnixPathCandidate;
            }

            return null;
        }
    }
}
