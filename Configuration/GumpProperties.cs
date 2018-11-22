using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Configuration
{
    public struct GumpProperties
    {      
        public GumpProperties(Type type, Dictionary<string, object> props)
        {
            Type = type;
            Properties = props;
        }

        public readonly Type Type;
        public readonly Dictionary<string, object> Properties;
    }
}
