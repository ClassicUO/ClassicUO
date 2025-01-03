#region license

// BSD 2-Clause License
//
// Copyright (c) 2025, andreakarasho
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//
// - Redistributions of source code must retain the above copyright notice, this
//   list of conditions and the following disclaimer.
//
// - Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

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
