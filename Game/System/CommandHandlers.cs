using ClassicUO.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.System
{
    class CommandHandlers
    {
        public static void RequestItemInfo()
        {
            if (!TargetSystem.IsTargeting)
            {
                TargetSystem.SetTargeting(TargetType.SetTargetClientSide, 6983686, 0);
                
            }
            else
            {
                TargetSystem.SetTargeting(TargetType.Nothing, 0, 0);
            }
        }
    }
}
