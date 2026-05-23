// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.IO;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using ClassicUO.Utility.Platforms;
using System;
using System.IO;

namespace ClassicUO.Assets
{
    /// <summary>
    /// Concrete <see cref="IUOFilePathResolver"/>. Owns the
    /// <see cref="UOFilesOverrideMap"/> + base directory and probes for
    /// <c>MainMisc.uop</c> to decide the UOP-vs-MUL flag.
    /// </summary>
    internal sealed class UOFilePathResolver : IUOFilePathResolver
    {
        private readonly UOFilesOverrideMap _overrideMap;

        public UOFilePathResolver(ClientVersion version, string basePath, UOFilesOverrideMap overrideMap)
        {
            BasePath = basePath;
            _overrideMap = overrideMap;
            IsUOPInstallation = version >= ClientVersion.CV_7000 && File.Exists(GetUOFilePath("MainMisc.uop"));
        }

        public string BasePath { get; }

        public bool IsUOPInstallation { get; }

        public string GetUOFilePath(string file)
        {
            if (!_overrideMap.TryGetValue(file.ToLowerInvariant(), out string uoFilePath))
            {
                uoFilePath = Path.Combine(BasePath, file);
            }

            //If the file with the given name doesn't exist, check for it with alternative casing if not on windows
            if (!PlatformHelper.IsWindows && !File.Exists(uoFilePath))
            {
                FileInfo finfo = new FileInfo(uoFilePath);
                var dir = Path.GetFullPath(finfo.DirectoryName ?? BasePath);

                if (Directory.Exists(dir))
                {
                    var files = Directory.GetFiles(dir);
                    var matches = 0;

                    foreach (var f in files)
                    {
                        if (string.Equals(f, uoFilePath, StringComparison.OrdinalIgnoreCase))
                        {
                            matches++;
                            uoFilePath = f;
                        }
                    }

                    if (matches > 1)
                    {
                        Log.Warn($"Multiple files with ambiguous case found for {file}, using {Path.GetFileName(uoFilePath)}. Check your data directory for duplicate files.");
                    }
                }
            }

            return uoFilePath;
        }
    }
}
