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

namespace ClassicUO.Game.Views
{
    internal abstract class View : IDrawable
    {
        protected static float PI = (float) Math.PI;
        private Vector3 _storedHue;
        public Rectangle Bounds;
        public Rectangle FrameInfo;

        private float _processAlpha = 1;
        private long _processAlphaTime = Constants.ALPHA_OBJECT_TIME;

        protected View(GameObject parent)
        {
            GameObject = parent;
            AllowedToDraw = true;
        }

        protected virtual bool CanProcessAlpha => true;

        public GameObject GameObject { get; }

        protected bool HasShadow { get; set; }

        protected bool IsFlipped { get; set; }

        protected float Rotation { get; set; }

        public bool IsSelected { get; set; }

        public Vector3 HueVector;

        public bool AllowedToDraw { get; set; }

        public SpriteTexture Texture { get; set; }

        public Rectangle GetOnScreenRectangle()
        {
            Rectangle prect = Rectangle.Empty;

            prect.X = (int) (GameObject.RealScreenPosition.X - FrameInfo.X + 22 + GameObject.Offset.X);
            prect.Y = (int) (GameObject.RealScreenPosition.Y - FrameInfo.Y + 22 + (GameObject.Offset.Y - GameObject.Offset.Z));
            prect.Width = FrameInfo.Width;
            prect.Height = FrameInfo.Height;

            return prect;
        }

        public virtual unsafe bool Draw(Batcher2D batcher, Vector3 position, MouseOverList list)
        {
            if (Texture == null || Texture.IsDisposed || !AllowedToDraw || GameObject.IsDisposed) return false;
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

                fixed (SpriteVertex* ptr = vertex)
                {
                    ptr[0].Position = center;
                    ptr[0].Position.X += cosx - -siny;
                    ptr[0].Position.Y -= sinx + -cosy;
                    ptr[1].Position = center;
                    ptr[1].Position.X += cosx - siny;
                    ptr[1].Position.Y += -sinx + -cosy;
                    ptr[2].Position = center;
                    ptr[2].Position.X += -cosx - -siny;
                    ptr[2].Position.Y += sinx + cosy;
                    ptr[3].Position = center;
                    ptr[3].Position.X += -cosx - siny;
                    ptr[3].Position.Y += sinx + -cosy;
                }
            }
            else if (IsFlipped)
            {
                vertex = SpriteVertex.PolyBufferFlipped;

                fixed (SpriteVertex* ptr = vertex)
                {
                    ptr[0].Position = position;
                    ptr[0].Position.X += Bounds.X + 44f;
                    ptr[0].Position.Y -= Bounds.Y;
                    ptr[0].TextureCoordinate.Y = 0;
                    ptr[1].Position = vertex[0].Position;
                    ptr[1].Position.Y += Bounds.Height;
                    ptr[2].Position = vertex[0].Position;
                    ptr[2].Position.X -= Bounds.Width;
                    ptr[2].TextureCoordinate.Y = 0;
                    ptr[3].Position = vertex[1].Position;
                    ptr[3].Position.X -= Bounds.Width;
                }
            }
            else
            {
                vertex = SpriteVertex.PolyBuffer;

                fixed (SpriteVertex* ptr = vertex)
                {
                    ptr[0].Position = position;
                    ptr[0].Position.X -= Bounds.X;
                    ptr[0].Position.Y -= Bounds.Y;
                    ptr[0].TextureCoordinate.Y = 0;
                    ptr[1].Position = vertex[0].Position;
                    ptr[1].Position.X += Bounds.Width;
                    ptr[1].TextureCoordinate.Y = 0;
                    ptr[2].Position = vertex[0].Position;
                    ptr[2].Position.Y += Bounds.Height;
                    ptr[3].Position = vertex[1].Position;
                    ptr[3].Position.Y += Bounds.Height;
                }
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


            //if (ProcessInitialAlpha)
            {
                {
                    if (CanProcessAlpha)
                    {
                        _processAlphaTime -= Engine.TicksFrame;

                        if (_processAlphaTime > 0)
                        {
                            _processAlpha = _processAlphaTime / Constants.ALPHA_OBJECT_VALUE;
                        }
                        else if (_processAlphaTime < 0)
                        {
                            _processAlpha = 0;
                        }

                        if (HueVector.Z < _processAlpha)
                            HueVector.Z = _processAlpha;
                    }

                    
                }
            }

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
            if (GameObject.Overheads != null)
            {
                for (int i = 0; i < GameObject.Overheads.Count; i++)
                {
                    View v = GameObject.Overheads[i].View;
                    v.Bounds.X = (v.Texture.Width >> 1) - 22;
                    v.Bounds.Y = offY + v.Texture.Height;
                    v.Bounds.Width = v.Texture.Width;
                    v.Bounds.Height = v.Texture.Height;
                    Engine.SceneManager.GetScene<GameScene>().Overheads.AddOverhead(GameObject.Overheads[i], position);
                    offY += v.Texture.Height;
                }
            }
        } 
    }
}