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
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Gumps
{
    internal class NameOverheadGump : Gump
    {
        private AlphaBlendControl _background;
        private Point _lockedPosition;
        private bool _positionLocked;
        private readonly RenderedText _renderedText;
        private Texture2D _borderColor = SolidColorTextureCache.GetTexture(Color.Black);

        public NameOverheadGump(uint serial) : base(serial, 0)
        {
            CanMove = false;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;

            Entity entity = World.Get(serial);

            if (entity == null)
            {
                Dispose();

                return;
            }

            _renderedText = RenderedText.Create
            (
                string.Empty,
                entity is Mobile m ? Notoriety.GetHue(m.NotorietyFlag) : (ushort) 0x0481,
                0xFF,
                true,
                FontStyle.BlackBorder,
                TEXT_ALIGN_TYPE.TS_CENTER,
                100,
                30,
                true
            );

            SetTooltip(entity);

            BuildGump();
        }

        public bool SetName()
        {
            Entity entity = World.Get(LocalSerial);

            if (entity == null)
            {
                return false;
            }

            if (entity is Item item)
            {
                if (!World.OPL.TryGetNameAndData(item, out string t, out _))
                {
                    t = StringHelper.CapitalizeAllWords(item.ItemData.Name);

                    if (string.IsNullOrEmpty(t))
                    {
                        t = ClilocLoader.Instance.GetString(1020000 + item.Graphic, true, t);
                    }
                }

                if (string.IsNullOrEmpty(t))
                {
                    return false;
                }

                if (!item.IsCorpse && item.Amount > 1)
                {
                    t += ": " + item.Amount;
                }

                FontsLoader.Instance.SetUseHTML(true);
                FontsLoader.Instance.RecalculateWidthByInfo = true;


                int width = FontsLoader.Instance.GetWidthUnicode(_renderedText.Font, t);

                if (width > Constants.OBJECT_HANDLES_GUMP_WIDTH)
                {
                    t = FontsLoader.Instance.GetTextByWidthUnicode
                    (
                        _renderedText.Font,
                        t,
                        Constants.OBJECT_HANDLES_GUMP_WIDTH,
                        true,
                        TEXT_ALIGN_TYPE.TS_CENTER,
                        (ushort) FontStyle.BlackBorder
                    );

                    width = Constants.OBJECT_HANDLES_GUMP_WIDTH;
                }

                _renderedText.MaxWidth = width;
                _renderedText.Text = t;

                FontsLoader.Instance.RecalculateWidthByInfo = false;
                FontsLoader.Instance.SetUseHTML(false);

                Width = _background.Width = Math.Max(60, _renderedText.Width) + 4;
                Height = _background.Height = Constants.OBJECT_HANDLES_GUMP_HEIGHT + 4;

                WantUpdateSize = false;

                return true;
            }

            if (!string.IsNullOrEmpty(entity.Name))
            {
                string t = entity.Name;

                int width = FontsLoader.Instance.GetWidthUnicode(_renderedText.Font, t);

                if (width > Constants.OBJECT_HANDLES_GUMP_WIDTH)
                {
                    t = FontsLoader.Instance.GetTextByWidthUnicode
                    (
                        _renderedText.Font,
                        t,
                        Constants.OBJECT_HANDLES_GUMP_WIDTH,
                        true,
                        TEXT_ALIGN_TYPE.TS_CENTER,
                        (ushort) FontStyle.BlackBorder
                    );

                    width = Constants.OBJECT_HANDLES_GUMP_WIDTH;
                }

                _renderedText.MaxWidth = width;

                _renderedText.Text = t;

                Width = _background.Width = Math.Max(60, _renderedText.Width) + 4;
                Height = _background.Height = Constants.OBJECT_HANDLES_GUMP_HEIGHT + 4;

                WantUpdateSize = false;

                return true;
            }

            return false;
        }

        private void BuildGump()
        {
            Entity entity = World.Get(LocalSerial);

            if (entity == null)
            {
                Dispose();

                return;
            }

            Add
            (
                _background = new AlphaBlendControl(.3f)
                {
                    WantUpdateSize = false,
                    Hue = entity is Mobile m ? Notoriety.GetHue(m.NotorietyFlag) : (ushort) 0x0481
                }
            );
        }

        protected override void CloseWithRightClick()
        {
            Entity entity = World.Get(LocalSerial);

            if (entity != null)
            {
                entity.ClosedObjectHandles = true;
            }

            base.CloseWithRightClick();
        }

        protected override void OnDragBegin(int x, int y)
        {
            _positionLocked = false;

            Entity entity = World.Get(LocalSerial);

            if (entity is Mobile || entity is Item it && it.IsDamageable)
            {
                if (UIManager.IsDragging)
                {
                    return;
                }

                BaseHealthBarGump gump = UIManager.GetGump<BaseHealthBarGump>(LocalSerial);
                gump?.Dispose();

                if (entity == World.Player)
                {
                    StatusGumpBase.GetStatusGump()?.Dispose();
                }

                if (ProfileManager.CurrentProfile.CustomBarsToggled)
                {
                    Rectangle rect = new Rectangle(0, 0, HealthBarGumpCustom.HPB_WIDTH, HealthBarGumpCustom.HPB_HEIGHT_SINGLELINE);

                    UIManager.Add
                    (
                        gump = new HealthBarGumpCustom(entity)
                        {
                            X = Mouse.Position.X - (rect.Width >> 1),
                            Y = Mouse.Position.Y - (rect.Height >> 1)
                        }
                    );
                }
                else
                {
                    Rectangle rect = GumpsLoader.Instance.GetTexture(0x0804).Bounds;

                    UIManager.Add
                    (
                        gump = new HealthBarGump(entity)
                        {
                            X = Mouse.LClickPosition.X - (rect.Width >> 1),
                            Y = Mouse.LClickPosition.Y - (rect.Height >> 1)
                        }
                    );
                }

                UIManager.AttemptDragControl(gump, true);
            }
            else if (entity != null)
            {
                GameActions.PickUp(LocalSerial, 0, 0);

                //if (entity.Texture != null)
                //    GameActions.PickUp(LocalSerial, entity.Texture.Width >> 1, entity.Texture.Height >> 1);
                //else
                //    GameActions.PickUp(LocalSerial, 0, 0);
            }
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                if (SerialHelper.IsMobile(LocalSerial))
                {
                    if (World.Player.InWarMode)
                    {
                        GameActions.Attack(LocalSerial);
                    }
                    else
                    {
                        GameActions.DoubleClick(LocalSerial);
                    }
                }
                else
                {
                    if (!GameActions.OpenCorpse(LocalSerial))
                    {
                        GameActions.DoubleClick(LocalSerial);
                    }
                }

                return true;
            }

            return false;
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                if (!ItemHold.Enabled)
                {
                    if (UIManager.IsDragging || Math.Max(Math.Abs(Mouse.LDragOffset.X), Math.Abs(Mouse.LDragOffset.Y)) >= 1)
                    {
                        _positionLocked = false;

                        return;
                    }
                }

                if (TargetManager.IsTargeting)
                {
                    switch (TargetManager.TargetingState)
                    {
                        case CursorTarget.Position:
                        case CursorTarget.Object:
                        case CursorTarget.Grab:
                        case CursorTarget.SetGrabBag:
                            TargetManager.Target(LocalSerial);
                            Mouse.LastLeftButtonClickTime = 0;

                            break;

                        case CursorTarget.SetTargetClientSide:
                            TargetManager.Target(LocalSerial);
                            Mouse.LastLeftButtonClickTime = 0;
                            UIManager.Add(new InspectorGump(World.Get(LocalSerial)));

                            break;

                        case CursorTarget.HueCommandTarget:
                            CommandManager.OnHueTarget(World.Get(LocalSerial));

                            break;
                    }
                }
                else
                {
                    if (ItemHold.Enabled && !ItemHold.IsFixedPosition)
                    {
                        uint drop_container = 0xFFFF_FFFF;
                        bool can_drop = false;
                        ushort dropX = 0;
                        ushort dropY = 0;
                        sbyte dropZ = 0;

                        Entity obj = World.Get(LocalSerial);

                        if (obj != null)
                        {
                            can_drop = obj.Distance <= Constants.DRAG_ITEMS_DISTANCE;

                            if (can_drop)
                            {
                                if (obj is Item it && it.ItemData.IsContainer || obj is Mobile)
                                {
                                    dropX = 0xFFFF;
                                    dropY = 0xFFFF;
                                    dropZ = 0;
                                    drop_container = obj.Serial;
                                }
                                else if (obj is Item it2 && (it2.ItemData.IsSurface || it2.ItemData.IsStackable && it2.DisplayedGraphic == ItemHold.DisplayedGraphic))
                                {
                                    dropX = obj.X;
                                    dropY = obj.Y;
                                    dropZ = obj.Z;

                                    if (it2.ItemData.IsSurface)
                                    {
                                        dropZ += (sbyte) (it2.ItemData.Height == 0xFF ? 0 : it2.ItemData.Height);
                                    }
                                    else
                                    {
                                        drop_container = obj.Serial;
                                    }
                                }
                            }
                            else
                            {
                                Client.Game.Scene.Audio.PlaySound(0x0051);
                            }

                            if (can_drop)
                            {
                                if (drop_container == 0xFFFF_FFFF && dropX == 0 && dropY == 0)
                                {
                                    can_drop = false;
                                }

                                if (can_drop)
                                {
                                    GameActions.DropItem
                                    (
                                        ItemHold.Serial,
                                        dropX,
                                        dropY,
                                        dropZ,
                                        drop_container
                                    );
                                }
                            }
                        }
                    }
                    else if (!DelayedObjectClickManager.IsEnabled)
                    {
                        DelayedObjectClickManager.Set(LocalSerial, Mouse.Position.X, Mouse.Position.Y, Time.Ticks + Mouse.MOUSE_DELAY_DOUBLE_CLICK);
                    }
                }
            }

            base.OnMouseUp(x, y, button);
        }

        protected override void OnMouseOver(int x, int y)
        {
            if (_positionLocked)
            {
                return;
            }

            if (SerialHelper.IsMobile(LocalSerial))
            {
                Mobile m = World.Mobiles.Get(LocalSerial);

                if (m == null)
                {
                    Dispose();

                    return;
                }

                _positionLocked = true;

                AnimationsLoader.Instance.GetAnimationDimensions
                (
                    m.AnimIndex,
                    m.GetGraphicForAnimation(),
                    /*(byte) m.GetDirectionForAnimation()*/
                    0,
                    /*Mobile.GetGroupForAnimation(m, isParent:true)*/
                    0,
                    m.IsMounted,
                    /*(byte) m.AnimIndex*/
                    0,
                    out int centerX,
                    out int centerY,
                    out int width,
                    out int height
                );

                _lockedPosition.X = (int) (m.RealScreenPosition.X + m.Offset.X + 22 + 5);

                _lockedPosition.Y = (int) (m.RealScreenPosition.Y + (m.Offset.Y - m.Offset.Z) - (height + centerY + 8) + (m.IsGargoyle && m.IsFlying ? -22 : !m.IsMounted ? 22 : 0));
            }

            base.OnMouseOver(x, y);
        }

        protected override void OnMouseExit(int x, int y)
        {
            _positionLocked = false;
            base.OnMouseExit(x, y);
        }

        public override void Update(double totalTime, double frameTime)
        {
            base.Update(totalTime, frameTime);

            Entity entity = World.Get(LocalSerial);

            if (entity == null || entity.IsDestroyed || !entity.UseObjectHandles || entity.ClosedObjectHandles)
            {
                Dispose();
            }
            else
            {
                if (entity == TargetManager.LastTargetInfo.Serial)
                {
                    _borderColor = SolidColorTextureCache.GetTexture(Color.Red);
                    _background.Hue = _renderedText.Hue = entity is Mobile m ? Notoriety.GetHue(m.NotorietyFlag) : (ushort) 0x0481;
                }
                else
                {
                    _borderColor = SolidColorTextureCache.GetTexture(Color.Black);
                    _background.Hue = _renderedText.Hue = entity is Mobile m ? Notoriety.GetHue(m.NotorietyFlag) : (ushort) 0x0481;
                }
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed || !SetName())
            {
                return false;
            }

            int gx = ProfileManager.CurrentProfile.GameWindowPosition.X;
            int gy = ProfileManager.CurrentProfile.GameWindowPosition.Y;
            int w = ProfileManager.CurrentProfile.GameWindowSize.X;
            int h = ProfileManager.CurrentProfile.GameWindowSize.Y;

            if (SerialHelper.IsMobile(LocalSerial))
            {
                Mobile m = World.Mobiles.Get(LocalSerial);

                if (m == null)
                {
                    Dispose();

                    return false;
                }

                if (_positionLocked)
                {
                    x = _lockedPosition.X;
                    y = _lockedPosition.Y;
                }
                else
                {
                    AnimationsLoader.Instance.GetAnimationDimensions
                    (
                        m.AnimIndex,
                        m.GetGraphicForAnimation(),
                        /*(byte) m.GetDirectionForAnimation()*/
                        0,
                        /*Mobile.GetGroupForAnimation(m, isParent:true)*/
                        0,
                        m.IsMounted,
                        /*(byte) m.AnimIndex*/
                        0,
                        out int centerX,
                        out int centerY,
                        out int width,
                        out int height
                    );

                    x = (int) (m.RealScreenPosition.X + m.Offset.X + 22 + 5);

                    y = (int) (m.RealScreenPosition.Y + (m.Offset.Y - m.Offset.Z) - (height + centerY + 8) + (m.IsGargoyle && m.IsFlying ? -22 : !m.IsMounted ? 22 : 0));
                }
            }
            else if (SerialHelper.IsItem(LocalSerial))
            {
                Item item = World.Items.Get(LocalSerial);

                if (item == null)
                {
                    Dispose();

                    return false;
                }

                ArtTexture texture = ArtLoader.Instance.GetTexture(item.Graphic);

                if (texture != null)
                {
                    x = item.RealScreenPosition.X + (int) item.Offset.X + 22 + 5;

                    y = item.RealScreenPosition.Y + (int) (item.Offset.Y - item.Offset.Z) + (texture.ImageRectangle.Height >> 1);
                }
                else
                {
                    x = item.RealScreenPosition.X + (int) item.Offset.X + 22 + 5;
                    y = item.RealScreenPosition.Y + (int) (item.Offset.Y - item.Offset.Z) + 22;
                }
            }


            ResetHueVector();

            Point p = Client.Game.Scene.Camera.WorldToScreen(new Point(x, y));
            x = p.X - (Width >> 1);
            y = p.Y - (Height >> 1);

            x += gx;
            y += gy;

            if (x < gx || x + Width > gx + w)
            {
                return false;
            }

            if (y < gy || y + Height > gy + h)
            {
                return false;
            }

            X = x;
            Y = y;

            batcher.DrawRectangle
            (
                _borderColor,
                x - 1,
                y - 1,
                Width + 1,
                Height + 1,
                ref HueVector
            );

            base.Draw(batcher, x, y);

            int renderedTextOffset = Math.Max(0, Width - _renderedText.Width - 4) >> 1;

            return _renderedText.Draw
            (
                batcher,
                Width,
                Height,
                x + 2 + renderedTextOffset,
                y + 2,
                Width,
                Height,
                0,
                0
            );
        }


        public override void Dispose()
        {
            _renderedText?.Destroy();
            base.Dispose();
        }
    }
}