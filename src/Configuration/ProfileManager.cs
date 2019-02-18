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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.UI.Gumps;
using ClassicUO.Utility;
using ClassicUO.Utility.Coroutines;

using Newtonsoft.Json;

namespace ClassicUO.Configuration
{
    internal class ProfileManager
    {
        public Profile Current { get; private set; }

        public void Load(string servername, string username, string charactername)
        {
            string path = FileSystemHelper.CreateFolderIfNotExists(Engine.ExePath, "Data", "Profiles", username, servername, charactername);

            if (!File.Exists(Path.Combine(path, "settings.json")))
            {
                Current = new Profile(username, servername, charactername);
            }
            else
            {
                Current = ConfigurationResolver.Load<Profile>(Path.Combine(path, "settings.json"), 
                new JsonSerializerSettings()
                {
                    TypeNameHandling = TypeNameHandling.All,
                    MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead                
                }) ?? new Profile(username, servername, charactername);
            }
        }

        public void UnLoadProfile() => Current = null;
    }
}
