using ClassicUO.Game.WorldObjects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Renderer.Views
{
    public class StaticView : View
    {
        public StaticView(in Static st) : base(st)
        {
            AllowedToDraw = !IsNoDrawable(st.Graphic);
        }

        public new Static WorldObject => (Static) base.WorldObject;

        public override bool Draw(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            if (!AllowedToDraw)
                return false;

            if (Texture == null || Texture.IsDisposed)
            {
                Texture = TextureManager.GetOrCreateStaticTexture(WorldObject.Graphic);
                Bounds = new Rectangle(Texture.Width / 2 - 22, Texture.Height - 44 + WorldObject.Position.Z * 4,
                    Texture.Width, Texture.Height);

                //if (AssetsLoader.TileData.IsFoliage((long)WorldObject.ItemData.Flags))
                //    HueVector = RenderExtentions.GetHueVector(0, false, true, false);
            }

            //var vv = position;
            //vv.Z = WorldObject.Position.Z;

            //if (AssetsLoader.TileData.IsBackground((long)WorldObject.ItemData.Flags) &&
            //    AssetsLoader.TileData.IsSurface((long)WorldObject.ItemData.Flags))
            //    vv.Z += 4;
            //else if (AssetsLoader.TileData.IsBackground((long)WorldObject.ItemData.Flags))
            //    vv.Z += 2;
            //else if (AssetsLoader.TileData.IsSurface((long)WorldObject.ItemData.Flags))
            //    vv.Z += 5;
            //else
            //    vv.Z += 6;

            //CalculateRenderDepth((sbyte)vv.Z, 10, WorldObject.ItemData.Height, (byte)WorldObject.Index);

            return base.Draw(spriteBatch, position);
        }
    }
}