using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game.Managers
{
    [Flags]
    enum NameOverheadTypeAllowed
    {
        All,
        Mobiles,
        Items,
    }

    static class NameOverHeadManager
    {
        public static NameOverheadTypeAllowed TypeAllowed { get; set; }

        private static NameOverHeadHandlerGump _gump;

        public static bool IsAllowed(Entity serial)
        {
            if (serial == null)
                return false;

            if (TypeAllowed == NameOverheadTypeAllowed.All)
                return true;

            if (serial.Serial.IsItem && TypeAllowed == NameOverheadTypeAllowed.Items)
                return true;

            return serial.Serial.IsMobile && TypeAllowed == NameOverheadTypeAllowed.Mobiles;
        }

        public static void Open()
        {
            if (_gump != null)
                return;

            _gump = new NameOverHeadHandlerGump();
            Engine.UI.Add(_gump);
        }

        public static void Close()
        {
            if (_gump != null)
            {
                Engine.UI.Remove<NameOverHeadHandlerGump>();
                _gump.Dispose();
                _gump = null;
            }
        }
    }
}
