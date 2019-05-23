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

using System.Linq;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class EquipmentSlot : Control
    {
        private readonly Layer _layer;
        private readonly Mobile _mobile;
        private ItemGumpFixed _itemGump;
        private Point _lastClickPosition;

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




        public override void Update(double totalMS, double frameMS)
        {
            if (Item != null && Item.IsDestroyed)
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
                        HighlightOnMouseOver = false
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
                //    if (_canDrag && totalMS >= _pickupTime)
                //    {
                //        _canDrag = false;
                //        AttempPickUp();
                //    }

                //    if (_sendClickIfNotDClick && totalMS >= _singleClickTime)
                //    {
                //        if (!World.ClientFlags.TooltipsEnabled)
                //            GameActions.SingleClick(Item);
                //        GameActions.OpenPopupMenu(Item);
                //        _sendClickIfNotDClick = false;
                //    }

                //if (_sendClickIfNotDClick && totalMS >= _singleClickTime)
                //{
                //    _sendClickIfNotDClick = false;
                //    GameActions.SingleClick(Item);
                //}
            }

            base.Update(totalMS, frameMS);
        }



        private class ItemGumpFixed : ItemGump
        {
            private readonly Point _originalSize;
            private readonly Point _point;

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


            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                Vector3 hue = Vector3.Zero;
                ShaderHuesTraslator.GetHueVector(ref hue, MouseIsOver && HighlightOnMouseOver ? 0x0035 : Item.Hue, Item.ItemData.IsPartialHue, 0);

                return batcher.Draw2D(Texture, x, y, Width, Height, _point.X, _point.Y, _originalSize.X, _originalSize.Y, ref hue);
            }

            //protected override void OnMouseClick(int x, int y, MouseButton button)
            //{
            //    Point p = new Point()
            //    {
            //        X = Mouse.Position.X + ParentX,
            //        Y = Mouse.Position.Y + ParentY
            //    };
            //    Parent.InvokeMouseClick(p, button);
            //}



            protected override bool Contains(int x, int y)
            {
                return true;
            }
        }
    }
}