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

using Microsoft.Xna.Framework;

using Newtonsoft.Json;

namespace ClassicUO.Configuration
{
    internal sealed class Settings
    {
        public static Settings GlobalSettings = new Settings();

        [JsonConstructor]
        public Settings()
        {
        }


        [JsonProperty(PropertyName = "username")]
        public string Username { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "password")]
        public string Password { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "ip")] public string IP { get; set; } = "127.0.0.1";

        [JsonProperty(PropertyName = "port")] public ushort Port { get; set; } = 2593;

        [JsonProperty(PropertyName = "ultimaonlinedirectory")]
        public string UltimaOnlineDirectory { get; set; } = "path/to/uo/";

        [JsonProperty(PropertyName = "clientversion")]
        public string ClientVersion { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "lastcharactername")]
        public string LastCharacterName { get; set; } = string.Empty;

        [JsonProperty(PropertyName = "cliloc")]
        public string ClilocFile { get; set; } = "Cliloc.enu";

        [JsonProperty(PropertyName = "lastservernum")]
        public ushort LastServerNum { get; set; } = 1;

        [JsonProperty(PropertyName = "fps")]
        public int FPS { get; set; } = 60;
        [JsonProperty(PropertyName = "window_position")] public Point? WindowPosition { get; set; }
        [JsonProperty(PropertyName = "debug")] public bool Debug { get; set; }

        [JsonProperty(PropertyName = "profiler")]
        public bool Profiler { get; set; } = true;

        [JsonProperty(PropertyName = "saveaccount")]
        public bool SaveAccount { get; set; }

        [JsonProperty(PropertyName = "autologin")]
        public bool AutoLogin { get; set; }

        [JsonProperty(PropertyName = "reconnect")]
        public bool Reconnect { get; set; }

        [JsonProperty(PropertyName = "reconnect_time")]
        public int ReconnectTime { get; set; }

        [JsonProperty(PropertyName = "login_music")]
        public bool LoginMusic { get; set; } = true;

        [JsonProperty(PropertyName = "login_music_volume")]
        public int LoginMusicVolume { get; set; } = 70;

        [JsonProperty(PropertyName = "shard_type")]
        public int ShardType { get; set; } // 0 = normal (no customization), 1 = old, 2 = outlands??

        [JsonProperty(PropertyName = "fixed_time_step")]
        public bool FixedTimeStep { get; set; } = true;

        [JsonProperty(propertyName: "run_mouse_in_separate_thread")]
        public bool RunMouseInASeparateThread { get; set; } = true;

        [JsonProperty(PropertyName = "plugins")]
        public string[] Plugins { get; set; } = {@"./Assistant/Razor.dll"};




        public const string SETTINGS_FILENAME = "settings.json";
        public static string CustomSettingsFilepath = null;

        public static string GetSettingsFilepath()
        {
            if (CustomSettingsFilepath != null)
            {
                if (Path.IsPathRooted(CustomSettingsFilepath))
                    return CustomSettingsFilepath;
                else
                    return Path.Combine(CUOEnviroment.ExecutablePath, CustomSettingsFilepath);
            }

            return Path.Combine(CUOEnviroment.ExecutablePath, SETTINGS_FILENAME);
        }



        public void Save()
        {
            // Make a copy of the settings object that we will use in the saving process
            Settings settingsToSave = JsonConvert.DeserializeObject<Settings>(JsonConvert.SerializeObject(this));

            // Make sure we don't save username and password if `saveaccount` flag is not set
            // NOTE: Even if we pass username and password via command-line arguments they won't be saved
            if (!settingsToSave.SaveAccount)
            {
                settingsToSave.Username = string.Empty;
                settingsToSave.Password = string.Empty;
            }

            // NOTE: We can do any other settings clean-ups here before we save them

            ConfigurationResolver.Save(settingsToSave, GetSettingsFilepath());
        }

        public bool IsValid()
        {
            bool valid = !string.IsNullOrWhiteSpace(UltimaOnlineDirectory);


            //if (string.IsNullOrWhiteSpace(ClientVersion) || ClientVersion == "0.0.0.0" || ClientVersion.Split(new [] { '.' }, StringSplitOptions.RemoveEmptyEntries).Length <= 2)
            //{
            //    valid = false;
            //}


            return valid;
        }
    }
}