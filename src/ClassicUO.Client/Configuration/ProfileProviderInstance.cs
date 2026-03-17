// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.IO;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Configuration
{
    internal sealed class ProfileProviderInstance : IProfileProvider
    {
        private readonly ISettingsProvider _settings;
        private string _rootPath;

        public ProfileProviderInstance(ISettingsProvider settings)
        {
            _settings = settings;
        }

        public GlobalProfile GlobalProfile { get; private set; }
        public Profile CurrentProfile { get; private set; }
        public string ProfilePath { get; private set; }

        private string RootPath
        {
            get
            {
                if (string.IsNullOrEmpty(_rootPath))
                {
                    if (string.IsNullOrWhiteSpace(_settings.ProfilesPath))
                    {
                        _rootPath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles");
                    }
                    else
                    {
                        _rootPath = _settings.ProfilesPath;
                    }
                }

                return _rootPath;
            }
        }

        public void Load(string servername, string username, string charactername)
        {
            GlobalProfile = ConfigurationResolver.Load<GlobalProfile>(
                Path.Combine(RootPath, "globalprofile.json"),
                ProfileJsonContext.DefaultToUse.GlobalProfile) ?? new GlobalProfile();

            string path = FileSystemHelper.CreateFolderIfNotExists(RootPath, username, servername, charactername);
            string fileToLoad = Path.Combine(path, "profile.json");

            ProfilePath = path;
            CurrentProfile = ConfigurationResolver.Load<Profile>(
                fileToLoad, ProfileJsonContext.DefaultToUse.Profile) ?? NewFromDefault();

            CurrentProfile.Username = username;
            CurrentProfile.ServerName = servername;
            CurrentProfile.CharacterName = charactername;

            ValidateFields(CurrentProfile);
        }

        public void SetProfileAsDefault(Profile profile)
        {
            Save(profile, RootPath, "default.json");
        }

        public Profile NewFromDefault()
        {
            return ConfigurationResolver.Load<Profile>(
                Path.Combine(RootPath, "default.json"),
                ProfileJsonContext.DefaultToUse.Profile) ?? new Profile();
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

        public void UnLoadProfile()
        {
            GlobalProfile = null;
            CurrentProfile = null;
        }

        public void Save(Profile profile, string path, string filename = "profile.json")
        {
            ConfigurationResolver.Save(profile, Path.Combine(path, filename), ProfileJsonContext.DefaultToUse.Profile);
            if (GlobalProfile != null)
            {
                ConfigurationResolver.Save(GlobalProfile, Path.Combine(RootPath, "globalprofile.json"), ProfileJsonContext.DefaultToUse.GlobalProfile);
            }
        }
    }
}
