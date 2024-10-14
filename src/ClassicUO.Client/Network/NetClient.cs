#region license

// Copyright (c) 2024, andreakarasho
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
using ClassicUO.Network.Sockets;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ClassicUO.Network;

#nullable enable

internal sealed class NetClient
{
    private const int BUFF_SIZE = 4096;

    public static NetClient Socket { get; } = new();

    private readonly byte[] _receiveBuffer = new byte[BUFF_SIZE];
    private readonly byte[] _sendBuffer = new byte[BUFF_SIZE];
    private readonly byte[] _decompressionBuffer = new byte[BUFF_SIZE * 3];
    private readonly Pipe _receivePipe = new(BUFF_SIZE * 3);
    private readonly Huffman _huffman = new();

    private bool _isCompressionEnabled;
    private uint? _localIP;
    private NetSocket? _socket;
    private CancellationTokenSource _source;
    private Task? _readLoopTask;
    private int _sendIndex;

    public bool IsConnected { get; private set; }
    public bool IsWebSocket { get; private set; }
    public NetStatistics Statistics { get; }
    public EncryptionHelper? Encryption { get; private set; }
    public PacketsTable? PacketsTable { get; private set; }
    public bool ServerDisconnectionExpected { get; set; }
    public uint LocalIP => GetLocalIP();

    public event EventHandler? Connected;
    public event EventHandler<SocketError>? Disconnected;

    public NetClient()
    {
        Statistics = new NetStatistics(this);
        _source = new();
    }

    public EncryptionType Load(ClientVersion clientVersion, EncryptionType encryption)
    {
        PacketsTable = new PacketsTable(clientVersion);

        if (encryption == 0)
            return encryption;

        Encryption = new EncryptionHelper(clientVersion);
        Log.Trace("Calculating encryption by client version...");
        Log.Trace($"encryption: {Encryption.EncryptionType}");

        if (Encryption.EncryptionType != encryption)
        {
            Log.Warn($"Encryption found: {Encryption.EncryptionType}");
            encryption = Encryption.EncryptionType;
        }

        return encryption;
    }

    public void Connect(string ip, ushort port)
    {
        IsWebSocket = ip.StartsWith("ws", StringComparison.InvariantCultureIgnoreCase);
        string addr = $"{(IsWebSocket ? "" : "tcp://")}{ip}:{port}";

        if (!Uri.TryCreate(addr, UriKind.RelativeOrAbsolute, out Uri? uri))
            throw new UriFormatException($"{nameof(NetClient)}::{nameof(Connect)} invalid Uri {addr}");

        Log.Trace($"Connecting to {uri}");

        ConnectAsyncCore(uri, IsWebSocket).Wait();
    }

    public void Disconnect()
    {
        _isCompressionEnabled = false;
        IsConnected = false;
        Statistics.Reset();
        _source.Cancel();
    }

    public Span<byte> CollectAvailableData()
    {
        return _receivePipe.GetAvailableSpanToRead();
    }

    public void CommitReadData(int size)
    {
        _receivePipe.CommitRead(size);
    }

    public void EnableCompression()
    {
        _isCompressionEnabled = true;
        _huffman.Reset();
    }

    public void DiscardOutgoingPackets()
    {
        _sendIndex = 0;
    }

    public void Flush()
    {
        ProcessSend();
        Statistics.Update();
    }

    public void Send(Span<byte> message, bool ignorePlugin = false, bool skipEncryption = false)
    {
        if (!IsConnected || message.IsEmpty)
            return;

        if (!ignorePlugin && !Plugin.ProcessSendPacket(ref message))
            return;

        if (message.IsEmpty)
            return;

        PacketLogger.Default?.Log(message, true);

        if (!skipEncryption)
            Encryption?.Encrypt(!_isCompressionEnabled, message, message, message.Length);

        lock (_sendBuffer)
        {
            Span<byte> span = _sendBuffer.AsSpan(_sendIndex);
            message.CopyTo(span);
            _sendIndex += message.Length;
        }

        Statistics.TotalBytesSent += (uint)message.Length;
        Statistics.TotalPacketsSent++;
    }

    private void ProcessEncryption(Span<byte> buffer)
    {
        if (!_isCompressionEnabled)
            return;

        Encryption?.Decrypt(buffer, buffer, buffer.Length);
    }

