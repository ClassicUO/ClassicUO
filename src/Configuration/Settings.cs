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
        
        [JsonProperty("lang")] public string Language { get; set; } = "";

        [JsonProperty("lastservernum")] public ushort LastServerNum { get; set; } = 1;

        [JsonProperty("last_server_name")] public string LastServerName { get; set; } = string.Empty;

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

        [JsonProperty("shard_type")] public int ShardType { get; set; } // 0 = normal (no customization), 1 = old, 2 = outlands??

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

            Settings settingsToSave = json.Decode<Settings>(); // JsonConvert.DeserializeObject<Settings>(JsonConvert.SerializeObject(this));

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