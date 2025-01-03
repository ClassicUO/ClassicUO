// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class QuestArrowGump : Gump
    {
        private GumpPic _arrow;
        private Direction _direction;
        private int _mx;
        private int _my;
        private bool _needHue;
        private float _timer;

        public QuestArrowGump(World world, uint serial, int mx, int my) : base(world, serial, serial)
        {
            CanMove = false;
            CanCloseWithRightClick = false;

            AcceptMouseInput = true;

            SetRelativePosition(mx, my);
            WantUpdateSize = false;
        }

        public void SetRelativePosition(int x, int y)
        {
            _mx = x;
            _my = y;
        }

        public override void Update()
        {
            base.Update();

            if (!World.InGame)
            {
                Dispose();
            }

            GameScene scene = Client.Game.GetScene<GameScene>();

            if (IsDisposed || ProfileManager.CurrentProfile == null || scene == null)
            {
                return;
            }

            Direction dir = (Direction) GameCursor.GetMouseDirection
            (
                World.Player.X,
                World.Player.Y,
                _mx,
                _my,
                0
            );

            ushort gumpID = (ushort) (0x1194 + ((int) dir + 1) % 8);

            if (_direction != dir || _arrow == null)
            {
                _direction = dir;

                if (_arrow == null)
                {
                    Add(_arrow = new GumpPic(0, 0, gumpID, 0));
                }
                else
                {
                    _arrow.Graphic = gumpID;
                }

                Width = _arrow.Width;
                Height = _arrow.Height;
            }

            int gox = World.Player.X - _mx;
            int goy = World.Player.Y - _my;


            int x = (Client.Game.Scene.Camera.Bounds.Width >> 1) - (gox - goy) * 22;
            int y = (Client.Game.Scene.Camera.Bounds.Height >> 1) - (gox + goy) * 22;

            x -= (int) World.Player.Offset.X;
            y -= (int) (World.Player.Offset.Y - World.Player.Offset.Z);
            y += World.Player.Z << 2;


            switch (dir)
            {
                case Direction.North:
                    x -= _arrow.Width;

                    break;

                case Direction.South:
                    y -= _arrow.Height;

                    break;

                case Direction.East:
                    x -= _arrow.Width;
                    y -= _arrow.Height;

                    break;

                case Direction.West: break;

                case Direction.Right:
                    x -= _arrow.Width;
                    y -= _arrow.Height / 2;

                    break;

                case Direction.Left:
                    x += _arrow.Width / 2;
                    y -= _arrow.Height / 2;

                    break;

                case Direction.Up:
                    x -= _arrow.Width / 2;
                    y += _arrow.Height / 2;

                    break;

                case Direction.Down:
                    x -= _arrow.Width / 2;
                    y -= _arrow.Height;

                    break;
            }

            var camera = scene.Camera;

            Point p = new Point(x, y);
            p = Client.Game.Scene.Camera.WorldToScreen(p);
            p.X += camera.Bounds.X;
            p.Y += camera.Bounds.Y;
            x = p.X;
            y = p.Y;

            if (x < camera.Bounds.X)
            {
                x = camera.Bounds.X;
            }
            else if (x > camera.Bounds.Right - _arrow.Width)
            {
                x = camera.Bounds.Right - _arrow.Width;
            }


            if (y < camera.Bounds.Y)
            {
                y = camera.Bounds.Y;
            }
            else if (y > camera.Bounds.Bottom - _arrow.Height)
            {
                y = camera.Bounds.Bottom - _arrow.Height;
            }

            X = x;
            Y = y;

            if (_timer < Time.Ticks)
            {
                _timer = Time.Ticks + 1000;
                _needHue = !_needHue;
            }

            _arrow.Hue = (ushort) (_needHue ? 0 : 0x21);
        }


        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            bool leftClick = button == MouseButtonType.Left;
            bool rightClick = button == MouseButtonType.Right;

            if (leftClick || rightClick)
            {
                GameActions.QuestArrow(rightClick);
            }
        }

        public override bool Contains(int x, int y)
        {
            if (_arrow == null)
            {
                return true;
            }

            return _arrow.Contains(x, y);
        }
    }
}