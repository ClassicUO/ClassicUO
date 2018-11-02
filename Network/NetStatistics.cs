using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Network
{
    public class NetStatistics
    {
        private uint _lastTotalBytesSended, _lastTotalByteReceived, _lastTotalPacketsSended, _lastTotalPacketsReceived;
        private uint _currentTotalBytesSended, _currentTotalByteReceived, _currentTotalPacketsSended, _currentTotalPacketsReceived;

        public NetStatistics()
        {

        }

        public DateTime ConnectedFrom { get; set; }

        public uint TotalBytesSended { get; set; }
        public uint TotalBytesReceived { get; set; }

        public uint TotalPacketsSended { get; set; }
        public uint TotalPacketsReceived { get; set; }

        public void Reset()
        {
            ConnectedFrom = DateTime.MinValue;
            _currentTotalBytesSended = _currentTotalByteReceived = _currentTotalPacketsSended = _currentTotalPacketsReceived = 0;
            _lastTotalBytesSended = _lastTotalByteReceived = _lastTotalPacketsSended = _lastTotalPacketsReceived = 0;
            TotalBytesReceived = TotalBytesSended = TotalPacketsReceived = TotalPacketsSended = 0;
        }

        public void Update()
        {
            _currentTotalByteReceived = _lastTotalByteReceived;
            _currentTotalBytesSended = _lastTotalBytesSended;

            _currentTotalPacketsReceived = _lastTotalPacketsReceived;
            _currentTotalPacketsSended = _lastTotalPacketsSended;


            _lastTotalByteReceived = TotalBytesReceived;
            _lastTotalBytesSended = TotalBytesSended;

            _lastTotalPacketsReceived = TotalPacketsReceived;
            _lastTotalPacketsSended = TotalPacketsSended;
        }

        public override string ToString() => $"Packets:\n >> {_lastTotalPacketsReceived - _currentTotalPacketsReceived}\n << {_lastTotalPacketsSended - _currentTotalPacketsSended}\nBytes:\n >> {GetSizeAdaptive(_lastTotalByteReceived - _currentTotalByteReceived)}\n << {GetSizeAdaptive(_lastTotalBytesSended - _currentTotalBytesSended)}";

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
