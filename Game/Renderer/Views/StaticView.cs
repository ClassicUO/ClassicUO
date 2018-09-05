using ClassicUO.Game.GameObjects;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Renderer.Views
{
    public class StaticView : View
    {
        public StaticView(Static st) : base(st)
        {
            AllowedToDraw = !IsNoDrawable(st.Graphic);
        }

        public new Static GameObject => (Static)base.GameObject;

        public override bool Draw(SpriteBatch3D spriteBatch,  Vector3 position)
        {
            if (!AllowedToDraw || GameObject.IsDisposed)
            {
                return false;
            }

            if (Texture == null || Texture.IsDisposed)
            {
                Texture = TextureManager.GetOrCreateStaticTexture(GameObject.Graphic);
                Bounds = new Rectangle(Texture.Width / 2 - 22, Texture.Height - 44 + GameObject.Position.Z * 4, Texture.Width, Texture.Height);

                //if (AssetsLoader.TileData.IsFoliage((long)GameObject.ItemData.Flags))
                //    HueVector = RenderExtentions.GetHueVector(0, false, true, false);
            }

            //var vv = position;
            //vv.Z = position.X + position.Y;

            //if (AssetsLoader.TileData.IsBackground((long)GameObject.ItemData.Flags) &&
            //    AssetsLoader.TileData.IsSurface((long)GameObject.ItemData.Flags))
            //    vv.Z += 0.001f * (GameObject.IsometricPosition.Z + 0.4f);
            //else if (AssetsLoader.TileData.IsBackground((long)GameObject.ItemData.Flags))
            //    vv.Z += 0.001f * (GameObject.IsometricPosition.Z + 0.2f);
            //else if (AssetsLoader.TileData.IsSurface((long)GameObject.ItemData.Flags))
            //    vv.Z += 0.001f * (GameObject.IsometricPosition.Z + 0.5f);
            //else
            //    vv.Z += 0.001f * (GameObject.IsometricPosition.Z + 0.6f);

            //CalculateRenderDepth((sbyte)vv.Z, 10, GameObject.ItemData.Height, (byte)GameObject.Index);

            return base.Draw(spriteBatch, position);
        }
    }
}