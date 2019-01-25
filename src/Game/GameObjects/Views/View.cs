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
using System.Diagnostics;
using System.Runtime.CompilerServices;

using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.Interfaces;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using IDrawable = ClassicUO.Interfaces.IDrawable;

namespace ClassicUO.Game.GameObjects
{
    internal abstract partial class GameObject : IDrawable
    {
        protected static float PI = (float) Math.PI;
        private Vector3 _storedHue;
        public Rectangle Bounds;
        public Rectangle FrameInfo;

        //private float _processAlpha = 1;
        //private long _processAlphaTime = -1;


        protected bool HasShadow { get; set; }

        protected bool IsFlipped { get; set; }

        protected float Rotation { get; set; }

        public bool IsSelected { get; set; }

        public Vector3 HueVector;

        public bool AllowedToDraw { get; set; } = true;

        public SpriteTexture Texture { get; set; }

        public Rectangle GetOnScreenRectangle()
        {
            Rectangle prect = Rectangle.Empty;

            prect.X = (int) (RealScreenPosition.X - FrameInfo.X + 22 + Offset.X);
            prect.Y = (int) (RealScreenPosition.Y - FrameInfo.Y + 22 + (Offset.Y - Offset.Z));
            prect.Width = FrameInfo.Width;
            prect.Height = FrameInfo.Height;

            return prect;
        }

        public byte AlphaHue { get; set; }

        public bool ProcessAlpha(int max)
        {
            bool result = false;

            int alpha = (int) AlphaHue;

            if (alpha > max)
            {
                alpha -= 25;

                if (alpha < max)
                    alpha = max;

                result = true;
            }
            else if (alpha < max)
            {
                alpha += 25;

                if (alpha > max)
                    alpha = max;

                result = true;
            }

            AlphaHue = (byte) alpha;
            return result;
        }

