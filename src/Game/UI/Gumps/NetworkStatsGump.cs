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

using ClassicUO.Configuration;
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

        private uint _ping, _deltaBytesReceived, _deltaBytesSent;

        public NetworkStatsGump() : base(0, 0)
        {
            CanMove = true;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;
            AcceptMouseInput = true;
            AcceptKeyboardInput = false;

            _ping = _deltaBytesReceived = _deltaBytesSent = 0;

            _fullDisplayMode = !ProfileManager.Current.NetworkStatsMinimized;

            Width = 200;
            Height = 200;

            Add(_trans = new AlphaBlendControl(.3f)
            {
                Width = Width,
                Height = Height
            });

            Add(_label = new Label("", true, 0x35, font: 1, style: FontStyle.BlackBorder)
            {
                X = 10,
                Y = 10
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

            if (!NetClient.Socket.IsConnected)
            {
                _ping = NetClient.LoginSocket.Statistics.Ping;
                _deltaBytesReceived = NetClient.LoginSocket.Statistics.DeltaBytesReceived;
                _deltaBytesSent = NetClient.LoginSocket.Statistics.DeltaBytesSent;
            }
            else if (!NetClient.Socket.IsDisposed)
            {
                _ping = NetClient.Socket.Statistics.Ping;
                _deltaBytesReceived = NetClient.Socket.Statistics.DeltaBytesReceived;
                _deltaBytesSent = NetClient.Socket.Statistics.DeltaBytesSent;
            }

            base.Update(totalMS, frameMS);
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {

            if (_ping < 100)
                _label.Hue = 0x44; // green
            else if (_ping < 150)
                _label.Hue = 0x34; // yellow
            else if (_ping < 200)
                _label.Hue = 0x31; // orange
            else
                _label.Hue = 0x20; // red

            if (FullDisplayMode) _label.Text = $"Ping: {_ping} ms\n{"In:"} {NetStatistics.GetSizeAdaptive(_deltaBytesReceived),-6} {"Out:"} {NetStatistics.GetSizeAdaptive(_deltaBytesSent),-6}";
            else _label.Text = $"Ping: {_ping} ms";

            return base.Draw(batcher, x, y);
        }

        protected override void OnDragEnd(int x, int y)
        {
            base.OnDragEnd(x, y);
            ProfileManager.Current.NetworkStatsPosition = Location;
        }
    }
}