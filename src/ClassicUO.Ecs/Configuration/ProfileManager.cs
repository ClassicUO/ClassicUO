// SPDX-License-Identifier: BSD-2-Clause

using System.IO;
using ClassicUO.Utility;

namespace ClassicUO.Configuration
{
    // ECS port of the legacy ProfileManager (src/ClassicUO.Client/Configuration/
    // ProfileManager.cs). Pure file I/O + path resolution — the loaded Profile /
    // GlobalProfile become ECS resources (the host swaps them via
    // commands.InsertResource), so this does NOT hold a static "CurrentProfile"
    // that systems read. Same on-disk layout as legacy:
    //   <RootPath>/<username>/<servername>/<charactername>/profile.json
    //   <RootPath>/globalprofile.json
    internal static class ProfileManager
    {
        public static string RootPath =>
            string.IsNullOrWhiteSpace(Settings.GlobalSettings.ProfilesPath)
                ? Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Profiles")
                : Settings.GlobalSettings.ProfilesPath;

        public static (Profile profile, GlobalProfile global, string path) Load(
            string servername, string username, string charactername)
        {
            var global = ConfigurationResolver.Load<GlobalProfile>(
                Path.Combine(RootPath, "globalprofile.json"),
                ProfileJsonContext.DefaultToUse.GlobalProfile) ?? new GlobalProfile();

            var path = FileSystemHelper.CreateFolderIfNotExists(RootPath, username, servername, charactername);

            var profile = ConfigurationResolver.Load<Profile>(
                Path.Combine(path, "profile.json"),
                ProfileJsonContext.DefaultToUse.Profile) ?? new Profile();

            profile.Username = username;
            profile.ServerName = servername;
            profile.CharacterName = charactername;

            return (profile, global, path);
        }

        public static void Save(Profile profile, GlobalProfile global, string path)
        {
            ConfigurationResolver.Save(profile, Path.Combine(path, "profile.json"), ProfileJsonContext.DefaultToUse.Profile);

            if (global != null)
                ConfigurationResolver.Save(global, Path.Combine(RootPath, "globalprofile.json"), ProfileJsonContext.DefaultToUse.GlobalProfile);
        }
    }
}
