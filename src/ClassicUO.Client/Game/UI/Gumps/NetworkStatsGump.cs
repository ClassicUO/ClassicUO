#region license

// Copyright (c) 2024, andreakarasho
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

using System;
using System.Text;
using System.Xml;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class NetworkStatsGump : Gump
    {
        private static Point _last_position = new Point(-1, -1);

        private uint _ping, _deltaBytesReceived, _deltaBytesSent;
        private uint _time_to_update;
        private readonly AlphaBlendControl _trans;
        private string _cacheText = string.Empty;


        public NetworkStatsGump(World world, int x, int y) : base(world, 0, 0)
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

            Add
            (
                _trans = new AlphaBlendControl(.7f)
                {
                    Width = Width,
                    Height = Height
                }
            );


            LayerOrder = UILayer.Over;

            WantUpdateSize = false;
        }

        public override GumpType GumpType => GumpType.NetStats;

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

        public override void Update()
        {
            base.Update();

            if (Time.Ticks > _time_to_update)
            {
                _time_to_update = Time.Ticks + 100;

                if (NetClient.Socket.IsConnected)
                {
                    _ping = NetClient.Socket.Statistics.Ping;
                    _deltaBytesReceived = NetClient.Socket.Statistics.DeltaBytesReceived;
                    _deltaBytesSent = NetClient.Socket.Statistics.DeltaBytesSent;
                }

                Span<char> span = stackalloc char[128];
                ValueStringBuilder sb = new ValueStringBuilder(span);
              
                if (IsMinimized)
                {
                    sb.Append($"Ping: {_ping} ms");
                }
                else
                {
                    sb.Append($"Ping: {_ping} ms\n{"In:"} {NetStatistics.GetSizeAdaptive(_deltaBytesReceived),-6} {"Out:"} {NetStatistics.GetSizeAdaptive(_deltaBytesSent),-6}");
                }

                _cacheText = sb.ToString();

                sb.Dispose();

                Vector2 size = Fonts.Bold.MeasureString(_cacheText);

                _trans.Width = Width = (int) (size.X + 20);
                _trans.Height = Height = (int) (size.Y + 20);
                WantUpdateSize = true;
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (!base.Draw(batcher, x, y))
            {
                return false;
            }

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

            if (_ping < 150)
            {
                hueVector.X = 0x44; // green
            }
            else if (_ping < 200)
            {
                hueVector.X = 0x34; // yellow
            }
            else if (_ping < 300)
            {
                hueVector.X = 0x31; // orange
            }
            else
            {
                hueVector.X = 0x20; // red
            }

            hueVector.Y = 1;

            batcher.DrawString
            (
                Fonts.Bold,
                _cacheText,
                x + 10,
                y + 10,
                hueVector
            );

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