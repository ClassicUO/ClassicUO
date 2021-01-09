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
using ClassicUO.Game.Data;
using ClassicUO.Game.Scenes;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.GameObjects
{
    internal sealed partial class AnimatedItemEffect
    {
        private static readonly Lazy<BlendState> _multiplyBlendState = new Lazy<BlendState>
        (
            () =>
            {
                BlendState state = new BlendState
                {
                    ColorSourceBlend = Microsoft.Xna.Framework.Graphics.Blend.Zero,
                    ColorDestinationBlend = Microsoft.Xna.Framework.Graphics.Blend.SourceColor
                };

                return state;
            }
        );

        private static readonly Lazy<BlendState> _screenBlendState = new Lazy<BlendState>
        (
            () =>
            {
                BlendState state = new BlendState
                {
                    ColorSourceBlend = Microsoft.Xna.Framework.Graphics.Blend.One,
                    ColorDestinationBlend = Microsoft.Xna.Framework.Graphics.Blend.One
                };

                return state;
            }
        );

        private static readonly Lazy<BlendState> _screenLessBlendState = new Lazy<BlendState>
        (
            () =>
            {
                BlendState state = new BlendState
                {
                    ColorSourceBlend = Microsoft.Xna.Framework.Graphics.Blend.DestinationColor,
                    ColorDestinationBlend = Microsoft.Xna.Framework.Graphics.Blend.InverseSourceAlpha
                };

                return state;
            }
        );

        private static readonly Lazy<BlendState> _normalHalfBlendState = new Lazy<BlendState>
        (
            () =>
            {
                BlendState state = new BlendState
                {
                    ColorSourceBlend = Microsoft.Xna.Framework.Graphics.Blend.DestinationColor,
                    ColorDestinationBlend = Microsoft.Xna.Framework.Graphics.Blend.SourceColor
                };

                return state;
            }
        );

        private static readonly Lazy<BlendState> _shadowBlueBlendState = new Lazy<BlendState>
        (
            () =>
            {
                BlendState state = new BlendState
                {
                    ColorSourceBlend = Microsoft.Xna.Framework.Graphics.Blend.SourceColor,
                    ColorDestinationBlend = Microsoft.Xna.Framework.Graphics.Blend.InverseSourceColor,
                    ColorBlendFunction = BlendFunction.ReverseSubtract
                };

                return state;
            }
        );

        public override bool Draw(UltimaBatcher2D batcher, int posX, int posY)
        {
            if (IsDestroyed || !AllowedToDraw)
            {
                return false;
            }

            if (AnimationGraphic == 0xFFFF)
            {
                return false;
            }

            ResetHueVector();

            ref StaticTiles data = ref TileDataLoader.Instance.StaticData[Graphic];

            posX += (int) Offset.X;
            posY -= (int) (Offset.Z - Offset.Y);

            ushort hue = Hue;

            if (ProfileManager.CurrentProfile.NoColorObjectsOutOfRange && Distance > World.ClientViewRange)
            {
                hue = Constants.OUT_RANGE_COLOR;
            }
            else if (World.Player.IsDead && ProfileManager.CurrentProfile.EnableBlackWhiteEffect)
            {
                hue = Constants.DEAD_RANGE_COLOR;
            }

            ShaderHueTranslator.GetHueVector(ref HueVector, hue, data.IsPartialHue, data.IsTranslucent ? .5f : 0);

            switch (Blend)
            {
                case GraphicEffectBlendMode.Multiply:
                    batcher.SetBlendState(_multiplyBlendState.Value);

                    DrawStatic
                    (
                        batcher,
                        AnimationGraphic,
                        posX,
                        posY,
                        ref HueVector
                    );

                    batcher.SetBlendState(null);

                    break;

                case GraphicEffectBlendMode.Screen:
                case GraphicEffectBlendMode.ScreenMore:
                    batcher.SetBlendState(_screenBlendState.Value);

                    DrawStatic
                    (
                        batcher,
                        AnimationGraphic,
                        posX,
                        posY,
                        ref HueVector
                    );

                    batcher.SetBlendState(null);

                    break;

                case GraphicEffectBlendMode.ScreenLess:
                    batcher.SetBlendState(_screenLessBlendState.Value);

                    DrawStatic
                    (
                        batcher,
                        AnimationGraphic,
                        posX,
                        posY,
                        ref HueVector
                    );

                    batcher.SetBlendState(null);

                    break;

                case GraphicEffectBlendMode.NormalHalfTransparent:
                    batcher.SetBlendState(_normalHalfBlendState.Value);

                    DrawStatic
                    (
                        batcher,
                        AnimationGraphic,
                        posX,
                        posY,
                        ref HueVector
                    );

                    batcher.SetBlendState(null);

                    break;

                case GraphicEffectBlendMode.ShadowBlue:
                    batcher.SetBlendState(_shadowBlueBlendState.Value);

                    DrawStatic
                    (
                        batcher,
                        AnimationGraphic,
                        posX,
                        posY,
                        ref HueVector
                    );

                    batcher.SetBlendState(null);

                    break;

                default:
                    //if (Graphic == 0x36BD)
                    //{
                    //    ResetHueVector();
                    //    HueVector.X = 0;
                    //    HueVector.Y = ShaderHueTranslator.SHADER_LIGHTS;
                    //    HueVector.Z = 0;
                    //    batcher.SetBlendState(BlendState.Additive);
                    //    base.Draw(batcher, posX, posY);
                    //    batcher.SetBlendState(null);
                    //}
                    //else

                    DrawStatic
                    (
                        batcher,
                        AnimationGraphic,
                        posX,
                        posY,
                        ref HueVector
                    );

                    break;
            }

            //Engine.DebugInfo.EffectsRendered++;


            if (data.IsLight && Source != null)
            {
                Client.Game.GetScene<GameScene>().AddLight(Source, Source, posX + 22, posY + 22);
            }

            return true;
        }
    }
}