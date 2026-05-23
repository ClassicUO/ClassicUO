// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Input;
using SDL3;

namespace ClassicUO.Game.Macros
{
    /// <summary>
    /// Concrete <see cref="IMacroLookup"/>. Linear scan of the host
    /// <see cref="MacroManager"/>'s linked macro list — fast enough because
    /// macro counts are tiny (dozens). Returns the first match.
    /// </summary>
    internal sealed class MacroLookup : IMacroLookup
    {
        public Macro FindMacro(MacroManager host, SDL.SDL_Keycode key, bool alt, bool ctrl, bool shift)
        {
            Macro obj = (Macro) host.Items;

            while (obj != null)
            {
                if (obj.Key == key && obj.Alt == alt && obj.Ctrl == ctrl && obj.Shift == shift)
                {
                    break;
                }

                obj = (Macro) obj.Next;
            }

            return obj;
        }

        public Macro FindMacro(MacroManager host, MouseButtonType button, bool alt, bool ctrl, bool shift)
        {
            Macro obj = (Macro) host.Items;

            while (obj != null)
            {
                if (obj.MouseButton == button && obj.Alt == alt && obj.Ctrl == ctrl && obj.Shift == shift)
                {
                    break;
                }

                obj = (Macro) obj.Next;
            }

            return obj;
        }

        public Macro FindMacro(MacroManager host, bool wheelUp, bool alt, bool ctrl, bool shift)
        {
            Macro obj = (Macro) host.Items;

            while (obj != null)
            {
                if (obj.WheelScroll == true && obj.WheelUp == wheelUp && obj.Alt == alt && obj.Ctrl == ctrl && obj.Shift == shift)
                {
                    break;
                }

                obj = (Macro) obj.Next;
            }

            return obj;
        }

        public Macro FindMacro(MacroManager host, string name)
        {
            Macro obj = (Macro) host.Items;

            while (obj != null)
            {
                if (obj.Name == name)
                {
                    break;
                }

                obj = (Macro) obj.Next;
            }

            return obj;
        }
    }
}
