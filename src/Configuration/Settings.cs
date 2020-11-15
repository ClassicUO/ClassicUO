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
using Microsoft.Xna.Framework;
using TinyJson;

namespace ClassicUO.Configuration
{
    internal sealed class Settings
    {
        public const string SETTINGS_FILENAME = "settings.json";
        public static Settings GlobalSettings = new Settings();
        public static string CustomSettingsFilepath = null;


        [JsonProperty("username")] public string Username { get; set; } = string.Empty;

        [JsonProperty("password")] public string Password { get; set; } = string.Empty;

        [JsonProperty("ip")] public string IP { get; set; } = "127.0.0.1";

        [JsonProperty("port")] public ushort Port { get; set; } = 2593;

        [JsonProperty("ultimaonlinedirectory")]
        public string UltimaOnlineDirectory { get; set; } = "";

        [JsonProperty("profilespath")] public string ProfilesPath { get; set; } = string.Empty;

        [JsonProperty("clientversion")] public string ClientVersion { get; set; } = string.Empty;

        [JsonProperty("lastcharactername")] public string LastCharacterName { get; set; } = string.Empty;

        [JsonProperty("cliloc")] public string ClilocFile { get; set; } = "Cliloc.enu";

        [JsonProperty("lastservernum")] public ushort LastServerNum { get; set; } = 1;

        [JsonProperty("fps")] public int FPS { get; set; } = 60;

        [JsonProperty("window_position")] public Point? WindowPosition { get; set; }
        [JsonProperty("window_size")] public Point? WindowSize { get; set; }

        [JsonProperty("is_win_maximized")] public bool IsWindowMaximized { get; set; } = true;

        [JsonProperty("saveaccount")] public bool SaveAccount { get; set; }

        [JsonProperty("autologin")] public bool AutoLogin { get; set; }

        [JsonProperty("reconnect")] public bool Reconnect { get; set; }

        [JsonProperty("reconnect_time")] public int ReconnectTime { get; set; } = 1;

        [JsonProperty("login_music")] public bool LoginMusic { get; set; } = true;

        [JsonProperty("login_music_volume")] public int LoginMusicVolume { get; set; } = 70;

        [JsonProperty("shard_type")]
        public int ShardType { get; set; } // 0 = normal (no customization), 1 = old, 2 = outlands??

        [JsonProperty("fixed_time_step")] public bool FixedTimeStep { get; set; } = true;

        [JsonProperty("run_mouse_in_separate_thread")]
        public bool RunMouseInASeparateThread { get; set; } = true;

        [JsonProperty("force_driver")] public byte ForceDriver { get; set; }

        [JsonProperty("use_verdata")] public bool UseVerdata { get; set; }

        [JsonProperty("maps_layouts")] public string MapsLayouts { get; set; }

        [JsonProperty("encryption")] public byte Encryption { get; set; }

        [JsonProperty("plugins")] public string[] Plugins { get; set; } = { @"./Assistant/Razor.dll" };

        public static string GetSettingsFilepath()
        {
            if (CustomSettingsFilepath != null)
            {
                if (Path.IsPathRooted(CustomSettingsFilepath))
                {
                    return CustomSettingsFilepath;
                }

                return Path.Combine(CUOEnviroment.ExecutablePath, CustomSettingsFilepath);
            }

            return Path.Combine(CUOEnviroment.ExecutablePath, SETTINGS_FILENAME);
        }


        public void Save()
        {
            // Make a copy of the settings object that we will use in the saving process
            string json = this.Encode(true);

            Settings
                settingsToSave =
                    json.Decode<Settings>(); // JsonConvert.DeserializeObject<Settings>(JsonConvert.SerializeObject(this));

            // Make sure we don't save username and password if `saveaccount` flag is not set
            // NOTE: Even if we pass username and password via command-line arguments they won't be saved
            if (!settingsToSave.SaveAccount)
            {
                settingsToSave.Username = string.Empty;
                settingsToSave.Password = string.Empty;
            }

            settingsToSave.ProfilesPath = string.Empty;

            // NOTE: We can do any other settings clean-ups here before we save them

            ConfigurationResolver.Save(settingsToSave, GetSettingsFilepath());
        }
    }
}