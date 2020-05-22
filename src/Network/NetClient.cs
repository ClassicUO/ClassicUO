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
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Network
{
    internal sealed class NetClient
    {
        private const int BUFF_SIZE = 0x80000;
        private int _incompletePacketLength;
        private bool _isCompressionEnabled;
        private byte[] _recvBuffer, _incompletePacketBuffer, _decompBuffer;
        private Socket _socket;
        private CircularBuffer _circularBuffer;
        private ConcurrentQueue<Packet> _recvQueue = new ConcurrentQueue<Packet>();
        private bool _connectAsync;
        private readonly bool _is_login_socket;

        private NetClient(bool connectAsync, bool is_login_socket)
        {
            _is_login_socket = is_login_socket;
            _connectAsync = connectAsync;
            Statistics = new NetStatistics();
        }

        public static NetClient LoginSocket { get; } = new NetClient(true, true);

        public static NetClient Socket { get; } = new NetClient(false, false);

        public bool IsConnected => _socket != null && _socket.Connected;

        public bool IsDisposed { get; private set; }

        public NetStatistics Statistics { get; }

        public uint ClientAddress
        {
            get
            {
                uint address;

                try
                {
                    IPHostEntry localEntry = Dns.GetHostEntry(Dns.GetHostName());

                    if (localEntry.AddressList.Length > 0)
                    {
    #pragma warning disable 618
                        address = (uint)localEntry.AddressList.FirstOrDefault(s => s.AddressFamily == AddressFamily.InterNetwork).Address;
    #pragma warning restore 618
                    }
                    else
                        address = 0x100007f;
                }
                catch (SocketException e)
                {
                    address = 0x100007f;
                }

                return ((address & 0xff) << 0x18) | ((address & 65280) << 8) | ((address >> 8) & 65280) | ((address >> 0x18) & 0xff);
            }
        }

        public event EventHandler Connected;
        public event EventHandler<SocketError> Disconnected;

        public static event EventHandler<Packet> PacketReceived;
        public static event EventHandler<PacketWriter> PacketSent;

        public static void EnqueuePacketFromPlugin(byte[] data, int length)
        {
            if (LoginSocket.IsDisposed && Socket.IsConnected)
            {
                Socket._recvQueue.Enqueue(new Packet(data, length) { Filter = true });
                Socket.Statistics.TotalPacketsReceived++;
            }
            else if (Socket.IsDisposed && LoginSocket.IsConnected)
            {
                Socket._recvQueue.Enqueue(new Packet(data, length) { Filter = true });
                LoginSocket.Statistics.TotalPacketsReceived++;
            }
            else
                Log.Error( "Attempt to write into a dead socket");
        }

        public bool Connect(string ip, ushort port)
        {
            IsDisposed = false;
            IPAddress address = ResolveIP(ip);

            if (address == null)
                return false;

            IPEndPoint endpoint = new IPEndPoint(address, port);
            Connect(endpoint);

            return true;
        }

        public void Connect(IPAddress address, ushort port)
        {
            IsDisposed = false;
            IPEndPoint endpoint = new IPEndPoint(address, port);
            Connect(endpoint);
        }

        private void Connect(IPEndPoint endpoint)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { ReceiveBufferSize = BUFF_SIZE, SendBufferSize = BUFF_SIZE};
            _socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, 1);
            _recvBuffer = new byte[BUFF_SIZE];
            _incompletePacketBuffer = new byte[BUFF_SIZE];
            _decompBuffer = new byte[BUFF_SIZE];
            _circularBuffer = new CircularBuffer();
            _recvQueue = new ConcurrentQueue<Packet>();
            Statistics.Reset();

            _socket.ReceiveTimeout = -1;
            _socket.SendTimeout = -1;

            if (_connectAsync)
            {
                Task.Run(() =>
                {
                    InternalConnect(endpoint);
                });
            }
            else
            {
                InternalConnect(endpoint);
            }
        }

        private void InternalConnect(EndPoint endpoint)
        {
            try
            {
                _socket.Connect(endpoint);

                if (_socket.Connected)
                {
                    Connected.Raise();
                    Statistics.ConnectedFrom = DateTime.Now;
                    StartRecv();
                }
                else 
                    Log.Error("socket not connected");
            }
            catch (SocketException e)
            {
                Log.Error($"Socket error when connecting:\n{e}");
                Disconnect(e.SocketErrorCode);
            }
        }

        public void Disconnect()
        {
            Disconnect(SocketError.Success);
        }

        private void Disconnect(SocketError error)
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            if (_socket == null)
                return;

            try
            {
                _socket.Shutdown(SocketShutdown.Both);
            }
            catch
            {
            }

            try
            {
                _socket.Close();
            }
            catch
            {
            }

            Log.Trace($"Disconnected [{(_is_login_socket ? "login socket" : "game socket")}]");

            _incompletePacketBuffer = null;
            _incompletePacketLength = 0;
            _recvBuffer = null;
            _isCompressionEnabled = false;
            _socket = null;
            _circularBuffer = null;

            if (error != 0)
                Disconnected.Raise(error);

            Statistics.Reset();
        }

        public void EnableCompression()
        {
            _isCompressionEnabled = true;
        }

        public void Send(PacketWriter p)
        {
            ref byte[] data = ref p.ToArray();
            int length = p.Length;

            if (Plugin.ProcessSendPacket(ref data, ref length))
            {
                PacketSent.Raise(p);
                Send(data, length, false);
            }
        }

        public void Send(byte[] data, bool ignorePlugin = false, bool skip_encryption = false)
        {
            int length = data.Length;

            if (!ignorePlugin && !Plugin.ProcessSendPacket(ref data, ref length))
            {
               return;
            }
            
            PacketSent.Raise(new PacketWriter(data));
            Send(data, length, skip_encryption);
        }

        private void Send(byte[] data, int length, bool skip_encryption)
        {
            if (_socket == null || IsDisposed)
                return;

            if (data != null && data.Length != 0 && length > 0)
            {
                if (!skip_encryption)
                    EncryptionHelper.Encrypt(_is_login_socket,
                                             ref data,
                                             ref data,
                                             length);

#if !DEBUG
                    //LogPacket(data, true);
#endif

                try
                {
                    lock (_socket)
                    {
                        int sent = _socket.Send(data, 0, length, SocketFlags.None);

                        if (sent > 0)
                        {
                            Statistics.TotalBytesSent += (uint) sent;
                            Statistics.TotalPacketsSent++;
                        }
                    }
                }
                catch (SocketException ex)
                {
                    Log.Error("socket error when sending:\n" + ex);
                    Disconnect(ex.SocketErrorCode);
                }
            }
        }


        public void Update()
        {
            while (_recvQueue.TryDequeue(out Packet p))
            {
                ref byte[] data = ref p.ToArray();
                int length = p.Length;

                if (p.Filter || Plugin.ProcessRecvPacket(ref data, ref length))
                {
                    PacketReceived.Raise(p);
                }
            }
        }

        private void ExtractPackets()
        {
            if (!IsConnected || _circularBuffer == null || _circularBuffer.Length <= 0) return;

            lock (_circularBuffer)
            {
                int length = _circularBuffer.Length;

                while (length > 0 && IsConnected)
                {
                    byte id = _circularBuffer.GetID();
                    if (id == 0xFF)
                        break;

                    int packetlength = PacketsTable.GetPacketLength(id);

                    if (packetlength == -1)
                    {
                        if (length >= 3)
                            packetlength = _circularBuffer.GetLength();
                        else
                            break;
                    }

                    if (length < packetlength)
                        break;


                    if (packetlength > 0)
                    {
                        byte[] data = new byte[packetlength];
                        packetlength = _circularBuffer.Dequeue(data, 0, packetlength);

#if !DEBUG
                    //LogPacket(data, false);
#endif
                        _recvQueue.Enqueue(new Packet(data, packetlength));
                        Statistics.TotalPacketsReceived++;
                    }
                    
                    length = _circularBuffer.Length;
                }
            }
        }


