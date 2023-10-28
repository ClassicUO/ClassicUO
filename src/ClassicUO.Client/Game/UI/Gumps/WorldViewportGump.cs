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
using Microsoft.Xna.Framework.Graphics;

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

        private static Microsoft.Xna.Framework.Graphics.Texture2D damageWindowOutline = SolidColorTextureCache.GetTexture(Color.White);
        public static Vector3 DamageWindowOutlineHue = ShaderHueTranslator.GetHueVector(32);

        public WorldViewportGump(GameScene scene) : base(0, 0)
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

                    if (Client.Version >= ClientVersion.CV_200)
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
                BORDER_WIDTH,
                BORDER_WIDTH,
                scene.Camera.Bounds.Width,
                scene.Camera.Bounds.Height
            );

            Add(_borderControl);
            Add(_button);
            Add(_systemChatControl);
            Resize();

            if (ProfileManager.CurrentProfile.LastVersionHistoryShown != CUOEnviroment.Version.ToString())
            {
                UIManager.Add(new VersionHistory());
                ProfileManager.CurrentProfile.LastVersionHistoryShown = CUOEnviroment.Version.ToString();
            }
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

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            bool res = base.Draw(batcher, x, y);

            if (World.InGame && ProfileManager.CurrentProfile != null && ProfileManager.CurrentProfile.EnableHealthIndicator)
            {
                float hpPercent = (float)World.Player.Hits / (float)World.Player.HitsMax;
                if (hpPercent <= ProfileManager.CurrentProfile.ShowHealthIndicatorBelow)
                {
                    int size = ProfileManager.CurrentProfile.HealthIndicatorWidth;
                    DamageWindowOutlineHue.Z = 1f - hpPercent;
                    batcher.Draw( //Top bar
                        damageWindowOutline,
                        new Rectangle(x + BORDER_WIDTH, y + BORDER_WIDTH, Width - (BORDER_WIDTH * 3), size),
                        DamageWindowOutlineHue
                        );

                    batcher.Draw( //Left Bar
                        damageWindowOutline,
                        new Rectangle(x + BORDER_WIDTH, y + BORDER_WIDTH + size, size, Height - (BORDER_WIDTH * 3) - (size*2)),
                        DamageWindowOutlineHue
                        );

                    batcher.Draw( //Right Bar
                        damageWindowOutline,
                        new Rectangle(x + Width - (BORDER_WIDTH * 2) - size, y + BORDER_WIDTH + size, size, Height - (BORDER_WIDTH * 3) - (size*2)),
                        DamageWindowOutlineHue
                        );

                    batcher.Draw( //Bottom bar
                        damageWindowOutline,
                        new Rectangle(x + BORDER_WIDTH, y + Height - (BORDER_WIDTH * 2) - size, Width - (BORDER_WIDTH * 3), size),
                        DamageWindowOutlineHue
                        );
                }
            }

            return res;
        }
    }

    internal class BorderControl : Control
    {
        private int _borderSize;

        private ushort h_border = 0x0A8C;
        private ushort v_border = 0x0A8D;
        private ushort h_bottom_border = 0x0A8C;
        private ushort v_right_border = 0x0A8D;
        private ushort t_left = 0xffff, t_right = 0xffff, b_left = 0xffff, b_right = 0xffff;

        public int BorderSize { get { return _borderSize; } set { _borderSize = value; } }
        public ushort H_Border { get { return h_border; } set { h_border = value; } }
        public ushort V_Border { get { return v_border; } set { v_border = value; } }
        public ushort V_Right_Border { get { return v_right_border; } set { v_right_border = value; } }
        public ushort H_Bottom_Border { get { return h_bottom_border; } set { h_bottom_border = value; } }
        public ushort T_Left { get { return t_left; } set { t_left = value; } }
        public ushort T_Right { get { return t_right; } set { t_right = value; } }
        public ushort B_Left { get { return b_left; } set { b_left = value; } }
        public ushort B_Right { get { return b_right; } set { b_right = value; } }

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

        public void DefaultGraphics()
        {
            h_border = 0x0A8C;
            v_border = 0x0A8D;
            h_bottom_border = 0x0A8C;
            v_right_border = 0x0A8D;
            t_left = 0xffff; t_right = 0xffff; b_left = 0xffff; b_right = 0xffff;
            _borderSize = 4;
        }

        private Texture2D GetGumpTexture(uint g, out Rectangle bounds)
        {
            ref readonly var texture = ref Client.Game.Gumps.GetGump(g);
            bounds = texture.UV;
            return texture.Texture;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);
            Rectangle pos;

            if (Hue != 0)
            {
                hueVector.X = Hue;
                hueVector.Y = 1;
            }
            hueVector.Z = Alpha;

            var texture = GetGumpTexture(h_border, out var bounds);
            if (texture != null)
            {
                pos = new Rectangle
                (
                    x,
                    y,
                    Width,
                    _borderSize
                );
                if (t_left != 0xffff)
                {
                    pos.X += _borderSize;
                    pos.Width -= _borderSize;
                }
                if (t_right != 0xffff)
                    pos.Width -= _borderSize;
                // sopra
                batcher.DrawTiled
                (
                    texture,
                    pos,
                    bounds,
                    hueVector
                );
            }

            texture = GetGumpTexture(h_bottom_border, out bounds);
            if (texture != null)
            {
                pos = new Rectangle
                (
                    x,
                    y + Height - _borderSize,
                    Width,
                    _borderSize
                );
                if (b_left != 0xffff)
                {
                    pos.X += _borderSize;
                    pos.Width -= _borderSize;
                }
                if (b_right != 0xffff)
                    pos.Width -= _borderSize;
                // sotto
                batcher.DrawTiled
                (
                    texture,
                    pos,
                    bounds,
                    hueVector
                );
            }

            texture = GetGumpTexture(v_border, out bounds);
            if (texture != null)
            {
                pos = new Rectangle
                (
                    x,
                    y,
                    _borderSize,
                    Height
                );
                if (t_left != 0xffff)
                {
                    pos.Y += _borderSize;
                    pos.Height -= _borderSize;
                }
                if (b_left != 0xffff)
                    pos.Height -= _borderSize;
                //sx
                batcher.DrawTiled
                (
                    texture,
                    pos,
                    bounds,
                    hueVector
                );
            }

            texture = GetGumpTexture(v_right_border, out bounds);
            if (texture != null)
            {
                pos = new Rectangle
                (
                    x + Width - _borderSize,
                    y,
                    _borderSize,
                    Height
                );
                if (t_right != 0xffff)
                {
                    pos.Y += _borderSize;
                    pos.Height -= _borderSize;
                }
                if (b_right != 0xffff)
                    pos.Height -= _borderSize;
                //dx
                batcher.DrawTiled
                (
                    texture,
                    pos,
                    bounds,
                    hueVector
                );
            }

            if (t_left != 0xffff)
            {
                texture = GetGumpTexture(t_left, out bounds);
                if (texture != null)
                    batcher.Draw(
                        texture,
                        new Rectangle(x, y, bounds.Width, bounds.Height),
                        bounds,
                        hueVector
                        );
            }
            if (t_right != 0xffff)
            {
                texture = GetGumpTexture(t_right, out bounds);
                if (texture != null)
                    batcher.Draw(
                    texture,
                    new Rectangle(x + Width - _borderSize, y, bounds.Width, bounds.Height),
                    bounds,
                    hueVector
                    );
            }
            if (b_left != 0xffff)
            {
                texture = GetGumpTexture(b_left, out bounds);
                if (texture != null)
                    batcher.Draw(
                    texture,
                    new Rectangle(x, y + Height - _borderSize, bounds.Width, bounds.Height),
                    bounds,
                    hueVector
                    );
            }
            if (b_right != 0xffff)
            {
                texture = GetGumpTexture(b_right, out bounds);
                if (texture != null)
                    batcher.Draw(
                    texture,
                    new Rectangle(x + Width - _borderSize, y + Height - _borderSize, bounds.Width, bounds.Height),
                    bounds,
                    hueVector
                    );
            }

            return base.Draw(batcher, x, y);
        }
    }
}
