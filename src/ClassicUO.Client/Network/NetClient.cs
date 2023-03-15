﻿#region license

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
using System.Net;
using System.Net.Sockets;

namespace ClassicUO.Network
{
    sealed class SocketWrapper : IDisposable
    {
        private Socket _socket;

        public bool IsConnected => _socket?.Connected ?? false;

        public EndPoint LocalEndPoint => _socket?.LocalEndPoint;


        public event EventHandler OnConnected, OnDisconnected;
        public event EventHandler<SocketError> OnError;
        public event EventHandler<ArraySegment<byte>> OnDataReceived;


        public void Connect(string ip, int port)
        {
            if (IsConnected) return;

            Console.WriteLine("conneting to: {0},{1}", ip, port);

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true
            };

            try
            {
                _socket.Connect(ip, port);

                if (!IsConnected)
                {
                    OnError?.Invoke(this, SocketError.NotConnected);
                    return;
                }

                OnConnected?.Invoke(this, EventArgs.Empty);
            }
            catch (SocketException socketEx)
            {
                Log.Error($"error while connecting {socketEx}");
                OnError?.Invoke(this, socketEx.SocketErrorCode);
            }
            catch (Exception ex)
            {
                Log.Error($"error while connecting {ex}");
                OnError?.Invoke(this, SocketError.SocketError);
            }
           

            //_socket.BeginConnect(ip, port, e =>
            //{
            //    _socket.EndConnect(e);

            //    if (!IsConnected)
            //    {
            //        OnError?.Invoke(this, SocketError.NotConnected);
            //        return;
            //    }

            //    OnConnected?.Invoke(this, EventArgs.Empty);
            //    //BeginRead(new byte[1024 * 4]);
            //}, this);
        }

        public void Send(byte[] buffer, int offset, int count)
        {
            var sent = _socket.Send(buffer, offset, count, SocketFlags.None, out var errorCode);

            if (sent <= 0 || errorCode != SocketError.Success)
            {

            }
        }

        public int Read(byte[] buffer, int length = 4096)
        {
            if (!IsConnected) return 0;

            var available = Math.Min(length, _socket.Available);
            var done = 0;

            while (done < available)
            {
                var toRead = Math.Min(length, available - done);
                var read = _socket.Receive(buffer, done, toRead, SocketFlags.None, out var errorCode);

                if (read <= 0 || errorCode != SocketError.Success)
                {
                    OnDisconnected?.Invoke(this, EventArgs.Empty);

                    if (errorCode != SocketError.Success)
                    {
                        OnError?.Invoke(this, errorCode);
                    }

                    _socket.Close();
                    _socket.Dispose();

                    return 0;
                }

                done += read;
            }

            return done;
        }

        public void Disconnect()
        {
            _socket?.Close();
            Dispose();
        }

        public void Dispose()
        {
            _socket?.Dispose();
        }

        private void EndRead(IAsyncResult asyncResult)
        {
            if (!IsConnected)
                return;

            var buffer = (byte[])asyncResult.AsyncState;

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

            OnDataReceived?.Invoke(this, new ArraySegment<byte>(buffer, 0, read));

            BeginRead(buffer);
        }

