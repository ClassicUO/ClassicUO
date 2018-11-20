//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net;
//using System.Net.Sockets;

//using ClassicUO.Utility;
//using ClassicUO.Utility.Logging;

//namespace ClassicUO.Network
//{
//    internal sealed class NetClient
//    {
//        private const int BUFF_SIZE = 0x10000;
//        private readonly object _sync = new object();
//        private int _incompletePacketLength;
//        private bool _isCompressionEnabled;
//        private Queue<Packet> _queue = new Queue<Packet>(), _workingQueue = new Queue<Packet>();
//        private byte[] _recvBuffer, _incompletePacketBuffer, _decompBuffer;
//        private TcpClient _tcpClient;

//        private NetClient()
//        {
//            Statistics = new NetStatistics();
//        }

//        public static NetClient LoginSocket { get; } = new NetClient();

//        public static NetClient Socket { get; } = new NetClient();

//        public bool IsConnected => _tcpClient != null && _tcpClient.Connected;

//        public bool IsDisposed { get; private set; }

//        public NetStatistics Statistics { get; }

//        public uint ClientAddress
//        {
//            get
//            {
//                IPHostEntry localEntry = Dns.GetHostEntry(Dns.GetHostName());
//                uint address;

//                if (localEntry.AddressList.Length > 0)
//                {
//#pragma warning disable 618
//                    address = (uint) localEntry.AddressList.FirstOrDefault(s => s.AddressFamily == AddressFamily.InterNetwork).Address;
//#pragma warning restore 618
//                }
//                else
//                    address = 0x100007f;

//                return ((address & 0xff) << 0x18) | ((address & 65280) << 8) | ((address >> 8) & 65280) | ((address >> 0x18) & 0xff);
//            }
//        }

//        public event EventHandler Connected, Disconnected;

//        public static event EventHandler<Packet> PacketReceived, PacketSended;

//        public void Connect(string ip, ushort port)
//        {
//            IsDisposed = false;
//            IPAddress address = ResolveIP(ip);
//            IPEndPoint endpoint = new IPEndPoint(address, port);
//            Connect(endpoint);
//        }

//        public void Connect(IPAddress address, ushort port)
//        {
//            IsDisposed = false;
//            IPEndPoint endpoint = new IPEndPoint(address, port);
//            Connect(endpoint);
//        }

//        private void Connect(IPEndPoint endPoint)
//        {
//            _tcpClient = new TcpClient
//            {
//                NoDelay = true,
//            };
//            _recvBuffer = new byte[BUFF_SIZE];
//            _incompletePacketBuffer = new byte[BUFF_SIZE];
//            _decompBuffer = new byte[BUFF_SIZE];
//            _queue.Clear();
//            _workingQueue.Clear();

//            Statistics.Reset();

//            _tcpClient.Connect(endPoint);

//            if (IsConnected)
//            {
//                Connected.Raise();
//                Statistics.ConnectedFrom = DateTime.Now;
//                _tcpClient.Client.BeginReceive(_recvBuffer, 0, BUFF_SIZE, SocketFlags.None, OnReceive, null);
//            }
//            else
//                Disconnect();
//        }

//        public void Update()
//        {
//            if (!IsConnected)
//                return;

//            lock (_sync)
//            {
//                Queue<Packet> temp = _workingQueue;
//                _workingQueue = _queue;
//                _queue = temp;
//            }

//            while (_queue.Count > 0)
//                PacketReceived.Raise(_queue.Dequeue());
//        }

//        private void OnReceive(IAsyncResult result)
//        {
//            if (!IsConnected)
//                return;

//            int length = _tcpClient.Client.EndReceive(result);

//            if (length > 0)
//            {
//                Statistics.TotalBytesReceived += (uint) length;

//                byte[] buffer = _recvBuffer;

//                if (_isCompressionEnabled)
//                    DecompressBuffer(ref buffer, ref length);

//                ExtractPackets(buffer, ref length);

//                if (length > 0)
//                    Log.Message(LogTypes.Warning, $"Seems there is a incomplete packet. Length: {length}");

//                _tcpClient.Client.BeginReceive(_recvBuffer, 0, BUFF_SIZE, SocketFlags.None, OnReceive, null);
//            }
//        }

