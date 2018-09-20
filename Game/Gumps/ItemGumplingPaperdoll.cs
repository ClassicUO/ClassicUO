using ClassicUO.Game.GameObjects;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.Gumps
{
    class ItemGumplingPaperdoll : ItemGumpling
    {
        private Hue _hueOverride;
        private ushort _gumpIndex;

        public ItemGumplingPaperdoll(int x, int y, Item item) : base(item)
        {
            X = x; Y = y;
            HighlightOnMouseOver = false;
        }

        public int SlotIndex { get; set; }
        public bool IsFemale { get; set; }



        public override bool Draw(SpriteBatchUI spriteBatch, Vector3 position, Vector3? hue = null)
        {
            if (Texture == null || Texture.IsDisposed)
            {
                _gumpIndex = (ushort)(Item.ItemData.AnimID + (IsFemale ? 60000 : 50000));
                Texture = IO.Resources.Gumps.GetGumpTexture(_gumpIndex);
                Width = Texture.Width;
                Height = Texture.Height;
            }

            spriteBatch.Draw2D(Texture, position, RenderExtentions.GetHueVector(Item.Hue));

            return base.Draw(spriteBatch, position, hue);
        }

        protected override bool Contains(int x, int y)
            => IO.Resources.Gumps.Contains(_gumpIndex, x, y);
        

    }
}
