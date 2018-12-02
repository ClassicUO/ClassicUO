#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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
using System.Collections.Generic;

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Gumps.Controls
{
    internal class ItemGump : GumpControl
    {
        private readonly List<Label> _labels = new List<Label>();
        private bool _clickedCanDrag;
        private Point _clickedPoint, _labelClickedPosition;
        private float _picUpTime;
        private float _sClickTime;
        private bool _sendClickIfNotDClick;


        public ItemGump(Item item)
        {
            AcceptMouseInput = true;
            Item = item;
            X = item.X;
            Y = item.Y;
            HighlightOnMouseOver = true;
            CanPickUp = true;
            var texture = Art.GetStaticTexture(item.DisplayedGraphic);
            Texture = texture;
            Width = texture.Width;
            Height = texture.Height;
        }

        public Item Item { get; }

        public bool HighlightOnMouseOver { get; set; }

        public bool CanPickUp { get; set; }

        public override void Update(double totalMS, double frameMS)
        {
            if (Item.IsDisposed)
            {
                Dispose();

                return;
            }

            Texture.Ticks = (long) totalMS;

            if (_clickedCanDrag && totalMS >= _picUpTime)
            {
                _clickedCanDrag = false;
                AttempPickUp();
            }

            if (_sendClickIfNotDClick && totalMS >= _sClickTime)
            {
                GameActions.SingleClick(Item);
                _sendClickIfNotDClick = false;
            }

            UpdateLabel();
            base.Update(totalMS, frameMS);
        }

        public override bool Draw(SpriteBatchUI spriteBatch, Point position, Vector3? hue = null)
        {
            Vector3 huev = ShaderHuesTraslator.GetHueVector(MouseIsOver && HighlightOnMouseOver ? 0x0035 : Item.Hue, TileData.IsPartialHue(Item.ItemData.Flags), 0, false);
            spriteBatch.Draw2D(Texture, position, huev);
            if (Item.Amount > 1 && TileData.IsStackable(Item.ItemData.Flags) && Item.DisplayedGraphic == Item.Graphic)
                spriteBatch.Draw2D(Texture, new Point(position.X + 5, position.Y + 5), huev);
            return base.Draw(spriteBatch, position, huev);
        }

        protected override bool Contains(int x, int y)
        {
            if (Texture.Contains(x, y))
                return true;

            if (Item.Amount > 1 && TileData.IsStackable(Item.ItemData.Flags))
            {
                if (Texture.Contains(x - 5, y - 5))
                    return true;
            }

            return false;
        }

        protected override void OnMouseDown(int x, int y, MouseButton button)
        {
            _clickedCanDrag = true;
            float totalMS = CoreGame.Ticks;
            _picUpTime = totalMS + 800f;
            _clickedPoint = new Point(x, y);
        }

        protected override void OnMouseUp(int x, int y, MouseButton button)
        {
            _clickedCanDrag = false;

            if (button == MouseButton.Left)
            {
                GameScene gs = SceneManager.GetScene<GameScene>();

                if (!gs.IsHoldingItem || !gs.IsMouseOverUI)
                    return;

                gs.SelectedObject = Item;

                if (TileData.IsContainer(Item.ItemData.Flags))
                    gs.DropHeldItemToContainer(Item);
                else if (gs.HeldItem.Graphic == Item.Graphic && TileData.IsStackable(gs.HeldItem.ItemData.Flags))
                    gs.MergeHeldItem(Item);
                else
                {
                    if (Item.Container.IsItem)
                        gs.DropHeldItemToContainer(World.Items.Get(Item.Container), X + (Mouse.Position.X - ScreenCoordinateX), Y + (Mouse.Position.Y - ScreenCoordinateY));
                }
            }
        }

        protected override void OnMouseOver(int x, int y)
        {
            if (_clickedCanDrag && Math.Abs(_clickedPoint.X - x) + Math.Abs(_clickedPoint.Y - y) > 3)
            {
                _clickedCanDrag = false;
                AttempPickUp();
            }
        }

        protected override void OnMouseClick(int x, int y, MouseButton button)
        {
            _labelClickedPosition.X = x;
            _labelClickedPosition.Y = y;

            if (_clickedCanDrag)
            {
                _clickedCanDrag = false;
                _sendClickIfNotDClick = true;
                float totalMS = CoreGame.Ticks;
                _sClickTime = totalMS + Mouse.MOUSE_DELAY_DOUBLE_CLICK;
            }
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
        {
            GameActions.DoubleClick(Item);
            _sendClickIfNotDClick = false;

            return true;
        }

        public override void Dispose()
        {
            UpdateLabel(true);
            base.Dispose();
        }

        private void AttempPickUp()
        {
            if (CanPickUp)
            {
                if (this is ItemGumpPaperdoll)
                {
                    Rectangle bounds = Art.GetStaticTexture(Item.DisplayedGraphic).Bounds;
                    GameActions.PickUp(Item, bounds.Width >> 1, bounds.Height >> 1);
                }
                else
                    GameActions.PickUp(Item, _clickedPoint);
            }
        }

        private void UpdateLabel(bool isDisposing = false)
        {
            if (!isDisposing && Item.OverHeads.Count > 0)
            {
                if (_labels.Count <= 0)
                {
                    foreach (TextOverhead overhead in Item.OverHeads)
                    {
                        overhead.Initialized = true;
                        overhead.TimeToLive = 4000;

                        Label label = new Label(overhead.Text, overhead.IsUnicode, overhead.Hue, overhead.MaxWidth, style: overhead.Style, align: TEXT_ALIGN_TYPE.TS_CENTER, timeToLive: overhead.TimeToLive)
                        {
                            FadeOut = true
                        };
                        label.ControlInfo.Layer = UILayer.Over;
                        UIManager.Add(label);
                        _labels.Add(label);
                    }
                }

                int y = 0;

                for (int i = _labels.Count - 1; i >= 0; i--)
                {
                    Label l = _labels[i];
                    l.X = ScreenCoordinateX + _clickedPoint.X - (l.Width >> 1);
                    l.Y = ScreenCoordinateY + _clickedPoint.Y - (l.Height >> 1) + y;
                    y += l.Height;
                }
            }
            else if (_labels.Count > 0)
            {
                _labels.ForEach(s => s.Dispose());
                _labels.Clear();
            }
        }
    }
}