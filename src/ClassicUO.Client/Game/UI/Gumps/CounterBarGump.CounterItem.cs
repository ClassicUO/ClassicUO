// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassicUO.Game.UI.Gumps
{
    internal partial class CounterBarGump
    {
        private class CounterItem : Control
        {
            private const int UPDATE_INTERVAL = 100;
            private const int DRAG_OFFSET = 22;
            private const float HIGHLIGHT_AMOUNT_CHANGED_DURATION = 5000f;
            private const int HIGHLIGHT_AMOUNT_CHANGED_UP_HUE = 1165; //icelight
            private const int HIGHLIGHT_AMOUNT_CHANGED_DOWN_HUE = 1166; //firelight
            private int _amount;
            private uint _lastChangeTime;
            private readonly ImageWithText _image;
            private uint _time;
            private AlphaBlendControl _background;
            private readonly CounterBarGump _gump;

            public CounterItem(CounterBarGump gump, ushort graphic, ushort? hue, int compareTo)
            {
                _gump = gump;
                CompareTo = compareTo;
                
                AcceptMouseInput = true;
                WantUpdateSize = false;
                CanMove = true;
                CanCloseWithRightClick = false;

                Add(_background = new AlphaBlendControl(0.0f) { X = 0, Y = 0 });
                Add(_image = new ImageWithText());

                SetGraphic(graphic, hue);
            }

            public ushort Graphic { get; private set; }

            public ushort? Hue { get; private set; }

            public int CompareTo { get; private set; }

            public void SetGraphic(ushort graphic, ushort? hue)
            {
                _image.ChangeGraphic(graphic, hue ?? 0);

                Graphic = graphic;
                Hue = hue;

                ConfigureContextMenu();
            }

            internal void ConfigureContextMenu()
            {
                ContextMenu = new ContextMenuControl(_gump);
                if (Graphic != 0)
                {
                    ContextMenu.Add(ResGumps.UseObject, Use);
                    ContextMenu.Add(ResGumps.CounterCompareTo, CompareToSelected);
                    ContextMenu.Add(Hue != null ? ResGumps.CounterIgnoreHueOff : ResGumps.CounterIgnoreHueOn, ToggleIgnoreHue);
                }
                else
                {
                    _gump.ConfigureContextMenu(ContextMenu); // Placeholders receive context menu from parent
                }
            }

            private void ToggleIgnoreHue()
            {
                if (Hue != null)
                {
                    Hue = null;
                }
                else
                {
                    Hue = 0;
                }

                SetGraphic(Graphic, Hue);
            }

            private void CompareToSelected()
            {
                UIManager.Add(new EntryDialog(_gump.World, 250, 160,
                    string.Format("{0}\n{1}", ResGumps.CounterCompareToDialogText1, ResGumps.CounterCompareToDialogText2),
                    CompareToDialogClosed, _amount.ToString()));
            }

            private void CompareToDialogClosed(string newValue)
            {
                if (string.IsNullOrEmpty(newValue))
                {
                    CompareTo = 0;
                }
                else if (int.TryParse(newValue, out int parsedValue))
                {
                    CompareTo = parsedValue;
                }
                else
                {
                    UIManager.Add(new EntryDialog(_gump.World, 250, 180,
                        string.Format("{0}\n{1}\n\n{2}", ResGumps.CounterCompareToDialogText1, ResGumps.CounterCompareToDialogText2, ResGumps.CounterCompareToDialogInvalid),
                        CompareToDialogClosed, newValue));
                }
            }

            public void RemoveItem()
            {
                _image?.ChangeGraphic(0, 0);
                _amount = 0;
                Graphic = 0;

                Dispose();

                if (RootParent is CounterBarGump g)
                {
                    g.SetupLayout();
                }
            }

            public void Use()
            {
                if (Graphic == 0)
                {
                    return;
                }

                Item item = _gump.World.Player.FindItem(Graphic, Hue ?? 0xFFFF);

                if (item != null)
                {
                    GameActions.DoubleClick(_gump.World, item);
                }
            }

            protected override void OnMouseOver(int x, int y)
            {
                base.OnMouseOver(x, y);
                if (Hue == null)
                {
                    if (_gump.World.Player.FindItemByLayer(Layer.Backpack)?.FindItem(Graphic) is { } item)
                        SetTooltip(item);
                }
                else
                {
                    if (_gump.World.Player.FindItemByLayer(Layer.Backpack)?.FindItem(Graphic, Hue.Value) is { } item)
                        SetTooltip(item);
                }
            }

            protected override void OnMouseExit(int x, int y)
            {
                base.OnMouseExit(x, y);
                ClearTooltip();
            }

            protected override void OnDragBegin(int x, int y)
            {
                if (_gump.ReadOnly)
                {
                    return;
                }

                DraggableGump gump = new DraggableGump(_gump.World)
                {
                    X = Mouse.LClickPosition.X - DRAG_OFFSET,
                    Y = Mouse.LClickPosition.Y - DRAG_OFFSET
                };
                gump.Add(this);
                X = 0;
                Y = 0;

                UIManager.Add(gump);

                UIManager.AttemptDragControl(gump, true);

                _gump.SetupLayout();
            }

            protected override void OnDragEnd(int x, int y)
            {
                if (_gump.ReadOnly)
                {
                    return;
                }

                Control oldParent = this.Parent;
                if (oldParent is DraggableGump)
                {
                    FinalizeDragDrop(oldParent, UIManager.GetAllMouseOverControlsOfType<CounterBarGump>());
                }

                base.OnDragEnd(x, y);
            }

            private void FinalizeDragDrop(Control oldParent, IEnumerable<Control> hoveredControls)
            {
                if (!hoveredControls.Any())
                {
                    oldParent.Dispose();
                    return;
                }

                int desiredIndex = Math.Min(hoveredControls.Count() - 1, 1);

                CounterItem item = hoveredControls.First() as CounterItem;
                CounterBarGump bar = hoveredControls.First().RootParent as CounterBarGump;

                if (bar != null)
                {
                    if (item != null)
                    {
                        bar._dataBox.Insert(bar._dataBox.Children.IndexOf(item), this);
                        bar.SetupLayout();
                    }
                    else
                    {
                        bar._dataBox.Add(this);
                        bar.SetupLayout();
                    }

                }
                oldParent.Dispose();
            }

            protected override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                switch (button)
                {
                    case MouseButtonType.Left:
                        if (Client.Game.UO.GameCursor.ItemHold.Enabled)
                        {
                            if (!_gump.ReadOnly)
                            {
                                SetGraphic(
                                    Client.Game.UO.GameCursor.ItemHold.Graphic,
                                    Client.Game.UO.GameCursor.ItemHold.Hue
                                );
                            }

                            GameActions.DropItem(
                                Client.Game.UO.GameCursor.ItemHold.Serial,
                                Client.Game.UO.GameCursor.ItemHold.X,
                                Client.Game.UO.GameCursor.ItemHold.Y,
                                0,
                                Client.Game.UO.GameCursor.ItemHold.Container
                            );
                        }
                        else if (ProfileManager.CurrentProfile.CastSpellsByOneClick)
                        {
                            Use();
                        }
                        break;
                    case MouseButtonType.Right when Keyboard.Alt:
                        if (!_gump.ReadOnly)
                        {
                            RemoveItem();
                        }
                        break;
                    case MouseButtonType.Right:
                        base.OnMouseUp(x, y, button);
                        break;
                    default:
                        if (Graphic != 0)
                        {
                            base.OnMouseUp(x, y, button);
                        }

                        break;
                }
            }

            protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
            {
                if (
                    button == MouseButtonType.Left
                    && !ProfileManager.CurrentProfile.CastSpellsByOneClick
                )
                {
                    Use();
                }

                return true;
            }

            private int CalculateDisplayAmount()
            {
                return _amount - CompareTo;
            }

            public override void Update()
            {
                base.Update();

                if (!IsDisposed)
                {
                    _image.Width = Width;
                    _image.Height = Height;

                    _background.Width = Width;
                    _background.Height = Height;
                }

                if (Parent == null || !Parent.IsEnabled || _time >= Time.Ticks)
                {
                    return;
                }

                _time = Time.Ticks + UPDATE_INTERVAL;

                if (Graphic == 0)
                {
                    _image.SetAmount(string.Empty);
                    return;
                }

                int previousAmount = _amount;
                _amount = _gump.World.Player.GetTotalAmountOfItem(Graphic, Hue);

                UpdateOnChangeAnimation(previousAmount);

                _image.SetAmount(CalculateDisplayAmountText());
            }

            private void UpdateOnChangeAnimation(int previousAmount)
            {
                if (ProfileManager.CurrentProfile.CounterBarHighlightOnChange)
                {
                    if (_amount > previousAmount)
                    {
                        _background.Hue = HIGHLIGHT_AMOUNT_CHANGED_UP_HUE;
                        _lastChangeTime = Time.Ticks;
                    }
                    else if (_amount < previousAmount)
                    {
                        _background.Hue = HIGHLIGHT_AMOUNT_CHANGED_DOWN_HUE;
                        _lastChangeTime = Time.Ticks;
                    }

                    _background.Alpha = Math.Max(0, 1 - (Time.Ticks - _lastChangeTime) / HIGHLIGHT_AMOUNT_CHANGED_DURATION);
                }
            }

            private string CalculateDisplayAmountText()
            {
                int displayAmount = CalculateDisplayAmount();
                string prefix = CalculateAmountPrefix(displayAmount);

                if (string.IsNullOrEmpty(prefix) && displayAmount == 1)
                {
                    // don't show a number if there is only one!
                    return string.Empty;
                }

                string displayText = prefix + displayAmount.ToString();
                if (ProfileManager.CurrentProfile.CounterBarDisplayAbbreviatedAmount)
                {
                    if (
                        Math.Abs(displayAmount) >= ProfileManager.CurrentProfile.CounterBarAbbreviatedAmount
                    )
                    {
                        displayText = prefix + StringHelper.IntToAbbreviatedString(displayAmount);
                    }
                }

                return displayText;
            }

            private string CalculateAmountPrefix(int displayAmount)
            {
                string prefix;
                if (CompareTo == 0)
                {
                    prefix = "";
                }
                else if (displayAmount == 0)
                {
                    prefix = "±";
                }
                else if (displayAmount > 0)
                {
                    prefix = "+";
                }
                else
                {
                    prefix = "";  // a negative number already comes with its prefix
                }

                return prefix;
            }

            public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
            {
                base.AddToRenderLists(renderLists, x, y, ref layerDepthRef);
                float layerDepth = layerDepthRef;

                Texture2D color = SolidColorTextureCache.GetTexture(
                    MouseIsOver
                        ? Color.Yellow
                        : ProfileManager.CurrentProfile.CounterBarHighlightOnAmount
                        && CalculateDisplayAmount() < ProfileManager.CurrentProfile.CounterBarHighlightAmount
                        && Graphic != 0
                            ? Color.Red
                            : Color.Gray
                );

                Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

                renderLists.AddGumpNoAtlas(batcher =>
                {
                    batcher.DrawRectangle(color, x, y, Width, Height, hueVector, layerDepth);

                    return true;
                });

                return true;
            }

            private class ImageWithText : Control
            {
                private readonly Label _label;
                private ushort _graphic;
                private ushort _hue;
                private bool _partial;

                public ImageWithText()
                {
                    CanMove = true;
                    WantUpdateSize = true;
                    AcceptMouseInput = false;

                    _label = new Label("", true, 0x35, 0, 1, FontStyle.BlackBorder)
                    {
                        X = 2,
                        Y = Height - 15
                    };

                    Add(_label);
                }

                public void ChangeGraphic(ushort graphic, ushort hue)
                {
                    if (graphic != 0)
                    {
                        _graphic = graphic;
                        _hue = hue;
                        _partial = Client.Game.UO.FileManager.TileData.StaticData[graphic].IsPartialHue;
                    }
                    else
                    {
                        _graphic = 0;
                    }
                }

                public override void Update()
                {
                    base.Update();

                    if (Parent != null)
                    {
                        Width = Parent.Width;
                        Height = Parent.Height;
                        _label.Y = Parent.Height - 15;
                    }
                }

                public override bool AddToRenderLists(RenderLists renderLists, int x, int y, ref float layerDepthRef)
                {
                    if (_graphic != 0)
                    {
                        float layerDepth = layerDepthRef;
                        ref readonly var artInfo = ref Client.Game.UO.Arts.GetArt(_graphic);
                        var rect = Client.Game.UO.Arts.GetRealArtBounds(_graphic);

                        Vector3 hueVector = ShaderHueTranslator.GetHueVector(_hue, _partial, 1f);

                        Point originalSize = new Point(Width, Height);
                        Point point = new Point();

                        if (rect.Width < Width)
                        {
                            originalSize.X = rect.Width;
                            point.X = (Width >> 1) - (originalSize.X >> 1);
                        }

                        if (rect.Height < Height)
                        {
                            originalSize.Y = rect.Height;
                            point.Y = (Height >> 1) - (originalSize.Y >> 1);
                        }

                        var texture = artInfo.Texture;
                        var sourceRectangle = artInfo.UV;
                        renderLists.AddGumpWithAtlas
                        (
                            (batcher) =>
                            {
                                batcher.Draw(
                                    texture,
                                    new Rectangle(x + point.X, y + point.Y, originalSize.X, originalSize.Y),
                                    new Rectangle(
                                        sourceRectangle.X + rect.X,
                                        sourceRectangle.Y + rect.Y,
                                        rect.Width,
                                        rect.Height
                                    ),
                                    hueVector,
                                    layerDepth
                                );
                                return true;
                            }
                        );
                    }

                    return base.AddToRenderLists(renderLists, x, y, ref layerDepthRef);
                }

                public void SetAmount(string amount)
                {
                    _label.Text = amount;
                }
            }
        }
    }
}