    private void ProcessSend()
    {
        if (!IsConnected)
            return;

        try
        {
            lock (_sendBuffer)
            {
                if (_sendIndex == 0)
                    return;

                Memory<byte> buffer = _sendBuffer.AsMemory(0, _sendIndex);

                int toSend = _sendIndex;

                while (toSend > 0)
                {
                    ValueTask<int> task = _socket!.SendAsync(buffer, CancellationToken.None);

                    if (task.IsCompletedSuccessfully)
                        toSend -= task.Result;
                    else
                        toSend -= task.AsTask().GetAwaiter().GetResult();
                }

                _sendIndex = 0;
            }
        }
        catch (SocketException ex)
        {
            Log.Error("socket error when sending:\n" + ex);

            Disconnect();
            Disconnected?.Invoke(this, ex.SocketErrorCode);
        }
        catch (Exception ex)
        {
            if (ex.InnerException is SocketException socketEx)
            {
                Log.Error("main exception:\n" + ex);
                Log.Error("socket error when sending:\n" + socketEx);

                Disconnect();
                Disconnected?.Invoke(this, socketEx.SocketErrorCode);
            }
            else
            {
                Log.Error("fatal error when sending:\n" + ex);

                Disconnect();
                Disconnected?.Invoke(this, SocketError.SocketError);

                throw;
            }
        }
    }

    private Span<byte> ProcessCompression(Span<byte> buffer)
    {
        if (!_isCompressionEnabled)
            return buffer;

        if (_huffman.Decompress(buffer, _decompressionBuffer, out int size))
            return _decompressionBuffer.AsSpan(..size);

        return [];
    }

    private uint GetLocalIP()
    {
        if (!_localIP.HasValue)
        {
            try
            {
                byte[]? addressBytes = _socket?.LocalEndPoint?.Address.MapToIPv4().GetAddressBytes();

                if (addressBytes is { Length: > 0 })
                    _localIP = (uint)(addressBytes[0] | (addressBytes[1] << 8) | (addressBytes[2] << 16) | (addressBytes[3] << 24));

                if (!_localIP.HasValue || _localIP == 0)
                    _localIP = 0x100007f;
            }
            catch (Exception ex)
            {
                Log.Error($"error while retriving local endpoint address: \n{ex}");

                _localIP = 0x100007f;
            }
        }

        return _localIP.Value;
    }

    private async Task ConnectAsyncCore(Uri uri, bool isWebSocket)
    {
        Task? prevReadLoopTask = _readLoopTask;

        if (prevReadLoopTask is not null)
        {
            _source.Cancel();
            await prevReadLoopTask;

            _sendIndex = 0;
            _huffman.Reset();
            Statistics.Reset();
        }

        ServerDisconnectionExpected = false;

        _source = new();
        _socket = isWebSocket ? new WebSocket() : new TcpSocket();

        CancellationToken token = _source.Token;

        try
        {
            await _socket.ConnectAsync(uri, token);

            IsConnected = true;
            Statistics.Reset();
            Connected?.Invoke(this, EventArgs.Empty);

            _readLoopTask = Task.Run(() => ReadLoop(_socket, token));
        }
        catch
        {
            IsConnected = false;
            Disconnected?.Invoke(this, SocketError.ConnectionReset);
            _socket.Dispose();
        }
    }

    private async Task ReadLoop(NetSocket socket, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                Memory<byte> buffer = _receiveBuffer.AsMemory();

                int bytesRead = await socket.ReceiveAsync(buffer, token);
                if (bytesRead == 0)
                    throw new SocketException((int)SocketError.ConnectionReset);

                Span<byte> span = buffer.Span[..bytesRead];

                Statistics.TotalBytesReceived += (uint)buffer.Length;

                ProcessEncryption(span);
                span = ProcessCompression(span);

                if (span.IsEmpty)
                    throw new Exception("Huffman decompression failed");

                Span<byte> targetSpan = _receivePipe.GetAvailableSpanToWrite();
                if (targetSpan.IsEmpty)
                    throw new Exception("Receive pipe is full");

                if (span.Length <= targetSpan.Length)
                {
                    span.CopyTo(targetSpan);
                    _receivePipe.CommitWrited(span.Length);
                }
                else
                {
                    span[..targetSpan.Length].CopyTo(targetSpan);
                    _receivePipe.CommitWrited(targetSpan.Length);
                    span = span[targetSpan.Length..];

                    targetSpan = _receivePipe.GetAvailableSpanToWrite();
                    if (span.Length > targetSpan.Length)
                        throw new Exception("Receive pipe is full");

                    span.CopyTo(targetSpan);
                    _receivePipe.CommitWrited(span.Length);
                }
            }

            await socket.DisconnectAsync();
        }
        catch (OperationCanceledException)
        {
            Disconnected?.Invoke(this, SocketError.Success);
        }
        catch (SocketException se)
        {
            if (se.SocketErrorCode == SocketError.ConnectionReset && ServerDisconnectionExpected)
                Disconnected?.Invoke(this, SocketError.Success);
            else
                Disconnected?.Invoke(this, se.SocketErrorCode);
        }
        catch
        {
            Disconnected?.Invoke(this, SocketError.Fault);
        }
        finally
        {
            IsConnected = false;
            socket.Dispose();
        }
    }
}

#nullable disable