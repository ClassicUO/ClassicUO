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
using System.Buffers;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace ClassicUO.Network
{
    sealed class NetworkDataRader : IDisposable
    {
        private Socket _socket;


        public bool IsConnected => _socket?.Connected ?? false;
        public EndPoint LocalEndPoint => _socket?.LocalEndPoint;


        public event EventHandler OnConnected, OnDisconnected;
        public event EventHandler<SocketError> OnError;
        public event EventHandler<ArraySegment<byte>> OnPacketReceived;


        public void Connect(string ip, int port)
        {
            if (IsConnected) return;

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true
            };

            _socket.BeginConnect(ip, port, e =>
            {
                _socket.EndConnect(e);

                if (!IsConnected)
                {
                    OnError?.Invoke(this, SocketError.NotConnected);
                    return;
                }

                OnConnected?.Invoke(this, EventArgs.Empty);
                //BeginRead(new byte[1024 * 4]);
            }, this);
        }

        public void Send(byte[] buffer, int offset, int count)
        {
            _socket.Send(buffer, offset, count, SocketFlags.None);
        }

        private readonly byte[] _readBuffer = new byte[4096];

        public int Read(CircularBuffer dest)
        {
            var available = _socket.Available;
            if (available <= 0)
                return available;

            var done = 0;
            var buffer = _readBuffer;

            try
            {
                while (done < available)
                {
                    var toRead = Math.Min(buffer.Length, available - done);
                    var read = _socket.Receive(buffer, 0, toRead, SocketFlags.None, out var errorCode);

                    if (read <= 0 || errorCode != SocketError.Success)
                    {
                        Disconnect();

                        return 0;
                    }


                    if (toRead != read)
                    {

                    }

                    dest.Enqueue(buffer, 0, read);

                    done += read;
                }
            }
            finally
            {
            }

            if (done != available)
            {

            }

            return done;
        }

        public void Disconnect()
        {
            Dispose();
        }

        public void Dispose()
        {
            _socket?.Dispose();
        }

        private void EndRead(IAsyncResult asyncResult)
        {
            var buffer = (byte[])asyncResult.AsyncState;

            try
            {
                if (!IsConnected)
                    return;

                var read = _socket.EndReceive(asyncResult, out var error);
                if (read <= 0 || error != SocketError.Success)
                {
                    OnDisconnected?.Invoke(this, EventArgs.Empty);

                    if (error != SocketError.Success)
                    {
                        OnError?.Invoke(this, error);
                    }

                    _socket.Close();
                    _socket.Dispose();

                    return;
                }

                OnPacketReceived?.Invoke(this, new ArraySegment<byte>(buffer, 0, read));

                BeginRead(buffer);
            }
            catch (Exception ex)
            {

            }
            finally
            {
                //ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private void BeginRead(byte[] buffer)
        {
            _socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, EndRead, buffer);
        }
    }

    internal sealed class NetClient
    {
        private const int BUFF_SIZE = 0x10000;

        private bool _isCompressionEnabled;
        private readonly bool _isLoginSocket;
        private readonly NetworkDataRader _socket;
        private uint? _localIP;
        private readonly CircularBuffer _recvCompressedStream,
            _recvUncompressedStream,
            _recvUncompressedUnfinishedStream,
            _recvCompressedUnfinishedStream,
            _sendStream,
            _pluginsDataStream;

        private NetClient(bool isLoginSocket)
        {
            _isLoginSocket = isLoginSocket;
            Statistics = new NetStatistics(this);
            _recvCompressedStream = new CircularBuffer();
            _recvUncompressedStream = new CircularBuffer();
            _recvCompressedUnfinishedStream = new CircularBuffer();
            _sendStream = new CircularBuffer();
            _pluginsDataStream = new CircularBuffer();
            _recvUncompressedUnfinishedStream = new CircularBuffer();

            _socket = new NetworkDataRader();
            _socket.OnPacketReceived += OnDataReceived;
            _socket.OnConnected += (o, e) => Connected?.Invoke(this, EventArgs.Empty);
            _socket.OnDisconnected += (o, e) => Disconnected?.Invoke(this, SocketError.Success);
            _socket.OnError += (o, e) => Disconnected?.Invoke(this, SocketError.SocketError);
        }



        public static NetClient LoginSocket { get; } = new NetClient(true);
        public static NetClient Socket { get; } = new NetClient(false);
        public bool IsConnected => _socket != null && _socket.IsConnected;
        public bool IsDisposed { get; private set; }
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
                lock (Socket._pluginsDataStream)
                    Socket._pluginsDataStream.Enqueue(data, 0, length);
                Socket.Statistics.TotalPacketsReceived++;
            }
            else if (Socket.IsDisposed && LoginSocket.IsConnected)
            {
                lock (LoginSocket._pluginsDataStream)
                    LoginSocket._pluginsDataStream.Enqueue(data, 0, length);
                LoginSocket.Statistics.TotalPacketsReceived++;
            }
            else
            {
                Log.Error("Attempt to write into a dead socket");
            }
        }

        public void Connect(string ip, ushort port)
        {
            Statistics.Reset();

            _recvCompressedStream.Clear();
            _recvUncompressedStream.Clear();
            _recvCompressedUnfinishedStream.Clear();
            _sendStream.Clear();
            _pluginsDataStream.Clear();
            _recvUncompressedUnfinishedStream.Clear();

            IsDisposed = false;

            _socket.Connect(ip, port);
        }


        public void Disconnect()
        {
            IsDisposed = true;
            _isCompressionEnabled = false;
            Statistics.Reset();
            _socket.Disconnect();
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
            if (_socket == null || IsDisposed || !_socket.IsConnected)
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

                _sendStream.Enqueue(data, 0, length);

                Statistics.TotalBytesSent += (uint)length;
                Statistics.TotalPacketsSent++;
            }
        }


        public void Update()
        {
            ProcessRecv();

            ExtractPackets(_recvUncompressedStream, true);
            ExtractPackets(_pluginsDataStream, false);

            ProcessSend();
        }

        private byte[] _readingBuffer = new byte[4096];

        private void ExtractPackets(CircularBuffer stream, bool allowPlugins)
        {
            lock (stream)
            {
                if (!IsConnected) return;

                var packetBuffer = _readingBuffer;

                while (stream.Length > 0)
                {
                    if (!GetPacketInfo(stream, stream.Length, out var packetID, out int offset, out int packetlength))
                    {
                        Log.Warn($"Invalid ID: {packetID:X2} | off: {offset} | len: {packetlength} | stream.pos: {stream.Length}");

                        break;
                    }

                    if (stream.Length < packetlength)
                    {
                        Log.Warn($"need more data ID: {packetID:X2} | off: {offset} | len: {packetlength} | stream.pos: {stream.Length}");

                        // need more data
                        break;
                    }

                    while (packetlength > packetBuffer.Length)
                    {
                        Array.Resize(ref packetBuffer, packetBuffer.Length * 2);
                    }

                    var r = stream.Dequeue(packetBuffer, 0, packetlength);

                    if (CUOEnviroment.PacketLog)
                    {
                        LogPacket(packetBuffer, packetlength, false);
                    }

                    // TODO: the pluging function should allow Span<byte> or unsafe type only.
                    // The current one is a bad style decision.
                    // It will be fixed once the new plugin system is done.
                    if (!allowPlugins || Plugin.ProcessRecvPacket(packetBuffer, ref packetlength))
                    {
                        PacketHandlers.Handlers.AnalyzePacket(packetBuffer, offset, packetlength);

                        Statistics.TotalPacketsReceived++;
                    }
                }
            }
        }

        private static bool GetPacketInfo(CircularBuffer buffer, int bufferLen, out byte packetID, out int packetOffset, out int packetLen)
        {
            if (buffer == null || bufferLen <= 0)
            {
                packetID = 0xFF;
                packetLen = 0;
                packetOffset = 0;

                return false;
            }

            packetLen = PacketsTable.GetPacketLength(packetID = buffer[0]);
            packetOffset = 1;

            if (packetLen == -1)
            {
                if (bufferLen < 3)
                {
                    return false;
                }

                var b0 = buffer[1];
                var b1 = buffer[2];

                packetLen = b1 | (b0 << 8);
                packetOffset = 3;

                if (packetLen <= 0)
                {

                }
            }

            return true;
        }

        private void OnDataReceived(object sender, ArraySegment<byte> e)
        {
            lock (_recvCompressedStream)
            {
                lock (_recvUncompressedUnfinishedStream)
                {
                    if (_recvUncompressedUnfinishedStream.Length != 0)
                    {
                        Span<byte> tmp = stackalloc byte[4096];
                        var max = _recvUncompressedUnfinishedStream.Length;
                        var done = 0;
                        while (done < max)
                        {
                            var toRead = Math.Min(tmp.Length, max);

                            _recvUncompressedUnfinishedStream.Dequeue(tmp, 0, toRead);
                            _recvCompressedStream.Enqueue(tmp, 0, toRead);

                            done += toRead;
                        }

                        _recvUncompressedUnfinishedStream.Clear();
                    }
                }

                _recvCompressedStream.Enqueue(e.Array, e.Offset, e.Count);
            }
        }

        private readonly byte[] _compressedBuffer = new byte[BUFF_SIZE];
        private readonly byte[] _uncompressedBuffer = new byte[BUFF_SIZE];

        private void ProcessRecv()
        {
            if (IsDisposed)
            {
                return;
            }

            if (!IsConnected && !IsDisposed)
            {
                //Disconnect();

                return;
            }

            try
            {
                lock (_recvCompressedStream)
                {
                    _ = _socket.Read(_recvCompressedStream);

                    var size = _recvCompressedStream.Length;

                    if (size <= 0)
                    {
                        return;
                    }

                    Statistics.TotalBytesReceived += (uint)size;

                    _ = _recvCompressedStream.Dequeue(_compressedBuffer, 0, size);

                    ProcessEncryption(_compressedBuffer.AsSpan(0, size));

                    var uncompressedData = DecompressBuffer(_compressedBuffer.AsSpan(0, size));

                    lock (_recvUncompressedStream)
                        _recvUncompressedStream.Enqueue(uncompressedData);
                }
            }
            catch (SocketException ex)
            {
                Log.Error("socket error when receving:\n" + ex);
                _logFile?.Write($"disconnection  -  error when reading to the socket buffer: {ex}");

                // Disconnect(ex.SocketErrorCode);
            }
            catch (Exception ex)
            {
                if (ex.InnerException is SocketException socketEx)
                {
                    Log.Error("main exception:\n" + ex);
                    Log.Error("socket error when receving:\n" + socketEx);

                    _logFile?.Write($"disconnection  -  error when reading to the socket buffer [2]: {socketEx}");
                    // Disconnect(socketEx.SocketErrorCode);
                }
                else
                {
                    Log.Error("fatal error when receving:\n" + ex);

                    _logFile?.Write($"disconnection  -  error when reading to the socket buffer [3]: {ex}");

                    Disconnect();

                    throw;
                }
            }
        }

        private void ProcessEncryption(Span<byte> buffer)
        {
            if (_isLoginSocket) return;

            EncryptionHelper.Decrypt(buffer, buffer, buffer.Length);
        }

        private void ProcessSend()
        {
            if (IsDisposed)
            {
                return;
            }

            if (!IsConnected && !IsDisposed)
            {
                //Disconnect();

                return;
            }

            var length = _sendStream.Length;

            if (length <= 0)
            {
                return;
            }

            try
            {
                var read = _sendStream.DequeSegment(length, out var segment);
                _socket.Send(segment.Array, segment.Offset, segment.Count);
            }
            catch (SocketException ex)
            {
                Log.Error("socket error when sending:\n" + ex);
                _logFile?.Write($"disconnection  -  error during writing to the socket buffer: {ex}");

                //Disconnect(ex.SocketErrorCode);
            }
            catch (Exception ex)
            {
                if (ex.InnerException is SocketException socketEx)
                {
                    Log.Error("main exception:\n" + ex);
                    Log.Error("socket error when sending:\n" + socketEx);

                    _logFile?.Write($"disconnection  -  error during writing to the socket buffer [2]: {socketEx}");
                    //Disconnect(socketEx.SocketErrorCode);
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
            }
        }

        private readonly Huffman _huffman = new Huffman();

        private Span<byte> DecompressBuffer(Span<byte> buffer)
        {
            if (!_isCompressionEnabled) 
                return buffer;

            var size = /*buffer.Length * 4 + 2; //*/ 65536;
            _huffman.Decompress(buffer, _uncompressedBuffer.AsSpan(0, buffer.Length * 4 + 2), ref size);

            return _uncompressedBuffer.AsSpan(0, size);
            //var incompletelength = _recvCompressedUnfinishedStream.Length;
            //var sourcelength = incompletelength + buffer.Length;
            //var dest = _uncompressedBuffer;

            //if (incompletelength > 0)
            //{
            //    _recvCompressedUnfinishedStream.Dequeue(dest, 0, incompletelength);
            //    _recvCompressedUnfinishedStream.Clear();
            //}

            ////buffer.CopyTo(dest);

            ////Buffer.BlockCopy(buffer, 0, source, incompletelength, length);

            //int processedOffset = 0;
            //int sourceOffset = 0;
            //int offset = 0;

            //while (Huffman.DecompressChunk
            //(
            //    buffer,
            //    ref sourceOffset,
            //    sourcelength,
            //    dest,
            //    offset,
            //    out int outSize
            //))
            //{
            //    processedOffset = sourceOffset;
            //    offset += outSize;
            //}

            //if (processedOffset < sourcelength)
            //{
            //    _recvCompressedUnfinishedStream.Enqueue(dest, processedOffset, sourcelength - processedOffset);
            //}

            //return dest.AsSpan(0, offset);
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