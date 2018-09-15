using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Configuration
{
    class SettingsModel
    {
        public SettingsModel()
        {

        }
        public int Id { get; set; }
        public string ProfileName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string IP { get; set; }
        public ushort Port { get; set; }
        public string LastCharacterName { get; set; }
        public string UltimaOnlineDirectory { get; set; }
        public string ClientVersion { get; set; }
    }
}