#if !DEBUG
        private static LogFile _logFile;

        private static void LogPacket(byte[] buffer, bool toServer)
        {
            if (_logFile == null)
                _logFile = new LogFile(FileSystemHelper.CreateFolderIfNotExists(CUOEnviroment.ExecutablePath, "Logs", "Network"), "packets.log");

            int length = buffer.Length;
            int pos = 0;

            StringBuilder output = new StringBuilder();
            output.AppendFormat("{0}   -   ID {1}   Length: {2}\n", (toServer ? "Client -> Server" : "Server -> Client"), buffer[0], buffer.Length);

            if (buffer[0] == 0x80 || buffer[0] == 0x91)
            {
                output.AppendLine("[ACCOUNT CREDENTIALS HIDDEN]");
            }
            else
            {
                output.AppendLine("        0  1  2  3  4  5  6  7   8  9  A  B  C  D  E  F");
                output.AppendLine("       -- -- -- -- -- -- -- --  -- -- -- -- -- -- -- --");

                int byteIndex = 0;

                int whole = length >> 4;
                int rem = length & 0xF;

                for (int i = 0; i < whole; ++i, byteIndex += 16)
                {
                    StringBuilder bytes = new StringBuilder(49);
                    StringBuilder chars = new StringBuilder(16);

                    for (int j = 0; j < 16; ++j)
                    {
                        int c = buffer[pos++];

                        bytes.Append(c.ToString("X2"));

                        if (j != 7)
                        {
                            bytes.Append(' ');
                        }
                        else
                        {
                            bytes.Append("  ");
                        }

                        if (c >= 0x20 && c < 0x80)
                        {
                            chars.Append((char)c);
                        }
                        else
                        {
                            chars.Append('.');
                        }
                    }

                    output.Append(byteIndex.ToString("X4"));
                    output.Append("   ");
                    output.Append(bytes);
                    output.Append("  ");
                    output.AppendLine(chars.ToString());
                }

                if (rem != 0)
                {
                    StringBuilder bytes = new StringBuilder(49);
                    StringBuilder chars = new StringBuilder(rem);

                    for (int j = 0; j < 16; ++j)
                    {
                        if (j < rem)
                        {
                            int c = buffer[pos++];

                            bytes.Append(c.ToString("X2"));

                            if (j != 7)
                            {
                                bytes.Append(' ');
                            }
                            else
                            {
                                bytes.Append("  ");
                            }

                            if (c >= 0x20 && c < 0x80)
                            {
                                chars.Append((char)c);
                            }
                            else
                            {
                                chars.Append('.');
                            }
                        }
                        else
                        {
                            bytes.Append("   ");
                        }
                    }

                    output.Append(byteIndex.ToString("X4"));
                    output.Append("   ");
                    output.Append(bytes);
                    output.Append("  ");
                    output.AppendLine(chars.ToString());
                }
            }


            output.AppendLine();
            output.AppendLine();

            _logFile.Write(output.ToString());
        }
