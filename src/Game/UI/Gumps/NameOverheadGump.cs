using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.UI.Gumps
{
    class NameOverheadGump : Gump
    {
        private readonly Label _label;
        private readonly AlphaBlendControl _background;


        public NameOverheadGump(Entity entity) : base(entity.Serial, 0)
        {
            CanMove = false;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
            Entity = entity;

            Hue hue = entity is Mobile m ? Notoriety.GetHue(m.NotorietyFlag) : (Hue)999;

            _label = new Label(string.Empty, true, hue, style: FontStyle.BlackBorder, align: TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = 2,
                Y = 2,
            };

            Add(_background = new AlphaBlendControl(.3f)
            {
                WantUpdateSize = false,
                Width = _label.Width + 4,
                Height = _label.Height + 4
            });
            Add(_label);

            X = (int) entity.RealScreenPosition.X;
            Y = (int) entity.RealScreenPosition.Y; 
        }

        public Entity Entity { get; }

        private void SetName()
        {
            if (string.IsNullOrEmpty(_label.Text))
            {
                if (Entity is Item item && item.Properties.Any())
                {
                    Property prop = item.Properties.FirstOrDefault();
                    _label.Text = FileManager.Cliloc.Translate((int)prop.Cliloc, prop.Args, true);

                    _background.Width = _label.Width + 4;
                    _background.Height = _label.Height + 4;

                    WantUpdateSize = true;
                }
                else if (!string.IsNullOrEmpty(Entity.Name))
                {
                    _label.Text = Entity.Name;
                    _background.Width = _label.Width + 4;
                    _background.Height = _label.Height + 4;

                    WantUpdateSize = true;
                }
            }
        }

        private static void GetAnimationDimensions(Mobile mobile, byte frameIndex, out int height, out int centerY)
        {
            byte dir = 0 & 0x7F;
            byte animGroup = 0;
            bool mirror = false;
            FileManager.Animations.GetAnimDirection(ref dir, ref mirror);

            if (frameIndex == 0xFF)
                frameIndex = (byte)mobile.AnimIndex;
            FileManager.Animations.GetAnimationDimensions(frameIndex, mobile.GetGraphicForAnimation(), dir, animGroup, out int centerX, out centerY, out int w, out height);
            if (centerX == 0 && centerY == 0 && w == 0 && height == 0)
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
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                GameActions.DoubleClick(Entity);
            }

            return true;
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (Entity == null || Entity.IsDisposed || !Entity.UseObjectHandles || Entity.ClosedObjectHandles)
            {
                Dispose();
            }
            else
            {
                SetName();
            }
        }

        public override bool Draw(Batcher2D batcher, Point position, Vector3? hue = null)
        {
            Point gWinPos = Engine.Profile.Current.GameWindowPosition;
            Point gWinSize = Engine.Profile.Current.GameWindowSize;
            float scale = Engine.Profile.Current.ScaleZoom;


            if (Entity is Mobile m)
            {                    
               GetAnimationDimensions(m, 0xFF, out int height, out int centerY);

                float x = (m.RealScreenPosition.X + gWinPos.X) / scale;
                float y = (m.RealScreenPosition.Y + gWinPos.Y) / scale;

                X = (int)(x + m.Offset.X) - Width / 2 + 22;
                Y = (int)(y + (m.Offset.Y - m.Offset.Z) - (height + centerY + 8)) - Height / 2 + (m.IsMounted ? 0 : 22);
            }
            else
            {
                X = (int)Entity.RealScreenPosition.X;
                Y = (int)Entity.RealScreenPosition.Y;
            }

            if (_edge == null)
            {
                _edge = new Texture2D(batcher.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
                _edge.SetData(new Color[] { Color.Gray });
            }

            batcher.DrawRectangle(_edge, new Rectangle(position.X - 1, position.Y - 1, Width + 1, Height + 1), Vector3.Zero);

            return base.Draw(batcher, position, hue);
        }

        private Texture2D _edge;

        public override void Dispose()
        {
            _edge?.Dispose();
            base.Dispose();
        }
    }
}
