#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Configuration
{
    public static class ProfileManager
    {
        public static Profile CurrentProfile { get; private set; }
        public static string ProfilePath { get; private set; }

        private static string _rootPath;
        private static string RootPath
        {
            get
            {
                if (string.IsNullOrEmpty(_rootPath))
                {
                    _rootPath = string.IsNullOrWhiteSpace(Settings.GlobalSettings.ProfilesPath)
                        ? Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles")
                        : Settings.GlobalSettings.ProfilesPath;
                }
                return _rootPath;
            }
        }

        public static void SetProfileAsDefault(Profile profile)
        {
            ConfigurationResolver.Save(profile, Path.Combine(RootPath, "default.json"), ProfileJsonContext.DefaultToUse);
        }

        public static Profile NewFromDefault()
        {
            return ConfigurationResolver.Load<Profile>(Path.Combine(RootPath, "default.json"), ProfileJsonContext.DefaultToUse) ?? new Profile();
        }

        public static List<string> GetAllProfilePaths()
        {
            var results = new List<string>();
            if (!Directory.Exists(RootPath))
                return results;

            foreach (string dir in Directory.GetDirectories(RootPath, "*", SearchOption.AllDirectories))
            {
                string profileFile = Path.Combine(dir, "profile.json");
                if (File.Exists(profileFile) && !dir.Equals(ProfilePath, System.StringComparison.OrdinalIgnoreCase))
                    results.Add(dir);
            }
            return results;
        }

        public static List<string> GetSameServerProfilePaths()
        {
            if (CurrentProfile == null || string.IsNullOrEmpty(CurrentProfile.ServerName))
                return new List<string>();

            return GetAllProfilePaths()
                .Where(p => p.IndexOf(CurrentProfile.ServerName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        public static void OverrideProfiles(Profile profile, List<string> targetPaths)
        {
            foreach (string path in targetPaths)
            {
                ConfigurationResolver.Save(profile, Path.Combine(path, "profile.json"), ProfileJsonContext.DefaultToUse);
            }
        }

        public static void Load(string servername, string username, string charactername)
        {
            string path = FileSystemHelper.CreateFolderIfNotExists(RootPath, username, servername, charactername);
            string fileToLoad = Path.Combine(path, "profile.json");

            ProfilePath = path;
            CurrentProfile = ConfigurationResolver.Load<Profile>(fileToLoad, ProfileJsonContext.DefaultToUse) ?? NewFromDefault();

            CurrentProfile.Username = username;
            CurrentProfile.ServerName = servername;
            CurrentProfile.CharacterName = charactername;

            ValidateFields(CurrentProfile);

            ClassicUO.Game.Managers.IgnoreManager.Initialize();
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

            if (profile.WindowClientBounds.X < 1024)
            {
                profile.WindowClientBounds = new Point(1024, profile.WindowClientBounds.Y);
            }

            if (profile.WindowClientBounds.Y < 768)
            {
                profile.WindowClientBounds = new Point(profile.WindowClientBounds.X, 768);
            }
        }

        public static void UnLoadProfile()
        {
            CurrentProfile = null;
        }
    }
}