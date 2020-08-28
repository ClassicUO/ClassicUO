#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion

using System.IO;

using ClassicUO.Utility;

using Microsoft.Xna.Framework;

using TinyJson;

namespace ClassicUO.Configuration
{
    internal static class ProfileManager
    {
        public static Profile Current { get; private set; }

        public static void Load(string servername, string username, string charactername)
        {
            string path = FileSystemHelper.CreateFolderIfNotExists(CUOEnviroment.ExecutablePath, "Data", "Profiles", username, servername, charactername);
            string fileToLoad = Path.Combine(path, "profile.json");

            Current = ConfigurationResolver.Load<Profile>(fileToLoad) ?? new Profile();

            Current.Username = username;
            Current.ServerName = servername;
            Current.CharacterName = charactername;

            ValidateFields(Current);
        }


        private static void ValidateFields(Profile profile)
        {
            if (profile == null)
                return;

            if (profile.WindowClientBounds.X < 600)
                profile.WindowClientBounds = new Point(600, profile.WindowClientBounds.Y);
            if (profile.WindowClientBounds.Y < 480)
                profile.WindowClientBounds = new Point(profile.WindowClientBounds.X, 480);
            
        }

        public static void UnLoadProfile()
        {
            Current = null;
        }
    }
}