//        public void Send(PacketWriter packet)
//        {
//            if (IsConnected)
//            {
//                byte[] data = packet.ToArray();
//                Packet p = new Packet(data, data.Length);
//                PacketSended.Raise(p);
//                Send(data);
//            }
//        }

//        private void Send(byte[] data)
//        {
//            if (data == null || data.Length <= 0)
//                return;

//            try
//            {
//                int sent = _tcpClient.Client.Send(data);
//                if (sent <= 0)
//                    Disconnect();
//                else
//                {
//                    Statistics.TotalBytesSended += (uint) data.Length;
//                    Statistics.TotalPacketsSended++;
//                }
//            }
//            catch
//            {
//                Disconnect();
//            }
//        }

//        public void Disconnect()
//        {
//            if (IsDisposed)
//                return;
//            IsDisposed = true;

//            if (_tcpClient == null)
//                return;
//            Update();

//            try
//            {
//                _tcpClient.Client.Shutdown(SocketShutdown.Both);
//                _tcpClient.Client.Close();
//            }
//            catch
//            {
//            }

//            _tcpClient.Dispose();
//            _tcpClient = null;
//            _incompletePacketBuffer = null;
//            _incompletePacketLength = 0;
//            _recvBuffer = null;
//            _isCompressionEnabled = false;
//            Disconnected.Raise();

//            Statistics.Reset();
//        }

//        public void EnableCompression()
//        {
//            _isCompressionEnabled = true;
//        }

//        private unsafe void ExtractPackets(byte[] buffer, ref int length)
//        {
//            int offset = 0;

//            while (length > 0 && IsConnected)
//            {
//                byte packetID = buffer[offset];
//                int packetLength = PacketsTable.GetPacketLength(packetID);

//                if (packetLength == -1)
//                {
//                    if (length >= 3)
//                        packetLength = buffer[offset + 2] | (buffer[offset + 1] << 8);
//                    else
//                        break;
//                }

//                if (length < packetLength)
//                    break;
//                byte[] packetData = new byte[packetLength];

//                fixed (byte* bufferPtr = &buffer[offset])
//                {
//                    fixed (byte* packetPtr = &packetData[0]) Buffer.MemoryCopy(bufferPtr, packetPtr, packetLength, packetLength);
//                }

//                lock (_sync)
//                {
//                    _workingQueue.Enqueue(new Packet(packetData, packetLength));
//                }

//                length -= packetLength;
//                offset += packetLength;
//            }
//        }

//        private void DecompressBuffer(ref byte[] buffer, ref int length)
//        {
//            byte[] source = _decompBuffer;
//            int incompletelength = _incompletePacketLength;
//            int sourcelength = incompletelength + length;

//            if (incompletelength > 0)
//            {
//                Buffer.BlockCopy(_incompletePacketBuffer, 0, source, 0, _incompletePacketLength);
//                _incompletePacketLength = 0;
//            }

//            // if outbounds exception, BUFF_SIZE must be increased
//            Buffer.BlockCopy(buffer, 0, source, incompletelength, length);
//            int processedOffset = 0;
//            int sourceOffset = 0;
//            int offset = 0;

//            while (Huffman.DecompressChunk(ref source, ref sourceOffset, sourcelength, ref buffer, offset, out int outSize))
//            {
//                processedOffset = sourceOffset;
//                offset += outSize;
//            }

//            length = offset;

//            if (processedOffset < sourcelength)
//            {
//                int l = sourcelength - processedOffset;
//                Buffer.BlockCopy(source, processedOffset, _incompletePacketBuffer, _incompletePacketLength, l);
//                _incompletePacketLength += l;
//            }
//        }

//        private static IPAddress ResolveIP(string addr)
//        {
//            IPAddress result = IPAddress.None;

//            if (string.IsNullOrEmpty(addr)) return result;

//            if (!IPAddress.TryParse(addr, out result))
//            {
//                try
//                {
//                    IPHostEntry hostEntry = Dns.GetHostEntry(addr);

//                    if (hostEntry.AddressList.Length != 0)
//                        result = hostEntry.AddressList[hostEntry.AddressList.Length - 1];
//                }
//                catch
//                {
//                }
//            }

//            return result;
//        }
//    }
//}
