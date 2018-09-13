#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
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
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Map;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using IDrawable = ClassicUO.Renderer.IDrawable;

namespace ClassicUO.Game.Views
{
    public abstract class View : IDrawable
    {
        protected static float PI = (float)Math.PI;


        protected View(GameObject parent)
        {
            GameObject = parent;
            AllowedToDraw = true;
            SortZ = parent.Position.Z;
        }

        public GameObject GameObject { get; }
        public bool AllowedToDraw { get; set; }
        public sbyte SortZ { get; protected set; }

        public SpriteTexture Texture { get; set; }
        protected Rectangle Bounds { get; set; }
        public Vector3 HueVector { get; set; }
        protected bool HasShadow { get; set; }
        protected bool IsFlipped { get; set; }
        protected float Rotation { get; set; }
        protected int TextureWidth { get; set; } = 1;

        public bool IsSelected { get; set; }


        protected bool PreDraw(Vector3 position)
        {
            if (GameObject is IDeferreable deferreable)
            {
                Tile tile;
                Direction check;

                if (GameObject is Mobile mobile && mobile.IsMoving)
                {
                    Direction dir = mobile.Direction;

                    if (( dir & Direction.Up ) == Direction.Left || ( dir & Direction.Up ) == Direction.South || ( dir & Direction.Up ) == Direction.East)
                    {
                        tile = World.Map.GetTile(GameObject.Position.X, GameObject.Position.Y + 1);
                        check = dir & Direction.Up;
                    }
                    else if (( dir & Direction.Up ) == Direction.Down)
                    {
                        tile = World.Map.GetTile(GameObject.Position.X + 1, GameObject.Position.Y + 1);
                        check = Direction.Down;
                    }
                    else
                    {
                        tile = World.Map.GetTile(GameObject.Position.X + 1, GameObject.Position.Y);
                        check = Direction.East;
                    }
                }
                else
                {
                    tile = World.Map.GetTile(GameObject.Position.X, GameObject.Position.Y + 1);
                    check = Direction.South;
                }

                if (tile != null)
                {
                    if (deferreable.DeferredObject == null)
                    {
                        deferreable.DeferredObject = new DeferredEntity();
                    }
                    else
                    {
                        deferreable.DeferredObject.Reset();
                    }

                    deferreable.DeferredObject.AtPosition = position;
                    deferreable.DeferredObject.Entity = GameObject;
                    deferreable.DeferredObject.AssociatedTile = tile;
                    deferreable.DeferredObject.Map = World.Map;

                    if (GameObject is Mobile mob)
                    {
                        if (!Pathfinder.TryGetNextZ(mob, mob.Position, check, out sbyte z))
                        {
                            return false;
                        }

                        deferreable.DeferredObject.Z = z;
                        deferreable.DeferredObject.Position = new Position(0xFFFF, 0xFFFF, z);
                    }
                    else
                    {
                        deferreable.DeferredObject.Z = GameObject.Position.Z;
                        deferreable.DeferredObject.Position = new Position(0xFFFF, 0xFFFF, GameObject.Position.Z);
                    }

                    tile.AddGameObject(deferreable.DeferredObject);

                    return true;
                }
            }

            return false;
        }

        public virtual bool Draw(SpriteBatch3D spriteBatch, Vector3 position)
        {
            if (Texture == null || Texture.IsDisposed || !AllowedToDraw || GameObject.IsDisposed)
            {
                return false;
            }

            Texture.Ticks = World.Ticks;

            SpriteVertex[] vertex;

            if (Rotation != 0)
            {
                float w = Bounds.Width / 2f;
                float h = Bounds.Height / 2f;
                Vector3 center = position - new Vector3(Bounds.X - 44 + w, Bounds.Y + h, 0);
                float sinx = (float)Math.Sin(Rotation) * w;
                float cosx = (float)Math.Cos(Rotation) * w;
                float siny = (float)Math.Sin(Rotation) * h;
                float cosy = (float)Math.Cos(Rotation) * h;

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

            /*var pos = Service.Get<InputManager>().MousePosition;

            int x = pos.X - (int)vertex[0].Position.X;
            int y = pos.Y - (int)vertex[0].Position.Y;


            if (Art.Contains(GameObject.Graphic, x, y))
            {
                if (_selected != GameObject && _selected != null)
                    _selected.View.HueVector = RenderExtentions.GetHueVector(_selected.Hue);
                _selected = GameObject;
                HueVector = RenderExtentions.GetHueVector(33);
            }
            else
            {
                HueVector = RenderExtentions.GetHueVector(GameObject.Hue);
            }*/

            if (vertex[0].Hue != HueVector)
                vertex[0].Hue = vertex[1].Hue = vertex[2].Hue = vertex[3].Hue = HueVector;



            if (!spriteBatch.DrawSprite(Texture, vertex))
                return false;

            //MousePick(vertex);

            return true;
        }


        private static readonly GameObject _selected;

        public virtual bool DrawInternal(SpriteBatch3D spriteBatch, Vector3 position)
        {
            return false;
        }


        protected virtual void MousePick(SpriteVertex[] vertex)
        {
        }

        protected virtual void MessageOverHead(SpriteBatch3D spriteBatch, Vector3 position, int offY)
        {
            for (int i = 0; i < GameObject.OverHeads.Count; i++)
            {
                var v = GameObject.OverHeads[i].View;
                v.Bounds = new Rectangle(v.Texture.Width / 2 - 22, offY + v.Texture.Height, v.Texture.Width, v.Texture.Height);
                GameTextManager.AddView(v, position);
                offY += v.Texture.Height;
            }
        }

        public static bool IsNoDrawable(ushort g)
        {
            switch (g)
            {
                case 0x0001:
                case 0x21BC:
                case 0x9E4C:
                case 0x9E64:
                case 0x9E65:
                case 0x9E7D:
                    return true;
            }

            if (g != 0x63D3)
            {
                if (g >= 0x2198 && g <= 0x21A4)
                {
                    return true;
                }

                long flags = (long)TileData.StaticData[g].Flags;

                if (!TileData.IsNoDiagonal(flags) || TileData.IsAnimated(flags) && World.Player != null && World.Player.Race == RaceType.GARGOYLE)
                {
                    return false;
                }
            }

            return true;
        }
    }
}