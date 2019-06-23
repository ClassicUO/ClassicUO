#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//      
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using System;

using ClassicUO.Game.Data;
using ClassicUO.Game.Scenes;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.GameObjects
{
    internal sealed partial class AnimatedItemEffect
    {
        private static readonly Lazy<BlendState> _multiplyBlendState = new Lazy<BlendState>(() =>
        {
            BlendState state = new BlendState
            {
                ColorSourceBlend = Microsoft.Xna.Framework.Graphics.Blend.Zero, ColorDestinationBlend = Microsoft.Xna.Framework.Graphics.Blend.SourceColor
            };

            return state;
        });

        private static readonly Lazy<BlendState> _screenBlendState = new Lazy<BlendState>(() =>
        {
            BlendState state = new BlendState
            {
                ColorSourceBlend = Microsoft.Xna.Framework.Graphics.Blend.One, ColorDestinationBlend = Microsoft.Xna.Framework.Graphics.Blend.One
            };

            return state;
        });

        private static readonly Lazy<BlendState> _screenLessBlendState = new Lazy<BlendState>(() =>
        {
            BlendState state = new BlendState
            {
                ColorSourceBlend = Microsoft.Xna.Framework.Graphics.Blend.DestinationColor, ColorDestinationBlend = Microsoft.Xna.Framework.Graphics.Blend.InverseSourceAlpha
            };

            return state;
        });

        private static readonly Lazy<BlendState> _normalHalfBlendState = new Lazy<BlendState>(() =>
        {
            BlendState state = new BlendState
            {
                ColorSourceBlend = Microsoft.Xna.Framework.Graphics.Blend.DestinationColor, ColorDestinationBlend = Microsoft.Xna.Framework.Graphics.Blend.SourceColor
            };

            return state;
        });

        private static readonly Lazy<BlendState> _shadowBlueBlendState = new Lazy<BlendState>(() =>
        {
            BlendState state = new BlendState
            {
                ColorSourceBlend = Microsoft.Xna.Framework.Graphics.Blend.SourceColor, ColorDestinationBlend = Microsoft.Xna.Framework.Graphics.Blend.InverseSourceColor, ColorBlendFunction = BlendFunction.ReverseSubtract
            };

            return state;
        });
        private Graphic _displayedGraphic = Graphic.INVALID;

        public override bool Draw(UltimaBatcher2D batcher, int posX, int posY)
        {
            if (IsDestroyed)
                return false;

            if (AnimationGraphic == Graphic.INVALID)
                return false;

            ResetHueVector();

            ushort hue = Hue;

            if ((AnimationGraphic != _displayedGraphic || Texture == null || Texture.IsDisposed) && AnimationGraphic != Graphic.INVALID)
            {
                _displayedGraphic = AnimationGraphic;
                Texture = FileManager.Art.GetTexture(AnimationGraphic);

                if (Source != null)
                    Source.Texture = Texture;
                Bounds = new Rectangle((Texture.Width >> 1) - 22, Texture.Height - 44, Texture.Width, Texture.Height);
            }

            if (Texture != null)
            {
                Bounds.X = (Texture.Width >> 1) - 22 - (int) Offset.X;
                Bounds.Y = Texture.Height - 44 + (int) (Offset.Z - Offset.Y);
            }

            ref readonly StaticTiles data = ref FileManager.TileData.StaticData[Graphic];

            bool isPartial = data.IsPartialHue;
            bool isTransparent = data.IsTranslucent;


            if (Engine.Profile.Current.HighlightGameObjects && SelectedObject.LastObject == this)
            {
                HueVector.X = 0x0023;
                HueVector.Y = 1;
            }
            else if (Engine.Profile.Current.NoColorObjectsOutOfRange && Distance > World.ClientViewRange)
            {
                HueVector.X = Constants.OUT_RANGE_COLOR;
                HueVector.Y = 1;
            }
            else if (World.Player.IsDead && Engine.Profile.Current.EnableBlackWhiteEffect)
            {
                HueVector.X = Constants.DEAD_RANGE_COLOR;
                HueVector.Y = 1;
            }
            else
                ShaderHuesTraslator.GetHueVector(ref HueVector, hue, isPartial, isTransparent ? .5f : 0);

            switch (Blend)
            {
                case GraphicEffectBlendMode.Multiply:
                    batcher.SetBlendState(_multiplyBlendState.Value);
                    base.Draw(batcher, posX, posY);
                    batcher.SetBlendState(null);

                    break;

                case GraphicEffectBlendMode.Screen:
                case GraphicEffectBlendMode.ScreenMore:
                    batcher.SetBlendState(_screenBlendState.Value);
                    base.Draw(batcher, posX, posY);
                    batcher.SetBlendState(null);

                    break;

                case GraphicEffectBlendMode.ScreenLess:
                    batcher.SetBlendState(_screenLessBlendState.Value);
                    base.Draw(batcher, posX, posY);
                    batcher.SetBlendState(null);

                    break;

                case GraphicEffectBlendMode.NormalHalfTransparent:
                    batcher.SetBlendState(_normalHalfBlendState.Value);
                    base.Draw(batcher, posX, posY);
                    batcher.SetBlendState(null);

                    break;

                case GraphicEffectBlendMode.ShadowBlue:
                    batcher.SetBlendState(_shadowBlueBlendState.Value);
                    base.Draw(batcher, posX, posY);
                    batcher.SetBlendState(null);

                    break;

                default:
                    base.Draw(batcher, posX, posY);

                    break;
            }

            Engine.DebugInfo.EffectsRendered++;


            if (data.IsLight && (Source is Static || Source is Multi))
            {
                Engine.SceneManager.GetScene<GameScene>()
                      .AddLight(Source, Source, posX + 22, posY + 22);
            }

            return true;
        }

        public override void Select(int x, int y)
        {
            if (SelectedObject.Object == this)
                return;

            if (SelectedObject.IsPointInStatic(Graphic, x - Bounds.X, y - Bounds.Y))
                SelectedObject.Object = this;
        }
    }
}