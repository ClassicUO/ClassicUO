// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class WorldViewportGump : Gump
    {
        public const int BORDER_WIDTH = 5;
        private readonly BorderControl _borderControl;
        private readonly Button _button;
        private bool _clicked;
        private Point _lastSize,
            _savedSize;
        private readonly GameScene _scene;
        private readonly SystemChatControl _systemChatControl;

        public WorldViewportGump(World world, GameScene scene) : base(world, 0, 0)
        {
            _scene = scene;
            AcceptMouseInput = false;
            CanMove = !ProfileManager.CurrentProfile.GameWindowLock;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;
            LayerOrder = UILayer.Under;
            X = scene.Camera.Bounds.X - BORDER_WIDTH;
            Y = scene.Camera.Bounds.Y - BORDER_WIDTH;
            _savedSize = _lastSize = new Point(
                scene.Camera.Bounds.Width,
                scene.Camera.Bounds.Height
            );

            _button = new Button(0, 0x837, 0x838, 0x838);

            _button.MouseDown += (sender, e) =>
            {
                if (!ProfileManager.CurrentProfile.GameWindowLock)
                {
                    _clicked = true;
                }
            };

            _button.MouseUp += (sender, e) =>
            {
                if (!ProfileManager.CurrentProfile.GameWindowLock)
                {
                    Point n = ResizeGameWindow(_lastSize);

                    UIManager.GetGump<OptionsGump>()?.UpdateVideo();

                    if (Client.Game.UO.Version >= ClientVersion.CV_200)
                    {
                        NetClient.Socket.Send_GameWindowSize((uint)n.X, (uint)n.Y);
                    }

                    _clicked = false;
                }
            };

            _button.SetTooltip(ResGumps.ResizeGameWindow);
            Width = scene.Camera.Bounds.Width + BORDER_WIDTH * 2;
            Height = scene.Camera.Bounds.Height + BORDER_WIDTH * 2;

            _borderControl = new BorderControl(0, 0, Width, Height, 4);

            _borderControl.DragEnd += (sender, e) =>
            {
                UIManager.GetGump<OptionsGump>()?.UpdateVideo();
            };

            UIManager.SystemChat = _systemChatControl = new SystemChatControl(
                this,
                BORDER_WIDTH,
                BORDER_WIDTH,
                scene.Camera.Bounds.Width,
                scene.Camera.Bounds.Height
            );

            Add(_borderControl);
            Add(_button);
            Add(_systemChatControl);
            Resize();
        }

        public override void Update()
        {
            base.Update();

            if (IsDisposed)
            {
                return;
            }

            if (Mouse.IsDragging)
            {
                Point offset = Mouse.LDragOffset;

                _lastSize = _savedSize;

                if (_clicked && offset != Point.Zero)
                {
                    int w = _lastSize.X + offset.X;
                    int h = _lastSize.Y + offset.Y;

                    if (w < 640)
                    {
                        w = 640;
                    }

                    if (h < 480)
                    {
                        h = 480;
                    }

                    if (w > Client.Game.Window.ClientBounds.Width - BORDER_WIDTH)
                    {
                        w = Client.Game.Window.ClientBounds.Width - BORDER_WIDTH;
                    }

                    if (h > Client.Game.Window.ClientBounds.Height - BORDER_WIDTH)
                    {
                        h = Client.Game.Window.ClientBounds.Height - BORDER_WIDTH;
                    }

                    _lastSize.X = w;
                    _lastSize.Y = h;
                }

                if (
                    _scene.Camera.Bounds.Width != _lastSize.X
                    || _scene.Camera.Bounds.Height != _lastSize.Y
                )
                {
                    Width = _lastSize.X + BORDER_WIDTH * 2;
                    Height = _lastSize.Y + BORDER_WIDTH * 2;
                    _scene.Camera.Bounds.Width = _lastSize.X;
                    _scene.Camera.Bounds.Height = _lastSize.Y;

                    Resize();
                }
            }
        }

        protected override void OnDragEnd(int x, int y)
        {
            base.OnDragEnd(x, y);

            Point position = Location;

            if (position.X + Width - BORDER_WIDTH > Client.Game.Window.ClientBounds.Width)
            {
                position.X = Client.Game.Window.ClientBounds.Width - (Width - BORDER_WIDTH);
            }

            if (position.X < -BORDER_WIDTH)
            {
                position.X = -BORDER_WIDTH;
            }

            if (position.Y + Height - BORDER_WIDTH > Client.Game.Window.ClientBounds.Height)
            {
                position.Y = Client.Game.Window.ClientBounds.Height - (Height - BORDER_WIDTH);
            }

            if (position.Y < -BORDER_WIDTH)
            {
                position.Y = -BORDER_WIDTH;
            }

            Location = position;
            _scene.Camera.Bounds.X = position.X + BORDER_WIDTH;
            _scene.Camera.Bounds.Y = position.Y + BORDER_WIDTH;

            UIManager.GetGump<OptionsGump>()?.UpdateVideo();
            UpdateGameWindowPos();
        }

        protected override void OnMove(int x, int y)
        {
            base.OnMove(x, y);

            _scene.Camera.Bounds.X = ScreenCoordinateX + BORDER_WIDTH;
            _scene.Camera.Bounds.Y = ScreenCoordinateY + BORDER_WIDTH;

            UpdateGameWindowPos();
        }

        private void UpdateGameWindowPos()
        {
            if (_scene != null)
            {
                _scene.UpdateDrawPosition = true;
            }
        }

        private void Resize()
        {
            _borderControl.Width = Width;
            _borderControl.Height = Height;
            _button.X = Width - (_button.Width >> 1);
            _button.Y = Height - (_button.Height >> 1);
            _scene.Camera.Bounds.Width = _systemChatControl.Width = Width - BORDER_WIDTH * 2;
            _scene.Camera.Bounds.Height = _systemChatControl.Height = Height - BORDER_WIDTH * 2;
            _systemChatControl.Resize();
            WantUpdateSize = true;

            UpdateGameWindowPos();
        }

        public void SetGameWindowPosition(Point pos)
        {
            Location = pos;

            _scene.Camera.Bounds.X = ScreenCoordinateX + BORDER_WIDTH;
            _scene.Camera.Bounds.Y = ScreenCoordinateY + BORDER_WIDTH;

            UpdateGameWindowPos();
        }

        public Point ResizeGameWindow(Point newSize)
        {
            if (newSize.X < 640)
            {
                newSize.X = 640;
            }

            if (newSize.Y < 480)
            {
                newSize.Y = 480;
            }

            //Resize();
            _lastSize = _savedSize = newSize;

            if (
                _scene.Camera.Bounds.Width != _lastSize.X
                || _scene.Camera.Bounds.Height != _lastSize.Y
            )
            {
                _scene.Camera.Bounds.Width = _lastSize.X;
                _scene.Camera.Bounds.Height = _lastSize.Y;
                Width = _scene.Camera.Bounds.Width + BORDER_WIDTH * 2;
                Height = _scene.Camera.Bounds.Height + BORDER_WIDTH * 2;

                Resize();
            }

            return newSize;
        }

        public override bool Contains(int x, int y)
        {
            if (
                x >= BORDER_WIDTH
                && x < Width - BORDER_WIDTH * 2
                && y >= BORDER_WIDTH
                && y
                    < Height
                        - BORDER_WIDTH * 2
                        - (
                            _systemChatControl?.TextBoxControl != null
                            && _systemChatControl.IsActive
                                ? _systemChatControl.TextBoxControl.Height
                                : 0
                        )
            )
            {
                return false;
            }

            return base.Contains(x, y);
        }
    }

    internal class BorderControl : Control
    {
        private readonly int _borderSize;

        const ushort H_BORDER = 0x0A8C;
        const ushort V_BORDER = 0x0A8D;

        public BorderControl(int x, int y, int w, int h, int borderSize)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
            _borderSize = borderSize;
            CanMove = true;
            AcceptMouseInput = true;
        }

        public ushort Hue { get; set; }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            if (Hue != 0)
            {
                hueVector.X = Hue;
                hueVector.Y = 1;
            }

            ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(H_BORDER);

            // sopra
            batcher.DrawTiled(
                gumpInfo.Texture,
                new Rectangle(x, y, Width, _borderSize),
                gumpInfo.UV,
                hueVector
            );

            // sotto
            batcher.DrawTiled(
                gumpInfo.Texture,
                new Rectangle(x, y + Height - _borderSize, Width, _borderSize),
                gumpInfo.UV,
                hueVector
            );

            gumpInfo = ref Client.Game.UO.Gumps.GetGump(V_BORDER);
            //sx
            batcher.DrawTiled(
                gumpInfo.Texture,
                new Rectangle(x, y, _borderSize, Height),
                gumpInfo.UV,
                hueVector
            );

            //dx
            batcher.DrawTiled(
                gumpInfo.Texture,
                new Rectangle(
                    x + Width - _borderSize,
                    y + (gumpInfo.UV.Width >> 1),
                    _borderSize,
                    Height - _borderSize
                ),
                gumpInfo.UV,
                hueVector
            );

            return base.Draw(batcher, x, y);
        }
    }
}
