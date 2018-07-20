using System;
using System.Collections.Generic;
using System.Text;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.WorldObjects.Views
{
    public class ItemView : View
    {
        private Graphic _originalGraphic;
        private Hue _hue;

        public ItemView(in Item item) : base(item)
        {
            if (AssetsLoader.TileData.IsWet((long)AssetsLoader.TileData.StaticData[item.Graphic].Flags))
                SortZ++;
        }

        public new Item WorldObject => (Item)base.WorldObject;


        public override bool Draw(in SpriteBatch3D spriteBatch, in Vector3 position)
        {
            if (!AllowedToDraw)
                return false;


            if (_originalGraphic != WorldObject.DisplayedGraphic)
            {
                _originalGraphic = WorldObject.DisplayedGraphic;
                Texture = TextureManager.GetOrCreateStaticTexture(_originalGraphic);
                Bounds = new Rectangle(Texture.Width / 2 - 22, Texture.Height - 44 + (WorldObject.Position.Z * 4), Texture.Width, Texture.Height);
            }

            if (_hue != WorldObject.Hue)
            {
                _hue = WorldObject.Hue;
                HueVector = RenderExtentions.GetHueVector(_hue, AssetsLoader.TileData.IsPartialHue((long)WorldObject.ItemData.Flags), false, false);
            }


            if (WorldObject.Amount > 1 &&  AssetsLoader.TileData.IsStackable((long)WorldObject.ItemData.Flags) && WorldObject.DisplayedGraphic == WorldObject.Graphic)
            {
                Vector3 offsetDrawPosition = new Vector3(position.X - 5, position.Y - 5, 0);
                base.Draw(spriteBatch, offsetDrawPosition);
            }

            return base.Draw(spriteBatch, position);
        }

        protected override void MousePick(in SpriteVertex[] vertex)
        {
            base.MousePick(vertex);
        }
    }
}
