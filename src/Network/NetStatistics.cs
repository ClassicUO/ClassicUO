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

using System;
using System.Diagnostics;

namespace ClassicUO.Network
{
    internal class NetStatistics
    {
        private NetClient _socket;
        private uint _lastTotalBytesReceived, _lastTotalBytesSent, _lastTotalPacketsReceived, _lastTotalPacketsSent;
        private byte _pingIdx;

        private readonly uint[] _pings = new uint[5];
        private readonly Stopwatch _pingStopwatch = new Stopwatch();



        public NetStatistics(NetClient socket)
        {
            _socket = socket;
        }





        public DateTime ConnectedFrom { get; set; }

        public uint TotalBytesReceived { get; set; }

        public uint TotalBytesSent { get; set; }

        public uint TotalPacketsReceived { get; set; }

        public uint TotalPacketsSent { get; set; }

        public uint DeltaBytesReceived { get; private set; }

        public uint DeltaBytesSent { get; private set; }

        public uint DeltaPacketsReceived { get; private set; }

        public uint DeltaPacketsSent { get; private set; }

        public uint Ping
        {
            get
            {
                byte count = 0;
                uint sum = 0;

                for (byte i = 0; i < 5; i++)
                {
                    if (_pings[i] != 0)
                    {
                        count++;
                        sum += _pings[i];
                    }
                }

                if (count == 0)
                {
                    return 0;
                }

                return sum / count;
            }
        }

        public void PingReceived()
        {
            _pings[_pingIdx++] = (uint) _pingStopwatch.ElapsedMilliseconds;

            if (_pingIdx >= _pings.Length)
            {
                _pingIdx = 0;
            }

            _pingStopwatch.Stop();
        }

        public void SendPing()
        {
            if (!_socket.IsConnected || _socket.IsDisposed)
            {
                return;
            }

            _pingStopwatch.Restart();
            _socket.Send(new PPing());
        }

        public void Reset()
        {
            _pingStopwatch.Reset();
            ConnectedFrom = DateTime.MinValue;
            _lastTotalBytesReceived = _lastTotalBytesSent = _lastTotalPacketsReceived = _lastTotalPacketsSent = 0;
            TotalBytesReceived = TotalBytesSent = TotalPacketsReceived = TotalPacketsSent = 0;
            DeltaBytesReceived = DeltaBytesSent = DeltaPacketsReceived = DeltaPacketsSent = 0;
        }

        public void Update()
        {
            DeltaBytesReceived = TotalBytesReceived - _lastTotalBytesReceived;
            DeltaBytesSent = TotalBytesSent - _lastTotalBytesSent;
            DeltaPacketsReceived = TotalPacketsReceived - _lastTotalPacketsReceived;
            DeltaPacketsSent = TotalPacketsSent - _lastTotalPacketsSent;
            _lastTotalBytesReceived = TotalBytesReceived;
            _lastTotalBytesSent = TotalBytesSent;
            _lastTotalPacketsReceived = TotalPacketsReceived;
            _lastTotalPacketsSent = TotalPacketsSent;
        }

        public override string ToString()
        {
            return
                $"Packets:\n >> {DeltaPacketsReceived}\n << {DeltaPacketsSent}\nBytes:\n >> {GetSizeAdaptive(DeltaBytesReceived)}\n << {GetSizeAdaptive(DeltaBytesSent)}";
        }

        public static string GetSizeAdaptive(long bytes)
        {
            decimal num = bytes;
            string arg = "B";

            if (!(num < 1024m))
            {
                arg = "KB";
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
            }

            return $"{Math.Round(num, 2):0.##} {arg}";
        }
    }
}