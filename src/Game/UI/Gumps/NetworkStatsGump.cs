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

using System.IO;
using System.Text;
using System.Xml;

using ClassicUO.Configuration;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class NetworkStatsGump : Gump
    {
        private readonly AlphaBlendControl _trans;

        private uint _ping, _deltaBytesReceived, _deltaBytesSent;
        private uint _time_to_update;
        private static Point _last_position = new Point(-1, -1);
        private StringBuilder _sb = new StringBuilder();

        public NetworkStatsGump(int x, int y) : base(0, 0)
        {
            CanMove = true;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = true;
            AcceptMouseInput = true;
            AcceptKeyboardInput = false;

            _ping = _deltaBytesReceived = _deltaBytesSent = 0;

            X = _last_position.X <= 0 ? x : _last_position.X;
            Y = _last_position.Y <= 0 ? y : _last_position.Y;
            Width = 100;
            Height = 30;

            Add(_trans = new AlphaBlendControl(.3f)
            {
                Width = Width,
                Height = Height
            });


            ControlInfo.Layer = UILayer.Over;

            WantUpdateSize = false;
        }

        public override GUMP_TYPE GumpType => GUMP_TYPE.GT_NETSTATS;

        public bool IsMinimized { get; set; }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                IsMinimized = !IsMinimized;

                return true;
            }

            return false;
        }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (Time.Ticks > _time_to_update)
            {
                _sb.Clear();

                _time_to_update = Time.Ticks + 100;

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

                if (IsMinimized)
                    _sb.Append($"Ping: {_ping} ms");
                else
                    _sb.Append($"Ping: {_ping} ms\n{"In:"} {NetStatistics.GetSizeAdaptive(_deltaBytesReceived),-6} {"Out:"} {NetStatistics.GetSizeAdaptive(_deltaBytesSent),-6}");


                var size = Fonts.Bold.MeasureString(_sb.ToString());

                _trans.Width = Width = (int) (size.X + 20);
                _trans.Height = Height = (int) (size.Y + 20);
                WantUpdateSize = true;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (!base.Draw(batcher, x, y))
                return false;

            ResetHueVector();

            if (_ping < 100)
                _hueVector.X = 0x44; // green
            else if (_ping < 150)
                _hueVector.X = 0x34; // yellow
            else if (_ping < 200)
                _hueVector.X = 0x31; // orange
            else
                _hueVector.X = 0x20; // red
            _hueVector.Y = 1;

            batcher.DrawString(Fonts.Bold, _sb.ToString(), x + 10, y + 10, ref _hueVector);

            return true;
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);

            writer.WriteAttributeString("minimized", IsMinimized.ToString());
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);

            bool.TryParse(xml.GetAttribute("minimized"), out bool b);
            IsMinimized = b;
        }

        protected override void OnDragEnd(int x, int y)
        {
            base.OnDragEnd(x, y);
            _last_position.X = ScreenCoordinateX;
            _last_position.Y = ScreenCoordinateY;
        }

        protected override void OnMove(int x, int y)
        {
            base.OnMove(x, y);

            _last_position.X = ScreenCoordinateX;
            _last_position.Y = ScreenCoordinateY;
        }
    }
}