        public virtual bool Draw(Batcher2D batcher, Vector3 position, MouseOverList list)
        {
            //if (Texture == null || Texture.IsDisposed || !AllowedToDraw || GameObject.IsDisposed) return false;
            Texture.Ticks = Engine.Ticks;
            SpriteVertex[] vertex;

            if (Rotation != 0.0f)
            {
                float w = Bounds.Width / 2f;
                float h = Bounds.Height / 2f;
                Vector3 center = position - new Vector3(Bounds.X - 44 + w, Bounds.Y + h, 0);
                float sinx = (float) Math.Sin(Rotation) * w;
                float cosx = (float) Math.Cos(Rotation) * w;
                float siny = (float) Math.Sin(Rotation) * h;
                float cosy = (float) Math.Cos(Rotation) * h;

                vertex = SpriteVertex.PolyBufferFlipped;            
                vertex[0].Position = center;
                vertex[0].Position.X += cosx - -siny;
                vertex[0].Position.Y -= sinx + -cosy;
                vertex[1].Position = center;
                vertex[1].Position.X += cosx - siny;
                vertex[1].Position.Y += -sinx + -cosy;
                vertex[2].Position = center;
                vertex[2].Position.X += -cosx - -siny;
                vertex[2].Position.Y += sinx + cosy;
                vertex[3].Position = center;
                vertex[3].Position.X += -cosx - siny;
                vertex[3].Position.Y += sinx + -cosy;                
            }
            else if (IsFlipped)
            {
                vertex = SpriteVertex.PolyBufferFlipped;
                vertex[0].Position = position;
                vertex[0].Position.X += Bounds.X + 44f;
                vertex[0].Position.Y -= Bounds.Y;
                vertex[0].TextureCoordinate.Y = 0;
                vertex[1].Position = vertex[0].Position;
                vertex[1].Position.Y += Bounds.Height;
                vertex[2].Position = vertex[0].Position;
                vertex[2].Position.X -= Bounds.Width;
                vertex[2].TextureCoordinate.Y = 0;
                vertex[3].Position = vertex[1].Position;
                vertex[3].Position.X -= Bounds.Width;              
            }
            else
            {
                vertex = SpriteVertex.PolyBuffer;
                vertex[0].Position = position;
                vertex[0].Position.X -= Bounds.X;
                vertex[0].Position.Y -= Bounds.Y;
                vertex[0].TextureCoordinate.Y = 0;
                vertex[1].Position = vertex[0].Position;
                vertex[1].Position.X += Bounds.Width;
                vertex[1].TextureCoordinate.Y = 0;
                vertex[2].Position = vertex[0].Position;
                vertex[2].Position.Y += Bounds.Height;
                vertex[3].Position = vertex[1].Position;
                vertex[3].Position.Y += Bounds.Height;               
            }

            if (Engine.Profile.Current.HighlightGameObjects)
            {
                if (IsSelected)
                {
                    if (_storedHue == Vector3.Zero)
                        _storedHue = HueVector;
                    HueVector = ShaderHuesTraslator.SelectedHue;
                }
                else if (_storedHue != Vector3.Zero)
                {
                    HueVector = _storedHue;
                    _storedHue = Vector3.Zero;
                }
            }


            //if (CanProcessAlpha)
            //{
            //    long ticks = Engine.Ticks;

            //    if (_processAlphaTime == -1)
            //        _processAlphaTime = ticks + Constants.ALPHA_OBJECT_TIME;
            //    else
            //        ticks -= Constants.ALPHA_OBJECT_TIME;

            //    if (_processAlphaTime < ticks) // finished!
            //    {
            //        _processAlpha = _isAlphaReverse ? 1 : 0;
            //        CanProcessAlpha = false;
            //        _isAlphaReverse = !_isAlphaReverse;
            //    }
            //    else
            //    {
            //        if (_isAlphaReverse)
            //            _processAlpha = 1.0f - ((_processAlphaTime - Engine.Ticks) / (float)Constants.ALPHA_OBJECT_TIME);
            //        else
            //            _processAlpha = ((_processAlphaTime - Engine.Ticks) / (float) Constants.ALPHA_OBJECT_TIME);
            //    }

            //    if (HueVector.Z < _processAlpha)
            //        HueVector.Z = _processAlpha;
            //}


            //if (HueVector.Z != Alpha)
            HueVector.Z = 1f - (AlphaHue / 255f);

            if (vertex[0].Hue != HueVector)
                vertex[0].Hue = vertex[1].Hue = vertex[2].Hue = vertex[3].Hue = HueVector;

            //if (HasShadow)
            //{
            //    SpriteVertex[] vertexS = new SpriteVertex[4]
            //    {
            //        vertex[0],
            //        vertex[1],
            //        vertex[2],
            //        vertex[3]
            //    };

            //    batcher.DrawShadow(Texture, vertexS, new Vector2(position.X + 22, position.Y + GameObject.Offset.Y - GameObject.Offset.Z + 22), IsFlipped, ShadowZDepth);
            //}

            if (!batcher.DrawSprite(Texture, vertex))
                return false;

            MousePick(list, vertex);

            return true;
        }

        protected virtual void MousePick(MouseOverList list, SpriteVertex[] vertex)
        {
        }

        protected virtual void MessageOverHead(Batcher2D batcher, Vector3 position, int offY)
        {
            if (Overheads != null)
            {
                for (int i = 0; i < Overheads.Count; i++)
                {
                    TextOverhead v = Overheads[i];
                    v.Bounds.X = (v.Texture.Width >> 1) - 22;
                    v.Bounds.Y = offY + v.Texture.Height;
                    v.Bounds.Width = v.Texture.Width;
                    v.Bounds.Height = v.Texture.Height;
                    Engine.SceneManager.GetScene<GameScene>().Overheads.AddOverhead(Overheads[i], position);
                    offY += v.Texture.Height;
                }
            }
        } 
    }
}