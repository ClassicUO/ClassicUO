using System;
using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class NearbyItems : Gump
    {
        public const int SIZE = 75;
        public static NearbyItems NearbyItemGump;
        public NearbyItems() : base(0, 0)
        {
            if (NearbyItemGump != null)
                NearbyItemGump.Dispose();

            NearbyItemGump = this;

            CanMove = false;

            BuildGump();

            X = Mouse.Position.X - (Width / 2);
            Y = Mouse.Position.Y - (Height / 2);
        }

        private void BuildGump()
        {
            List<NearbyItemDisplay> items = new List<NearbyItemDisplay>();

            foreach (Item i in World.Items.Values)
            {
                if (!i.OnGround) continue;

                if (i.Distance > Constants.DRAG_ITEMS_DISTANCE) continue;

                if (i.IsLocked && !i.ItemData.IsContainer) continue;

                items.Add(new NearbyItemDisplay(i));
            }

            if (items.Count == 0)
            {
                Dispose();
                return;
            }

            int gridSize = (int)Math.Ceiling(Math.Sqrt(items.Count));

            int ii = 0;
            int nx = 0;
            int ny = 0;
            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    if (ii >= items.Count) break;

                    items[ii].X = nx;
                    items[ii].Y = ny;
                    Add(items[ii]);
                    nx += SIZE;
                    ii++;
                }
                nx = 0;
                ny += SIZE;
            }

            Width = gridSize * SIZE;
            Height = gridSize * SIZE;
        }

        public override void Update()
        {
            base.Update();
            if (!Keyboard.Ctrl)
                Dispose();
        }

        public override void Dispose()
        {
            base.Dispose();
            NearbyItemGump = null;
        }
    }

    internal class NearbyItemDisplay : Control
    {
        private readonly Item item;
        private Point originalSize;
        private float scale = (ProfileManager.CurrentProfile.GridContainersScale / 100f);
        private Vector3 hueVector;
        private Rectangle realArtRectBounds;
        private Point point = new Point();
        private readonly SpriteInfo itemSpriteInfo;
        private readonly AlphaBlendControl background;


        public NearbyItemDisplay(Item item)
        {
            Width = NearbyItems.SIZE;
            Height = NearbyItems.SIZE;
            background = new AlphaBlendControl() { Width = Width, Height = Height };
            originalSize = new Point(Width, Height);
            this.item = item;
            hueVector = ShaderHueTranslator.GetHueVector(item.Hue, item.ItemData.IsPartialHue, 1f);
            realArtRectBounds = Client.Game.Arts.GetRealArtBounds((uint)item.DisplayedGraphic);
            itemSpriteInfo = Client.Game.Arts.GetArt((uint)(item.DisplayedGraphic));

            HitBox loot = new HitBox(0, 0, Width, Height / 2);
            loot.Add(new TextBox("Loot", TrueTypeLoader.EMBEDDED_FONT, 16, Width, Color.White, FontStashSharp.RichText.TextHorizontalAlignment.Center, false));
            loot.MouseDown += (s, e) =>
            {
                GameActions.GrabItem(item, item.Amount);
                Dispose();
            };
            Add(loot);

            HitBox use = new HitBox(0, Height / 2, Width, Height / 2);
            TextBox tb;
            use.Add(tb = new TextBox("Use", TrueTypeLoader.EMBEDDED_FONT, 16, Width, Color.White, FontStashSharp.RichText.TextHorizontalAlignment.Center, false));
            tb.Y = use.Height - tb.MeasuredSize.Y;
            use.MouseDown += (s, e) =>
            {
                GameActions.DoubleClick(item);
            };
            Add(use);


            if (realArtRectBounds.Width < Width)
            {
                if (ProfileManager.CurrentProfile.GridContainerScaleItems)
                    originalSize.X = (ushort)(realArtRectBounds.Width * scale);
                else
                    originalSize.X = realArtRectBounds.Width;

                point.X = (Width >> 1) - (originalSize.X >> 1);
            }
            else if (realArtRectBounds.Width > Width)
            {
                if (ProfileManager.CurrentProfile.GridContainerScaleItems)
                    originalSize.X = (ushort)(Width * scale);
                else
                    originalSize.X = Width;
                point.X = (Width >> 1) - (originalSize.X >> 1);
            }

            if (realArtRectBounds.Height < Height)
            {
                if (ProfileManager.CurrentProfile.GridContainerScaleItems)
                    originalSize.Y = (ushort)(realArtRectBounds.Height * scale);
                else
                    originalSize.Y = realArtRectBounds.Height;

                point.Y = (Height >> 1) - (originalSize.Y >> 1);
            }
            else if (realArtRectBounds.Height > Height)
            {
                if (ProfileManager.CurrentProfile.GridContainerScaleItems)
                    originalSize.Y = (ushort)(Height * scale);
                else
                    originalSize.Y = Height;

                point.Y = (Height >> 1) - (originalSize.Y >> 1);
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            background.Draw(batcher, x, y);

            batcher.Draw
            (
                itemSpriteInfo.Texture,
                new Rectangle
                (
                    x + point.X,
                    y + point.Y,
                    originalSize.X,
                    originalSize.Y
            ),
            new Rectangle
            (
                    itemSpriteInfo.UV.X + realArtRectBounds.X,
                    itemSpriteInfo.UV.Y + realArtRectBounds.Y,
                    realArtRectBounds.Width,
                    realArtRectBounds.Height
                ),
                hueVector
            );

            base.Draw(batcher, x, y);

            return true;
        }
    }
}
