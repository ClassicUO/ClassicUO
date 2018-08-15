using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.AssetsLoader;
using ClassicUO.Game.Map;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.GameObjects.Interfaces;
using Microsoft.Xna.Framework;
using IDrawable = ClassicUO.Game.GameObjects.Interfaces.IDrawable;
using IUpdateable = ClassicUO.Game.GameObjects.Interfaces.IUpdateable;

namespace ClassicUO.Game.Renderer.Views
{
    public abstract class View : IDrawable, IUpdateable
    {
        protected static float PI = (float) Math.PI;


        protected View(in GameObject parent)
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


        public virtual void Update(in double frameMS)
        {
            if (GameObject.IsDisposed)
                return;

            for (int i = 0; i < GameObject.OverHeads.Count; i++)
            {
                var gt = GameObject.OverHeads[i];

                gt.View.Update(frameMS);

                if (gt.IsDisposed)
                {
                    GameObject.RemoveGameTextAt(i);
                    i--;
                }
            }
        }


        protected bool PreDraw(in Vector3 position)
        {
            if (GameObject is IDeferreable deferreable)
            {
                Tile tile;
                Direction check;

                //int offset = (int)Math.Ceiling(TextureWidth / 44f) / 2;
                //if (offset < 1)
                const int offset = 1;

                if (GameObject is Mobile mobile && mobile.IsWalking)
                {
                    Direction dir = mobile.Direction;

                    if ((dir & Direction.Up) == Direction.Left || (dir & Direction.Up) == Direction.South || (dir & Direction.Up) == Direction.East)
                    {
                        tile = World.Map.GetTile(GameObject.Position.X, GameObject.Position.Y + offset);
                        check = dir & Direction.Up;
                    }
                    else if ((dir & Direction.Up) == Direction.Down)
                    {
                        tile = World.Map.GetTile(GameObject.Position.X + offset, GameObject.Position.Y + offset);
                        check = Direction.Down;
                    }
                    else
                    {
                        tile = World.Map.GetTile(GameObject.Position.X + offset, GameObject.Position.Y);
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
                        deferreable.DeferredObject = new DeferredEntity();
                    else
                        deferreable.DeferredObject.Reset();

                    deferreable.DeferredObject.AtPosition = position;
                    deferreable.DeferredObject.Entity = GameObject;
                    deferreable.DeferredObject.AssociatedTile = tile;
                    deferreable.DeferredObject.Map = World.Map;

                    if (GameObject is Mobile mob)
                    {
                        if (!Pathfinder.TryGetNextZ(mob, mob.Position, check, out sbyte z))
                            return false;

                        deferreable.DeferredObject.Z = z;
                        deferreable.DeferredObject.Position = new Position(0xFFFF, 0xFFFF, z);
                    }
                    else
                    {
                        deferreable.DeferredObject.Z = GameObject.Position.Z;
                        deferreable.DeferredObject.Position = new Position(0xFFFF, 0xFFFF, GameObject.Position.Z);
                    }

                    tile.AddWorldObject(deferreable.DeferredObject);

                    return true;
                }
            }

            return false;
        }

        public virtual bool Draw(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            if (Texture == null || !AllowedToDraw)
                return false;

            Texture.Ticks = World.Ticks;

            SpriteVertex[] vertex;

            if (Rotation != 0)
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


            if (vertex[0].Hue != HueVector)
                vertex[0].Hue = vertex[1].Hue = vertex[2].Hue = vertex[3].Hue = HueVector;

            if (!spriteBatch.DrawSprite(Texture, vertex))
                return false;

            MousePick(vertex);

            return true;
        }

        public virtual bool DrawInternal(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            return false;
        }

       

        protected virtual void MousePick(in SpriteVertex[] vertex)
        {
        }

        protected virtual void MessageOverHead(in SpriteBatch3D spriteBatch, in Vector3 position, int offY)
        {
            for (int i = 0; i < GameObject.OverHeads.Count; i++)
            {
                var v = GameObject.OverHeads[i].View;
                v.Bounds = new Rectangle(v.Texture.Width / 2 - 22, offY + v.Texture.Height, v.Texture.Width, v.Texture.Height);
                GameTextRenderer.AddView(v, position);
                offY += v.Texture.Height;
            }
        }

        public static bool IsNoDrawable(in ushort g)
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
                    return true;

                long flags = (long) TileData.StaticData[g].Flags;

                if (!TileData.IsNoDiagonal(flags) || TileData.IsAnimated(flags) && World.Player != null && World.Player.Race == RaceType.GARGOYLE)
                    return false;
            }

            return true;
        }
    }
}