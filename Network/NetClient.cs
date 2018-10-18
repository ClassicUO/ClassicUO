#region license

//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Network
{
    public sealed class NetClient
    {
        private const int BUFF_SIZE = 0x10000;

        private static readonly BufferPool _pool = new BufferPool(10, BUFF_SIZE);
        private readonly object _sendLock = new object();
        private CircularBuffer _circularBuffer;

        private int _incompletePacketLength;
        private bool _isCompressionEnabled, _sending;
        private byte[] _recvBuffer, _incompletePacketBuffer;
        private SocketAsyncEventArgs _sendEventArgs, _recvEventArgs;
        private SendQueue _sendQueue;
        private readonly object _sync = new object();
        private Queue<Packet> _queue = new Queue<Packet>(), _workingQueue = new Queue<Packet>();

        private Socket _socket;

        private NetClient()
        {
        }

        public static NetClient LoginSocket { get; } = new NetClient();
        public static NetClient Socket { get; } = new NetClient();

        public bool IsConnected => _socket != null && _socket.Connected;

        public bool IsDisposed { get; private set; }

        public uint ClientAddress
        {
            get
            {
                IPHostEntry localEntry = Dns.GetHostEntry(Dns.GetHostName());
                uint address;

                if (localEntry.AddressList.Length > 0)
                {
#pragma warning disable 618
                    address = (uint) localEntry.AddressList
                        .FirstOrDefault(s => s.AddressFamily == AddressFamily.InterNetwork).Address;
#pragma warning restore 618
                }
                else
                    address = 0x100007f;

                return ((address & 0xff) << 0x18) | ((address & 65280) << 8) | ((address >> 8) & 65280) |
                       ((address >> 0x18) & 0xff);
            }
        }

        public event EventHandler Connected, Disconnected;
        public static event EventHandler<Packet> PacketReceived, PacketSended;


        public void Connect(string ip, ushort port)
        {
            IsDisposed = _sending = false;

            IPAddress address = ResolveIP(ip);
            IPEndPoint endpoint = new IPEndPoint(address, port);
            Connect(endpoint);
        }

        public void Connect(IPAddress address, ushort port)
        {
            IsDisposed = _sending = false;
            IPEndPoint endpoint = new IPEndPoint(address, port);
            Connect(endpoint);
        }

        private void Connect(IPEndPoint endpoint)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, 1);

            _recvBuffer = _pool.GetFreeSegment();
            _incompletePacketBuffer = _pool.GetFreeSegment();
            _sendQueue = new SendQueue();
            _circularBuffer = new CircularBuffer();

            _sendEventArgs = new SocketAsyncEventArgs();
            _sendEventArgs.Completed += IO_Socket;

            _recvEventArgs = new SocketAsyncEventArgs();
            _recvEventArgs.Completed += IO_Socket;
            _recvEventArgs.SetBuffer(_recvBuffer, 0, _recvBuffer.Length);

            _queue.Clear();
            _workingQueue.Clear();

            SocketAsyncEventArgs connectEventArgs = new SocketAsyncEventArgs();
            connectEventArgs.Completed += (sender, e) =>
            {
                if (e.SocketError == SocketError.Success)
                {
                    Connected.Raise();
                    StartRecv();
                }
                else
                {
                    Log.Message(LogTypes.Error, e.SocketError.ToString());
                    Disconnect();
                }
            };
            connectEventArgs.RemoteEndPoint = endpoint;
            _socket.ConnectAsync(connectEventArgs);
        }

        public void Disconnect()
        {
            if (IsDisposed)
                return;
            IsDisposed = true;

            if (_socket == null)
                return;


            Flush();

            try
            {
                _socket.Shutdown(SocketShutdown.Both);
            }
            catch
            {
            }

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
            
            Disconnected.Raise();
        }

        public void EnableCompression()
        {
            _isCompressionEnabled = true;
        }

        public void Send(PacketWriter p)
        {
            byte[] data = p.ToArray();
            Packet packet = new Packet(data, p.Length);
            PacketSended.Raise(packet);

            if (!packet.Filter) Send(data);
        }


        public void Update()
        {
            //if (!IsConnected)
            //    return;

            lock (_sync)
            {
                Queue<Packet> temp = _workingQueue;
                _workingQueue = _queue;
                _queue = temp;
            }

            while (_queue.Count > 0)
            {
                PacketReceived.Raise(_queue.Dequeue());
            }

            Flush();            
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

                    byte[] data = /*BUFF_SIZE >= packetlength ? _pool.GetFreeSegment() : */new byte[packetlength];
                    packetlength = _circularBuffer.Dequeue(data, 0, packetlength);

                    
                    //PacketReceived?.Invoke(null, packet);

                    //_packetsToRead.Enqueue(packet);

                    lock (_sync)
                    {
                        Packet packet = new Packet(data, packetlength);
                        _workingQueue.Enqueue(packet);
                    }

                    length = _circularBuffer.Length;

                    //if (BUFF_SIZE >= packetlength) _pool.AddFreeSegment(data);
                }
            }
        }


        private void Send(byte[] data)
        {
            if (_socket == null) return;

            if (data != null)
            {
                if (data.Length <= 0) return;

                try
                {
                    lock (_sendLock)
                    {
                        SendQueue.Gram gram;
                        lock (_sendQueue) gram = _sendQueue.Enqueue(data, 0, data.Length);

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
                Disconnect();
        }

        private void IO_Socket(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessRecv(e);
                    if (!IsDisposed) StartRecv();

                    break;

                case SocketAsyncOperation.Send:
                    ProcessSend(e);

                    if (IsDisposed) return;

                    SendQueue.Gram gram;

                    lock (_sendQueue)
                    {
                        gram = _sendQueue.Dequeue();
                        if (gram == null && _sendQueue.IsFlushReady) gram = _sendQueue.CheckFlushReady();
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
            try
            {
                bool ok = false;
                do
                {
                    ok = !_socket.ReceiveAsync(_recvEventArgs);
                    if (ok)
                        ProcessRecv(_recvEventArgs);
                } while (ok);
            }
            catch (Exception e)
            {
                Disconnect();
            }
        }

        private void ProcessRecv(SocketAsyncEventArgs e)
        {
            int bytesLen = e.BytesTransferred;

            if (bytesLen > 0 && e.SocketError == SocketError.Success)
            {
                byte[] buffer = _recvBuffer;

                if (_isCompressionEnabled) DecompressBuffer(ref buffer, ref bytesLen);

                lock (_circularBuffer) _circularBuffer.Enqueue(buffer, 0, bytesLen);

                ExtractPackets();
            }
            else
                Disconnect();
        }

        private void DecompressBuffer(ref byte[] buffer, ref int length)
        {
            byte[] source = _pool.GetFreeSegment();
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

            while (Huffman.DecompressChunk(ref source, ref sourceOffset, sourcelength, ref buffer, offset,
                out int outSize))
            {
                processedOffset = sourceOffset;
                offset += outSize;
            }

            length = offset;

            if (processedOffset >= sourcelength)
                _pool.AddFreeSegment(source);
            else
            {
                int l = sourcelength - processedOffset;
                Buffer.BlockCopy(source, processedOffset, _incompletePacketBuffer, _incompletePacketLength, l);
                _incompletePacketLength += l;
            }
        }

        private void StartSend()
        {
            if (!_socket.SendAsync(_sendEventArgs)) IO_Socket(null, _sendEventArgs);
        }

        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
            }
            else
                Disconnect();
        }

        private void Flush()
        {
            if (_socket == null) return;

            lock (_sendLock)
            {
                if (_sending) return;

                SendQueue.Gram gram;

                lock (_sendQueue)
                {
                    if (!_sendQueue.IsFlushReady) return;

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