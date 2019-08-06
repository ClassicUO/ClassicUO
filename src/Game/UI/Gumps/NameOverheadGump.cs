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
using System.Linq;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
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
        private float _clickTiming;
        private bool _isPressed;
        private const int MIN_WIDTH = 60;
        private bool _positionLocked;
        private Point _lockedPosition;

        public NameOverheadGump(Entity entity) : base(entity.Serial, 0)
        {
            CanMove = false;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
            Entity = entity;

            Hue hue = entity is Mobile m ? Notoriety.GetHue(m.NotorietyFlag) : (Hue) 0x0481;

            _renderedText = RenderedText.Create(String.Empty, hue, 0xFF, true, FontStyle.BlackBorder, TEXT_ALIGN_TYPE.TS_CENTER, 100, 30, true);

            Add(_background = new AlphaBlendControl(.3f)
            {
                WantUpdateSize = false,
                Hue = hue
            });
        }

        public Entity Entity { get; }

        private bool SetName()
        {
            if (string.IsNullOrEmpty(_renderedText.Text))
            {
                if (Entity is Item item)
                {
                    string t;

                    if (item.Properties.Any())
                    {
                        Property prop = item.Properties.FirstOrDefault();
                        t = FileManager.Cliloc.Translate((int) prop.Cliloc, prop.Args, true);
                    }
                    else
                    {
                        t = StringHelper.CapitalizeAllWords(item.ItemData.Name);

                        if (string.IsNullOrEmpty(t))
                            t = FileManager.Cliloc.Translate(1020000 + item.Graphic, capitalize:true);
                    }

                    if (string.IsNullOrEmpty(t))
                        return false;
                    
                    FileManager.Fonts.SetUseHTML(true);
                    FileManager.Fonts.RecalculateWidthByInfo = true;


                    int width = FileManager.Fonts.GetWidthUnicode(_renderedText.Font, t);

                    if (width > 100)
                    {
                        t = FileManager.Fonts.GetTextByWidthUnicode(_renderedText.Font, t, 100, true, TEXT_ALIGN_TYPE.TS_CENTER, (ushort)FontStyle.BlackBorder);
                        width = 100;
                    }

                    //if (width > 100)
                    //    width = 100;

                    //width = FileManager.Fonts.GetWidthExUnicode(_renderedText.Font, t, width, TEXT_ALIGN_TYPE.TS_CENTER, (ushort) (FontStyle.BlackBorder /*| FontStyle.Cropped*/));

                    //if (width > 100)
                    //    width = 100;

                    _renderedText.MaxWidth = width;

                    FileManager.Fonts.RecalculateWidthByInfo = false;
                    FileManager.Fonts.SetUseHTML(false);

                    _renderedText.Text = t;

                    Width = _background.Width = _renderedText.Width + 4;
                    Height = _background.Height = _renderedText.Height + 4;

                    WantUpdateSize = false;

                    return true;
                }


                if (!string.IsNullOrEmpty(Entity.Name))
                {
                    string t = Entity.Name;

                    int width = FileManager.Fonts.GetWidthUnicode(_renderedText.Font, t);

                    if (width > 100)
                    {
                        t = FileManager.Fonts.GetTextByWidthUnicode(_renderedText.Font, t, 100, true, TEXT_ALIGN_TYPE.TS_CENTER, (ushort)FontStyle.BlackBorder);
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
            if (Entity is Mobile mob)
            {
                if (Engine.UI.IsDragging)
                    return;

                GameActions.RequestMobileStatus(mob);

                Engine.UI.GetGump<HealthBarGump>(mob)?.Dispose();

                if (mob == World.Player)
                    StatusGumpBase.GetStatusGump()?.Dispose();

                Rectangle rect = FileManager.Gumps.GetTexture(0x0804).Bounds;
                HealthBarGump currentHealthBarGump;
                Engine.UI.Add(currentHealthBarGump = new HealthBarGump(mob) {X = Mouse.Position.X - (rect.Width >> 1), Y = Mouse.Position.Y - (rect.Height >> 1)});
                Engine.UI.AttemptDragControl(currentHealthBarGump, Mouse.Position, true);
            }
            else
                GameActions.PickUp(Entity);
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                _isPressed = false;
                _clickTiming = 0;

                if (World.Player.InWarMode && Entity is Mobile)
                    GameActions.Attack(Entity);
                else
                    GameActions.DoubleClick(Entity);
            }

            return true;
        }

        protected override void OnMouseUp(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                GameScene scene = Engine.SceneManager.GetScene<GameScene>();

                if (!scene.IsHoldingItem)
                {
                    if (Engine.UI.IsDragging || Math.Max(Math.Abs(Mouse.LDroppedOffset.X), Math.Abs(Mouse.LDroppedOffset.Y)) >= 1)
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
                            TargetManager.TargetGameObject(Entity);
                            Mouse.LastLeftButtonClickTime = 0;

                            break;

                        case CursorTarget.SetTargetClientSide:
                            TargetManager.TargetGameObject(Entity);
                            Mouse.LastLeftButtonClickTime = 0;
                            Engine.UI.Add(new InfoGump(Entity));

                            break;

                        case CursorTarget.HueCommandTarget:
                            CommandManager.OnHueTarget(Entity);

                            break;

                    }
                }
                else
                {
                    if (scene.IsHoldingItem)
                    {
                        if (Entity.Distance < Constants.DRAG_ITEMS_DISTANCE)
                        {
                            if (Entity.Serial.IsItem)
                                scene.DropHeldItemToContainer(World.Items.Get(Entity));
                            else if (Entity.Serial.IsMobile)
                                scene.MergeHeldItem(World.Mobiles.Get(Entity));
                        }
                        else
                            scene.Audio.PlaySound(0x0051);

                        return;
                    }

                    _clickTiming += Mouse.MOUSE_DELAY_DOUBLE_CLICK;

                    if (_clickTiming > 0)
                        _isPressed = true;
                }
            }

            base.OnMouseUp(x, y, button);
        }

        protected override void OnMouseOver(int x, int y)
        {
            if (_positionLocked)
                return;

            float scale = Engine.SceneManager.GetScene<GameScene>().Scale;

            if (Entity is Mobile m)
            {
                _positionLocked = true;

                FileManager.Animations.GetAnimationDimensions(m.AnimIndex,
                                                              m.GetGraphicForAnimation(),
                                                              /*(byte) m.GetDirectionForAnimation()*/ 0,
                                                              /*Mobile.GetGroupForAnimation(m, isParent:true)*/ 0,
                                                              m.IsMounted,
                                                              /*(byte) m.AnimIndex*/ 0,
                                                              out int centerX,
                                                              out int centerY,
                                                              out int width,
                                                              out int height);

                _lockedPosition.X = (int) ((Entity.RealScreenPosition.X + m.Offset.X + 22) / scale);
                _lockedPosition.Y = (int) ((Entity.RealScreenPosition.Y + (m.Offset.Y - m.Offset.Z) - (height + centerY + 8) + (!m.IsMounted ? 22 : 0)) / scale);
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

            if (Entity == null || Entity.IsDestroyed || !Entity.UseObjectHandles || Entity.ClosedObjectHandles) Dispose();

            if (_isPressed)
            {
                if (Engine.UI.IsDragging)
                {
                    _clickTiming = 0;
                    _isPressed = false;

                    return;
                }

                _clickTiming -= (float) frameMS;

                if (_clickTiming <= 0)
                {
                    _clickTiming = 0;
                    _isPressed = false;

                    if (!World.ClientFeatures.TooltipsEnabled)
                        GameActions.SingleClick(Entity);
                    GameActions.OpenPopupMenu(Entity);
                }
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed || !SetName())
                return false;

            float scale = Engine.SceneManager.GetScene<GameScene>().Scale;

            int gx = Engine.Profile.Current.GameWindowPosition.X;
            int gy = Engine.Profile.Current.GameWindowPosition.Y;
            int w = Engine.Profile.Current.GameWindowSize.X;
            int h = Engine.Profile.Current.GameWindowSize.Y;

            if (Entity is Mobile m)
            {
                if (_positionLocked)
                {
                    x = _lockedPosition.X;
                    y = _lockedPosition.Y;
                }
                else
                {

                    FileManager.Animations.GetAnimationDimensions(m.AnimIndex,
                                                                  m.GetGraphicForAnimation(),
                                                                  /*(byte) m.GetDirectionForAnimation()*/ 0,
                                                                  /*Mobile.GetGroupForAnimation(m, isParent:true)*/ 0,
                                                                  m.IsMounted,
                                                                  /*(byte) m.AnimIndex*/ 0,
                                                                  out int centerX,
                                                                  out int centerY,
                                                                  out int width,
                                                                  out int height);

                    x = (int) ((Entity.RealScreenPosition.X + m.Offset.X + 22) / scale);
                    y = (int) ((Entity.RealScreenPosition.Y + (m.Offset.Y - m.Offset.Z) - (height + centerY + 8) + (!m.IsMounted ? 22 : 0)) / scale);
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
                x = (int) ((Entity.RealScreenPosition.X + 22) / scale);
                y = (int) ((Entity.RealScreenPosition.Y + 22) / scale);
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

            batcher.DrawRectangle(Textures.GetTexture(Color.Black), x - 1, y - 1, Width + 1, Height + 1, ref _hueVector);

            base.Draw(batcher, x, y);

            int renderedTextOffset = Math.Max(0, Width - _renderedText.Width - 4) >> 1;

            return _renderedText.Draw(batcher, x + 2 + renderedTextOffset, y + 2, Width, Height, 0, 0);
        }


        public override void Dispose()
        {
            _renderedText?.Destroy();
            base.Dispose();
        }
    }
}
