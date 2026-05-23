// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Assets
{
    /// <summary>
    /// Resolves on-disk locations for UO asset files relative to a configured
    /// install <see cref="BasePath"/>, honoring any user-supplied filename
    /// overrides. Also exposes whether the install is a modern UOP package
    /// (vs. a legacy MUL package), since several loaders branch on that flag.
    /// </summary>
    internal interface IUOFilePathResolver
    {
        /// <summary>Root directory of the UO install.</summary>
        string BasePath { get; }

        /// <summary>True when the install ships MainMisc.uop (modern UOP layout).</summary>
        bool IsUOPInstallation { get; }

        /// <summary>
        /// Resolves the full path for an asset filename, applying the override
        /// table first and then falling back to case-insensitive lookup on
        /// non-Windows platforms when the literal name does not exist.
        /// </summary>
        string GetUOFilePath(string file);
    }
}