        private void BeginRead(byte[] buffer)
            => _socket?.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, EndRead, buffer);
    }

    internal sealed class NetClient
    {
        private const int BUFF_SIZE = 0x10000;

        private static LogFile _logFile;

        private readonly byte[] _compressedBuffer = new byte[BUFF_SIZE];
        private readonly byte[] _uncompressedBuffer = new byte[BUFF_SIZE];
        private readonly Huffman _huffman = new Huffman();
        private bool _isCompressionEnabled;
        private readonly SocketWrapper _socket;
        private uint? _localIP;
        private readonly CircularBuffer _sendStream;

        public NetClient()
        {
            Statistics = new NetStatistics(this);
            _sendStream = new CircularBuffer();

            _socket = new SocketWrapper();
            _socket.OnConnected += (o, e) => { Statistics.Reset(); Connected?.Invoke(this, EventArgs.Empty); };
            _socket.OnDisconnected += (o, e) => Disconnected?.Invoke(this, SocketError.Success);
            _socket.OnError += (o, e) => Disconnected?.Invoke(this, SocketError.SocketError);
        }


        public static NetClient Socket { get; set; } = new NetClient();
       

        public bool IsConnected => _socket != null && _socket.IsConnected;
            
        public NetStatistics Statistics { get; }

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


        public event EventHandler Connected;
        public event EventHandler<SocketError> Disconnected;



        public void Connect(string ip, ushort port)
        {
            _sendStream.Clear();
            _huffman.Reset();
            Statistics.Reset();

            _socket.Connect(ip, port);
        }


        public void Disconnect()
        {
            _isCompressionEnabled = false;
            Statistics.Reset();
            _socket.Disconnect();
        }

        public void EnableCompression()
        {
            _isCompressionEnabled = true;
            _huffman.Reset();
            _sendStream.Clear();
        }

        public Span<byte> GetNetworkData()
        {
            return ProcessRecv();
        }

        public void Flush()
        {
            ProcessSend();
            Statistics.Update();
        }

        public void Send(byte[] data, int length, bool ignorePlugin = false, bool skipEncryption = false)
        {
            if (!ignorePlugin && !Plugin.ProcessSendPacket(data, ref length))
            {
                return;
            }

            Send(data, length, skipEncryption);
        }

        private void Send(byte[] data, int length, bool skipEncryption)
        {
            if (!IsConnected)
            {
                return;
            }

            if (data != null && data.Length != 0 && length > 0)
            {
                if (CUOEnviroment.PacketLog)
                {
                    LogPacket(data, length, true);
                }

                if (!skipEncryption)
                {
                    EncryptionHelper.Encrypt(!_isCompressionEnabled, data, data, length);
                }

                //_socket.Send(data, 0, length);
                _sendStream.Enqueue(data, 0, length);

                Statistics.TotalBytesSent += (uint)length;
                Statistics.TotalPacketsSent++;
            }
        }

        private Span<byte> ProcessRecv()
        {
            if (!IsConnected) 
                return Span<byte>.Empty;

            try
            {
                var size = _socket.Read(_compressedBuffer);

                if (size <= 0)
                {
                    return Span<byte>.Empty;
                }

                Statistics.TotalBytesReceived += (uint)size;

                var span = _compressedBuffer.AsSpan(0, size);

                ProcessEncryption(span);

                return DecompressBuffer(span);
            }
            catch (SocketException ex)
            {
                Log.Error("socket error when receving:\n" + ex);
                _logFile?.Write($"disconnection  -  error when reading to the socket buffer: {ex}");
                
                Disconnect();
                Disconnected?.Invoke(this, ex.SocketErrorCode);
            }
            catch (Exception ex)
            {
                if (ex.InnerException is SocketException socketEx)
                {
                    Log.Error("main exception:\n" + ex);
                    Log.Error("socket error when receving:\n" + socketEx);

                    _logFile?.Write($"disconnection  -  error when reading to the socket buffer [2]: {socketEx}");
                   
                    Disconnect();
                    Disconnected?.Invoke(this, socketEx.SocketErrorCode);
                }
                else
                {
                    Log.Error("fatal error when receving:\n" + ex);

                    _logFile?.Write($"disconnection  -  error when reading to the socket buffer [3]: {ex}");

                    Disconnect();
                    Disconnected?.Invoke(this, SocketError.SocketError);

                    throw;
                }
            }

            return Span<byte>.Empty;
        }

        private void ProcessEncryption(Span<byte> buffer)
        {
            if (!_isCompressionEnabled) return;

            EncryptionHelper.Decrypt(buffer, buffer, buffer.Length);
        }

        private readonly byte[] _sendingBuffer = new byte[4096];

        private void ProcessSend()
        {
            if (!IsConnected) return;

            try
            {
                while (_sendStream.Length > 0)
                {
                    var read = _sendStream.Dequeue(_sendingBuffer, 0, _sendingBuffer.Length);
                    if(read <= 0)
                    {
                        break;
                    }

                    LogPacket(_sendingBuffer, read, true, "SENDING");

                    _socket.Send(_sendingBuffer, 0, read);
                } 
            }
            catch (SocketException ex)
            {
                Log.Error("socket error when sending:\n" + ex);
                _logFile?.Write($"disconnection  -  error during writing to the socket buffer: {ex}");

                Disconnect();
                Disconnected?.Invoke(this, ex.SocketErrorCode);
            }
            catch (Exception ex)
            {
                if (ex.InnerException is SocketException socketEx)
                {
                    Log.Error("main exception:\n" + ex);
                    Log.Error("socket error when sending:\n" + socketEx);

                    _logFile?.Write($"disconnection  -  error during writing to the socket buffer [2]: {socketEx}");
                   
                    Disconnect();
                    Disconnected?.Invoke(this, socketEx.SocketErrorCode);
                }
                else
                {
                    Log.Error("fatal error when sending:\n" + ex);

                    _logFile?.Write($"disconnection  -  error during writing to the socket buffer [3]: {ex}");

                    Disconnect();
                    Disconnected?.Invoke(this, SocketError.SocketError);

                    throw;
                }
            }
        }

        private Span<byte> DecompressBuffer(Span<byte> buffer)
        {
            if (!_isCompressionEnabled)
                return buffer;

            var size = 65536;
            if (!_huffman.Decompress(buffer, _uncompressedBuffer, ref size))
            {
                Disconnect();
                Disconnected?.Invoke(this, SocketError.SocketError);

                return Span<byte>.Empty;
            }

            return _uncompressedBuffer.AsSpan(0, size);
        }

        
        private static void LogPacket(Span<byte> buffer, int length, bool toServer, string t = "")
        {
            if (_logFile == null)
                _logFile = new LogFile(FileSystemHelper.CreateFolderIfNotExists(CUOEnviroment.ExecutablePath, "Logs", "Network"), "packets.log");

            Span<char> span = stackalloc char[256];
            ValueStringBuilder output = new ValueStringBuilder(span);
            {
                int off = sizeof(ulong) + 2;

                output.Append(' ', off);
                output.Append(string.Format("Ticks: {0} | {1} |  ID: {2:X2}   Length: {3}   {4}\n", Time.Ticks, (toServer ? "Client -> Server" : "Server -> Client"), buffer[0], length, t));

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