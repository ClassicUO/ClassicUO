// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Data
{
    internal enum GraphicEffectBlendMode
    {
        Normal = 0x00,                // normal, black is transparent
        Multiply = 0x01,              // darken
        Screen = 0x02,                // lighten
        ScreenMore = 0x03,            // lighten more (slightly)
        ScreenLess = 0x04,            // lighten less
        NormalHalfTransparent = 0x05, // transparent but with black edges - 50% transparency?
        ShadowBlue = 0x06,            // complete shadow with blue edges
        ScreenRed = 0x07              // transparent more red
    }
}