#endif


        private void StartRecv()
        {
            if (IsConnected && !IsDisposed)
                _socket.BeginReceive(_recvBuffer, 0, BUFF_SIZE, SocketFlags.None, ProcessRecv, null);
        }


        private void ProcessRecv(IAsyncResult e)
        {
            if (IsDisposed)
                return;

            if (!IsConnected && !IsDisposed)
            {
                Disconnect();
                return;
            }

            try
            {
                int bytesLen = _socket.EndReceive(e);

                if (bytesLen > 0)
                {
                    Statistics.TotalBytesReceived += (uint) bytesLen;

                    byte[] buffer = _recvBuffer;

                    if (!_is_login_socket)
                    {
                        EncryptionHelper.Decrypt(ref buffer, ref buffer, bytesLen);
                    }

                    if (_isCompressionEnabled)
                    {
                        DecompressBuffer(ref buffer, ref bytesLen);
                    }

                    lock (_circularBuffer)
                        _circularBuffer.Enqueue(buffer, 0, bytesLen);

                    ExtractPackets();

                    StartRecv();
                }
                else
                {
                    Log.Warn("Server sent 0 bytes. Closing connection");
                    Disconnect(SocketError.SocketError);
                }
            }
            catch (SocketException socketException)
            {
                Log.Error("socket error when receiving:\n" + socketException);
                Disconnect(socketException.SocketErrorCode);
            }
            catch (Exception ex)
            {
                Log.Error("fatal error when receiving:\n" + ex);
                Disconnect();
            }
           
        }

        private void DecompressBuffer(ref byte[] buffer, ref int length)
        {
            byte[] source = _decompBuffer;
            int incompletelength = _incompletePacketLength;
            int sourcelength = incompletelength + length;

            if (incompletelength > 0)
            {
                Buffer.BlockCopy(_incompletePacketBuffer, 0, source, 0, _incompletePacketLength);
                _incompletePacketLength = 0;
            }

            // if outbounds exception, BUFF_SIZE must be increased
            Buffer.BlockCopy(buffer, 0, source, incompletelength, length);
            int processedOffset = 0;
            int sourceOffset = 0;
            int offset = 0;

            while (Huffman.DecompressChunk(ref source, ref sourceOffset, sourcelength, ref buffer, offset, out int outSize))
            {
                processedOffset = sourceOffset;
                offset += outSize;
            }

            length = offset;

            if (processedOffset < sourcelength)
            {
                int l = sourcelength - processedOffset;
                Buffer.BlockCopy(source, processedOffset, _incompletePacketBuffer, _incompletePacketLength, l);
                _incompletePacketLength += l;
            }
        }

        private static IPAddress ResolveIP(string addr)
        {
            IPAddress result = IPAddress.None;

            if (string.IsNullOrEmpty(addr)) return result;

            if (!IPAddress.TryParse(addr, out result))
            {
                try
                {
                    IPHostEntry hostEntry = Dns.GetHostEntry(addr);

                    if (hostEntry.AddressList.Length != 0)
                        result = hostEntry.AddressList[hostEntry.AddressList.Length - 1];
                }
                catch
                {
                }
            }

            return result;
        }
    }
}