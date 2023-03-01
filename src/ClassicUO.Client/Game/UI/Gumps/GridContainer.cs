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
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class GridContainer : ResizableGump
    {

        private int _lastX = 100;
        private int _lastY = 100;
        private readonly AlphaBlendControl _background;
        private readonly Item _container;
        private const int X_SPACING = 1;
        private const int Y_SPACING = 1;
        private const int GRID_ITEM_SIZE = 50;
        private const int BORDER_WIDTH = 4;
        private const int DEFAULT_WIDTH =
            (BORDER_WIDTH * 2)     //The borders around the container, one on the left and one on the right
            + 15                   //The width of the scroll bar
            + (GRID_ITEM_SIZE * 5) //How many items to fit in left to right
            + (X_SPACING * 5)      //Spacing between each grid item(x4 items)
            + 6;                   //Because the border acts weird
        private const int DEFAULT_HEIGHT = 27 + (BORDER_WIDTH * 2) + (GRID_ITEM_SIZE + Y_SPACING) * 4;
        private readonly Label _containerNameLabel;
        private GridScrollArea _scrollArea;
        private int _lastWidth = DEFAULT_WIDTH;
        private int _lastHeight = DEFAULT_HEIGHT;
        private readonly StbTextBox _searchBox;
        private readonly NiceButton _openRegularGump;
        private ushort _ogContainer;
        private readonly bool _DEBUG = false;

        public GridContainer(uint local, ushort ogContainer) : base(DEFAULT_WIDTH, DEFAULT_HEIGHT, DEFAULT_WIDTH, DEFAULT_HEIGHT, local, 0)
        {
            _ogContainer = ogContainer;
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

            _searchBox = new StbTextBox(0xFF, 20, 100, true, FontStyle.Solid, 0x0481)
            {
                X = _containerNameLabel.Width + 5,
                Y = BORDER_WIDTH,
                Multiline = false,
                Width = 100,
                Height = 20
            };
            _searchBox.TextChanged += (sender, e) => { updateItems(); };
            _searchBox.DragBegin += (sender, e) => { InvokeDragBegin(e.Location); };

            Add(new AlphaBlendControl(0.5f)
            {
                Hue = 0x0481,
                X = _searchBox.X,
                Y = _searchBox.Y,
                Width = _searchBox.Width,
                Height = _searchBox.Height
            }); //Search box background
            Add(_searchBox);

            _openRegularGump = new NiceButton(Width - 20 - BORDER_WIDTH, BORDER_WIDTH, 20, 20, ButtonAction.Activate, "[X]", 1, TEXT_ALIGN_TYPE.TS_CENTER, 0x0481)
            {
                ButtonParameter = 1,
                IsSelectable = false,
            };
            _openRegularGump.SetTooltip("Open the original style container");

            Add(_openRegularGump);
            _scrollArea = new GridScrollArea(
                _background.X,
                _containerNameLabel.Height + _background.Y + 1,
                _background.Width - BORDER_WIDTH,
                _background.Height - BORDER_WIDTH - (_containerNameLabel.Height + 1)
                );
            _scrollArea.AcceptMouseInput = true;
            _scrollArea.CanMove = true;
            _scrollArea.ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways;
            _scrollArea.MouseUp += _scrollArea_MouseUp;
            _scrollArea.DragBegin += _scrollArea_DragBegin;
            Add(_scrollArea);
            InvalidateContents = true;
        }

        private void _scrollArea_DragBegin(object sender, MouseEventArgs e)
        {
            InvokeDragBegin(e.Location);
        }

        private void _scrollArea_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtonType.Left && _scrollArea.MouseIsOver)
            {
                if (Client.Game.GameCursor.ItemHold.Enabled)
                {
                    if (_DEBUG)
                        Console.WriteLine("Scroll Area Mouse Up Item Drop");
                    GameActions.DropItem(Client.Game.GameCursor.ItemHold.Serial, 0xFFFF, 0xFFFF, 0, _container.Serial);
                    InvalidateContents = true;
                    UpdateContents();
                }
            }
            else if (e.Button == MouseButtonType.Right)
            {
                InvokeMouseCloseGumpWithRClick();
            }
        }

        public override void OnButtonClick(int buttonID)
        {
            switch (buttonID)
            {
                case 1:
                    OpenOldContainer(_container);
                    break;
            }
        }

        private void OpenOldContainer(uint serial)
        {
            ContainerGump container = UIManager.GetGump<ContainerGump>(serial);
            bool playsound = false;
            int x, y;
            Item item = World.Items.Get<Item>(serial);

            if (item == null || item.IsDestroyed) return;

            ushort graphic = _ogContainer;
            if (Client.Version >= Utility.ClientVersion.CV_706000 && ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.UseLargeContainerGumps)
            {
                GumpsLoader loader = GumpsLoader.Instance;

                switch (graphic)
                {
                    case 0x0048:
                        if (loader.GetGumpTexture(0x06E8, out _) != null)
                        {
                            graphic = 0x06E8;
                        }

                        break;

                    case 0x0049:
                        if (loader.GetGumpTexture(0x9CDF, out _) != null)
                        {
                            graphic = 0x9CDF;
                        }

                        break;

                    case 0x0051:
                        if (loader.GetGumpTexture(0x06E7, out _) != null)
                        {
                            graphic = 0x06E7;
                        }

                        break;

                    case 0x003E:
                        if (loader.GetGumpTexture(0x06E9, out _) != null)
                        {
                            graphic = 0x06E9;
                        }

                        break;

                    case 0x004D:
                        if (loader.GetGumpTexture(0x06EA, out _) != null)
                        {
                            graphic = 0x06EA;
                        }

                        break;

                    case 0x004E:
                        if (loader.GetGumpTexture(0x06E6, out _) != null)
                        {
                            graphic = 0x06E6;
                        }

                        break;

                    case 0x004F:
                        if (loader.GetGumpTexture(0x06E5, out _) != null)
                        {
                            graphic = 0x06E5;
                        }

                        break;

                    case 0x004A:
                        if (loader.GetGumpTexture(0x9CDD, out _) != null)
                        {
                            graphic = 0x9CDD;
                        }

                        break;

                    case 0x0044:
                        if (loader.GetGumpTexture(0x9CE3, out _) != null)
                        {
                            graphic = 0x9CE3;
                        }

                        break;
                }
            }


            if (container != null)
            {
                x = container.ScreenCoordinateX;
                y = container.ScreenCoordinateY;
                container.Dispose();
            }
            else
            {
                ContainerManager.CalculateContainerPosition(serial, graphic);
                x = ContainerManager.X;
                y = ContainerManager.Y;
                playsound = true;
            }


            UIManager.Add
            (
                new ContainerGump(item, graphic, playsound)
                {
                    X = x,
                    Y = y,
                    InvalidateContents = true
                }
            );

            UIManager.RemovePosition(serial);
            this.Dispose();
        }

        private void updateItems()
        {
            if (_DEBUG)
                Console.WriteLine($"{Width}x{Height} Update Items called");
            int x = X_SPACING;
            int y = Y_SPACING;
            _background.Width = Width - (BORDER_WIDTH * 2);
            _background.Height = Height - (BORDER_WIDTH * 2);

            foreach (Control child in _scrollArea.Children)
                if (child is GridItem)
                    child.Dispose();

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
                    if (_DEBUG)
                        Console.Write($"[{it.Name}]");
                    GridItem gridItem = new GridItem(it, GRID_ITEM_SIZE, _container, this);

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
            InvalidateContents = false;
        }

        protected override void UpdateContents()
        {
            if (_DEBUG)
                Console.WriteLine("contents called");
            if (InvalidateContents && !IsDisposed && IsVisible)
                updateItems();
            _openRegularGump.X = Width - 20 - (BORDER_WIDTH * 2);
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
            
            if (this._lastWidth != this.Width ||this. _lastHeight != this.Height)
            {
                _scrollArea.Width = _background.Width - BORDER_WIDTH;
                _scrollArea.Height = _background.Height - BORDER_WIDTH - (_containerNameLabel.Height + 1);
                _lastHeight = this.Height;
                _lastWidth = this.Width;
                this.RequestUpdateContents();
                this.UpdateContents();
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
            private readonly Item _item;
            private readonly GridContainer _gridContainer;
            private readonly Item _container;
            private readonly bool _DEBUG = false;

            public GridItem(uint serial, int size, Item container, GridContainer gridContainer)
            {
                _container = container;
                _gridContainer = gridContainer;
                Point startDrag = new Point(0, 0);

                LocalSerial = serial;

                _item = World.Items.Get(serial);

                if (_item == null)
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

                int itemAmt = (_item.ItemData.IsStackable ? _item.Amount : 1);
                if (itemAmt > 1)
                {
                    Label _count = new Label(itemAmt.ToString(), true, 0x0481, align: TEXT_ALIGN_TYPE.TS_LEFT, maxwidth: size);
                    _count.X = 1;
                    _count.Y = size - _count.Height;

                    Add(_count);
                }

                _hit = new HitBox(0, 0, size, size, null, 0f);
                Add(_hit);

                _hit.SetTooltip(_item);
                _hit.MouseEnter += _hit_MouseEnter;
                _hit.MouseExit += _hit_MouseExit;
                _hit.MouseUp += _hit_MouseUp;
                _hit.MouseDoubleClick += _hit_MouseDoubleClick;

                WantUpdateSize = false;
            }

            private void _hit_MouseDoubleClick(object sender, MouseDoubleClickEventArgs e)
            {
                if (_DEBUG)
                    Console.WriteLine("Mouse D click");
                GameActions.DoubleClick(_item);
                e.Result = true;
            }

            private void _hit_MouseUp(object sender, MouseEventArgs e)
            {
                if (_DEBUG)
                    Console.WriteLine("Mouse Up");
                if (e.Button == MouseButtonType.Left && Mouse.LDragOffset == Point.Zero)
                {
                    if (Client.Game.GameCursor.ItemHold.Enabled)
                    {
                        if (_item.ItemData.IsContainer)
                            GameActions.DropItem(Client.Game.GameCursor.ItemHold.Serial, 0xFFFF, 0xFFFF, 0, _item.Serial);
                        else if (_item.ItemData.IsStackable && _item.Graphic == Client.Game.GameCursor.ItemHold.Graphic)
                            GameActions.DropItem(Client.Game.GameCursor.ItemHold.Serial, _item.X, _item.Y, 0, _item.Serial);
                        else
                            GameActions.DropItem(Client.Game.GameCursor.ItemHold.Serial, 0xFFFF, 0xFFFF, 0, _container.Serial);
                        _gridContainer.InvalidateContents = true;
                        _gridContainer.UpdateContents();
                    }
                    else if (TargetManager.IsTargeting)
                    {
                        TargetManager.Target(_item);
                    }
                    else
                    {
                        Point offset = Mouse.LDragOffset;
                        if (Math.Abs(offset.X) < Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS && Math.Abs(offset.Y) < Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
                        {
                            DelayedObjectClickManager.Set(_item, X + GRID_ITEM_SIZE, Y + GRID_ITEM_SIZE, 1);
                        }
                    }
                }
                else if (e.Button == MouseButtonType.Right)
                {
                    //Possibly menu for locking a slot?
                }
            }

            private void _hit_MouseExit(object sender, MouseEventArgs e)
            {
                if (_DEBUG)
                    Console.WriteLine("Mouse Exit");
                if (Mouse.LButtonPressed && !mousePressedWhenEntered)
                {
                    Point offset = Mouse.LDragOffset;
                    if (Math.Abs(offset.X) >= Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS || Math.Abs(offset.Y) >= Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
                    {
                        GameActions.PickUp(_item, e.X, e.Y);
                    }
                }
            }

            private void _hit_MouseEnter(object sender, MouseEventArgs e)
            {
                if (_DEBUG)
                    Console.WriteLine("Mouse Entered");
                if (Mouse.LButtonPressed)
                    mousePressedWhenEntered = true;
                else
                    mousePressedWhenEntered = false;
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

        private class GridScrollArea : Control
        {
            private readonly ScrollBarBase _scrollBar;
            private int _lastWidth;
            private int _lastHeight;
            private int _lastScrollValue = 0;

            public GridScrollArea
            (
                int x,
                int y,
                int w,
                int h,
                int scroll_max_height = -1
            )
            {
                X = x;
                Y = y;
                Width = w;
                Height = h;
                _lastWidth = w;
                _lastHeight = h;

                _scrollBar = new ScrollBar(Width - 14, 0, Height);


                ScrollMaxHeight = scroll_max_height;

                _scrollBar.MinValue = 0;
                _scrollBar.MaxValue = scroll_max_height >= 0 ? scroll_max_height : Height;
                _scrollBar.Parent = this;

                AcceptMouseInput = true;
                WantUpdateSize = false;
                CanMove = true;
                ScrollbarBehaviour = ScrollbarBehaviour.ShowWhenDataExceedFromView;
            }


            public int ScrollMaxHeight { get; set; } = -1;
            public ScrollbarBehaviour ScrollbarBehaviour { get; set; }
            public int ScrollValue => _scrollBar.Value;
            public int ScrollMinValue => _scrollBar.MinValue;
            public int ScrollMaxValue => _scrollBar.MaxValue;


            public Rectangle ScissorRectangle;


            public override void Update()
            {
                base.Update();

                CalculateScrollBarMaxValue();

                if (Width != _lastWidth || Height != _lastHeight)
                {
                    _scrollBar.X = Width - 14;
                    _scrollBar.Height = Height;
                    _lastWidth = Width;
                    _lastHeight = Height;
                }

                if (ScrollbarBehaviour == ScrollbarBehaviour.ShowAlways)
                {
                    _scrollBar.IsVisible = true;
                }
                else if (ScrollbarBehaviour == ScrollbarBehaviour.ShowWhenDataExceedFromView)
                {
                    _scrollBar.IsVisible = _scrollBar.MaxValue > _scrollBar.MinValue;
                }
            }

            public void Scroll(bool isup)
            {
                if (isup)
                {
                    _scrollBar.Value -= _scrollBar.ScrollStep;
                    _lastScrollValue = _scrollBar.Value;
                }
                else
                {
                    _scrollBar.Value += _scrollBar.ScrollStep;
                    _lastScrollValue = _scrollBar.Value;
                }
            }

            public override bool Draw(UltimaBatcher2D batcher, int x, int y)
            {
                _scrollBar.Draw(batcher, x + _scrollBar.X, y + _scrollBar.Y);

                if (batcher.ClipBegin(x + ScissorRectangle.X, y + ScissorRectangle.Y, Width - 14 + ScissorRectangle.Width, Height + ScissorRectangle.Height))
                {
                    for (int i = 1; i < Children.Count; i++)
                    {
                        Control child = Children[i];

                        if (!child.IsVisible)
                        {
                            continue;
                        }

                        int finalY = y + child.Y - _scrollBar.Value + ScissorRectangle.Y;

                        child.Draw(batcher, x + child.X, finalY);
                    }

                    batcher.ClipEnd();
                }

                return true;
            }


            protected override void OnMouseWheel(MouseEventType delta)
            {
                switch (delta)
                {
                    case MouseEventType.WheelScrollUp:
                        _scrollBar.Value -= _scrollBar.ScrollStep;

                        break;

                    case MouseEventType.WheelScrollDown:
                        _scrollBar.Value += _scrollBar.ScrollStep;

                        break;
                }
            }

            public override void Clear()
            {
                for (int i = 1; i < Children.Count; i++)
                {
                    Children[i].Dispose();
                }
            }

            private void CalculateScrollBarMaxValue()
            {
                _scrollBar.Height = ScrollMaxHeight >= 0 ? ScrollMaxHeight : Height;
                bool maxValue = _scrollBar.Value == _scrollBar.MaxValue && _scrollBar.MaxValue != 0;

                int startX = 0, startY = 0, endX = 0, endY = 0;

                for (int i = 1; i < Children.Count; i++)
                {
                    Control c = Children[i];

                    if (c.IsVisible && !c.IsDisposed)
                    {
                        if (c.X < startX)
                        {
                            startX = c.X;
                        }

                        if (c.Y < startY)
                        {
                            startY = c.Y;
                        }

                        if (c.Bounds.Right > endX)
                        {
                            endX = c.Bounds.Right;
                        }

                        if (c.Bounds.Bottom > endY)
                        {
                            endY = c.Bounds.Bottom;
                        }
                    }
                }

                int width = Math.Abs(startX) + Math.Abs(endX);
                int height = Math.Abs(startY) + Math.Abs(endY) - _scrollBar.Height;
                height = Math.Max(0, height - (-ScissorRectangle.Y + ScissorRectangle.Height));

                if (height > 0)
                {
                    _scrollBar.MaxValue = height;

                    if (maxValue)
                    {
                        _scrollBar.Value = _scrollBar.MaxValue;
                    }
                }
                else
                {
                    _scrollBar.Value = _scrollBar.MaxValue = 0;
                }

                _scrollBar.UpdateOffset(0, Offset.Y);

                for (int i = 1; i < Children.Count; i++)
                {
                    Children[i].UpdateOffset(0, -_scrollBar.Value + ScissorRectangle.Y);
                }
            }
        }
    }
}