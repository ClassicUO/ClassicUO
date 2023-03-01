#region license

// Copyright (c) 2021, andreakarasho
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

using ClassicUO.Network.Encryption;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

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
        private const int BUFF_SIZE = 0x10000;

        private bool _isCompressionEnabled;
        private ConcurrentQueue<byte[]> _pluginRecvQueue = new ConcurrentQueue<byte[]>();
        private readonly bool _isLoginSocket;
        private Socket _socket;
        private uint? _localIP;
        private readonly MemoryStream _recvCompressedStream, _recvUncompressedStream, _recvCompressedUnfinishedStream, _sendStream;


        private NetClient(bool is_login_socket)
        {
            _isLoginSocket = is_login_socket;
            Statistics = new NetStatistics(this);

            _recvCompressedStream = new MemoryStream();
            _recvUncompressedStream = new MemoryStream();
            _recvCompressedUnfinishedStream = new MemoryStream();
            _sendStream = new MemoryStream();
        }


        public static NetClient LoginSocket { get; } = new NetClient(true);
        public static NetClient Socket { get; } = new NetClient(false);
        public bool IsConnected => _socket != null && _socket.Connected;
        public bool IsDisposed { get; private set; }
        public ClientSocketStatus Status { get; private set; }
        public uint LocalIP
        {
            get
            {
                if (!_localIP.HasValue)
                {
                    try
                    {
                        byte[] addressBytes = (_socket?.LocalEndPoint as IPEndPoint)?.Address.MapToIPv4().GetAddressBytes();

                        if (addressBytes != null && addressBytes.Length != 0)
                        {
                            _localIP = (uint)(addressBytes[0] | (addressBytes[1] << 8) | (addressBytes[2] << 16) | (addressBytes[3] << 24));
                        }

                        if (!_localIP.HasValue || _localIP == 0)
                        {
                            _localIP = 0x100007f;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"error while retriving local endpoint address: \n{ex}");

                        _localIP = 0x100007f;
                    }
                }

                return _localIP.Value;
            }
        }
        public NetStatistics Statistics { get; }



        public event EventHandler Connected;
        public event EventHandler<SocketError> Disconnected;



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

        public async Task<bool> Connect(string ip, ushort port)
        {
            IsDisposed = false;
            IPAddress address = ResolveIP(ip);

            if (address == null)
            {
                return false;
            }

            return await Connect(address, port);
        }

        public async Task<bool> Connect(IPAddress address, ushort port)
        {
            IsDisposed = false;

            if (Status != ClientSocketStatus.Disconnected)
            {
                Log.Warn($"Socket status: {Status}");

                return false;
            }

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.NoDelay = true; // it's important to disable the Nagle algo or it will cause lag


            _recvCompressedStream.Seek(0, SeekOrigin.Begin);
            _recvUncompressedStream.Seek(0, SeekOrigin.Begin);
            _recvCompressedUnfinishedStream.Seek(0, SeekOrigin.Begin);
            _sendStream.Seek(0, SeekOrigin.Begin);

            _pluginRecvQueue = new ConcurrentQueue<byte[]>();
            Statistics.Reset();

            Status = ClientSocketStatus.Connecting;

            try
            {
                return await _socket
                    .ConnectAsync(address, port)
                    .ContinueWith
                    (
                        (t) =>
                        {
                            if (!t.IsFaulted && _socket.Connected)
                            {
                                Status = ClientSocketStatus.Connected;
                                Connected.Raise();
                                Statistics.ConnectedFrom = DateTime.Now;

                                return true;
                            }


                            Status = ClientSocketStatus.Disconnected;
                            Log.Error("socket not connected");

                            return false;
                        },
                        TaskContinuationOptions.ExecuteSynchronously
                    );
            }
            catch (SocketException e)
            {
                Log.Error($"Socket error when connecting:\n{e}");
                _logFile?.Write($"connection error: {e}");

                Disconnect(e.SocketErrorCode);

                return false;
            }
        }

        public void Disconnect() => Disconnect(SocketError.Success);

        private void Disconnect(SocketError error)
        {
            _logFile?.Write($"disconnection  -  socket_error: {error}");

            if (IsDisposed)
            {
                return;
            }

            Status = ClientSocketStatus.Disconnected;
            IsDisposed = true;

            if (_socket == null)
            {
                return;
            }

            try
            {
                _socket.Close();
            }
            catch
            {
            }

            try
            {
                _socket?.Dispose();
            }
            catch
            {
            }

            Log.Trace($"Disconnected [{(_isLoginSocket ? "login socket" : "game socket")}]");


            _isCompressionEnabled = false;
            _socket = null;
            _localIP = null;

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

        public void Send(byte[] data, int length, bool ignorePlugin = false, bool skip_encryption = false)
        {
            if (!ignorePlugin && !Plugin.ProcessSendPacket(data, ref length))
            {
                return;
            }

            Send(data, length, skip_encryption);
        }

        private void Send(byte[] data, int length, bool skip_encryption)
        {
            if (_socket == null || IsDisposed || !_socket.Connected)
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
                    EncryptionHelper.Encrypt(_isLoginSocket, data, data, length);
                }

                _sendStream.Write(data, 0, length);

                Statistics.TotalBytesSent += (uint)length;
                Statistics.TotalPacketsSent++;
            }
        }


        public void Update()
        {
            ProcessRecv();
            ExtractPackets();
            ProcessPluginPackets();
            ProcessSend();
        }

        private void ExtractPackets()
        {
            if (!IsConnected || _recvUncompressedStream.Position <= 0)
            {
                return;
            }

            var length = (int)_recvUncompressedStream.Position;
            var done = 0;

            _recvUncompressedStream.Seek(0, SeekOrigin.Begin);
            var packetBuffer = System.Buffers.ArrayPool<byte>.Shared.Rent(512);

            while (done < length && IsConnected)
            {
                if (!GetPacketInfo(_recvUncompressedStream, length, out int offset, out int packetlength))
                {
                    break;
                }

                if ((length - done) < packetlength)
                {
                    // need more data
                    break;
                }

                if (packetlength > packetBuffer.Length)
                {
                    System.Buffers.ArrayPool<byte>.Shared.Return(packetBuffer, false); // TODO: clear?
                    packetBuffer = System.Buffers.ArrayPool<byte>.Shared.Rent(packetlength);
                }

                _ = _recvUncompressedStream.Read(packetBuffer, 0, packetlength);

                if (CUOEnviroment.PacketLog)
                {
                    LogPacket(packetBuffer, packetlength, false);
                }

                // TODO: the pluging function should allow Span<byte> or unsafe type only.
                // The current one is a bad style decision.
                // It will be fixed once the new plugin system is done.
                if (Plugin.ProcessRecvPacket(packetBuffer, ref packetlength))
                {
                    PacketHandlers.Handlers.AnalyzePacket(packetBuffer, offset, packetlength);

                    Statistics.TotalPacketsReceived++;
                }

                done += packetlength;
            }

            // if length < done the packet is not fully processed, so keep this data
            _recvUncompressedStream.Seek(length - done, SeekOrigin.Begin);
            System.Buffers.ArrayPool<byte>.Shared.Return(packetBuffer, false); // TODO: clear?
        }

        private static bool GetPacketInfo(MemoryStream buffer, int bufferLength, out int packetOffset, out int packetLen)
        {
            if (buffer == null || bufferLength <= 0)
            {
                packetLen = 0;
                packetOffset = 0;

                return false;
            }

            packetLen = PacketsTable.GetPacketLength(buffer.ReadByte());
            packetOffset = 1;

            if (packetLen == -1)
            {
                if (bufferLength < 3)
                {
                    return false;
                }

                var b0 = buffer.ReadByte();
                var b1 = buffer.ReadByte();

                packetLen = b1 | (b0 << 8);

                packetOffset = 3;
            }

            buffer.Seek(-packetOffset, SeekOrigin.Current);

            return true;
            //return bufferLength >= length;
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

            int available = _socket.Available;

            if (available <= 0)
            {
                return;
            }

            const int SIZE_TO_READ = 4096;


            var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(SIZE_TO_READ);
            try
            {
                while (available > 0)
                {
                    var toRead = Math.Min(SIZE_TO_READ, available);
                    var received = _socket.Receive(buffer, 0, toRead, SocketFlags.None, out var errorCode);

                    // TODO: add a check if the toRead != received ??
                    available -= toRead;

                    if (errorCode != SocketError.Success)
                    {
                        break;
                    }

                    if (received <= 0)
                    {
                        break;
                    }

                    Statistics.TotalBytesReceived += (uint)received;
                    _recvCompressedStream.Write(buffer, 0, received);
                }


                var pos = (int)_recvCompressedStream.Position;

                if (pos == 0 || pos < available)
                {
                    // ERROR
                }

                var compressedBuffer = System.Buffers.ArrayPool<byte>.Shared.Rent(0x10000); // pos?

                try
                {
                    _recvCompressedStream.Seek(0, SeekOrigin.Begin);
                    _ = _recvCompressedStream.Read(compressedBuffer, 0, pos);
                    _recvCompressedStream.Seek(0, SeekOrigin.Begin);

                    ProcessEncryption(compressedBuffer, pos);
                    DecompressBuffer(compressedBuffer, ref pos);

                    _recvUncompressedStream.Write(compressedBuffer, 0, pos);
                }
                finally
                {
                    System.Buffers.ArrayPool<byte>.Shared.Return(compressedBuffer);
                }
            }
            catch (SocketException socketEx)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private void ProcessEncryption(byte[] buffer, int received)
        {
            if (_isLoginSocket) return;

            EncryptionHelper.Decrypt(buffer, buffer, received);
        }

        private void ProcessPluginPackets()
        {
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

                    offset = 3;
                }

                PacketHandlers.Handlers.AnalyzePacket(data, offset, data.Length);
            }
        }

        private void ProcessSend()
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

            var length = (int)_sendStream.Position;

            if (length <= 0)
            {
                return;
            }

            _sendStream.Seek(0, SeekOrigin.Begin);
            var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(length);

            try
            {
                var read = _sendStream.Read(buffer, 0, length);
                var sent = _socket.Send(buffer, 0, read, SocketFlags.None);
                _sendStream.Seek(0, SeekOrigin.Begin);
            }
            catch (SocketException ex)
            {
                Log.Error("socket error when sending:\n" + ex);
                _logFile?.Write($"disconnection  -  error during writing to the socket buffer: {ex}");

                Disconnect(ex.SocketErrorCode);
            }
            catch (Exception ex)
            {
                if (ex.InnerException is SocketException socketEx)
                {
                    Log.Error("main exception:\n" + ex);
                    Log.Error("socket error when sending:\n" + socketEx);

                    _logFile?.Write($"disconnection  -  error during writing to the socket buffer [2]: {socketEx}");
                    Disconnect(socketEx.SocketErrorCode);
                }
                else
                {
                    Log.Error("fatal error when sending:\n" + ex);

                    _logFile?.Write($"disconnection  -  error during writing to the socket buffer [3]: {ex}");

                    Disconnect();

                    throw;
                }
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private void DecompressBuffer(byte[] buffer, ref int length)
        {
            if (!_isCompressionEnabled) return;

            const int HUFFMAN_BUFFER_LEN = 0x10000;

            var source = System.Buffers.ArrayPool<byte>.Shared.Rent(HUFFMAN_BUFFER_LEN);

            try
            {
                var incompletelength = (int)_recvCompressedUnfinishedStream.Position;
                var sourcelength = incompletelength + length;

                if (incompletelength > 0)
                {
                    _recvCompressedUnfinishedStream.Seek(0, SeekOrigin.Begin);
                    _recvCompressedUnfinishedStream.Read(source, 0, incompletelength);
                    _recvCompressedUnfinishedStream.Seek(0, SeekOrigin.Begin);
                }

                Buffer.BlockCopy(buffer, 0, source, incompletelength, length);

                int processedOffset = 0;
                int sourceOffset = 0;
                int offset = 0;

                while (Huffman.DecompressChunk
                (
                    source,
                    ref sourceOffset,
                    sourcelength,
                    buffer,
                    offset,
                    out int outSize
                ))
                {
                    processedOffset = sourceOffset;
                    offset += outSize;
                }

                length = offset;

                if (processedOffset < sourcelength)
                {
                    _recvCompressedUnfinishedStream.Write(source, processedOffset, sourcelength - processedOffset);
                }
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(source, false); // TODO: clear?
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

        private static void LogPacket(Span<byte> buffer, int length, bool toServer)
        {
            if (_logFile == null)
                _logFile = new LogFile(FileSystemHelper.CreateFolderIfNotExists(CUOEnviroment.ExecutablePath, "Logs", "Network"), "packets.log");

            Span<char> span = stackalloc char[256];
            ValueStringBuilder output = new ValueStringBuilder(span);
            {
                int off = sizeof(ulong) + 2;

                output.Append(' ', off);
                output.Append(string.Format("Ticks: {0} | {1} |  ID: {2:X2}   Length: {3}\n", Time.Ticks, (toServer ? "Client -> Server" : "Server -> Client"), buffer[0], length));

                if (buffer[0] == 0x80 || buffer[0] == 0x91)
                {
                    output.Append(' ', off);
                    output.Append("[ACCOUNT CREDENTIALS HIDDEN]\n");
                }
                else
                {
                    output.Append(' ', off);
                    output.Append("0  1  2  3  4  5  6  7   8  9  A  B  C  D  E  F\n");

                    output.Append(' ', off);
                    output.Append("-- -- -- -- -- -- -- --  -- -- -- -- -- -- -- --\n");

                    ulong address = 0;

                    for (int i = 0; i < length; i += 16, address += 16)
                    {
                        output.Append($"{address:X8}");

                        for (int j = 0; j < 16; ++j)
                        {
                            if ((j % 8) == 0)
                            {
                                output.Append(" ");
                            }

                            if (i + j < length)
                            {
                                output.Append($" {buffer[i + j]:X2}");
                            }
                            else
                            {
                                output.Append("   ");
                            }
                        }

                        output.Append("  ");

                        for (int j = 0; j < 16 && i + j < length; ++j)
                        {
                            byte c = buffer[i + j];

                            if (c >= 0x20 && c < 0x80)
                            {
                                output.Append((char)c);
                            }
                            else
                            {
                                output.Append('.');
                            }
                        }

                        output.Append('\n');
                    }
                }

                output.Append('\n');
                output.Append('\n');

                _logFile.Write(output.ToString());

                output.Dispose();
            }
        }
    }
}