#region license
//  Copyright (C) 2019 ClassicUO Development Community on Github
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

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class EquipmentSlot : Control
    {
        private readonly Layer _layer;
        private readonly Mobile _mobile;
        private bool _canDrag, _sendClickIfNotDClick;
        private Point _clickPoint;
        private ItemGumpFixed _itemGump;
        private float _pickupTime, _singleClickTime;
        

        public EquipmentSlot(int x, int y, Mobile mobile, Layer layer)
        {
            X = x;
            Y = y;
            Width = 19;
            Height = 20;
            _mobile = mobile;
            _layer = layer;

            Add(new GumpPicTiled(0, 0, 19, 20, 0x243A)
            {
                AcceptMouseInput = false
            });

            Add(new GumpPic(0, 0, 0x2344, 0)
            {
                AcceptMouseInput = false
            });
            AcceptMouseInput = true;

            WantUpdateSize = false;
        }

        public Item Item { get; private set; }

        public Mobile Mobile
        {
            get => _mobile;
            set { }
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (Item != null && Item.IsDisposed)
            {
                Item = null;
                _itemGump.Dispose();
                _itemGump = null;
            }

            if (Item != _mobile.Equipment[(int) _layer])
            {
                if (_itemGump != null)
                {
                    _itemGump.Dispose();
                    _itemGump = null;
                }

                Item = _mobile.Equipment[(int) _layer];

                if (Item != null)
                {
                    Add(_itemGump = new ItemGumpFixed(Item, 18, 18)
                    {
                        HighlightOnMouseOver = false,
                        ShowLabel = false,
                    });

                    ArtTexture texture = (ArtTexture) _itemGump.Texture;
                    //int offsetX = (13 - texture.ImageRectangle.Width) >> 1;
                    //int offsetY = (14 - texture.ImageRectangle.Height) >> 1;
                    int tileX = 2;
                    int tileY = 3;
                    //tileX -= texture.ImageRectangle.X - offsetX;
                    //tileY -= texture.ImageRectangle.Y - offsetY;

                    int imgW = texture.ImageRectangle.Width;
                    int imgH = texture.ImageRectangle.Height;

                    if (imgW < 14)
                        tileX += 7 - (imgW >> 1);
                    else
                    {
                        tileX -= 2;

                        if (imgW > 18)
                            imgW = 18;
                    }

                    if (imgH < 14)
                        tileY += 7 - (imgH >> 1);
                    else
                    {
                        tileY -= 2;

                        if (imgH > 18)
                            imgH = 18;
                    }


                    _itemGump.X = tileX;
                    _itemGump.Y = tileY;
                    _itemGump.Width = imgW;
                    _itemGump.Height = imgH;
                }
            }

            if (Item != null)
            {
                if (_canDrag && totalMS >= _pickupTime)
                {
                    _canDrag = false;
                    AttempPickUp();
                }

                //if (_sendClickIfNotDClick && totalMS >= _singleClickTime)
                //{
                //    _sendClickIfNotDClick = false;
                //    GameActions.SingleClick(Item);
                //}
            }

            base.Update(totalMS, frameMS);
        }

        protected override void OnMouseDown(int x, int y, MouseButton button)
        {
            if (Item == null)
                return;
            _canDrag = true;
            float totalMS = Engine.Ticks;
            _pickupTime = totalMS + 800;
            _clickPoint.X = x;
            _clickPoint.Y = y;
        }

        protected override void OnMouseOver(int x, int y)
        {
            if (Item == null)
                return;

            if (_canDrag && Math.Abs(_clickPoint.X - x) + Math.Abs(_clickPoint.Y - y) > 3)
            {
                _canDrag = false;
                AttempPickUp();
            }
        }

        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            if (Item == null)
                return;

            if (_canDrag)
            {
                _canDrag = false;
                _sendClickIfNotDClick = true;
                float totalMS = Engine.Ticks;
                _singleClickTime = totalMS + Mouse.MOUSE_DELAY_DOUBLE_CLICK;
            }
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
        {
            if (Item == null)
                return false;
            GameActions.DoubleClick(Item);
            _sendClickIfNotDClick = false;

            return true;
        }

        private void AttempPickUp()
        {
            Rectangle bounds = FileManager.Art.GetTexture(Item.DisplayedGraphic).Bounds;
            GameActions.PickUp(Item, bounds.Width >> 1, bounds.Height >> 1);
        }


        class ItemGumpFixed : ItemGump
        {
            private Point _originalSize, _point;

            public ItemGumpFixed(Item item, int w, int h) : base(item)
            {
                Width = w;
                Height = h;
                WantUpdateSize = false;


                ArtTexture texture = (ArtTexture) Texture;

                _point.X = texture.ImageRectangle.X;
                _point.Y = texture.ImageRectangle.Y;
                _originalSize.X = texture.ImageRectangle.Width;
                _originalSize.Y = texture.ImageRectangle.Height;
            }


            public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
            {
                Vector3 huev = ShaderHuesTraslator.GetHueVector(MouseIsOver && HighlightOnMouseOver ? 0x0035 : Item.Hue, Item.ItemData.IsPartialHue, 0, false);

                return batcher.Draw2D(Texture, new Rectangle(position.X, position.Y, Width, Height), new Rectangle(_point.X, _point.Y, _originalSize.X, _originalSize.Y), huev);
            }
        }
    }
}