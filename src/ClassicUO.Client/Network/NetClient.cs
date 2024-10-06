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
    private readonly byte[] _decompressionBuffer = new byte[BUFF_SIZE * 3];
    private readonly Pipe _receivePipe = new(BUFF_SIZE * 3);
    private readonly Pipe _sendPipe = new(BUFF_SIZE);
    private readonly SendTrigger _sendTrigger;
    private readonly Huffman _huffman = new();

    private bool _isCompressionEnabled;
    private uint? _localIP;
    private NetSocket? _socket;
    private CancellationTokenSource _source;
    private Task? _readLoopTask;
    private Task? _writeLoopTask;

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
        _sendTrigger = new(_sendPipe);
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

    public void Disconnect(SocketError error = SocketError.Success)
    {
        if (!IsConnected)
            return;

        IsConnected = false;
        _isCompressionEnabled = false;
        _source.Cancel();
        _sendTrigger.Cancel();

        if (_readLoopTask is not null)
            Task.WaitAll(_readLoopTask, _writeLoopTask!);

        _sendPipe.Clear();
        _receivePipe.Clear();
        _socket!.Dispose();

        Statistics.Reset();
        Disconnected?.Invoke(this, error);
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

        int messageLength = message.Length;

        lock (_sendPipe)
        {
            Span<byte> span = _sendPipe.GetAvailableSpanToWrite();

            if (span.Length >= messageLength)
            {
                message.CopyTo(span);
                _sendPipe.CommitWrited(messageLength);
            }
            else
            {
                message[..span.Length].CopyTo(span);
                _sendPipe.CommitWrited(span.Length);

                message = message[span.Length..];
                span = _sendPipe.GetAvailableSpanToWrite();

                if (span.Length < message.Length)
                    throw new Exception("Send pipe is full");

                message.CopyTo(span);
                _sendPipe.CommitWrited(message.Length);
            }

            _sendTrigger.Trigger(messageLength);
        }

        Statistics.TotalBytesSent += (uint)messageLength;
        Statistics.TotalPacketsSent++;
    }

    public void SendPing()
    {
        if (!IsConnected)
            return;

        Statistics.SendPing();
    }

    public void ReceivePing(byte idx)
    {
        Statistics.PingReceived(idx);
    }

    public void UpdateStatistics(int receivedPacketCount)
    {
        Statistics.TotalPacketsReceived += (uint)receivedPacketCount;
        Statistics.Update();
    }

    private void ProcessEncryption(Span<byte> buffer)
    {
        if (!_isCompressionEnabled)
            return;

        Encryption?.Decrypt(buffer, buffer, buffer.Length);
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
        if (IsConnected)
            Disconnect();

        ServerDisconnectionExpected = false;

        _source = new();
        _socket = isWebSocket ? new WebSocket() : new TcpSocket();
        _sendTrigger.Reset();

        CancellationToken token = _source.Token;

        try
        {
            await _socket.ConnectAsync(uri, token);

            IsConnected = true;
            Statistics.Reset();
            Connected?.Invoke(this, EventArgs.Empty);

            _readLoopTask = Task.Run(() => ReadLoop(_socket, token));
            _writeLoopTask = Task.Run(() => WriteLoop(_socket, token));
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
        { }
        catch (SocketException se)
        {
            if (se.SocketErrorCode == SocketError.ConnectionReset && ServerDisconnectionExpected)
                Disconnect();
            else
                Disconnect(se.SocketErrorCode);
        }
        catch
        {
            Disconnect(SocketError.Fault);
        }
    }

    private async Task WriteLoop(NetSocket socket, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await _sendTrigger.Wait();

                Memory<byte> buffer = _sendPipe.GetAvailableMemoryToRead();

                int bufferLength = buffer.Length;
                if (bufferLength == 0)
                    continue;

                while (!buffer.IsEmpty)
                {
                    int bytesWritten = await socket!.SendAsync(buffer, token);
                    buffer = buffer[bytesWritten..];
                }

                _sendPipe.CommitRead(bufferLength);
            }
        }
        catch (Exception e) when (e is OperationCanceledException or SocketException)
        {
            // ignored: socket errors are handled by ReadLoop
        }
        catch
        {
            Disconnect();
        }
    }
}

#nullable disable