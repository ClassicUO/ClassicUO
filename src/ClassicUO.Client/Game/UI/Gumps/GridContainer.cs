#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class GridContainer : ResizableGump
    {

        private static int _lastX = 100;
        private static int _lastY = 100;
        private readonly AlphaBlendControl _background;
        private readonly Item _container;
        private const int X_SPACING = 1;
        private const int Y_SPACING = 1;
        private const int GRID_ITEM_SIZE = 50;
        private const int BORDER_WIDTH = 4;
        private const int DEFAULT_WIDTH =
            (BORDER_WIDTH * 2)     //The borders around the container, one on the left and one on the right
            + 15                   //The width of the scroll bar
            + (GRID_ITEM_SIZE * 4) //How many items to fit in left to right
            + (X_SPACING * 4)      //Spacing between each grid item(x4 items)
            + 6;                   //Because the border acts weird
        private const int DEFAULT_HEIGHT = (BORDER_WIDTH * 2) + (GRID_ITEM_SIZE + Y_SPACING) * 4;
        private readonly Label _containerNameLabel;
        private ScrollArea _scrollArea;
        private static int lastWidth = DEFAULT_WIDTH;
        private static int lastHeight = DEFAULT_HEIGHT;
        private readonly StbTextBox _searchBox;

        public GridContainer(uint local) : base(DEFAULT_WIDTH, DEFAULT_HEIGHT, DEFAULT_WIDTH, DEFAULT_HEIGHT, local, 0)
        {
            _container = World.Items.Get(local);

            if (_container == null)
            {
                Dispose();

                return;
            }

            X = _lastX;
            Y = _lastY;

            CanMove = true;
            AcceptMouseInput = true;
            WantUpdateSize = true;
            CanCloseWithRightClick = true;

            _background = new AlphaBlendControl();
            _background.Width = Width - (BORDER_WIDTH * 2);
            _background.Height = Height - (BORDER_WIDTH * 2);
            _background.X = BORDER_WIDTH;
            _background.Y = BORDER_WIDTH;
            Add(_background);

            Add
            (
                _containerNameLabel = new Label(GetContainerName(), true, 0x0481)
                {
                    X = _background.X + 2,
                    Y = 2
                }
            );

            Add(
                _searchBox = new StbTextBox(0xFF, 20, 100, true, FontStyle.Solid, 0x0481)
                {
                    X = _containerNameLabel.Width + 5,
                    Y = 2,
                    Multiline = false,
                    Width = 100,
                    Height = 20
                }
            );
            _searchBox.TextChanged += (sender, e) =>
            {
                UpdateContents();
            };


            updateScrollArea();
        }

        private void updateScrollArea()
        {
            if (_scrollArea != null)
                foreach (Control child in _scrollArea.Children)
                {
                    if (child is GridItem)
                        child.Dispose();
                }
            Remove(_scrollArea);
            _scrollArea = new ScrollArea(
                _background.X,
                _containerNameLabel.Height + _background.Y + 1,
                _background.Width - BORDER_WIDTH,
                _background.Height - BORDER_WIDTH - (_containerNameLabel.Height + 1),
                true
                );
            _scrollArea.AcceptMouseInput = true;
            _scrollArea.CanMove = true;
            _scrollArea.ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways;
            Add(_scrollArea);

            _scrollArea.MouseUp += (sender, e) =>
            {
                if (e.Button == MouseButtonType.Left)
                {
                    if (Client.Game.GameCursor.ItemHold.Enabled)
                    {
                        GameActions.DropItem(Client.Game.GameCursor.ItemHold.Serial, 0xFFFF, 0xFFFF, 0, _container);
                    }
                }
                else if (e.Button == MouseButtonType.Right)
                {
                    this.InvokeMouseCloseGumpWithRClick();
                }
            };

            _scrollArea.DragBegin += (sender, e) =>
            {
                this.InvokeDragBegin(e.Location);
            };
        }

        public override void OnButtonClick(int buttonID)
        {
        }

        private void updateItems()
        {
            int x = X_SPACING;
            int y = Y_SPACING;

            _background.Width = Width - (BORDER_WIDTH * 2);
            _background.Height = Height - (BORDER_WIDTH * 2);

            int count = 0;
            int line = 1;

            if (_container != null && _container.Items != null)
            {
                List<Item> contents = new List<Item>();
                for (LinkedObject i = _container.Items; i != null; i = i.Next)
                {
                    contents.Add((Item)i);
                }
                List<Item> sortedContents = contents.OrderBy((x) => x.Name).ToList<Item>();


                if (_searchBox.Text != "")
                {
                    sortedContents = sortedContents.FindAll((x) => x.Name.ToLower().Contains(_searchBox.Text.ToLower()));
                }

                foreach (Item it in sortedContents)
                {

                    GridItem gridItem = new GridItem(it, GRID_ITEM_SIZE, _container);

                    if (x + GRID_ITEM_SIZE + X_SPACING >= _scrollArea.Width - 14) //14 is the scroll bar width
                    {
                        x = X_SPACING;
                        ++line;

                        y += gridItem.Height + Y_SPACING;

                    }

                    gridItem.X = x + X_SPACING;
                    gridItem.Y = y;
                    _scrollArea.Add(gridItem);//, _pagesCount);

                    x += gridItem.Width + X_SPACING;
                    ++count;
                }
            }
        }

        protected override void UpdateContents()
        {
            updateScrollArea();
            updateItems();
            lastHeight = Height;
            lastWidth = Width;
        }

        public override void Dispose()
        {
            _lastX = X;
            _lastY = Y;

            base.Dispose();
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (!IsVisible || IsDisposed)
            {
                return false;
            }

            base.Draw(batcher, x, y);

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            return true;
        }


        public override void Update()
        {
            if (_container == null || _container.IsDestroyed || _container.OnGround && _container.Distance > 3)
            {
                Dispose();

                return;
            }

            base.Update();

            if (IsDisposed)
            {
                return;
            }

            if (lastWidth != Width || lastHeight != Height)
            {
                UpdateContents();
            }

            WantUpdateSize = true;

            if (_container != null && !_container.IsDestroyed && UIManager.MouseOverControl != null && (UIManager.MouseOverControl == this || UIManager.MouseOverControl.RootParent == this))
            {
                SelectedObject.Object = _container;
            }
        }

        private string GetContainerName()
        {
            return _container.Name?.Length > 0 ? _container.Name : "a container";
        }

        private class GridItem : Control
        {
            private readonly HitBox _hit;
            private bool mousePressedWhenEntered = false;

            public GridItem(uint serial, int size, Item container)
            {
                Point startDrag = new Point(0, 0);

                LocalSerial = serial;

                Item item = World.Items.Get(serial);

                if (item == null)
                {
                    Dispose();

                    return;
                }

                CanMove = false;

                AlphaBlendControl background = new AlphaBlendControl();
                background.Width = size;
                background.Height = size;
                Width = Height = size;
                Add(background);

                int itemAmt = (item.ItemData.IsStackable ? item.Amount : 1);
                if (itemAmt > 1)
                {
                    Label _count = new Label(itemAmt.ToString(), true, 0x0481, align: TEXT_ALIGN_TYPE.TS_LEFT, maxwidth: size);
                    _count.X = 1;
                    _count.Y = size - _count.Height;

                    Add(_count);
                }

                _hit = new HitBox(0, 0, size, size, null, 0f);
                Add(_hit);

                _hit.SetTooltip(item);

                _hit.MouseEnter += (sender, e) => {
                    if (Mouse.LButtonPressed)
                        mousePressedWhenEntered = true;
                    else
                        mousePressedWhenEntered = false;
                };

                _hit.MouseExit += (sender, e) =>
                {
                    if (Mouse.LButtonPressed && !mousePressedWhenEntered)
                    {
                        Point offset = Mouse.LDragOffset;
                        if (Math.Abs(offset.X) >= Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS || Math.Abs(offset.Y) >= Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
                        {
                            GameActions.PickUp(item, startDrag.X, startDrag.Y);
                        }
                    }
                };

                _hit.MouseUp += (sender, e) =>
                {
                    if (e.Button == MouseButtonType.Left)
                    {
                        if (Client.Game.GameCursor.ItemHold.Enabled)
                        {
                            if (item.ItemData.IsContainer)
                                GameActions.DropItem(Client.Game.GameCursor.ItemHold.Serial, 0xFFFF, 0xFFFF, 0, item);
                            else if (item.ItemData.IsStackable && item.Graphic == Client.Game.GameCursor.ItemHold.Graphic)
                                GameActions.DropItem(Client.Game.GameCursor.ItemHold.Serial, item.X, item.Y, 0, item);
                            else
                                GameActions.DropItem(Client.Game.GameCursor.ItemHold.Serial, item.X, item.Y, 0, container);
                        }
                        if (TargetManager.IsTargeting)
                        {
                            TargetManager.Target(item);
                        }
                        else
                        {
                            Point offset = Mouse.LDragOffset;
                            if (Math.Abs(offset.X) < Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS && Math.Abs(offset.Y) < Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
                            {
                                GameActions.OpenPopupMenu(item);
                                //GameActions.SingleClick(item);
                            }
                        }
                    }
                    else if (e.Button == MouseButtonType.Right)
                    {
                        //Context menu
                    }
                };

                _hit.MouseDoubleClick += (sender, e) =>
                {
                    GameActions.DoubleClick(item);
                    e.Result = true;
                };

                WantUpdateSize = false;
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                base.Draw(batcher, x, y);

                Item item = World.Items.Get(LocalSerial);

                Vector3 hueVector;

                if (item != null)
                {
                    var texture = ArtLoader.Instance.GetStaticTexture(item.DisplayedGraphic, out var bounds);
                    var rect = ArtLoader.Instance.GetRealArtBounds(item.DisplayedGraphic);

                    hueVector = ShaderHueTranslator.GetHueVector(item.Hue, item.ItemData.IsPartialHue, 1f);

                    Point originalSize = new Point(_hit.Width, _hit.Height);
                    Point point = new Point();

                    if (rect.Width < _hit.Width)
                    {
                        originalSize.X = rect.Width;
                        point.X = (_hit.Width >> 1) - (originalSize.X >> 1);
                    }

                    if (rect.Height < _hit.Height)
                    {
                        originalSize.Y = rect.Height;
                        point.Y = (_hit.Height >> 1) - (originalSize.Y >> 1);
                    }

                    batcher.Draw
                    (
                        texture,
                        new Rectangle
                        (
                            x + point.X,
                            y + point.Y + _hit.Y,
                            originalSize.X,
                            originalSize.Y
                        ),
                        new Rectangle
                        (
                            bounds.X + rect.X,
                            bounds.Y + rect.Y,
                            rect.Width,
                            rect.Height
                        ),
                        hueVector
                    );
                }

                hueVector = ShaderHueTranslator.GetHueVector(0);

                batcher.DrawRectangle
                (
                    SolidColorTextureCache.GetTexture(Color.Gray),
                    x,
                    y,
                    Width,
                    Height,
                    hueVector
                );

                if (_hit.MouseIsOver)
                {
                    hueVector.Z = 0.3f;

                    batcher.Draw
                    (
                        SolidColorTextureCache.GetTexture(Color.Yellow),
                        new Rectangle
                        (
                            x + 1,
                            y,
                            Width - 1,
                            Height
                        ),
                        hueVector
                    );

                    hueVector.Z = 1;
                }

                return true;
            }
        }
    }
}