#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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
using ClassicUO.Game.GameObjects;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.Views
{
    internal class AnimatedEffectView : View
    {
        private Graphic _displayedGraphic = Graphic.INVALID;

        public AnimatedEffectView(AnimatedItemEffect effect) : base(effect)
        {
        }

        private static readonly Lazy<BlendState> _multiplyBlendState = new Lazy<BlendState>(() =>
        {
            BlendState state = new BlendState();

            state.AlphaSourceBlend = state.ColorSourceBlend = Blend.Zero;
            state.AlphaDestinationBlend = state.ColorDestinationBlend = Blend.SourceColor;

            return state;
        });
  
        private static readonly Lazy<BlendState> _screenBlendState = new Lazy<BlendState>(() =>
        {
            BlendState state = new BlendState();

            state.AlphaSourceBlend = state.ColorSourceBlend = Blend.One;
            state.AlphaDestinationBlend = state.ColorDestinationBlend = Blend.One;

            return state;
        });

        private static readonly Lazy<BlendState> _screenLessBlendState = new Lazy<BlendState>(() =>
        {
            BlendState state = new BlendState();

            state.AlphaSourceBlend = state.ColorSourceBlend = Blend.DestinationColor;
            state.AlphaDestinationBlend = state.ColorDestinationBlend = Blend.InverseSourceAlpha;

            return state;
        });

        private static readonly Lazy<BlendState> _normalHalfBlendState = new Lazy<BlendState>(() =>
        {
            BlendState state = new BlendState();

            state.AlphaSourceBlend = state.ColorSourceBlend = Blend.DestinationColor;
            state.AlphaDestinationBlend = state.ColorDestinationBlend = Blend.SourceColor;

            return state;
        });

        private static readonly Lazy<BlendState> _shadowBlueBlendState = new Lazy<BlendState>(() =>
        {
            BlendState state = new BlendState();

            state.AlphaSourceBlend = state.ColorSourceBlend = Blend.SourceColor;
            state.AlphaDestinationBlend = state.ColorDestinationBlend = Blend.InverseSourceColor;
            state.AlphaBlendFunction = state.ColorBlendFunction = BlendFunction.ReverseSubtract;

            return state;
        });

        public override bool Draw(Batcher2D batcher, Vector3 position, MouseOverList objectList)
        {
            if (GameObject.IsDisposed)
                return false;
            AnimatedItemEffect effect = (AnimatedItemEffect)GameObject;

            if (effect.AnimationGraphic == Graphic.INVALID)
                return false;

            Hue hue = effect.Hue;

            if (effect.Source is Item)
            {
                if (Engine.Profile.Current.FieldsType == 1 && StaticFilters.IsField(effect.AnimationGraphic))
                {
                    effect.AnimIndex = 0;
                }
                else if (Engine.Profile.Current.FieldsType == 2)
                {
                    if (StaticFilters.IsFireField(effect.Graphic))
                    {
                        effect.AnimationGraphic = Constants.FIELD_REPLACE_GRAPHIC;
                        hue = 0x0020;
                    }
                    else if (StaticFilters.IsParalyzeField(effect.Graphic))
                    {
                        effect.AnimationGraphic = Constants.FIELD_REPLACE_GRAPHIC;
                        hue = 0x0058;
                    }
                    else if (StaticFilters.IsEnergyField(effect.Graphic))
                    {
                        effect.AnimationGraphic = Constants.FIELD_REPLACE_GRAPHIC;
                        hue = 0x0070;
                    } 
                    else if (StaticFilters.IsPoisonField(effect.Graphic))
                    {
                        effect.AnimationGraphic = Constants.FIELD_REPLACE_GRAPHIC;
                        hue = 0x0044;
                    }
                    else if (StaticFilters.IsWallOfStone(effect.Graphic))
                    {
                        effect.AnimationGraphic = Constants.FIELD_REPLACE_GRAPHIC;
                        hue = 0x038A;
                    }
                }
            }

            if ((effect.AnimationGraphic != _displayedGraphic || Texture == null || Texture.IsDisposed) && effect.AnimationGraphic != Graphic.INVALID)
            {
                _displayedGraphic = effect.AnimationGraphic;
                Texture = FileManager.Art.GetTexture(effect.AnimationGraphic);
                Bounds = new Rectangle((Texture.Width >> 1) - 22, Texture.Height - 44, Texture.Width, Texture.Height);
            }

            Bounds.X = (Texture.Width >> 1) - 22 - (int)effect.Offset.X;
            Bounds.Y = Texture.Height - 44 + (int)(effect.Offset.Z - effect.Offset.Y);

            StaticTiles data = FileManager.TileData.StaticData[_displayedGraphic];

            bool isPartial = data.IsPartialHue;
            bool isTransparent = data.IsTransparent;

            if (Engine.Profile.Current.NoColorObjectsOutOfRange && GameObject.Distance > World.ViewRange)
                HueVector = new Vector3(0x038E, 1, HueVector.Z);
            else
                HueVector = ShaderHuesTraslator.GetHueVector(hue, isPartial, isTransparent ? .5f : 0, false);

            switch (effect.Blend)
            {
                case GraphicEffectBlendMode.Multiply:
                    batcher.SetBlendState(_multiplyBlendState.Value);
                    base.Draw(batcher, position, objectList);
                    batcher.SetBlendState(null);
                    break;
                case GraphicEffectBlendMode.Screen:
                case GraphicEffectBlendMode.ScreenMore:
                    batcher.SetBlendState(_screenBlendState.Value);
                    base.Draw(batcher, position, objectList);
                    batcher.SetBlendState(null);
                    break;
                case GraphicEffectBlendMode.ScreenLess:
                    batcher.SetBlendState(_screenLessBlendState.Value);
                    base.Draw(batcher, position, objectList);
                    batcher.SetBlendState(null);
                    break;
                case GraphicEffectBlendMode.NormalHalfTransparent:
                    batcher.SetBlendState(_normalHalfBlendState.Value);
                    base.Draw(batcher, position, objectList);
                    batcher.SetBlendState(null);
                    break;
                case GraphicEffectBlendMode.ShadowBlue:
                    batcher.SetBlendState(_shadowBlueBlendState.Value);
                    base.Draw(batcher, position, objectList);
                    batcher.SetBlendState(null);
                    break;
                default:
                    base.Draw(batcher, position, objectList);
                    break;
            }

            Engine.DebugInfo.EffectsRendered++;

            return true;
        }

        protected override void MousePick(MouseOverList list, SpriteVertex[] vertex)
        {
            int x = list.MousePosition.X - (int) vertex[0].Position.X;
            int y = list.MousePosition.Y - (int) vertex[0].Position.Y;
            if (Texture.Contains(x, y))
                list.Add(GameObject, vertex[0].Position);
        }
    }
}