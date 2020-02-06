#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class NameOverheadGump : Gump
    {
        private readonly AlphaBlendControl _background;

        private readonly RenderedText _renderedText;
        private const int MIN_WIDTH = 60;
        private bool _positionLocked;
        private Point _lockedPosition;

        public NameOverheadGump(Entity entity) : base(entity.Serial, 0)
        {
            CanMove = false;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
            Entity = entity;

            ushort hue = entity is Mobile m ? Notoriety.GetHue(m.NotorietyFlag) : (ushort) 0x0481;

            _renderedText = RenderedText.Create(String.Empty, hue, 0xFF, true, FontStyle.BlackBorder, TEXT_ALIGN_TYPE.TS_CENTER, 100, 30, true);

            Add(_background = new AlphaBlendControl(.3f)
            {
                WantUpdateSize = false,
                Hue = hue
            });

            SetTooltip(entity);
        }

        public Entity Entity { get; }

        private bool SetName()
        {
            if (string.IsNullOrEmpty(_renderedText.Text))
            {
                if (Entity is Item item)
                {
                    if (!World.OPL.TryGetNameAndData(item, out string t, out _))
                    {
                        t = StringHelper.CapitalizeAllWords(item.ItemData.Name);

                        if (string.IsNullOrEmpty(t))
                            t = ClilocLoader.Instance.Translate(1020000 + item.Graphic, capitalize: true);
                    }

                    if (string.IsNullOrEmpty(t))
                        return false;

                    if (!item.IsCorpse && item.Amount > 1)
                        t += ": " + item.Amount;

                    FontsLoader.Instance.SetUseHTML(true);
                    FontsLoader.Instance.RecalculateWidthByInfo = true;


                    int width = FontsLoader.Instance.GetWidthUnicode(_renderedText.Font, t);

                    if (width > 100)
                    {
                        t = FontsLoader.Instance.GetTextByWidthUnicode(_renderedText.Font, t, 100, true, TEXT_ALIGN_TYPE.TS_CENTER, (ushort)FontStyle.BlackBorder);
                        width = 100;
                    }

                    //if (width > 100)
                    //    width = 100;

                    //width = FileManager.Fonts.GetWidthExUnicode(_renderedText.Font, t, width, TEXT_ALIGN_TYPE.TS_CENTER, (ushort) (FontStyle.BlackBorder /*| FontStyle.Cropped*/));

                    //if (width > 100)
                    //    width = 100;

                    _renderedText.MaxWidth = width;

                    _renderedText.Text = t;

                    FontsLoader.Instance.RecalculateWidthByInfo = false;
                    FontsLoader.Instance.SetUseHTML(false);

                    Width = _background.Width = _renderedText.Width + 4;
                    Height = _background.Height = _renderedText.Height + 4;

                    WantUpdateSize = false;

                    return true;
                }


                if (!string.IsNullOrEmpty(Entity.Name))
                {
                    string t = Entity.Name;

                    int width = FontsLoader.Instance.GetWidthUnicode(_renderedText.Font, t);

                    if (width > 100)
                    {
                        t = FontsLoader.Instance.GetTextByWidthUnicode(_renderedText.Font, t, 100, true, TEXT_ALIGN_TYPE.TS_CENTER, (ushort)FontStyle.BlackBorder);
                        width = 100;
                    }

                    //int width = FileManager.Fonts.GetWidthUnicode(_renderedText.Font, Entity.Name);

                    //if (width > 200)
                    //    width = 200;

                    //width = FileManager.Fonts.GetWidthExUnicode(_renderedText.Font, Entity.Name, width, TEXT_ALIGN_TYPE.TS_CENTER, (ushort)(FontStyle.BlackBorder));

                    //if (width > 200)
                    //    width = 200;

                    _renderedText.MaxWidth = width;

                    _renderedText.Text = t;

                    Width = _background.Width = Math.Max(_renderedText.Width + 4, MIN_WIDTH);
                    Height = _background.Height = _renderedText.Height + 4;

                    WantUpdateSize = false;

                    return true;
                }

                return false;
            }

            return true;
        }


        protected override void CloseWithRightClick()
        {
            Entity.ClosedObjectHandles = true;
            base.CloseWithRightClick();
        }

        protected override void OnDragBegin(int x, int y)
        {
            if (Entity is Mobile || Entity is Item it && it.IsDamageable)
            {
                if (UIManager.IsDragging)
                    return;

                GameActions.RequestMobileStatus(Entity);
                BaseHealthBarGump gump = UIManager.GetGump<BaseHealthBarGump>(Entity);
                gump?.Dispose();

                if (Entity == World.Player)
                    StatusGumpBase.GetStatusGump()?.Dispose();

                if (ProfileManager.Current.CustomBarsToggled)
                {
                    Rectangle rect = new Rectangle(0, 0, HealthBarGumpCustom.HPB_WIDTH, HealthBarGumpCustom.HPB_HEIGHT_SINGLELINE);
                    UIManager.Add(gump = new HealthBarGumpCustom(Entity) { X = Mouse.Position.X - (rect.Width >> 1), Y = Mouse.Position.Y - (rect.Height >> 1) });
                }
                else
                {
                    Rectangle rect = GumpsLoader.Instance.GetTexture(0x0804).Bounds;
                    UIManager.Add(gump = new HealthBarGump(Entity) { X = Mouse.Position.X - (rect.Width >> 1), Y = Mouse.Position.Y - (rect.Height >> 1) });
                }

                UIManager.AttemptDragControl(gump, Mouse.Position, true);
            }
            else
            {
                if (Entity.Texture != null)
                    GameActions.PickUp(Entity, Entity.Texture.Width >> 1, Entity.Texture.Height >> 1);
                else
                    GameActions.PickUp(Entity, 0, 0);
            }
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                if (World.Player.InWarMode && Entity is Mobile)
                    GameActions.Attack(Entity);
                else if (!GameActions.OpenCorpse(Entity))
                    GameActions.DoubleClick(Entity);

                return true;
            }

            return false;
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                GameScene scene = Client.Game.GetScene<GameScene>();

                if (!ItemHold.Enabled)
                {
                    if (UIManager.IsDragging || Math.Max(Math.Abs(Mouse.LDroppedOffset.X), Math.Abs(Mouse.LDroppedOffset.Y)) >= 1)
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
                            if (Entity != null)
                                TargetManager.Target(Entity);
                            Mouse.LastLeftButtonClickTime = 0;

                            break;

                        case CursorTarget.SetTargetClientSide:
                            if (Entity != null)
                                TargetManager.Target(Entity);
                            Mouse.LastLeftButtonClickTime = 0;
                            UIManager.Add(new InspectorGump(Entity));

                            break;

                        case CursorTarget.HueCommandTarget:
                            CommandManager.OnHueTarget(Entity);

                            break;

                    }
                }
                else
                {
                    if (ItemHold.Enabled)
                    {
                        if (Entity.Distance < Constants.DRAG_ITEMS_DISTANCE)
                        {
                            if (SerialHelper.IsItem(Entity.Serial))
                                scene.DropHeldItemToContainer(World.Items.Get(Entity));
                            else if (SerialHelper.IsMobile(Entity.Serial))
                                scene.MergeHeldItem(World.Mobiles.Get(Entity));
                        }
                        else
                            scene.Audio.PlaySound(0x0051);

                        return;
                    }
                    else if (!DelayedObjectClickManager.IsEnabled)
                    {
                        DelayedObjectClickManager.Set(Entity.Serial, Mouse.Position.X, Mouse.Position.Y, Time.Ticks + Mouse.MOUSE_DELAY_DOUBLE_CLICK);
                    }
                    
                }
            }

            base.OnMouseUp(x, y, button);
        }

        protected override void OnMouseOver(int x, int y)
        {
            if (_positionLocked)
                return;

            float scale = Client.Game.GetScene<GameScene>().Scale;

            if (Entity is Mobile m)
            {
                _positionLocked = true;

                AnimationsLoader.Instance.GetAnimationDimensions(m.AnimIndex,
                                                              m.GetGraphicForAnimation(),
                                                              /*(byte) m.GetDirectionForAnimation()*/ 0,
                                                              /*Mobile.GetGroupForAnimation(m, isParent:true)*/ 0,
                                                              m.IsMounted,
                                                              /*(byte) m.AnimIndex*/ 0,
                                                              out int centerX,
                                                              out int centerY,
                                                              out int width,
                                                              out int height);

                _lockedPosition.X = (int)((Entity.RealScreenPosition.X + m.Offset.X + 22) / scale);
                _lockedPosition.Y = (int)((Entity.RealScreenPosition.Y + (m.Offset.Y - m.Offset.Z) - (height + centerY + 8) + (m.IsGargoyle && m.IsFlying ? -22 : !m.IsMounted ? 22 : 0)) / scale);
            }

            base.OnMouseOver(x, y);

        }

        protected override void OnMouseExit(int x, int y)
        {
            _positionLocked = false;
            base.OnMouseExit(x, y);
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (Entity == null || Entity.IsDestroyed || !Entity.UseObjectHandles || Entity.ClosedObjectHandles)
                Dispose();
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed || !SetName())
                return false;

            float scale = Client.Game.GetScene<GameScene>().Scale;

            int gx = ProfileManager.Current.GameWindowPosition.X;
            int gy = ProfileManager.Current.GameWindowPosition.Y;
            int w = ProfileManager.Current.GameWindowSize.X;
            int h = ProfileManager.Current.GameWindowSize.Y;

            if (Entity is Mobile m)
            {
                if (_positionLocked)
                {
                    x = _lockedPosition.X;
                    y = _lockedPosition.Y;
                }
                else
                {

                    AnimationsLoader.Instance.GetAnimationDimensions(m.AnimIndex,
                                                                  m.GetGraphicForAnimation(),
                                                                  /*(byte) m.GetDirectionForAnimation()*/ 0,
                                                                  /*Mobile.GetGroupForAnimation(m, isParent:true)*/ 0,
                                                                  m.IsMounted,
                                                                  /*(byte) m.AnimIndex*/ 0,
                                                                  out int centerX,
                                                                  out int centerY,
                                                                  out int width,
                                                                  out int height);

                    x = (int)((Entity.RealScreenPosition.X + m.Offset.X + 22) / scale);
                    y = (int)((Entity.RealScreenPosition.Y + (m.Offset.Y - m.Offset.Z) - (height + centerY + 8) + (m.IsGargoyle && m.IsFlying ? -22 : !m.IsMounted ? 22 : 0)) / scale);
                }
            }
            else if (Entity.Texture != null)
            {
                switch (Entity.Texture)
                {
                    case ArtTexture artText:
                        x = (int)((Entity.RealScreenPosition.X + 22) / scale);
                        y = (int)((Entity.RealScreenPosition.Y - (artText.ImageRectangle.Height >> 1)) / scale);
                        break;
                    default:
                        x = (int)((Entity.RealScreenPosition.X + 22) / scale);
                        y = (int)((Entity.RealScreenPosition.Y - (Entity.Texture.Height >> 1)) / scale);
                        break;
                }
            }
            else
            {
                x = (int)((Entity.RealScreenPosition.X + 22) / scale);
                y = (int)((Entity.RealScreenPosition.Y + 22) / scale);
            }

            x -= Width >> 1;
            y -= Height >> 1;
            x += gx + 6;
            y += gy;

            X = x;
            Y = y;

            if (x < gx || x + Width > gx + w)
                return false;

            if (y < gy || y + Height > gy + h)
                return false;

            ResetHueVector();

            batcher.DrawRectangle(Texture2DCache.GetTexture(Color.Black), x - 1, y - 1, Width + 1, Height + 1, ref _hueVector);

            base.Draw(batcher, x, y);

            int renderedTextOffset = Math.Max(0, Width - _renderedText.Width - 4) >> 1;

            return _renderedText.Draw(batcher, Width, Height, x + 2 + renderedTextOffset, y + 2, Width, Height, 0, 0);
        }


        public override void Dispose()
        {
            _renderedText?.Destroy();
            base.Dispose();
        }
    }
}
