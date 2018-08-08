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

        [JsonProperty(PropertyName = "ip")]
        public string IP { get; set; }

        [JsonProperty(PropertyName = "port")]
        public ushort Port { get; set; }

        [JsonProperty(PropertyName = "lastcharactername")]
        public string LastCharacterName { get; set; }

        [JsonProperty(PropertyName = "ultimaonlinedirectory")]
        public string UltimaOnlineDirectory { get; set; }

        [JsonProperty(PropertyName = "clientversion")]
        public string ClientVersion { get; set; }
    }
}