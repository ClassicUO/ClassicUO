#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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

using System;
using System.IO;

using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

using Newtonsoft.Json;

namespace ClassicUO.Configuration
{
    internal static class ProfileManager
    {
        public static Profile Current { get; private set; }

        public static void Load(string servername, string username, string charactername)
        {
            string path = FileSystemHelper.CreateFolderIfNotExists(CUOEnviroment.ExecutablePath, "Data", "Profiles", username, servername, charactername);



            string fileToLoad = Path.Combine(path, "profile.json");

            if (!File.Exists(fileToLoad))
                Current = new Profile(username, servername, charactername);
            else
            {
                Current = ConfigurationResolver.Load<Profile>(fileToLoad,
                                                              new JsonSerializerSettings
                                                              {
                                                                  TypeNameHandling = TypeNameHandling.All,
                                                                  MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead
                                                              });
                if (Current == null)
                {
                    Current = new Profile(username, servername, charactername);
                }
                else
                {
                    Current.Username = username;
                    Current.ServerName = servername;
                    Current.CharacterName = charactername;
                }
            }
        }

        public static void UnLoadProfile()
        {
            Current = null;
        }
    }
}