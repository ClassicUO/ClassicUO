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

using ClassicUO.Assets;
using ClassicUO.Configuration;
using FontStyle = ClassicUO.Game.FontStyle;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace ClassicUO.Game.UI.Gumps
{
    internal class NameOverheadGump : Gump
    {
        private AlphaBlendControl _background;
        private Point _lockedPosition,
            _lastLeftMousePositionDown;
        private bool _positionLocked,
            _leftMouseIsDown,
            _isLastTarget,
            _needsNameUpdate;
        private UOLabel _text;
        private Texture2D _borderColor = SolidColorTextureCache.GetTexture(Color.Black);
        private Vector2 _textDrawOffset = Vector2.Zero;
        private static int currentHeight = 18;

        public static int CurrentHeight
        {
            get
            {
                if (NameOverHeadManager.IsShowing)
                {
                    return currentHeight;
                }

                return 0;
            }
            private set
            {
                currentHeight = value;
            }
        }

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

            _text = new UOLabel(string.Empty, ProfileManager.CurrentProfile.NamePlateFont, entity is Mobile m ? Notoriety.GetHue(m.NotorietyFlag) : (ushort)0x0481, TEXT_ALIGN_TYPE.TS_CENTER, 0, FontStyle.BlackBorder);

            SetTooltip(entity);

            BuildGump();
            SetName();
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
                    _needsNameUpdate = true;
                    if (!item.IsCorpse && item.Amount > 1)
                    {
                        t = item.Amount.ToString() + ' ';
                    }

                    if (string.IsNullOrEmpty(item.ItemData.Name))
                    {
                        t += ClilocLoader.Instance.GetString(1020000 + item.Graphic, true, t);
                    }
                    else
                    {
                        t += StringHelper.CapitalizeAllWords(
                            StringHelper.GetPluralAdjustedString(
                                item.ItemData.Name,
                                item.Amount > 1
                            )
                        );
                    }
                }
                else
                {
                    _needsNameUpdate = false;
                }

                if (string.IsNullOrEmpty(t))
                {
                    return false;
                }

                _text.Text = t;

                Width = _background.Width = _text.Width + 4;
                Height = _background.Height = CurrentHeight = _text.Height;
                _textDrawOffset.X = (Width - _text.Width - 4) >> 1;
                _textDrawOffset.Y = (Height - _text.Height) >> 1;
                WantUpdateSize = false;

                return true;
            }

            if (!string.IsNullOrEmpty(entity.Name))
            {
                string t = entity.Name;

                _text.Text = t;

                int baseHeight = _text.Height;
                bool isSelfOrParty = entity is Mobile mob && (mob.Equals(World.Player) || World.Party.Contains(mob.Serial));
                bool hasOtherBarBelow = entity is Mobile m2 && !m2.Equals(World.Player) && !World.Party.Contains(m2.Serial)
                    && m2.NotorietyFlag != NotorietyFlag.Invulnerable
                    && ProfileManager.CurrentProfile.NamePlateHealthBar;
                bool hasSelfBarsBelow = isSelfOrParty && ProfileManager.CurrentProfile.NamePlateHealthBar;
                int barExtra = hasOtherBarBelow ? 8 : hasSelfBarsBelow ? 20 : 0;
                Width = _background.Width = _text.Width + 4;
                Height = _background.Height = baseHeight + barExtra;
                CurrentHeight = Height;
                _textDrawOffset.X = (Width - _text.Width - 4) >> 1;
                _textDrawOffset.Y = hasOtherBarBelow || hasSelfBarsBelow ? 0 : (Height - _text.Height) >> 1;
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
                _background = new AlphaBlendControl(ProfileManager.CurrentProfile.NamePlateOpacity / 100f)
                {
                    WantUpdateSize = false,
                    Hue = entity is Mobile m ? Notoriety.GetHue(m.NotorietyFlag) : Notoriety.GetHue(NotorietyFlag.Gray)
                }
            );
        }

        protected override void CloseWithRightClick()
        {
            Entity entity = World.Get(LocalSerial);

            if (entity != null)
            {
                entity.ObjectHandlesStatus = ObjectHandlesStatus.CLOSED;
            }

            base.CloseWithRightClick();
        }

        private void DoDrag()
        {
            var delta = Mouse.Position - _lastLeftMousePositionDown;

            if (
                Math.Abs(delta.X) <= Constants.MIN_GUMP_DRAG_DISTANCE
                && Math.Abs(delta.Y) <= Constants.MIN_GUMP_DRAG_DISTANCE
            )
            {
                return;
            }

            _leftMouseIsDown = false;
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

                if (ProfileManager.CurrentProfile.CustomBarsToggled)
                {
                    Rectangle rect = new Rectangle(
                        0,
                        0,
                        HealthBarGumpCustom.HPB_WIDTH,
                        HealthBarGumpCustom.HPB_HEIGHT_SINGLELINE
                    );

                    UIManager.Add(
                        gump = new HealthBarGumpCustom(entity)
                        {
                            X = Mouse.Position.X - (rect.Width >> 1),
                            Y = Mouse.Position.Y - (rect.Height >> 1)
                        }
                    );
                }
                else
                {
                    ref readonly var gumpInfo = ref Client.Game.Gumps.GetGump(0x0804);

                    UIManager.Add(
                        gump = new HealthBarGump(entity)
                        {
                            X = Mouse.LClickPosition.X - (gumpInfo.UV.Width >> 1),
                            Y = Mouse.LClickPosition.Y - (gumpInfo.UV.Height >> 1)
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

        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                _lastLeftMousePositionDown = Mouse.Position;
                _leftMouseIsDown = true;
            }

            base.OnMouseDown(x, y, button);
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                _leftMouseIsDown = false;

                if (!Client.Game.GameCursor.ItemHold.Enabled)
                {
                    if (
                        UIManager.IsDragging
                        || Math.Max(Math.Abs(Mouse.LDragOffset.X), Math.Abs(Mouse.LDragOffset.Y))
                            >= 1
                    )
                    {
                        _positionLocked = false;

                        return;
                    }
                }

                if (TargetManager.IsTargeting)
                {
                    switch (TargetManager.TargetingState)
                    {
                        case CursorTarget.Internal:
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
                    if (
                        Client.Game.GameCursor.ItemHold.Enabled
                        && !Client.Game.GameCursor.ItemHold.IsFixedPosition
                    )
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
                                else if (
                                    obj is Item it2
                                    && (
                                        it2.ItemData.IsSurface
                                        || it2.ItemData.IsStackable
                                            && it2.DisplayedGraphic
                                                == Client.Game.GameCursor.ItemHold.DisplayedGraphic
                                    )
                                )
                                {
                                    dropX = obj.X;
                                    dropY = obj.Y;
                                    dropZ = obj.Z;

                                    if (it2.ItemData.IsSurface)
                                    {
                                        dropZ += (sbyte)(
                                            it2.ItemData.Height == 0xFF ? 0 : it2.ItemData.Height
                                        );
                                    }
                                    else
                                    {
                                        drop_container = obj.Serial;
                                    }
                                }
                            }
                            else
                            {
                                Client.Game.Audio.PlaySound(0x0051);
                            }

                            if (can_drop)
                            {
                                if (drop_container == 0xFFFF_FFFF && dropX == 0 && dropY == 0)
                                {
                                    can_drop = false;
                                }

                                if (can_drop)
                                {
                                    GameActions.DropItem(
                                        Client.Game.GameCursor.ItemHold.Serial,
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
                        DelayedObjectClickManager.Set(
                            LocalSerial,
                            Mouse.Position.X,
                            Mouse.Position.Y,
                            Time.Ticks + Mouse.MOUSE_DELAY_DOUBLE_CLICK
                        );
                    }
                }
            }

            base.OnMouseUp(x, y, button);
        }

        protected override void OnMouseOver(int x, int y)
        {
            if (_leftMouseIsDown)
            {
                DoDrag();
            }

            if (!_positionLocked && SerialHelper.IsMobile(LocalSerial))
            {
                Mobile m = World.Mobiles.Get(LocalSerial);

                if (m == null)
                {
                    Dispose();

                    return;
                }

                _positionLocked = true;
            }

            base.OnMouseOver(x, y);
        }

        protected override void OnMouseExit(int x, int y)
        {
            _positionLocked = false;
            base.OnMouseExit(x, y);
        }

        public override void Update()
        {
            base.Update();

            Entity entity = World.Get(LocalSerial);

            if (
                entity == null
                || entity.IsDestroyed
                || entity.ObjectHandlesStatus == ObjectHandlesStatus.NONE
                || entity.ObjectHandlesStatus == ObjectHandlesStatus.CLOSED
            )
            {
                Dispose();
            }
            else
            {
                if (entity.Serial == TargetManager.LastTargetInfo.Serial && !entity.Equals(World.Player))
                {
                    if (!_isLastTarget) //Only set this if it was not already last target
                    {
                        _borderColor = SolidColorTextureCache.GetTexture(Color.Red);
                        _background.Hue = (ushort)(_text.Hue = entity is Mobile m
                            ? Notoriety.GetHue(m.NotorietyFlag)
                            : (ushort)0x0481);
                        _isLastTarget = true;
                    }
                }
                else if (_isLastTarget)
                {
                    _borderColor = SolidColorTextureCache.GetTexture(Color.Black);
                    _background.Hue = (ushort)(_text.Hue = entity is Mobile m
                        ? Notoriety.GetHue(m.NotorietyFlag)
                        : (ushort)0x0481);
                    _isLastTarget = false;
                }

                if (_needsNameUpdate)
                {
                    SetName();
                }
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
            {
                return false;
            }

            bool _isMobile = false;
            double _hpPercent = 1;
            IsVisible = true;
            if (SerialHelper.IsMobile(LocalSerial))
            {
                Mobile m = World.Mobiles.Get(LocalSerial);

                if (m == null)
                {
                    Dispose();

                    return false;
                }

                if (!string.IsNullOrEmpty(NameOverHeadManager.Search))
                {
                    string sText = NameOverHeadManager.Search.ToLower();
                    if (m.Name == null || !m.Name.ToLower().Contains(sText))
                    {
                        if (World.OPL.TryGetNameAndData(m.Serial, out string name, out string data))
                        {
                            if (/*(data != null && !data.ToLower().Contains(sText)) && */(name != null && !name.ToLower().Contains(sText)))
                            {
                                IsVisible = false;
                                return true;
                            }
                        }
                        else
                        {
                            IsVisible = false;
                            return true;
                        }
                    }
                }

                _isMobile = true;
                _hpPercent = (double)m.Hits / (double)m.HitsMax;

                IsVisible = true;
                if (ProfileManager.CurrentProfile.NamePlateHideAtFullHealth && _hpPercent >= 1)
                {
                    if (ProfileManager.CurrentProfile.NamePlateHideAtFullHealthInWarmode)
                    {
                        if (World.Player.InWarMode)
                        {
                            IsVisible = false;
                            return false;
                        }

                    }
                    else
                    {
                        IsVisible = false;
                        return false;
                    }

                }

                Client.Game.Animations.GetAnimationDimensions(
                    m.AnimIndex,
                    m.GetGraphicForAnimation(),
                    /*(byte) m.GetDirectionForAnimation())*/
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

                x = (int)(m.RealScreenPosition.X + m.Offset.X + 22 + 5);
                y = (int)(
                    m.RealScreenPosition.Y
                    + (m.Offset.Y - m.Offset.Z)
                    - (height + centerY + 15)
                    + (
                        m.IsGargoyle && m.IsFlyingVisual
                            ? -22
                            : !m.IsMounted
                                ? 22
                                : 0
                    )
                    + 8
                );
            }
            else if (SerialHelper.IsItem(LocalSerial))
            {
                Item item = World.Items.Get(LocalSerial);

                if (item == null)
                {
                    Dispose();
                    return false;
                }

                if (!string.IsNullOrEmpty(NameOverHeadManager.Search))
                {
                    string sText = NameOverHeadManager.Search.ToLower();
                    if (item.Name == null || !item.Name.ToLower().Contains(sText))// && (!item.ItemData.Name?.ToLower().Contains(sText)))
                    {
                        if (World.OPL.TryGetNameAndData(item.Serial, out string name, out string data))
                        {
                            if ((data != null && !data.ToLower().Contains(sText)) && (name != null && !name.ToLower().Contains(sText)))
                            {
                                IsVisible = false;
                                return true;
                            }
                        }
                        else
                        {
                            IsVisible = false;
                            return true;
                        }
                    }
                }

                var bounds = Client.Game.Arts.GetRealArtBounds(item.Graphic);

                x = item.RealScreenPosition.X + (int)item.Offset.X + 22 + 5;
                y =
                    item.RealScreenPosition.Y
                    + (int)(item.Offset.Y - item.Offset.Z)
                    + (bounds.Height >> 1);
            }

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            Point p = Client.Game.Scene.Camera.WorldToScreen(new Point(x, y));
            x = p.X - (Width >> 1);
            y = p.Y - (Height);// >> 1);

            var camera = Client.Game.Scene.Camera;
            x += camera.Bounds.X;
            y += camera.Bounds.Y;

            if (x < camera.Bounds.X || x + Width > camera.Bounds.Right)
            {
                return false;
            }

            if (y < camera.Bounds.Y || y + Height > camera.Bounds.Bottom)
            {
                return false;
            }

            X = x;
            Y = y;

            hueVector.Z = ProfileManager.CurrentProfile.NamePlateBorderOpacity / 100f;

            batcher.DrawRectangle
            (
                _borderColor,
                x,
                y,
                Width,
                Height,
                hueVector
            );

            base.Draw(batcher, x, y);

            if (ProfileManager.CurrentProfile.NamePlateHealthBar && _isMobile)
            {
                Mobile m = World.Mobiles.Get(LocalSerial);
                var isPlayer = m is PlayerMobile;
                var isInParty = World.Party.Contains(m.Serial);
                var _alpha = ProfileManager.CurrentProfile.NamePlateHealthBarOpacity / 100f;

                if (isPlayer || isInParty)
                {
                    DrawSelfBarsBelowName(batcher, m, x, y, _alpha);
                }
                else if (m.NotorietyFlag != NotorietyFlag.Invulnerable)
                {
                    DrawHpBarBelowName(batcher, m, x, y, _alpha);
                }
            }

            if (_isLastTarget && _isMobile)
            {
                Mobile m = World.Mobiles.Get(LocalSerial);
                if (m != null && !m.Equals(World.Player))
                    DrawDistanceBar(batcher, m, x, y);
            }

            return _text.Draw(batcher, (int)(x + 2 + _textDrawOffset.X), (int)(y + 2 + _textDrawOffset.Y));
        }

        private const int DIST_BAR_HEIGHT = 5;
        private const int DIST_BAR_GAP = 2;

        private void DrawDistanceBar(UltimaBatcher2D batcher, Mobile m, int x, int y)
        {
            int dist = m.Distance;
            Color barColor = dist <= 6 ? Color.Green : dist <= 12 ? Color.Yellow : Color.Red;
            float fillFraction = dist <= 6 ? 1f : dist <= 12 ? (12f - dist) / 6f : 0.15f;
            int barWidth = Width;
            int barY = y + Height + DIST_BAR_GAP;
            Vector3 alpha = ShaderHueTranslator.GetHueVector(0, false, 0.85f, true);
            Vector3 border = ShaderHueTranslator.GetHueVector(0, false, 0.9f, true);
            Color borderColor = new Color(
                Math.Max(0, barColor.R - 80),
                Math.Max(0, barColor.G - 80),
                Math.Max(0, barColor.B - 80)
            );
            batcher.DrawRectangle(SolidColorTextureCache.GetTexture(borderColor), x + 1, barY, barWidth - 2, DIST_BAR_HEIGHT + 2, border);
            int fillW = Math.Max(1, (int)((barWidth - 4) * fillFraction));
            batcher.Draw(SolidColorTextureCache.GetTexture(barColor), new Vector2(x + 2, barY + 1), new Rectangle(0, 0, fillW, DIST_BAR_HEIGHT), alpha);
        }
        private const int BAR_GAP = 2;
        private const int HP_BAR_HEIGHT = 4;

        private void DrawSelfBarsBelowName(UltimaBatcher2D batcher, Mobile m, int x, int y, float alpha)
        {
            int barWidth = Width;
            int startY = y + Height - (HP_BAR_HEIGHT * 3 + BAR_GAP * 2 + 2);
            Vector3 barHue = ShaderHueTranslator.GetHueVector(0, false, alpha, true);
            Vector3 borderHue = ShaderHueTranslator.GetHueVector(0, false, 0.9f, true);

            double hpPercent = (double)m.Hits / m.HitsMax;
            Color hpColor = hpPercent > 0.6 ? Color.Green : hpPercent > 0.3 ? Color.Yellow : Color.Red;
            DrawColoredBar(batcher, x, startY, barWidth, SolidColorTextureCache.GetTexture(hpColor), hpColor, barHue, borderHue, hpPercent);

            startY += HP_BAR_HEIGHT + BAR_GAP;
            double mpPercent = (double)m.Mana / m.ManaMax;
            Color manaColor = Color.Blue;
            DrawColoredBar(batcher, x, startY, barWidth, SolidColorTextureCache.GetTexture(manaColor), manaColor, barHue, borderHue, mpPercent);

            startY += HP_BAR_HEIGHT + BAR_GAP;
            double spPercent = (double)m.Stamina / m.StaminaMax;
            Color staminaColor = Color.Red;
            DrawColoredBar(batcher, x, startY, barWidth, SolidColorTextureCache.GetTexture(staminaColor), staminaColor, barHue, borderHue, spPercent);
        }

        private void DrawColoredBar(UltimaBatcher2D batcher, int x, int barY, int barWidth, Texture2D texture, Color barColor, Vector3 barHue, Vector3 borderHue, double percent)
        {
            Color borderColor = new Color(
                Math.Max(0, barColor.R - 80),
                Math.Max(0, barColor.G - 80),
                Math.Max(0, barColor.B - 80)
            );
            batcher.DrawRectangle(
                SolidColorTextureCache.GetTexture(borderColor),
                x + 1,
                barY,
                barWidth - 2,
                HP_BAR_HEIGHT + 2,
                borderHue
            );
            int fillWidth = Math.Max(0, (int)((barWidth - 4) * percent));
            if (fillWidth > 0)
            {
                batcher.Draw(texture, new Vector2(x + 2, barY + 1), new Rectangle(0, 0, fillWidth, HP_BAR_HEIGHT), barHue);
            }
        }

        private void DrawHpBarBelowName(UltimaBatcher2D batcher, Mobile m, int x, int y, float alpha)
        {
            int barY = y + Height - HP_BAR_HEIGHT - 2;
            int barWidth = Width;
            double hpPercent = (double)m.Hits / m.HitsMax;
            Color barColor = hpPercent > 0.6 ? Color.Green : hpPercent > 0.3 ? Color.Yellow : Color.Red;
            Vector3 barHue = ShaderHueTranslator.GetHueVector(0, false, alpha, true);
            Vector3 borderHue = ShaderHueTranslator.GetHueVector(0, false, 0.9f, true);
            DrawColoredBar(batcher, x, barY, barWidth, SolidColorTextureCache.GetTexture(barColor), barColor, barHue, borderHue, hpPercent);
        }

        public override void Dispose()
        {
            _text.Dispose();
            base.Dispose();
        }
    }
}
