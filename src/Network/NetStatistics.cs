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
using System.Diagnostics;

using ClassicUO.Renderer;

namespace ClassicUO.Network
{
    internal class NetStatistics
    {
        private readonly Stopwatch _pingStopwatch = new Stopwatch();

        private readonly RenderedText _renderedText = RenderedText.Create(String.Empty, style: FontStyle.BlackBorder);

        private uint _currentTotalBytesSent, _currentTotalByteReceived, _currentTotalPacketsSent, _currentTotalPacketsReceived;
        private uint _lastTotalBytesSent, _lastTotalByteReceived, _lastTotalPacketsSent, _lastTotalPacketsReceived;

        public DateTime ConnectedFrom { get; set; }

        public uint TotalBytesSent { get; set; }

        public uint TotalBytesReceived { get; set; }

        public uint TotalPacketsSent { get; set; }

        public uint TotalPacketsReceived { get; set; }

        public uint Ping { get; private set; }


        public void PingReceived()
        {
            Ping = (uint) _pingStopwatch.ElapsedMilliseconds;
            _pingStopwatch.Stop();
        }

        public void SendPing()
        {
            if (!NetClient.Socket.IsConnected || NetClient.Socket.IsDisposed)
                return;

            _pingStopwatch.Restart();
            NetClient.Socket.Send(new PPing());
        }

        public void Draw(UltimaBatcher2D batcher, int x, int y)
        {
            batcher.Begin();
            _renderedText.Draw(batcher, x, y);
            batcher.End();
        }

        public void Reset()
        {
            ConnectedFrom = DateTime.MinValue;
            _currentTotalBytesSent = _currentTotalByteReceived = _currentTotalPacketsSent = _currentTotalPacketsReceived = 0;
            _lastTotalBytesSent = _lastTotalByteReceived = _lastTotalPacketsSent = _lastTotalPacketsReceived = 0;
            TotalBytesReceived = TotalBytesSent = TotalPacketsReceived = TotalPacketsSent = 0;
        }

        public void Update()
        {
            _currentTotalByteReceived = _lastTotalByteReceived;
            _currentTotalBytesSent = _lastTotalBytesSent;
            _currentTotalPacketsReceived = _lastTotalPacketsReceived;
            _currentTotalPacketsSent = _lastTotalPacketsSent;
            _lastTotalByteReceived = TotalBytesReceived;
            _lastTotalBytesSent = TotalBytesSent;
            _lastTotalPacketsReceived = TotalPacketsReceived;
            _lastTotalPacketsSent = TotalPacketsSent;

            ushort hue;

            if (Ping < 100)
                hue = 0x44; // green
            else if (Ping < 150)
                hue = 0x034; // yellow
            else if (Ping < 200)
                hue = 0x0031; // orange
            else
                hue = 0x20; // red

            _renderedText.Hue = hue;
            _renderedText.Text = $"Ping: {Ping} ms\nIn: {GetSizeAdaptive(_lastTotalByteReceived - _currentTotalByteReceived)}   Out: {GetSizeAdaptive(_lastTotalBytesSent - _currentTotalBytesSent)}";
        }

        public override string ToString()
        {
            return $"Packets:\n >> {_lastTotalPacketsReceived - _currentTotalPacketsReceived}\n << {_lastTotalPacketsSent - _currentTotalPacketsSent}\nBytes:\n >> {GetSizeAdaptive(_lastTotalByteReceived - _currentTotalByteReceived)}\n << {GetSizeAdaptive(_lastTotalBytesSent - _currentTotalBytesSent)}";
        }

        public static string GetSizeAdaptive(long bytes)
        {
            decimal num = bytes;
            var arg = "KB";
            num /= 1024m;

            if (!(num < 1024m))
            {
                arg = "MB";
                num /= 1024m;

                if (!(num < 1024m))
                {
                    arg = "GB";
                    num /= 1024m;
                }
            }

            return $"{Math.Round(num, 2):0.##} {arg}";
        }
    }
}