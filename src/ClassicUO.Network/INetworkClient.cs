// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Net.Sockets;
using ClassicUO.Network.Encryption;
using ClassicUO.Utility;

namespace ClassicUO.Network
{
    public interface INetworkClient
    {
        bool IsConnected { get; }
        NetStatistics Statistics { get; }
        EncryptionHelper? Encryption { get; }
        PacketsTable PacketsTable { get; }
        uint LocalIP { get; }

        event EventHandler Connected;
        event EventHandler<SocketError> Disconnected;

        EncryptionType Load(ClientVersion clientVersion, EncryptionType encryption);
        void Connect(string ip, ushort port);
        void Disconnect();
        void EnableCompression();
        ArraySegment<byte> CollectAvailableData();
        void Flush();
        void Send(Span<byte> message, bool ignorePlugin = false, bool skipEncryption = false);
    }
}
