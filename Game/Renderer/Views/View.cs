using System;
using ClassicUO.AssetsLoader;
using ClassicUO.Game.Map;
using ClassicUO.Game.WorldObjects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Renderer.Views
{
    public abstract class View : IDisposable
    {
        protected static float PI = (float) Math.PI;


        protected View(in WorldObject parent)
        {
            WorldObject = parent;
            AllowedToDraw = true;
            SortZ = parent.Position.Z;
        }

        public WorldObject WorldObject { get; }
        public bool AllowedToDraw { get; set; }
        public sbyte SortZ { get; protected set; }

        public SpriteTexture Texture { get; set; }
        protected Rectangle Bounds { get; set; }
        protected Vector3 HueVector { get; set; }
        protected bool HasShadow { get; set; }
        protected bool IsFlipped { get; set; }
        protected float Rotation { get; set; }

        protected int TextureWidth { get; set; } = 1;

        protected TextRenderer Text { get; } = new TextRenderer
        {
            Color = 33,
            IsUnicode = false
        };

        public ulong DepthValue { get; private set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual void Update(in double frameMS)
        {
        }

        protected void PreDraw(in Vector3 position)
        {
            Tile tile;
            Direction check;

            int offset = (int) Math.Ceiling(TextureWidth / 44f) / 2;
            //if (offset < 1)
            offset = 1;

            if (WorldObject is Mobile mobile && mobile.IsWalking)
            {
                Direction dir = mobile.Direction;

                if ((dir & Direction.Up) == Direction.Left || (dir & Direction.Up) == Direction.South || (dir & Direction.Up) == Direction.East)
                {
                    tile = World.Map.GetTile(WorldObject.Position.X, WorldObject.Position.Y + offset);
                    check = dir & Direction.Up;
                }
                else if ((dir & Direction.Up) == Direction.Down)
                {
                    tile = World.Map.GetTile(WorldObject.Position.X + offset, WorldObject.Position.Y + offset);
                    check = Direction.Down;
                }
                else
                {
                    tile = World.Map.GetTile(WorldObject.Position.X + offset, WorldObject.Position.Y);
                    check = Direction.East;
                }
            }
            else
            {
                tile = World.Map.GetTile(WorldObject.Position.X, WorldObject.Position.Y + 1);
                check = Direction.South;
            }

            if (tile != null)
            {
                if (WorldObject is Mobile mob)
                {
                    sbyte z = (sbyte) Pathfinder.GetNextZ(mob, mob.Position, check);
                    DeferredEntity deferred = new DeferredEntity(mob, position, z, "MOBILE DEF");
                    tile.AddWorldObject(deferred);
                }
                else
                {
                    DeferredEntity deferred = new DeferredEntity(WorldObject, position, WorldObject.Position.Z, "ITEM DEF");
                    tile.AddWorldObject(deferred);
                }
            }
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

        protected void CalculateRenderDepth(in sbyte z, in byte priority, in byte byte7, in byte byte8)
        {
            ulong tmp = 0;
            tmp |= (ulong) ((WorldObject.Position.X + WorldObject.Position.Y) & 0xFFFF);
            tmp <<= 8;
            byte tmpZ = (byte) ((z + 128) & 0xFF);
            tmp |= tmpZ;
            tmp <<= 8;
            tmp |= (ulong) (priority & 0xFF);
            tmp <<= 8;
            tmp |= (ulong) (byte7 & 0xFF);
            tmp <<= 8;
            tmp |= (ulong) (byte8 & 0xFF);

            DepthValue = tmp;
        }

        protected virtual void MousePick(in SpriteVertex[] vertex)
        {
        }

        protected virtual void MessageOverHead(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            Text.Text = $"SortZ: {SortZ}";
            Text.GenerateTexture(0, 0, TEXT_ALIGN_TYPE.TS_CENTER, 0);
        }


        ~View()
        {
            Dispose();
        }

        protected virtual void Dispose(in bool disposing)
        {
            if (disposing)
                if (Texture != null && Texture.IsDisposed) // disping happen into TextureManager.cs, here we clean up the referement
                {
                    Texture.Dispose();
                    Texture = null;
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