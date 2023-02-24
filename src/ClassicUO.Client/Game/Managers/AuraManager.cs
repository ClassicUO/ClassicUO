#region license

// Copyright (c) 2021, andreakarasho
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

using System;
using ClassicUO.Configuration;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Managers
{
    class Aura : IDisposable
    {
        private static readonly Lazy<BlendState> _blend = new Lazy<BlendState>
        (
            () => new BlendState
            {
                ColorSourceBlend = Blend.SourceAlpha,
                ColorDestinationBlend = Blend.InverseSourceAlpha
            }
        );

        private readonly Texture2D _texture;

        public Aura(int radius)
        {
            short w = 0;
            short h = 0;
            uint[] data = CircleOfTransparency.CreateCircleTexture(radius, ref w, ref h);

            for (int i = 0; i < data.Length; i++)
            {
                ref uint pixel = ref data[i];

                if (pixel != 0)
                {
                    ushort value = (ushort)(pixel << 3);

                    if (value > 0xFF)
                    {
                        value = 0xFF;
                    }

                    pixel = (uint)((value << 24) | (value << 16) | (value << 8) | value);
                }
            }

            _texture = new Texture2D(Client.Game.GraphicsDevice, w, h);
            _texture.SetData(data);
        }



        public void Draw(UltimaBatcher2D batcher, int x, int y, ushort hue, float depth)
        {
            x -= (_texture.Width >> 1);
            y -= (_texture.Height >> 1);

            Vector3 hueVec = ShaderHueTranslator.GetHueVector(hue, false, 1);

            batcher.SetBlendState(_blend.Value);
            batcher.Draw(_texture, new Vector2(x, y), null, hueVec, 0f, Vector2.Zero, 1f, SpriteEffects.None, depth);
            batcher.SetBlendState(null);
        }


        public void Dispose()
        {
            if (_texture != null && !_texture.IsDisposed)
            {
                _texture.Dispose();
            }
        }
    }

    internal static class AuraManager
    {
        private static readonly Aura _aura = new Aura(30);

        private static int _saveAuraUnderFeetType;

        public static bool IsEnabled
        {
            get
            {
                if (ProfileManager.CurrentProfile == null)
                {
                    return false;
                }

                switch (ProfileManager.CurrentProfile.AuraUnderFeetType)
                {
                    default:
                    case 0: return false;

                    case 1 when World.Player != null && World.Player.InWarMode: return true;
                    case 2 when Keyboard.Ctrl && Keyboard.Shift: return true;
                    case 3: return true;
                }
            }
        }

        public static void ToggleVisibility()
        {
            Profile currentProfile = ProfileManager.CurrentProfile;

            if (!IsEnabled)
            {
                _saveAuraUnderFeetType = currentProfile.AuraUnderFeetType;
                currentProfile.AuraUnderFeetType = 3;
            }
            else
            {
                currentProfile.AuraUnderFeetType = _saveAuraUnderFeetType;
            }
        }

        public static void Draw(UltimaBatcher2D batcher, int x, int y, ushort hue, float depth)
        {
            _aura.Draw(batcher, x, y, hue, depth);
        }
    }
}