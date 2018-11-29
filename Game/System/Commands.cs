using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.System
{
    public static class Commands
    {

        public static void Initialize()
        {
            CommandSystem.Register("info", (sender, args) => CommandHandlers.RequestItemInfo());
        }
    }
}
