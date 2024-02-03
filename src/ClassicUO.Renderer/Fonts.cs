#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using Microsoft.Xna.Framework.Graphics;
using System;

namespace ClassicUO.Renderer
{
    partial class FontResources
    {
        [EmbedResourceCSharp.FileEmbed("fonts/regular_font.xnb")]
        public static partial ReadOnlySpan<byte> GetRegularFont();

        [EmbedResourceCSharp.FileEmbed("fonts/bold_font.xnb")]
        public static partial ReadOnlySpan<byte> GetBoldFont();

        [EmbedResourceCSharp.FileEmbed("fonts/map1_font.xnb")]
        public static partial ReadOnlySpan<byte> GetMap1Font();

        [EmbedResourceCSharp.FileEmbed("fonts/map2_font.xnb")]
        public static partial ReadOnlySpan<byte> GetMap2Font();

        [EmbedResourceCSharp.FileEmbed("fonts/map3_font.xnb")]
        public static partial ReadOnlySpan<byte> GetMap3Font();

        [EmbedResourceCSharp.FileEmbed("fonts/map4_font.xnb")]
        public static partial ReadOnlySpan<byte> GetMap4Font();

        [EmbedResourceCSharp.FileEmbed("fonts/map5_font.xnb")]
        public static partial ReadOnlySpan<byte> GetMap5Font();

        [EmbedResourceCSharp.FileEmbed("fonts/map6_font.xnb")]
        public static partial ReadOnlySpan<byte> GetMap6Font();
    }

    public static class Fonts
    {
        public static void Initialize(GraphicsDevice device)
        {
            Regular = SpriteFont.Create(device, FontResources.GetRegularFont());
            Bold = SpriteFont.Create(device, FontResources.GetBoldFont());
            Map1 = SpriteFont.Create(device, FontResources.GetMap1Font());
            Map2 = SpriteFont.Create(device, FontResources.GetMap2Font());
            Map3 = SpriteFont.Create(device, FontResources.GetMap3Font());
            Map4 = SpriteFont.Create(device, FontResources.GetMap4Font());
            Map5 = SpriteFont.Create(device, FontResources.GetMap5Font());
            Map6 = SpriteFont.Create(device, FontResources.GetMap6Font());
        }

        public static SpriteFont Regular { get; private set; }
        public static SpriteFont Bold { get; private set; }
        public static SpriteFont Map1 { get; private set; }
        public static SpriteFont Map2 { get; private set; }
        public static SpriteFont Map3 { get; private set; }
        public static SpriteFont Map4 { get; private set; }
        public static SpriteFont Map5 { get; private set; }
        public static SpriteFont Map6 { get; private set; }
    }
}