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
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Network.Encryption;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Network
{
    enum ClientSocketStatus
    {
        Disconnected,
        Connecting,
        Connected,
    }

    internal sealed class NetClient
    {
        private const int BUFF_SIZE = 0x80000;

        private int _incompletePacketLength;
        private bool _isCompressionEnabled;
        private byte[] _recvBuffer, _incompletePacketBuffer, _decompBuffer, _packetBuffer;
        private CircularBuffer _circularBuffer;
        private ConcurrentQueue<byte[]> _pluginRecvQueue = new ConcurrentQueue<byte[]>();
        private readonly bool _is_login_socket;
        private TcpClient _tcpClient;
        private NetworkStream _netStream;


        private NetClient(bool is_login_socket)
        {
            _is_login_socket = is_login_socket;
            Statistics = new NetStatistics(this);
        }


        public static NetClient LoginSocket { get; } = new NetClient(true);

        public static NetClient Socket { get; } = new NetClient(false);



        public bool IsConnected => _tcpClient != null && _tcpClient.Connected;

        public bool IsDisposed { get; private set; }

        public ClientSocketStatus Status { get; private set; }

        public NetStatistics Statistics { get; }

        private static uint? _client_address;


        public static uint ClientAddress
        {
            get
            {
                if (!_client_address.HasValue)
                {
                    try
                    {
                        _client_address = 0x100007f;

                        //var address = GetLocalIpAddress();

                        //_client_address = ((address & 0xff) << 0x18) | ((address & 65280) << 8) | ((address >> 8) & 65280) | ((address >> 0x18) & 0xff);
                    }
                    catch
                    {
                        _client_address = 0x100007f;
                    }
                }

                return _client_address.Value;
            }
        }

        public event EventHandler Connected;
        public event EventHandler<SocketError> Disconnected;

        public static event EventHandler<PacketWriter> PacketSent;

        private static readonly Task<bool> TaskCompletedFalse = new Task<bool>(() => false);

        public static void EnqueuePacketFromPlugin(byte[] data, int length)
        {
            if (LoginSocket.IsDisposed && Socket.IsConnected)
            {
                Socket._pluginRecvQueue.Enqueue(data);
                Socket.Statistics.TotalPacketsReceived++;
            }
            else if (Socket.IsDisposed && LoginSocket.IsConnected)
            {
                LoginSocket._pluginRecvQueue.Enqueue(data);
                LoginSocket.Statistics.TotalPacketsReceived++;
            }
            else
            {
                Log.Error("Attempt to write into a dead socket");
            }
        }

        public Task<bool> Connect(string ip, ushort port)
        {
            IsDisposed = false;
            IPAddress address = ResolveIP(ip);

            if (address == null)
            {
                return TaskCompletedFalse;
            }

            return Connect(address, port);
        }

        public Task<bool> Connect(IPAddress address, ushort port)
        {
            IsDisposed = false;

            if (Status != ClientSocketStatus.Disconnected)
            {
                Log.Warn($"Socket status: {Status}");

                return TaskCompletedFalse;
            }

            _tcpClient = new TcpClient
            {
                ReceiveBufferSize = BUFF_SIZE,
                SendBufferSize = BUFF_SIZE,
                NoDelay = true,
                ReceiveTimeout = 0,
                SendTimeout = 0
            };

            _recvBuffer = new byte[BUFF_SIZE];
            _incompletePacketBuffer = new byte[BUFF_SIZE];
            _decompBuffer = new byte[BUFF_SIZE];
            _packetBuffer = new byte[4096 * 4];
            _circularBuffer = new CircularBuffer();
            _pluginRecvQueue = new ConcurrentQueue<byte[]>();
            Statistics.Reset();

            Status = ClientSocketStatus.Connecting;

            return InternalConnect(address, port);
        }

        private Task<bool> InternalConnect(IPAddress address, ushort port)
        {
            try
            {
                return _tcpClient
                       .ConnectAsync(address, port)
                          .ContinueWith(
                    (t) =>
                    {
                        if (!t.IsFaulted && _tcpClient.Connected)
                        {
                            _netStream = _tcpClient.GetStream();
                            Status = ClientSocketStatus.Connected;
                            Connected.Raise();
                            Statistics.ConnectedFrom = DateTime.Now;

                            return true;
                        }


                        Status = ClientSocketStatus.Disconnected;
                        Log.Error("socket not connected");

                        return false;

                    }, TaskContinuationOptions.ExecuteSynchronously);
            }
            catch (SocketException e)
            {
                Log.Error($"Socket error when connecting:\n{e}");
                Disconnect(e.SocketErrorCode);

                return TaskCompletedFalse;
            }
        }

        public void Disconnect()
        {
            Disconnect(SocketError.Success);
        }

        private void Disconnect(SocketError error)
        {
            if (IsDisposed)
            {
                return;
            }

            Status = ClientSocketStatus.Disconnected;
            IsDisposed = true;

            if (_tcpClient == null)
            {
                return;
            }

            try
            {
                _tcpClient.Close();
            }
            catch
            {
            }

            try
            {
                _netStream?.Dispose();
            }
            catch
            {
            }

            Log.Trace($"Disconnected [{(_is_login_socket ? "login socket" : "game socket")}]");

            _incompletePacketBuffer = null;
            _incompletePacketLength = 0;
            _recvBuffer = null;
            _isCompressionEnabled = false;
            _tcpClient = null;
            _netStream = null;
            _circularBuffer = null;

            if (error != 0)
            {
                Disconnected.Raise(error);
            }

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

        public void Send(byte[] data, int length, bool ignorePlugin = false, bool skip_encryption = false)
        {
            if (!ignorePlugin && !Plugin.ProcessSendPacket(ref data, ref length))
            {
                return;
            }

            PacketSent.Raise(new PacketWriter(data, length));
            Send(data, length, skip_encryption);
        }

        private void Send(byte[] data, int length, bool skip_encryption)
        {
            if (_tcpClient == null || IsDisposed)
            {
                return;
            }

            if (_netStream == null || !_tcpClient.Connected)
            {
                return;
            }

            if (data != null && data.Length != 0 && length > 0)
            {
                if (CUOEnviroment.PacketLog)
                {
                    LogPacket(data, length, true);
                }

                if (!skip_encryption)
                {
                    EncryptionHelper.Encrypt(_is_login_socket, ref data, ref data, length);
                }

                try
                {
                    _netStream.Write(data, 0, length);
                    _netStream.Flush();

                    Statistics.TotalBytesSent += (uint) length;
                    Statistics.TotalPacketsSent++;
                }
                catch (SocketException ex)
                {
                    Log.Error("socket error when sending:\n" + ex);
                    Disconnect(ex.SocketErrorCode);
                }
                catch (Exception ex)
                {
                    if (ex.InnerException is SocketException socketEx)
                    {
                        Log.Error("socket error when sending:\n" + socketEx);
                        Disconnect(socketEx.SocketErrorCode);
                    }
                    else
                    {
                        Log.Error("fatal error when receiving:\n" + ex);
                        Disconnect();
                    }
                }
            }
        }


        public void Update()
        {
            ProcessRecv();

            while (_pluginRecvQueue.TryDequeue(out byte[] data) && data != null && data.Length != 0)
            {
                int length = PacketsTable.GetPacketLength(data[0]);
                int offset = 1;

                if (length == -1)
                {
                    if (data.Length < 3)
                    {
                        continue;
                    }

                    //length = data[2] | (data[1] << 8);
                    offset = 3;
                }

                PacketHandlers.Handlers.AnalyzePacket(data, offset, data.Length);
            }
        }

        private void ExtractPackets()
        {
            if (!IsConnected || _circularBuffer == null || _circularBuffer.Length <= 0)
            {
                return;
            }

            lock (_circularBuffer)
            {
                int length = _circularBuffer.Length;

                while (length > 0 && IsConnected)
                {
                    if (!GetPacketInfo(_circularBuffer, length, out int offset, out int packetlength))
                    {
                        break;
                    }

                    if (packetlength > 0)
                    {
                        // Patch to maintain a retrocompatibiliy with older cuoapi
                        byte[] data = new byte[packetlength]; // _packetBuffer;

                        _circularBuffer.Dequeue(data, 0, packetlength);

                        if (CUOEnviroment.PacketLog)
                        {
                            LogPacket(data, packetlength, false);
                        }

                        if (Plugin.ProcessRecvPacket(ref data, ref packetlength))
                        {
                            PacketHandlers.Handlers.AnalyzePacket(data, offset, packetlength);

                            Statistics.TotalPacketsReceived++;
                        }
                    }

                    length = _circularBuffer?.Length ?? 0;
                }
            }
        }

        private static bool GetPacketInfo(CircularBuffer buffer, int bufferLength, out int offset, out int length)
        {
            if (buffer == null || bufferLength <= 0)
            {
                length = 0;
                offset = 0;

                return false;
            }

            length = PacketsTable.GetPacketLength(buffer.GetID());
            offset = 1;

            if (length == -1)
            {
                if (bufferLength < 3)
                {
                    return false;
                }

                length = buffer.GetLength();
                offset = 3;
            }

            return bufferLength >= length;
        }

        private void ProcessRecv()
        {
            if (IsDisposed || Status != ClientSocketStatus.Connected)
            {
                return;
            }

            if (!IsConnected && !IsDisposed)
            {
                Disconnect();

                return;
            }

            if (_tcpClient.Available <= 0)
            {
                return;
            }

            try
            {
                int received = _netStream.Read(_recvBuffer, 0, _tcpClient.Available);

                if (received > 0)
                {
                    Statistics.TotalBytesReceived += (uint) received;

                    byte[] buffer = _recvBuffer;

                    if (!_is_login_socket)
                    {
                        EncryptionHelper.Decrypt(ref buffer, ref buffer, received);
                    }

                    if (_isCompressionEnabled)
                    {
                        DecompressBuffer(ref buffer, ref received);
                    }

                    lock (_circularBuffer)
                    {
                        _circularBuffer.Enqueue(buffer, 0, received);
                    }

                    ExtractPackets();
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
                if (ex.InnerException is SocketException socketEx)
                {
                    Log.Error("socket error when receiving:\n" + socketEx);
                    Disconnect(socketEx.SocketErrorCode);
                }
                else
                {
                    Log.Error("fatal error when receiving:\n" + ex);
                    Disconnect();
                }
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

            while (Huffman.DecompressChunk
                (ref source, ref sourceOffset, sourcelength, ref buffer, offset, out int outSize))
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

            if (string.IsNullOrEmpty(addr))
            {
                return result;
            }

            if (!IPAddress.TryParse(addr, out result))
            {
                try
                {
                    IPHostEntry hostEntry = Dns.GetHostEntry(addr);

                    if (hostEntry.AddressList.Length != 0)
                    {
                        result = hostEntry.AddressList[hostEntry.AddressList.Length - 1];
                    }
                }
                catch
                {
                }
            }

            return result;
        }

        private static LogFile _logFile;
        private static void LogPacket(byte[] buffer, int length, bool toServer)
        {
            if (_logFile == null)
                _logFile = new LogFile
                (
                    FileSystemHelper.CreateFolderIfNotExists(CUOEnviroment.ExecutablePath, "Logs", "Network"),
                    "packets.log"
                );

            int pos = 0;

            StringBuilder output = new StringBuilder();

            output.AppendFormat
            (
                "{0}   -   ID {1:X2}   Length: {2}\n", (toServer ? "Client -> Server" : "Server -> Client"), buffer[0], buffer.Length
            );

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

    }
}