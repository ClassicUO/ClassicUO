using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace ClassicUO.Network
{
    public sealed class NetClient
    {
        public static NetClient Socket { get; } = new NetClient();

        public static event EventHandler Connected, Disconnected;
        public static event EventHandler<Packet> PacketReceived, PacketSended;

        const int BUFF_SIZE = 0x10000;

        private static readonly BufferPool _pool = new BufferPool(10, BUFF_SIZE);

        private Socket _socket;
        private SocketAsyncEventArgs _sendEventArgs, _recvEventArgs;
        private byte[] _recvBuffer, _incompletePacketBuffer;
        private bool _isDisposing, _isCompressionEnabled, _sending;
        private SendQueue _sendQueue;
        private readonly object _sendLock = new object();
        private CircularBuffer _circularBuffer;

        private int _incompletePacketLength;

        private NetClient()
        {

        }

        public bool IsConnected => _socket != null && _socket.Connected;



        public void Connect(in string ip, in ushort port)
        {
            _isDisposing = _sending = false;

            IPAddress address = ResolveIP(ip);
            IPEndPoint endpoint = new IPEndPoint(address, port);

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //_socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.Debug, 1);

            _recvBuffer = _pool.GetFreeSegment();
            _incompletePacketBuffer = _pool.GetFreeSegment();
            _sendQueue = new SendQueue();
            _circularBuffer = new CircularBuffer();

            _sendEventArgs = new SocketAsyncEventArgs();
            _sendEventArgs.Completed += IO_Socket;

            _recvEventArgs = new SocketAsyncEventArgs();
            _recvEventArgs.Completed += IO_Socket;
            _recvEventArgs.SetBuffer(_recvBuffer, 0, _recvBuffer.Length);


            SocketAsyncEventArgs connectEventArgs = new SocketAsyncEventArgs();
            connectEventArgs.Completed += (sender, e) =>
            {
                if (e.SocketError == SocketError.Success)
                {
                    Connected?.Invoke(null, EventArgs.Empty);
                    StartRecv();
                }
            };
            connectEventArgs.RemoteEndPoint = endpoint;
            _socket.ConnectAsync(connectEventArgs);
        }

        public void Disconnect()
        {
            if (_isDisposing)
                return;
            _isDisposing = true;

            Flush();

            try
            {
                _socket.Shutdown(SocketShutdown.Both);
            }
            catch { }
            _socket.Close();

            if (_recvBuffer != null)
            {
                lock (_pool)
                    _pool.AddFreeSegment(_recvBuffer);
            }
            if (_incompletePacketBuffer != null)
            {
                lock (_pool)
                    _pool.AddFreeSegment(_incompletePacketBuffer);
            }
            _incompletePacketBuffer = null;
            _incompletePacketLength = 0;
            _recvBuffer = null;
            _isCompressionEnabled = false;
            _socket = null;
            _recvEventArgs = null;
            _sendEventArgs = null;

            lock (_sendQueue)
            {
                if (!_sendQueue.IsEmpty)
                    _sendQueue.Clear();
            }

            _circularBuffer = null;

            Disconnected?.Invoke(null, EventArgs.Empty);
        }

        public void EnableCompression() => _isCompressionEnabled = true;

        public void Send(PacketWriter p)
        {
            byte[] data = p.ToArray();
            Packet packet = new Packet(data, p.Length);
            PacketSended?.Invoke(null, packet);

            if (!packet.Filter)
                Send(packet.ToArray());
        }


        public void Slice()
        {
            if (IsConnected)
            {
                ExtractPackets();
                Flush();
            }
        }

        private void ExtractPackets()
        {
            if (!IsConnected || _circularBuffer == null || _circularBuffer.Length <= 0)
                return;

            lock (_circularBuffer)
            {
                int length = _circularBuffer.Length;

                while (length > 0 && IsConnected)
                {
                    byte id = _circularBuffer.GetID();
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

                    byte[] data = BUFF_SIZE >= packetlength ? _pool.GetFreeSegment() : new byte[packetlength];
                    packetlength = _circularBuffer.Dequeue(data, 0, packetlength);

                    Packet packet = new Packet(data, packetlength);
                    PacketReceived?.Invoke(null, packet);

                    length = _circularBuffer.Length;

                    if (BUFF_SIZE >= packetlength)
                        _pool.AddFreeSegment(data);
                }
            }
        }

        private void Send(in byte[] data)
        {
            if(_socket == null)
                return;

            if (data != null)
            {
                if (data.Length <= 0)
                    return;

                try
                {
                    SendQueue.Gram gram;

                    lock (_sendLock)
                    {
                        lock (_sendQueue)
                            gram = _sendQueue.Enqueue(data, 0, data.Length);

                        if (gram != null && !_sending)
                        {
                            _sending = true;
                            _sendEventArgs.SetBuffer(gram.Buffer, 0, gram.Length);
                            StartSend();
                        }
                    }
                }
                catch (CapacityExceededException)
                {
                    Disconnect();
                }
            }
            else
            {
                Disconnect();
            }
        }

        private void IO_Socket(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessRecv(e);
                    if (!_isDisposing)
                        StartRecv();
                    break;

                case SocketAsyncOperation.Send:
                    ProcessSend(e);

                    if (_isDisposing)
                        return;

                    SendQueue.Gram gram;

                    lock (_sendQueue)
                    {
                        gram = _sendQueue.Dequeue();
                        if (gram == null && _sendQueue.IsFlushReady)
                            gram = _sendQueue.CheckFlushReady();
                    }

                    if (gram != null)
                    {
                        _sendEventArgs.SetBuffer(gram.Buffer, 0, gram.Length);
                        StartSend();
                    }
                    else
                    {
                        lock (_sendLock)
                            _sending = false;
                    }

                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
        }

        private void StartRecv()
        {
            if (!_socket.ReceiveAsync(_recvEventArgs))
                IO_Socket(null, _recvEventArgs);
        }

        private void ProcessRecv(in SocketAsyncEventArgs e)
        {
            int bytesLen = e.BytesTransferred;

            if (bytesLen > 0 && e.SocketError == SocketError.Success)
            {
                byte[] buffer = _recvBuffer;

                if (_isCompressionEnabled)
                {
                    byte[] source = _pool.GetFreeSegment();
                    int incompletelength = _incompletePacketLength;
                    int sourcelength = incompletelength + bytesLen;

                    if (incompletelength > 0)
                    {
                        Buffer.BlockCopy(_incompletePacketBuffer, 0, source, 0, _incompletePacketLength);
                        _incompletePacketLength = 0;
                    }

                    // if outbounds exception, BUFF_SIZE must be increased
                    Buffer.BlockCopy(buffer, 0, source, incompletelength, bytesLen);                

                    int processedOffset = 0;
                    int sourceOffset = 0;
                    int offset = 0;

                    while (Huffman.DecompressChunk(ref source, ref sourceOffset, sourcelength, ref buffer, offset, out int outSize))
                    {
                        processedOffset = sourceOffset;
                        offset += outSize;
                    }

                    bytesLen = offset;

                    if (processedOffset >= sourcelength)
                    {
                        _pool.AddFreeSegment(source);
                    }
                    else
                    {
                        int l = sourcelength - processedOffset;
                        Buffer.BlockCopy(source, processedOffset, _incompletePacketBuffer, _incompletePacketLength, l);
                        _incompletePacketLength += l;
                    }
                }

                lock (_circularBuffer)
                    _circularBuffer.Enqueue(buffer, 0, bytesLen);
            }
            else
                Disconnect();
        }

        private void StartSend()
        {
            if (!_socket.SendAsync(_sendEventArgs))
                IO_Socket(null, _sendEventArgs);
        }

        private void ProcessSend(in SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                
            }
            else
                Disconnect();
        }

        private void Flush()
        {
            if (_socket == null)
                return;

            lock (_sendLock)
            {
                if (_sending)
                    return;

                SendQueue.Gram gram;

                lock (_sendQueue)
                {
                    if (!_sendQueue.IsFlushReady)
                        return;
                    gram = _sendQueue.CheckFlushReady();
                }

                if (gram != null)
                {
                    _sending = true;
                    _sendEventArgs.SetBuffer(gram.Buffer, 0, gram.Length);
                    StartSend();
                }
            }
        }


        private IPAddress ResolveIP(in string addr)
        {
            IPAddress result = IPAddress.None;
            if (string.IsNullOrEmpty(addr))
                return result;

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
