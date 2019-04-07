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
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

using ClassicUO.Utility;

using Microsoft.Xna.Framework;

using Newtonsoft.Json;

namespace ClassicUO.Configuration
{
    internal sealed class Settings
    {
        [JsonConstructor]
        public Settings()
        {
        }

        [JsonProperty(PropertyName = "username")] public string Username { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "password")] public string Password { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "ip")] public string IP { get; set; } = "127.0.0.1";

        [JsonProperty(PropertyName = "port")] public ushort Port { get; set; } = 2593;

        [JsonProperty(PropertyName = "ultimaonlinedirectory")] public string UltimaOnlineDirectory { get; set; } = "Remember\\Double\\Backslashes\\";

        [JsonProperty(PropertyName = "clientversion")] public string ClientVersion { get; set; } = "0.0.0.0";

        [JsonProperty(PropertyName = "lastcharactername")] public string LastCharacterName { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "lastservernum")] public ushort LastServerNum { get; set; } = 1;

        [JsonProperty(PropertyName = "login_fps")]public int MaxLoginFPS { get; set; } = 60;

        [JsonProperty(PropertyName = "debug")] public bool Debug { get; set; } = false;

        [JsonProperty(PropertyName = "profiler")] public bool Profiler { get; set; } = true;

        [JsonProperty(PropertyName = "preload_maps")] public bool PreloadMaps { get; set; }

        [JsonProperty(PropertyName = "saveaccount")] public bool SaveAccount { get; set; } = false;

        [JsonProperty(PropertyName = "autologin")] public bool AutoLogin { get; set; } = false;

        [JsonProperty(PropertyName = "reconnect")] public bool Reconnect { get; set; } = false;

        [JsonProperty(PropertyName = "reconnect_time")] public int ReconnectTime { get; set; } = 0;

        [JsonProperty(PropertyName = "login_music")] public bool LoginMusic { get; set; } = true;

        [JsonProperty(PropertyName = "login_music_volume")] public int LoginMusicVolume { get; set; } = 70;

        [JsonProperty(PropertyName = "shard_type")] public int ShardType { get; set; } = 0; // 0 = normal (no customization), 1 = old, 2 = outlands??

        [JsonProperty(PropertyName = "fixed_time_step")] public bool FixedTimeStep { get; set; } = true;
        
        [JsonProperty(PropertyName = "plugins")] public string[] Plugins { get; set; } = {@".\Assistant\Razor.dll"};

        public void Save()
        {
            ConfigurationResolver.Save(this, Path.Combine(Engine.ExePath, Engine.SettingsFile));
        }
    }
}