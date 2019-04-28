using System.Collections.Generic;
using System.Text;

using SDL2;

namespace ClassicUO.Utility
{
    internal static class KeysTranslator
    {
        private static readonly Dictionary<SDL.SDL_Keycode, string> _keys = new Dictionary<SDL.SDL_Keycode, string>
        {
            {SDL.SDL_Keycode.SDLK_UNKNOWN, "NONE"},
            {SDL.SDL_Keycode.SDLK_BACKSPACE, "Backspace"},
            {SDL.SDL_Keycode.SDLK_TAB, "Tab"},
            {SDL.SDL_Keycode.SDLK_RETURN, "Return"},
            {SDL.SDL_Keycode.SDLK_ESCAPE, "Esc"},
            {SDL.SDL_Keycode.SDLK_SPACE, "Space"},
            {SDL.SDL_Keycode.SDLK_EXCLAIM, "!"},
            //{ SDL.SDL_Keycode.SDLK_QUOTEDBL = 34, // 0x00000022
            //{ SDL.SDL_Keycode.SDLK_HASH = 35, // 0x00000023
            {SDL.SDL_Keycode.SDLK_DOLLAR, "$"},
            {SDL.SDL_Keycode.SDLK_PERCENT, "%"},
            //{ SDL.SDL_Keycode.SDLK_AMPERSAND = 38, // 0x00000026
            {SDL.SDL_Keycode.SDLK_QUOTE, "'"},
            //{ SDL.SDL_Keycode.SDLK_LEFTPAREN = 40, // 0x00000028
            //{ SDL.SDL_Keycode.SDLK_RIGHTPAREN = 41, // 0x00000029
            {SDL.SDL_Keycode.SDLK_ASTERISK, "*"},
            {SDL.SDL_Keycode.SDLK_PLUS, "+"},
            {SDL.SDL_Keycode.SDLK_COMMA, ","},
            {SDL.SDL_Keycode.SDLK_MINUS, "-"},
            {SDL.SDL_Keycode.SDLK_PERIOD, "."},
            {SDL.SDL_Keycode.SDLK_SLASH, "/"},
            {SDL.SDL_Keycode.SDLK_0, "0"},
            {SDL.SDL_Keycode.SDLK_1, "1"},
            {SDL.SDL_Keycode.SDLK_2, "2"},
            {SDL.SDL_Keycode.SDLK_3, "3"},
            {SDL.SDL_Keycode.SDLK_4, "4"},
            {SDL.SDL_Keycode.SDLK_5, "5"},
            {SDL.SDL_Keycode.SDLK_6, "6"},
            {SDL.SDL_Keycode.SDLK_7, "7"},
            {SDL.SDL_Keycode.SDLK_8, "8"},
            {SDL.SDL_Keycode.SDLK_9, "9"},
            {SDL.SDL_Keycode.SDLK_COLON, ":"},
            {SDL.SDL_Keycode.SDLK_SEMICOLON, ";"},
            {SDL.SDL_Keycode.SDLK_LESS, "<"},
            {SDL.SDL_Keycode.SDLK_EQUALS, "="},
            {SDL.SDL_Keycode.SDLK_GREATER, ">"},
            {SDL.SDL_Keycode.SDLK_QUESTION, "?"},
            //{ SDL.SDL_Keycode.SDLK_AT = 64, // 0x00000040
            {SDL.SDL_Keycode.SDLK_LEFTBRACKET, "["},
            {SDL.SDL_Keycode.SDLK_BACKSLASH, "\\"},
            {SDL.SDL_Keycode.SDLK_RIGHTBRACKET, "]"},
            {SDL.SDL_Keycode.SDLK_CARET, "-"},
            {SDL.SDL_Keycode.SDLK_UNDERSCORE, "_"},
            {SDL.SDL_Keycode.SDLK_BACKQUOTE, "BACKQUOTE"}, //  = 96, // 0x00000060
            {SDL.SDL_Keycode.SDLK_a, "A"},
            {SDL.SDL_Keycode.SDLK_b, "B"},
            {SDL.SDL_Keycode.SDLK_c, "C"},
            {SDL.SDL_Keycode.SDLK_d, "D"},
            {SDL.SDL_Keycode.SDLK_e, "E"},
            {SDL.SDL_Keycode.SDLK_f, "F"},
            {SDL.SDL_Keycode.SDLK_g, "G"},
            {SDL.SDL_Keycode.SDLK_h, "H"},
            {SDL.SDL_Keycode.SDLK_i, "I"},
            {SDL.SDL_Keycode.SDLK_j, "J"},
            {SDL.SDL_Keycode.SDLK_k, "K"},
            {SDL.SDL_Keycode.SDLK_l, "L"},
            {SDL.SDL_Keycode.SDLK_m, "M"},
            {SDL.SDL_Keycode.SDLK_n, "N"},
            {SDL.SDL_Keycode.SDLK_o, "O"},
            {SDL.SDL_Keycode.SDLK_p, "P"},
            {SDL.SDL_Keycode.SDLK_q, "Q"},
            {SDL.SDL_Keycode.SDLK_r, "R"},
            {SDL.SDL_Keycode.SDLK_s, "S"},
            {SDL.SDL_Keycode.SDLK_t, "T"},
            {SDL.SDL_Keycode.SDLK_u, "U"},
            {SDL.SDL_Keycode.SDLK_v, "V"},
            {SDL.SDL_Keycode.SDLK_w, "W"},
            {SDL.SDL_Keycode.SDLK_x, "X"},
            {SDL.SDL_Keycode.SDLK_y, "Y"},
            {SDL.SDL_Keycode.SDLK_z, "Z"},
            {SDL.SDL_Keycode.SDLK_DELETE, "DEL"},
            {SDL.SDL_Keycode.SDLK_CAPSLOCK, "CAPS"},
            {SDL.SDL_Keycode.SDLK_F1, "F1"},
            {SDL.SDL_Keycode.SDLK_F2, "F2"},
            {SDL.SDL_Keycode.SDLK_F3, "F3"},
            {SDL.SDL_Keycode.SDLK_F4, "F4"},
            {SDL.SDL_Keycode.SDLK_F5, "F5"},
            {SDL.SDL_Keycode.SDLK_F6, "F6"},
            {SDL.SDL_Keycode.SDLK_F7, "F7"},
            {SDL.SDL_Keycode.SDLK_F8, "F8"},
            {SDL.SDL_Keycode.SDLK_F9, "F9"},
            {SDL.SDL_Keycode.SDLK_F10, "F10"},
            {SDL.SDL_Keycode.SDLK_F11, "F11"},
            {SDL.SDL_Keycode.SDLK_F12, "F12"},
            {SDL.SDL_Keycode.SDLK_PRINTSCREEN, "Print"},
            {SDL.SDL_Keycode.SDLK_SCROLLLOCK, "Lock"},
            {SDL.SDL_Keycode.SDLK_PAUSE, "Pause"},
            {SDL.SDL_Keycode.SDLK_INSERT, "Ins"},
            {SDL.SDL_Keycode.SDLK_HOME, "Home"},
            {SDL.SDL_Keycode.SDLK_PAGEUP, "PG UP"},
            {SDL.SDL_Keycode.SDLK_END, "END"},
            {SDL.SDL_Keycode.SDLK_PAGEDOWN, "PG DOWN"},
            {SDL.SDL_Keycode.SDLK_RIGHT, "Right"},
            {SDL.SDL_Keycode.SDLK_LEFT, "Left"},
            {SDL.SDL_Keycode.SDLK_DOWN, "Down"},
            {SDL.SDL_Keycode.SDLK_UP, "Up"},
            //{ SDL.SDL_Keycode.SDLK_NUMLOCKCLEAR = 1073741907, // 0x40000053
            {SDL.SDL_Keycode.SDLK_KP_DIVIDE, "/"},
            {SDL.SDL_Keycode.SDLK_KP_MULTIPLY, "*"},
            {SDL.SDL_Keycode.SDLK_KP_MINUS, "-"},
            {SDL.SDL_Keycode.SDLK_KP_PLUS, "+"},
            {SDL.SDL_Keycode.SDLK_KP_ENTER, "Enter"},
            {SDL.SDL_Keycode.SDLK_KP_1, "NUM 1"},
            {SDL.SDL_Keycode.SDLK_KP_2, "NUM 2"},
            {SDL.SDL_Keycode.SDLK_KP_3, "NUM 3"},
            {SDL.SDL_Keycode.SDLK_KP_4, "NUM 4"},
            {SDL.SDL_Keycode.SDLK_KP_5, "NUM 5"},
            {SDL.SDL_Keycode.SDLK_KP_6, "NUM 6"},
            {SDL.SDL_Keycode.SDLK_KP_7, "NUM 7"},
            {SDL.SDL_Keycode.SDLK_KP_8, "NUM 8"},
            {SDL.SDL_Keycode.SDLK_KP_9, "NUM 9"},
            {SDL.SDL_Keycode.SDLK_KP_0, "NUM 0"},
            {SDL.SDL_Keycode.SDLK_KP_PERIOD, "."},
            //{ SDL.SDL_Keycode.SDLK_APPLICATION = 1073741925, // 0x40000065
            //{ SDL.SDL_Keycode.SDLK_POWER = 1073741926, // 0x40000066
            {SDL.SDL_Keycode.SDLK_KP_EQUALS, "="}
            //{ SDL.SDL_Keycode.SDLK_F13 = 1073741928, // 0x40000068
            //{ SDL.SDL_Keycode.SDLK_F14 = 1073741929, // 0x40000069
            //{ SDL.SDL_Keycode.SDLK_F15 = 1073741930, // 0x4000006A
            //{ SDL.SDL_Keycode.SDLK_F16 = 1073741931, // 0x4000006B
            //{ SDL.SDL_Keycode.SDLK_F17 = 1073741932, // 0x4000006C
            //{ SDL.SDL_Keycode.SDLK_F18 = 1073741933, // 0x4000006D
            //{ SDL.SDL_Keycode.SDLK_F19 = 1073741934, // 0x4000006E
            //{ SDL.SDL_Keycode.SDLK_F20 = 1073741935, // 0x4000006F
            //{ SDL.SDL_Keycode.SDLK_F21 = 1073741936, // 0x40000070
            //{ SDL.SDL_Keycode.SDLK_F22 = 1073741937, // 0x40000071
            //{ SDL.SDL_Keycode.SDLK_F23 = 1073741938, // 0x40000072
            //{ SDL.SDL_Keycode.SDLK_F24 = 1073741939, // 0x40000073
            //{ SDL.SDL_Keycode.SDLK_EXECUTE = 1073741940, // 0x40000074
            //{ SDL.SDL_Keycode.SDLK_HELP = 1073741941, // 0x40000075
            //{ SDL.SDL_Keycode.SDLK_MENU = 1073741942, // 0x40000076
            //{ SDL.SDL_Keycode.SDLK_SELECT = 1073741943, // 0x40000077
            //{ SDL.SDL_Keycode.SDLK_STOP = 1073741944, // 0x40000078
            //{ SDL.SDL_Keycode.SDLK_AGAIN = 1073741945, // 0x40000079
            //{ SDL.SDL_Keycode.SDLK_UNDO = 1073741946, // 0x4000007A
            //{ SDL.SDL_Keycode.SDLK_CUT = 1073741947, // 0x4000007B
            //{ SDL.SDL_Keycode.SDLK_COPY = 1073741948, // 0x4000007C
            //{ SDL.SDL_Keycode.SDLK_PASTE = 1073741949, // 0x4000007D
            //{ SDL.SDL_Keycode.SDLK_FIND = 1073741950, // 0x4000007E
            //{ SDL.SDL_Keycode.SDLK_MUTE = 1073741951, // 0x4000007F
            //{ SDL.SDL_Keycode.SDLK_VOLUMEUP = 1073741952, // 0x40000080
            //{ SDL.SDL_Keycode.SDLK_VOLUMEDOWN = 1073741953, // 0x40000081
            //{ SDL.SDL_Keycode.SDLK_KP_COMMA = 1073741957, // 0x40000085
            //{ SDL.SDL_Keycode.SDLK_KP_EQUALSAS400 = 1073741958, // 0x40000086
            //{ SDL.SDL_Keycode.SDLK_ALTERASE = 1073741977, // 0x40000099
            //{ SDL.SDL_Keycode.SDLK_SYSREQ = 1073741978, // 0x4000009A
            //{ SDL.SDL_Keycode.SDLK_CANCEL = 1073741979, // 0x4000009B
            //{ SDL.SDL_Keycode.SDLK_CLEAR = 1073741980, // 0x4000009C
            //{ SDL.SDL_Keycode.SDLK_PRIOR = 1073741981, // 0x4000009D
            //{ SDL.SDL_Keycode.SDLK_RETURN2 = 1073741982, // 0x4000009E
            //{ SDL.SDL_Keycode.SDLK_SEPARATOR = 1073741983, // 0x4000009F
            //{ SDL.SDL_Keycode.SDLK_OUT = 1073741984, // 0x400000A0
            //{ SDL.SDL_Keycode.SDLK_OPER = 1073741985, // 0x400000A1
            //{ SDL.SDL_Keycode.SDLK_CLEARAGAIN = 1073741986, // 0x400000A2
            //{ SDL.SDL_Keycode.SDLK_CRSEL = 1073741987, // 0x400000A3
            //{ SDL.SDL_Keycode.SDLK_EXSEL = 1073741988, // 0x400000A4
            //{ SDL.SDL_Keycode.SDLK_KP_00 = 1073742000, // 0x400000B0
            //{ SDL.SDL_Keycode.SDLK_KP_000 = 1073742001, // 0x400000B1
            //{ SDL.SDL_Keycode.SDLK_THOUSANDSSEPARATOR = 1073742002, // 0x400000B2
            //{ SDL.SDL_Keycode.SDLK_DECIMALSEPARATOR = 1073742003, // 0x400000B3
            //{ SDL.SDL_Keycode.SDLK_CURRENCYUNIT = 1073742004, // 0x400000B4
            //{ SDL.SDL_Keycode.SDLK_CURRENCYSUBUNIT = 1073742005, // 0x400000B5
            //{ SDL.SDL_Keycode.SDLK_KP_LEFTPAREN = 1073742006, // 0x400000B6
            //{ SDL.SDL_Keycode.SDLK_KP_RIGHTPAREN = 1073742007, // 0x400000B7
            //{ SDL.SDL_Keycode.SDLK_KP_LEFTBRACE = 1073742008, // 0x400000B8
            //{ SDL.SDL_Keycode.SDLK_KP_RIGHTBRACE = 1073742009, // 0x400000B9
            //{ SDL.SDL_Keycode.SDLK_KP_TAB = 1073742010, // 0x400000BA
            //{ SDL.SDL_Keycode.SDLK_KP_BACKSPACE = 1073742011, // 0x400000BB
            //{ SDL.SDL_Keycode.SDLK_KP_A = 1073742012, // 0x400000BC
            //{ SDL.SDL_Keycode.SDLK_KP_B = 1073742013, // 0x400000BD
            //{ SDL.SDL_Keycode.SDLK_KP_C = 1073742014, // 0x400000BE
            //{ SDL.SDL_Keycode.SDLK_KP_D = 1073742015, // 0x400000BF
            //{ SDL.SDL_Keycode.SDLK_KP_E = 1073742016, // 0x400000C0
            //{ SDL.SDL_Keycode.SDLK_KP_F = 1073742017, // 0x400000C1
            //{ SDL.SDL_Keycode.SDLK_KP_XOR = 1073742018, // 0x400000C2
            //{ SDL.SDL_Keycode.SDLK_KP_POWER = 1073742019, // 0x400000C3
            //{ SDL.SDL_Keycode.SDLK_KP_PERCENT = 1073742020, // 0x400000C4
            //{ SDL.SDL_Keycode.SDLK_KP_LESS = 1073742021, // 0x400000C5
            //{ SDL.SDL_Keycode.SDLK_KP_GREATER = 1073742022, // 0x400000C6
            //{ SDL.SDL_Keycode.SDLK_KP_AMPERSAND = 1073742023, // 0x400000C7
            //{ SDL.SDL_Keycode.SDLK_KP_DBLAMPERSAND = 1073742024, // 0x400000C8
            //{ SDL.SDL_Keycode.SDLK_KP_VERTICALBAR = 1073742025, // 0x400000C9
            //{ SDL.SDL_Keycode.SDLK_KP_DBLVERTICALBAR = 1073742026, // 0x400000CA
            //{ SDL.SDL_Keycode.SDLK_KP_COLON = 1073742027, // 0x400000CB
            //{ SDL.SDL_Keycode.SDLK_KP_HASH = 1073742028, // 0x400000CC
            //{ SDL.SDL_Keycode.SDLK_KP_SPACE = 1073742029, // 0x400000CD
            //{ SDL.SDL_Keycode.SDLK_KP_AT = 1073742030, // 0x400000CE
            //{ SDL.SDL_Keycode.SDLK_KP_EXCLAM = 1073742031, // 0x400000CF
            //{ SDL.SDL_Keycode.SDLK_KP_MEMSTORE = 1073742032, // 0x400000D0
            //{ SDL.SDL_Keycode.SDLK_KP_MEMRECALL = 1073742033, // 0x400000D1
            //{ SDL.SDL_Keycode.SDLK_KP_MEMCLEAR = 1073742034, // 0x400000D2
            //{ SDL.SDL_Keycode.SDLK_KP_MEMADD = 1073742035, // 0x400000D3
            //{ SDL.SDL_Keycode.SDLK_KP_MEMSUBTRACT = 1073742036, // 0x400000D4
            //{ SDL.SDL_Keycode.SDLK_KP_MEMMULTIPLY = 1073742037, // 0x400000D5
            //{ SDL.SDL_Keycode.SDLK_KP_MEMDIVIDE = 1073742038, // 0x400000D6
            //{ SDL.SDL_Keycode.SDLK_KP_PLUSMINUS = 1073742039, // 0x400000D7
            //{ SDL.SDL_Keycode.SDLK_KP_CLEAR = 1073742040, // 0x400000D8
            //{ SDL.SDL_Keycode.SDLK_KP_CLEARENTRY = 1073742041, // 0x400000D9
            //{ SDL.SDL_Keycode.SDLK_KP_BINARY = 1073742042, // 0x400000DA
            //{ SDL.SDL_Keycode.SDLK_KP_OCTAL = 1073742043, // 0x400000DB
            //{ SDL.SDL_Keycode.SDLK_KP_DECIMAL = 1073742044, // 0x400000DC
            //{ SDL.SDL_Keycode.SDLK_KP_HEXADECIMAL = 1073742045, // 0x400000DD
            //{ SDL.SDL_Keycode.SDLK_LCTRL = 1073742048, // 0x400000E0
            //{ SDL.SDL_Keycode.SDLK_LSHIFT = 1073742049, // 0x400000E1
            //{ SDL.SDL_Keycode.SDLK_LALT = 1073742050, // 0x400000E2
            //{ SDL.SDL_Keycode.SDLK_LGUI = 1073742051, // 0x400000E3
            //{ SDL.SDL_Keycode.SDLK_RCTRL = 1073742052, // 0x400000E4
            //{ SDL.SDL_Keycode.SDLK_RSHIFT = 1073742053, // 0x400000E5
            //{ SDL.SDL_Keycode.SDLK_RALT = 1073742054, // 0x400000E6
            //{ SDL.SDL_Keycode.SDLK_RGUI = 1073742055, // 0x400000E7
            //{ SDL.SDL_Keycode.SDLK_MODE = 1073742081, // 0x40000101
            //{ SDL.SDL_Keycode.SDLK_AUDIONEXT = 1073742082, // 0x40000102
            //{ SDL.SDL_Keycode.SDLK_AUDIOPREV = 1073742083, // 0x40000103
            //{ SDL.SDL_Keycode.SDLK_AUDIOSTOP = 1073742084, // 0x40000104
            //{ SDL.SDL_Keycode.SDLK_AUDIOPLAY = 1073742085, // 0x40000105
            //{ SDL.SDL_Keycode.SDLK_AUDIOMUTE = 1073742086, // 0x40000106
            //{ SDL.SDL_Keycode.SDLK_MEDIASELECT = 1073742087, // 0x40000107
            //{ SDL.SDL_Keycode.SDLK_WWW = 1073742088, // 0x40000108
            //{ SDL.SDL_Keycode.SDLK_MAIL = 1073742089, // 0x40000109
            //{ SDL.SDL_Keycode.SDLK_CALCULATOR = 1073742090, // 0x4000010A
            //{ SDL.SDL_Keycode.SDLK_COMPUTER = 1073742091, // 0x4000010B
            //{ SDL.SDL_Keycode.SDLK_AC_SEARCH = 1073742092, // 0x4000010C
            //{ SDL.SDL_Keycode.SDLK_AC_HOME = 1073742093, // 0x4000010D
            //{ SDL.SDL_Keycode.SDLK_AC_BACK = 1073742094, // 0x4000010E
            //{ SDL.SDL_Keycode.SDLK_AC_FORWARD = 1073742095, // 0x4000010F
            //{ SDL.SDL_Keycode.SDLK_AC_STOP = 1073742096, // 0x40000110
            //{ SDL.SDL_Keycode.SDLK_AC_REFRESH = 1073742097, // 0x40000111
            //{ SDL.SDL_Keycode.SDLK_AC_BOOKMARKS = 1073742098, // 0x40000112
            //{ SDL.SDL_Keycode.SDLK_BRIGHTNESSDOWN = 1073742099, // 0x40000113
            //{ SDL.SDL_Keycode.SDLK_BRIGHTNESSUP = 1073742100, // 0x40000114
            //{ SDL.SDL_Keycode.SDLK_DISPLAYSWITCH = 1073742101, // 0x40000115
            //{ SDL.SDL_Keycode.SDLK_KBDILLUMTOGGLE = 1073742102, // 0x40000116
            //{ SDL.SDL_Keycode.SDLK_KBDILLUMDOWN = 1073742103, // 0x40000117
            //{ SDL.SDL_Keycode.SDLK_KBDILLUMUP = 1073742104, // 0x40000118
            //{ SDL.SDL_Keycode.SDLK_EJECT = 1073742105, // 0x40000119
            //{ SDL.SDL_Keycode.SDLK_SLEEP = 1073742106, // 0x4000011A
        };

