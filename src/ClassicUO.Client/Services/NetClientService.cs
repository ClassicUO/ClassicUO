using ClassicUO.Network;
using System;
using System.Net.Sockets;

namespace ClassicUO.Services
{
    internal class NetClientService : IService
    {
        private readonly NetClient _netClient;

        public NetClientService(NetClient netClient)
        {
            _netClient = netClient;
        }

        public NetStatistics Statistics => _netClient.Statistics;
        public NetClient Socket => _netClient;

        public ArraySegment<byte> CollectAvailableData()
        {
            return _netClient.CollectAvailableData();
        }

        public void Flush() => _netClient.Flush();

        public void SendPing()
        {
            _netClient.Statistics.SendPing();
        }

        public void Disconnect()
        {
            _netClient.Disconnect();
        }

        public void RegisterDisconnectedEvent(EventHandler<SocketError> handler)
        {
            _netClient.Disconnected += handler;
        }

        public void UnregisterDisconnectedEvent(EventHandler<SocketError> handler)
        {
            _netClient.Disconnected -= handler;
        }
    }
}
