using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Utility;
using ClassicUO.Ecs.NetworkPlugins;
using Microsoft.Xna.Framework;
using TinyEcs;

namespace ClassicUO.Ecs;

using PacketsMap = Dictionary<byte, OnPacket>;
using NetworkEntitiesMap = Dictionary<uint, EcsID>;

delegate void OnPacket(ReadOnlySpan<byte> buffer);

struct OnLoginRequest
{
    public string Address;
    public ushort Port;
}

readonly struct NetworkPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        scheduler.AddResource(new NetClient());
        scheduler.AddResource(new PacketsMap());
        scheduler.AddResource(new NetworkEntitiesMap());

        scheduler.AddEvent<OnLoginRequest>();

        scheduler.AddPlugin<LoginPacketsPlugin>();
        scheduler.AddPlugin<InGamePacketsPlugin>();


        scheduler.AddSystem(
        (
            EventReader<OnLoginRequest> requets,
            Res<NetClient> network,
            Res<GameContext> gameCtx,
            Res<Settings> settings
        ) => {
            foreach (var request in requets)
            {
                PacketsTable.AdjustPacketSizeByVersion(gameCtx.Value.ClientVersion);
                network.Value.Connect(request.Address, request.Port);

                Console.WriteLine("Socket is connected ? {0}", network.Value.IsConnected);

                if (!network.Value.IsConnected)
                    continue;

                if (gameCtx.Value.ClientVersion >= ClientVersion.CV_6040)
                {
                    // NOTE: im forcing the use of latest client just for convenience rn
                    var major = (byte) ((uint)gameCtx.Value.ClientVersion >> 24);
                    var minor = (byte) ((uint)gameCtx.Value.ClientVersion >> 16);
                    var build = (byte) ((uint)gameCtx.Value.ClientVersion >> 8);
                    var extra = (byte) gameCtx.Value.ClientVersion;

                    network.Value.Send_Seed(network.Value.LocalIP, major, minor, build, extra);
                }
                else
                {
                    network.Value.Send_Seed_Old(network.Value.LocalIP);
                }

                network.Value.Send_FirstLogin(settings.Value.Username, Crypter.Decrypt(settings.Value.Password));

                break;
            }
        }).RunIf((EventReader<OnLoginRequest> requets) => !requets.IsEmpty);

        scheduler.AddSystem((Res<NetClient> network, Res<PacketsMap> packetsMap) => {
            var availableData = network.Value.CollectAvailableData();

            var realBuffer = availableData.AsSpan();
            while (!realBuffer.IsEmpty)
            {
                var packetId = realBuffer[0];
                var packetLen = PacketsTable.GetPacketLength(packetId);
                var packetHeaderOffset = sizeof(byte);

                if (packetLen == -1)
                {
                    if (realBuffer.Length < 3)
                        return;

                    packetLen = BinaryPrimitives.ReadInt16BigEndian(realBuffer[packetHeaderOffset..]);
                    packetHeaderOffset += sizeof(ushort);
                }

                Console.WriteLine(">> packet-in: ID 0x{0:X2} | Len: {1}", packetId, packetLen);

                if (packetsMap.Value.TryGetValue(packetId, out var handler))
                {
                    handler(realBuffer[packetHeaderOffset .. packetLen]);
                }

                realBuffer = realBuffer[packetLen ..];
            }

            network.Value.Flush();
        }).RunIf((Res<NetClient> network) => network.Value!.IsConnected);
    }
}