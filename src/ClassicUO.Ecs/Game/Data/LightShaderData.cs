// SPDX-License-Identifier: BSD-2-Clause

using static ClassicUO.Game.Data.LightColors;

namespace ClassicUO.Game.Data
{
    internal struct LightShaderData
    {
        public LightShaderData(uint rgb, LightShaderCurve redcurve = LightShaderCurve.Standard, LightShaderCurve greencurve = LightShaderCurve.Standard, LightShaderCurve bluecurve = LightShaderCurve.Standard)
        {
            RGB = rgb;
            Hue = 0;
            RedCurve = redcurve;
            GreenCurve = greencurve;
            BlueCurve = bluecurve;
        }

        public LightShaderData(ushort hue)
        {
            RGB = 0;
            Hue = hue;
            RedCurve = GreenCurve = BlueCurve = LightShaderCurve.Standard;
        }

        public uint RGB;
        public ushort Hue;
        public LightShaderCurve RedCurve;
        public LightShaderCurve GreenCurve;
        public LightShaderCurve BlueCurve;
    }
}
