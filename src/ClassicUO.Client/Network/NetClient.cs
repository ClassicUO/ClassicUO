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
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ClassicUO.Network
{
    sealed class SocketWrapper : IDisposable
    {
        private TcpClient _socket;

        public bool IsConnected => _socket?.Client?.Connected ?? false;

        public EndPoint LocalEndPoint => _socket?.Client?.LocalEndPoint;


        public event EventHandler OnConnected, OnDisconnected;
        public event EventHandler<SocketError> OnError;


        private const int ConnectTimeoutMs = 10000;

        public void Connect(string ip, int port)
        {
            if (IsConnected) return;

            _socket = new TcpClient();
            _socket.NoDelay = true;

            try
            {
                var result = _socket.BeginConnect(ip, port, null, null);
                if (!result.AsyncWaitHandle.WaitOne(ConnectTimeoutMs))
                {
                    Disconnect();
                    Log.Error($"connection timeout to {ip}:{port}");
                    OnError?.Invoke(this, SocketError.TimedOut);
                    return;
                }

                _socket.EndConnect(result);

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
        }

        public void Send(byte[] buffer, int offset, int count)
        {
            var stream = _socket.GetStream();
            stream.Write(buffer, offset, count);
            stream.Flush();
        }

        public int Read(byte[] buffer)
        {
            if (!IsConnected) return 0;

            var available = _socket.Available;
            if (available == 0)
                return -1;

            available = Math.Min(buffer.Length, available);
            var done = 0;
            var stream = _socket.GetStream();

            while (done < available)
            {
                var toRead = Math.Min(buffer.Length, available - done);
                var read = stream.Read(buffer, done, toRead);

                if (read <= 0)
                {
                    OnDisconnected?.Invoke(this, EventArgs.Empty);
                    Disconnect();
                    return 0;
                }

                done += read;
            }

            return done;
        }

        public void Disconnect()
        {
            var s = _socket;
            _socket = null;
            if (s == null) return;
            try
            {
                if (s.Client?.Connected == true)
                    s.Close();
            }
            catch { }
            try { s.Dispose(); } catch { }
        }

        public void Dispose()
        {
            Disconnect();
        }
    }

    internal sealed class NetClient
    {
        private const int BUFF_SIZE = 0x10000;
        private const int RECEIVE_DRAIN_SIZE = 32768;
        private const int RECEIVE_BACKPRESSURE_HIGH_WATERMARK_BYTES = 1024 * 1024;
        private const int RECEIVE_BACKPRESSURE_LOW_WATERMARK_BYTES = 256 * 1024;

        private readonly byte[] _compressedBuffer = new byte[4096];
        private readonly byte[] _uncompressedBuffer = new byte[BUFF_SIZE];
        private readonly byte[] _sendingBuffer = new byte[4096];
        private readonly byte[] _receiveDrainBuffer = new byte[RECEIVE_DRAIN_SIZE];
        private readonly byte[] _networkReadBuffer = new byte[4096];
        private readonly Huffman _huffman = new Huffman();
        private bool _isCompressionEnabled;
        private readonly SocketWrapper _socket;
        private uint? _localIP;
        private readonly CircularBuffer _sendStream;
        private readonly ConcurrentQueue<byte[]> _receiveQueue = new ConcurrentQueue<byte[]>();
        private long _queuedReceiveBytes;
        private int _queuedReceiveMessages;
        private volatile bool _receiveBackpressure;
        private Thread _networkThread;
        private volatile bool _networkRunning;
        private volatile bool _pendingDisconnect;
        private SocketError _pendingDisconnectError;

        public NetClient()
        {
            Statistics = new NetStatistics(this);
            _sendStream = new CircularBuffer();

            _socket = new SocketWrapper();
            _socket.OnConnected += (o, e) =>
            {
                Statistics.Reset();
                _pendingDisconnect = false;
                _networkRunning = true;
                _networkThread = new Thread(NetworkLoop) { IsBackground = true };
                _networkThread.Start();
                Connected?.Invoke(this, EventArgs.Empty);
            };
            _socket.OnDisconnected += (o, e) => RaiseDisconnectedOnGameThread(SocketError.Success);
            _socket.OnError += (o, e) => RaiseDisconnectedOnGameThread(e);
        }

        private void RaiseDisconnectedOnGameThread(SocketError e)
        {
            _pendingDisconnect = true;
            _pendingDisconnectError = e;
        }

        private void NetworkLoop()
        {
            while (_networkRunning && _socket.IsConnected)
            {
                try
                {
                    long queuedBytes = Interlocked.Read(ref _queuedReceiveBytes);

                    if (_receiveBackpressure)
                    {
                        if (queuedBytes <= RECEIVE_BACKPRESSURE_LOW_WATERMARK_BYTES)
                        {
                            _receiveBackpressure = false;
                        }
                        else
                        {
                            ProcessSendFromQueue();
                            Thread.Sleep(1);
                            continue;
                        }
                    }
                    else if (queuedBytes >= RECEIVE_BACKPRESSURE_HIGH_WATERMARK_BYTES)
                    {
                        _receiveBackpressure = true;
                        ProcessSendFromQueue();
                        Thread.Sleep(1);
                        continue;
                    }

                    int n = _socket.Read(_networkReadBuffer);
                    if (n > 0)
                    {
                        Statistics.TotalBytesReceived += (uint)n;
                        var copy = ArrayPool<byte>.Shared.Rent(n);
                        Array.Copy(_networkReadBuffer, copy, n);

                        Span<byte> span = copy.AsSpan(0, n);
                        ProcessEncryption(span);
                        Span<byte> decompressed = DecompressBuffer(span);

                        ArrayPool<byte>.Shared.Return(copy);

                        if (!decompressed.IsEmpty)
                        {
                            byte[] message = new byte[decompressed.Length];
                            decompressed.CopyTo(message.AsSpan());
                            _receiveQueue.Enqueue(message);
                            Interlocked.Add(ref _queuedReceiveBytes, message.Length);
                            Interlocked.Increment(ref _queuedReceiveMessages);
                        }
                    }
                    else if (n == 0)
                    {
                        break;
                    }
                    else
                    {
                        Thread.SpinWait(100);
                    }
                }
                catch (SocketException ex)
                {
                    Log.Error("socket error in network thread:\n" + ex);
                    RaiseDisconnectedOnGameThread(ex.SocketErrorCode);
                    break;
                }
                catch (Exception ex)
                {
                    if (ex.InnerException is SocketException socketEx)
                    {
                        Log.Error("socket error in network thread:\n" + socketEx);
                        RaiseDisconnectedOnGameThread(socketEx.SocketErrorCode);
                    }
                    else
                    {
                        Log.Error("network thread error:\n" + ex);
                        RaiseDisconnectedOnGameThread(SocketError.SocketError);
                    }
                    break;
                }

                ProcessSendFromQueue();
                if (_receiveQueue.IsEmpty && _sendStream.Length == 0)
                    Thread.SpinWait(50);
            }
            _networkRunning = false;
        }

        private void ProcessSendFromQueue()
        {
            if (!_socket.IsConnected) return;
            try
            {
                while (true)
                {
                    int read;
                    lock (_sendStream)
                    {
                        read = _sendStream.Dequeue(_sendingBuffer, 0, _sendingBuffer.Length);
                    }
                    if (read <= 0) break;
                    _socket.Send(_sendingBuffer, 0, read);
                }
            }
            catch (SocketException ex)
            {
                Log.Error("socket error when sending (network thread):\n" + ex);
                RaiseDisconnectedOnGameThread(ex.SocketErrorCode);
            }
            catch (Exception ex)
            {
                if (ex.InnerException is SocketException socketEx)
                {
                    Log.Error("socket error when sending:\n" + socketEx);
                    RaiseDisconnectedOnGameThread(socketEx.SocketErrorCode);
                }
                else
                {
                    Log.Error("send error in network thread:\n" + ex);
                    RaiseDisconnectedOnGameThread(SocketError.SocketError);
                }
            }
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
    public long QueuedReceiveBytes => Interlocked.Read(ref _queuedReceiveBytes);
    public int QueuedReceiveMessages => Volatile.Read(ref _queuedReceiveMessages);



        public void Connect(string ip, ushort port)
        {
            _sendStream.Clear();
            ClearReceiveQueue();
            _huffman.Reset();
            Statistics.Reset();
            _pendingDisconnect = false;
            _receiveBackpressure = false;

            _socket.Connect(ip, port);
        }

        public void Disconnect()
        {
            _networkRunning = false;
            _isCompressionEnabled = false;
            Statistics.Reset();
            ClearReceiveQueue();
            _socket.Disconnect();
        }

        private void ClearReceiveQueue()
        {
            while (_receiveQueue.TryDequeue(out _)) { }
            Interlocked.Exchange(ref _queuedReceiveBytes, 0);
            Interlocked.Exchange(ref _queuedReceiveMessages, 0);
        }

        public void EnableCompression()
        {
            _isCompressionEnabled = true;
            _huffman.Reset();
            _sendStream.Clear();
        }

        public Span<byte> CollectAvailableData()
        {
            if (_pendingDisconnect)
            {
                _pendingDisconnect = false;
                Disconnect();
                Disconnected?.Invoke(this, _pendingDisconnectError);
                return Span<byte>.Empty;
            }

            int totalLen = 0;
            var overflow = default(System.Collections.Generic.List<byte[]>);

            while (_receiveQueue.TryDequeue(out byte[] chunk))
            {
                Interlocked.Add(ref _queuedReceiveBytes, -chunk.Length);
                Interlocked.Decrement(ref _queuedReceiveMessages);

                if (totalLen + chunk.Length <= _receiveDrainBuffer.Length)
                {
                    Array.Copy(chunk, 0, _receiveDrainBuffer, totalLen, chunk.Length);
                    totalLen += chunk.Length;
                }
                else
                {
                    (overflow ??= new System.Collections.Generic.List<byte[]>()).Add(chunk);
                }
            }

            if (overflow != null)
            {
                foreach (var c in overflow)
                {
                    _receiveQueue.Enqueue(c);
                    Interlocked.Add(ref _queuedReceiveBytes, c.Length);
                    Interlocked.Increment(ref _queuedReceiveMessages);
                }
            }

            if (totalLen <= 0)
                return Span<byte>.Empty;

            return _receiveDrainBuffer.AsSpan(0, totalLen);
        }

        public bool TryDequeuePacket(out byte[] packet)
        {
            packet = null;

            if (_pendingDisconnect)
            {
                _pendingDisconnect = false;
                Disconnect();
                Disconnected?.Invoke(this, _pendingDisconnectError);
                return false;
            }

            if (!_receiveQueue.TryDequeue(out packet))
            {
                return false;
            }

            Interlocked.Add(ref _queuedReceiveBytes, -packet.Length);
            Interlocked.Decrement(ref _queuedReceiveMessages);

            return true;
        }

        public void Flush()
        {
            if (_pendingDisconnect)
            {
                _pendingDisconnect = false;
                Disconnect();
                Disconnected?.Invoke(this, _pendingDisconnectError);
                return;
            }
            Statistics.Update();
        }

        public void Send(Span<byte> message, bool ignorePlugin = false, bool skipEncryption = false)
        {
            if (!IsConnected || message.IsEmpty)
            {
                return;
            }

            if (!ignorePlugin && !Plugin.ProcessSendPacket(ref message))
            {
                return;
            }

            if (message.IsEmpty) return;

            PacketLogger.Default?.Log(message, true);

            if (!skipEncryption)
            {
                EncryptionHelper.Encrypt(!_isCompressionEnabled, message, message, message.Length);
            }

            lock (_sendStream)
            {
                //_socket.Send(data, 0, length);
                _sendStream.Enqueue(message);
            }

            Statistics.TotalBytesSent += (uint)message.Length;
            Statistics.TotalPacketsSent++;
        }

        private void ProcessEncryption(Span<byte> buffer)
        {
            if (!_isCompressionEnabled) return;

            EncryptionHelper.Decrypt(buffer, buffer, buffer.Length);
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
    }
}