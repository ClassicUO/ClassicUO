#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Configuration
{
    internal static class ProfileManager
    {
        private static readonly string _path = Path.Combine(Engine.ExePath, "Data");


        public static Profile Current { get; set; }


        public static void Load(string name)
        {
            string ext = Path.GetExtension(name);

            if (string.IsNullOrEmpty(ext))
                name = name + ".json";

            if (File.Exists(name))
            {
                Current = ConfigurationResolver.Load<Profile>(name);
            }

        }

        public static void Save()
        {
            if (!Directory.Exists(_path))
                Directory.CreateDirectory(_path);

            ConfigurationResolver.Save(Current, Current.Path);
        }
    }
}