        private static readonly Dictionary<SDL.SDL_Keymod, string> _mods = new Dictionary<SDL.SDL_Keymod, string>
        {
            {SDL.SDL_Keymod.KMOD_LSHIFT, "Shift"},
            {SDL.SDL_Keymod.KMOD_RSHIFT, "R Shift"},

            {SDL.SDL_Keymod.KMOD_LCTRL, "Ctrl"},
            {SDL.SDL_Keymod.KMOD_RCTRL, "R Ctrl"},

            {SDL.SDL_Keymod.KMOD_LALT, "Alt"},
            {SDL.SDL_Keymod.KMOD_RALT, "R Alt"}
        };


        public static string TryGetKey(SDL.SDL_Keycode key, SDL.SDL_Keymod mod = SDL.SDL_Keymod.KMOD_NONE)
        {
            if (_keys.TryGetValue(key, out string value))
            {
                StringBuilder sb = new StringBuilder();

                bool isshift = (mod & SDL.SDL_Keymod.KMOD_SHIFT) != SDL.SDL_Keymod.KMOD_NONE;
                bool isctrl = (mod & SDL.SDL_Keymod.KMOD_CTRL) != SDL.SDL_Keymod.KMOD_NONE;
                bool isalt = (mod & SDL.SDL_Keymod.KMOD_ALT) != SDL.SDL_Keymod.KMOD_NONE;


                if (isshift)
                    sb.Append("Shift ");

                if (isctrl)
                    sb.Append("Ctrl ");

                if (isalt)
                    sb.Append("Alt ");


                sb.Append(value);

                return sb.ToString();
            }

            return string.Empty;
        }
    }
}