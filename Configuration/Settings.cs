#region license

//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
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

using Newtonsoft.Json;

namespace ClassicUO.Configuration
{
    public class Settings
    {
        [JsonConstructor]
        public Settings()
        {
        }

        [JsonProperty(PropertyName = "username")]
        public string Username { get; set; }

        [JsonProperty(PropertyName = "password")]
        public string Password { get; set; }

        [JsonProperty(PropertyName = "ip")] public string IP { get; set; }

        [JsonProperty(PropertyName = "port")] public ushort Port { get; set; }

        [JsonProperty(PropertyName = "lastcharactername")]
        public string LastCharacterName { get; set; }

        [JsonProperty(PropertyName = "ultimaonlinedirectory")]
        public string UltimaOnlineDirectory { get; set; }

        [JsonProperty(PropertyName = "clientversion")]
        public string ClientVersion { get; set; }
    }
}