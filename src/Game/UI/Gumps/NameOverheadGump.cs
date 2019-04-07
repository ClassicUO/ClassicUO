using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Game.Managers;
using ClassicUO.Renderer;
using ClassicUO.Utility;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Gumps
{
    class NameOverheadGump : Gump
    {
        private readonly AlphaBlendControl _background;

        private readonly RenderedText _renderedText;
        private bool _isPressed = false;
        private float _clickTiming = 0;

        public NameOverheadGump(Entity entity) : base(entity.Serial, 0)
        {
            CanMove = false;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
            Entity = entity;

            Hue hue = entity is Mobile m ? Notoriety.GetHue(m.NotorietyFlag) : (Hue)0x0481;

            _renderedText = new RenderedText()
            {
                IsUnicode = true,
                Font = 0xFF,
                Hue = hue,
                FontStyle = FontStyle.BlackBorder,
                Align = TEXT_ALIGN_TYPE.TS_CENTER,
                IsHTML = true,
            };


            Add(_background = new AlphaBlendControl(.3f)
            {
                WantUpdateSize = false,
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
                    }

                    FileManager.Fonts.SetUseHTML(true);
                    FileManager.Fonts.RecalculateWidthByInfo = true;

                    int width = FileManager.Fonts.GetWidthUnicode(_renderedText.Font, t);

                    if (width > 200)
                        width = 200;

                    width = FileManager.Fonts.GetWidthExUnicode(_renderedText.Font, t, width, TEXT_ALIGN_TYPE.TS_CENTER, (ushort)FontStyle.BlackBorder);

                    if (width > 200)
                        width = 200;

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
                    _renderedText.Text = Entity.Name;
                    Width = _background.Width = _renderedText.Width + 4;
                    Height = _background.Height = _renderedText.Height + 4;

                    WantUpdateSize = false;

                    return true;
                }

                return false;
            }

            return true;
        }

        private static void GetAnimationDimensions(Mobile mobile, byte frameIndex, out int centerX, out int centerY, out int width, out int height)
        {
            byte dir = 0 & 0x7F;
            byte animGroup = 0;
            bool mirror = false;
            FileManager.Animations.GetAnimDirection(ref dir, ref mirror);

            if (frameIndex == 0xFF)
                frameIndex = (byte)mobile.AnimIndex;
            FileManager.Animations.GetAnimationDimensions(frameIndex, mobile.GetGraphicForAnimation(), dir, animGroup, out centerX, out centerY, out width, out height);
            if (centerX == 0 && centerY == 0 && width == 0 && height == 0)
                height = mobile.IsMounted ? 100 : 60;
        }



        protected override void CloseWithRightClick()
        {
            Entity.ClosedObjectHandles = true;
            base.CloseWithRightClick();
        }

        protected override void OnDragBegin(int x, int y)
        {
            if (Entity.Serial.IsMobile)
            {
                GameActions.RequestMobileStatus(Entity);

                Engine.UI.GetByLocalSerial<HealthBarGump>(Entity)?.Dispose();

                if (Entity == World.Player)
                    StatusGumpBase.GetStatusGump()?.Dispose();

                Rectangle rect = FileManager.Gumps.GetTexture(0x0804).Bounds;
                HealthBarGump currentHealthBarGump;
                Engine.UI.Add(currentHealthBarGump = new HealthBarGump((Mobile) Entity) { X = Mouse.Position.X - (rect.Width >> 1), Y = Mouse.Position.Y - (rect.Height >> 1) });
                Engine.UI.AttemptDragControl(currentHealthBarGump, Mouse.Position, true);
            }
            else
            {
                GameActions.PickUp(Entity);
            }
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                _isPressed = false;
                _clickTiming = 0;
                GameActions.DoubleClick(Entity);
            }

            return true;
        }

        protected override void OnMouseUp(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                if (Engine.UI.IsDragging)
                    return;

                if (TargetManager.IsTargeting)
                {
                    switch (TargetManager.TargetingState)
                    {
                        case CursorTarget.Object:
                            TargetManager.TargetGameObject(Entity);
                            Mouse.LastLeftButtonClickTime = 0;
                            break;

                        case CursorTarget.SetTargetClientSide:
                            TargetManager.TargetGameObject(Entity);
                            Mouse.LastLeftButtonClickTime = 0;
                            Engine.UI.Add(new InfoGump(Entity));
                            break;

                        default:
                            break;
                    }
                }
                else
                {
                    _clickTiming += Mouse.MOUSE_DELAY_DOUBLE_CLICK;
                    if (_clickTiming > 0)
                        _isPressed = true;
                }
            }
            base.OnMouseUp(x, y, button);
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (Entity == null || Entity.IsDestroyed || !Entity.UseObjectHandles || Entity.ClosedObjectHandles || !Input.Keyboard.Ctrl || !Input.Keyboard.Shift)
            {
                Dispose();
            }
            if (_isPressed)
            {
                if (Engine.UI.IsDragging)
                {
                    _clickTiming = 0;
                    _isPressed = false;
                    return;
                }

                _clickTiming -= (float)frameMS;
                if (_clickTiming <= 0)
                {
                    _clickTiming = 0;
                    _isPressed = false;
                    GameActions.OpenPopupMenu(Entity);
                }
            }
        }

        public override bool Draw(Batcher2D batcher, int x, int y)
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
                GetAnimationDimensions(m, 0, out int centerX, out int centerY, out int width, out int height);

                x = (int)((Entity.RealScreenPosition.X + m.Offset.X + 22) / scale);
                y = (int)((Entity.RealScreenPosition.Y + (m.Offset.Y - m.Offset.Z) - (height + centerY + 8) ) / scale);
            }
            else
            {
                x = (int)((Entity.RealScreenPosition.X + 22) / scale);
                y = (int)((Entity.RealScreenPosition.Y) / scale);
            }
           
            x -= Width / 2;
            y -= Height / 2;
            x += gx + 6;
            y += gy;

            X = x;
            Y = y;

            if (x < gx || x + Width > gx + w)
                return false;
            if (y < gy || y + Height > gy + h)
                return false;
           
            batcher.DrawRectangle(Textures.GetTexture(Color.Gray), x - 1, y - 1, Width + 1, Height + 1, Vector3.Zero);
            
            base.Draw(batcher, x, y);
            return _renderedText.Draw(batcher, x + 2, y + 2, Width, Height, 0, 0);
        }


        public override void Dispose()
        {
            _renderedText?.Destroy();
            base.Dispose();
        }
    }
}
