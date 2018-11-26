using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace ClassicUO.Configuration
{
    internal class Profile
    {
        [JsonConstructor]
        public Profile(string path, string name)
        {
            Name = name;
            Path = System.IO.Path.Combine(path, Name + ".json");
        }

        [JsonProperty]
        public string Name { get; }
        [JsonProperty]
        public string Path { get; }
        [JsonProperty]
        public Dictionary<string, object> Properties { get; } = new Dictionary<string, object>();
    }
}
