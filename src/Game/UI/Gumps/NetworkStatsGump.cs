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

using System.Text;

using ClassicUO.Configuration;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Gumps
{
    internal class NetworkStatsGump : Gump
    {

        private readonly Label _label;
        private readonly AlphaBlendControl _trans;

        private bool _fullDisplayMode;

        public NetworkStatsGump() : base(0, 0)
        {
            CanMove = true;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;
            AcceptMouseInput = true;
            AcceptKeyboardInput = false;

            _fullDisplayMode = !ProfileManager.Current.NetworkStatsMinimized;

            Width = 200;
            Height = 200;

            Add(_trans = new AlphaBlendControl(.3f)
            {
                Width = Width, Height = Height
            });

            Add(_label = new Label("", true, 0x35, font: 1, style: FontStyle.BlackBorder)
            {
                X = 10, Y = 10
            });

            ControlInfo.Layer = UILayer.Over;

            WantUpdateSize = false;
        }

        public bool FullDisplayMode
        {
            get => _fullDisplayMode;
            set
            {
                _fullDisplayMode = value;
                ProfileManager.Current.NetworkStatsMinimized = !_fullDisplayMode;
            }
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                FullDisplayMode = !FullDisplayMode;

                return true;
            }

            return false;
        }

        public override void Update(double totalMS, double frameMS)
        {
            _trans.Width = Width = _label.Width + 20;
            _trans.Height = Height = _label.Height + 20;

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            GameScene scene = Client.Game.GetScene<GameScene>();

            if (!NetClient.Socket.IsConnected)
            {
                _label.Hue = NetClient.LoginSocket.Statistics.RenderedText.Hue;
                _label.Text = NetClient.LoginSocket.Statistics.RenderedText.Text;
            }
            else if (!NetClient.Socket.IsDisposed)
            {
                _label.Hue = NetClient.Socket.Statistics.RenderedText.Hue;
                _label.Text = NetClient.Socket.Statistics.RenderedText.Text;
            }
            else
            {
                _label.Hue = 0x20;
                _label.Text = "Disconnected";
            }
                       
            if (!FullDisplayMode) _label.Text = _label.Text.Split('\n')[0];

            return base.Draw(batcher, x, y);
        }

        protected override void OnDragEnd(int x, int y)
        {
            base.OnDragEnd(x, y);
            ProfileManager.Current.NetworkStatsPosition = Location;
        }
    }
}