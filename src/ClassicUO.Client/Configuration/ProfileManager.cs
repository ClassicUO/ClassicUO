// SPDX-License-Identifier: BSD-2-Clause

using System.IO;
using System.Linq;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Configuration
{
    internal static class ProfileManager
    {
        private const string MODERN_CHARACTER_SUBDIRECTORY_FORMAT = "0x{0:X}_{1}";
        public static Profile CurrentProfile { get; private set; }
        public static string ProfilePath { get; private set; }

        private static string _rootPath;
        private static string RootPath
        {
            get
            {
                if (string.IsNullOrEmpty(_rootPath))
                {
                    if (string.IsNullOrWhiteSpace(Settings.GlobalSettings.ProfilesPath))
                    {
                        _rootPath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles");
                    }
                    else
                    {
                        _rootPath = Settings.GlobalSettings.ProfilesPath;
                    }
                }

                return _rootPath;
            }
        }

        public static void Load(string servername, string username, string charactername, uint serial)
        {
            string characterPathSegment = SelectCharacterSubfolder(servername, username, charactername, serial);

            string path = FileSystemHelper.CreateFolderIfNotExists(RootPath, username, servername, characterPathSegment);
            string fileToLoad = Path.Combine(path, "profile.json");

            ProfilePath = path;
            CurrentProfile = ConfigurationResolver.Load<Profile>(fileToLoad, ProfileJsonContext.DefaultToUse.Profile) ?? NewFromDefault();

            CurrentProfile.Username = username;
            CurrentProfile.ServerName = servername;
            CurrentProfile.CharacterName = charactername;

            ValidateFields(CurrentProfile);
        }

        private static string SelectCharacterSubfolder(string servername, string username, string charactername, uint serial)
        {
            string modernSubdirectory = FileSystemHelper.ReplaceInvalidPathCharacters(string.Format(MODERN_CHARACTER_SUBDIRECTORY_FORMAT, serial, charactername));

            string characterPathSegment = modernSubdirectory;

            if (Directory.Exists(Path.Combine(RootPath, username, servername)))
            {
                // we have prior data for this user and server
                string baseCharacterPath = Path.Combine(
                    RootPath,
                    FileSystemHelper.ReplaceInvalidPathCharacters(username),
                    FileSystemHelper.ReplaceInvalidPathCharacters(servername)
                    );
                string[] subdirectories = [.. Directory.GetDirectories(baseCharacterPath).Select(s => new DirectoryInfo(s).Name)];


                if (!subdirectories.Contains(modernSubdirectory))
                {
                    string match = subdirectories.FirstOrDefault(s => s.StartsWith(string.Format(MODERN_CHARACTER_SUBDIRECTORY_FORMAT, serial, "")));

                    if (match != null)
                    {
                        // we found a match based on serial but with a different name
                        // the serial takes precedence, since the name could have changed (incognito, morph, etc.)
                        characterPathSegment = match;
                    }
                    else if (subdirectories.Contains(FileSystemHelper.ReplaceInvalidPathCharacters(charactername)))
                    {
                        // we found a (legacy) match based on character name but with missing serial, so we will rename the folder to the modern format
                        // this pins the character to the serial, so we can uniquely identify the profile even if the character name changes
                        Directory.Move(Path.Combine(RootPath, username, servername, FileSystemHelper.ReplaceInvalidPathCharacters(charactername)), Path.Combine(RootPath, username, servername, modernSubdirectory));
                    }
                }
            }

            return characterPathSegment;
        }

        public static void SetProfileAsDefault(Profile profile)
        {
            profile.SaveAs(RootPath, "default.json");
        }

        public static Profile NewFromDefault()
        {
            return ConfigurationResolver.Load<Profile>(Path.Combine(RootPath, "default.json"), ProfileJsonContext.DefaultToUse.Profile) ?? new Profile();
        }

        private static void ValidateFields(Profile profile)
        {
            if (profile == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(profile.ServerName))
            {
                throw new InvalidDataException();
            }

            if (string.IsNullOrEmpty(profile.Username))
            {
                throw new InvalidDataException();
            }

            if (string.IsNullOrEmpty(profile.CharacterName))
            {
                throw new InvalidDataException();
            }

            if (profile.WindowClientBounds.X < 600)
            {
                profile.WindowClientBounds = new Point(600, profile.WindowClientBounds.Y);
            }

            if (profile.WindowClientBounds.Y < 480)
            {
                profile.WindowClientBounds = new Point(profile.WindowClientBounds.X, 480);
            }
        }

        public static void UnLoadProfile()
        {
            CurrentProfile = null;
        }
    }
}
