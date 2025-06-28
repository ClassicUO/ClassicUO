using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Network;
using ClassicUO.Utility;
using TinyEcs;
using ClassicUO.Network.Encryption;
using ClassicUO.Assets;
using System.Runtime.InteropServices;

namespace ClassicUO.Ecs;

delegate void OnPacket(ReadOnlySpan<byte> buffer);

struct OnLoginRequest
{
    public string Username;
    public string Password;
    public string Address;
    public ushort Port;
}

internal sealed class PacketsMap : Dictionary<byte, OnPacket>;

readonly struct NetworkPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        scheduler.AddResource(new CircularBuffer());
        scheduler.AddResource(new PacketsMap());
        scheduler.AddEvent<OnLoginRequest>();

        var setupSocketFn = SetupSocket;
        scheduler.OnStartup(setupSocketFn);

        scheduler.AddPlugin<LoginPacketsPlugin>();
        scheduler.AddPlugin<InGamePacketsPlugin>();

        scheduler.OnExit(GameState.GameScreen, (Res<NetClient> network, Res<CircularBuffer> buffer, Res<GameContext> gameCtx) =>
        {
            gameCtx.Value.Map = -1;
            gameCtx.Value.PlayerSerial = 0;
            network.Value.Disconnect();
            buffer.Value.Clear();
        });

        scheduler.OnUpdate((Res<NetClient> network) => network.Value.Send_Ping(0xFF))
            .RunIf((Res<GameContext> gameCtx, Res<NetClient> network) => network.Value!.IsConnected && gameCtx.Value.PlayerSerial != 0)
            .RunIf((Time time, Local<float> updateTime) =>
            {
                if (updateTime.Value >= time.Total)
                    return false;

                updateTime.Value = time.Total + 1000f;
                return true;
            });

        var handleLoginRequestsFn = HandleLoginRequests;
        scheduler.OnUpdate(handleLoginRequestsFn)
            .RunIf((EventReader<OnLoginRequest> loginRequests) => !loginRequests.IsEmpty);

        var packetReaderFn = PacketReader;
        scheduler.OnUpdate(packetReaderFn)
            .RunIf((Res<NetClient> network) => network.Value!.IsConnected);
    }

    void SetupSocket(Res<Settings> settings, Res<UOFileManager> fileManager, SchedulerState sched)
    {
        var socket = new NetClient();
        settings.Value.Encryption = (byte)socket.Load(fileManager.Value.Version, (EncryptionType)settings.Value.Encryption);
        sched.AddResource(socket);
    }

    void HandleLoginRequests(
        EventReader<OnLoginRequest> loginRequests,
        Res<NetClient> network,
        Res<GameContext> gameCtx,
        Res<Settings> settings
    )
    {
        foreach (var request in loginRequests)
        {
            network.Value.Connect(request.Address, request.Port);
            Console.WriteLine("Socket is connected ? {0}", network.Value.IsConnected);

            if (!network.Value.IsConnected)
                continue;

            network.Value.Encryption?.Initialize(true, network.Value.LocalIP);

            if (gameCtx.Value.ClientVersion >= ClientVersion.CV_6040)
            {
                // NOTE: im forcing the use of latest client just for convenience rn
                var major = (byte)((uint)gameCtx.Value.ClientVersion >> 24);
                var minor = (byte)((uint)gameCtx.Value.ClientVersion >> 16);
                var build = (byte)((uint)gameCtx.Value.ClientVersion >> 8);
                var extra = (byte)gameCtx.Value.ClientVersion;

                network.Value.Send_Seed(network.Value.LocalIP, major, minor, build, extra);
            }
            else
            {
                network.Value.Send_Seed_Old(network.Value.LocalIP);
            }

            network.Value.Send_FirstLogin(request.Username, Crypter.Decrypt(request.Password));

            break;
        }
        loginRequests.Clear();
    }

    void PacketReader(
        Query<Data<WasmMod>> queryMods,
        Res<NetClient> network,
        Res<PacketsMap> packetsMap,
        Res<CircularBuffer> buffer,
        Local<byte[]> packetBuffer
    )
    {
        // buffer.Value ??= new();
        packetBuffer.Value ??= new byte[4096];

        var availableData = network.Value.CollectAvailableData();
        var span = availableData.AsSpan();
        if (!span.IsEmpty)
            buffer.Value.Enqueue(span);

        while (buffer.Value.Length > 0)
        {
            var packetId = buffer.Value[0];
            var packetLen = (int)network.Value.PacketsTable.GetPacketLength(packetId);
            var packetHeaderOffset = sizeof(byte);

            if (packetLen == -1)
            {
                if (buffer.Value.Length < 3)
                    break;

                var b0 = buffer.Value[1];
                var b1 = buffer.Value[2];

                packetLen = (b0 << 8) | b1;
                packetHeaderOffset += sizeof(ushort);
            }

            if (buffer.Value.Length < packetLen)
            {
                Console.WriteLine("needs more data for packet 0x{0:X2}", packetId);
                break;
            }

            while (packetLen > packetBuffer.Value.Length)
                Array.Resize(ref packetBuffer.Value, packetBuffer.Value.Length * 2);

            _ = buffer.Value.Dequeue(packetBuffer.Value, 0, packetLen);

            // Console.WriteLine(">> packet-in: ID 0x{0:X2} | Len: {1}", packetId, packetLen);

            var sp = packetBuffer.Value.AsSpan(0, packetLen + packetHeaderOffset);

            foreach ((_, var mod) in queryMods)
            {
                if (mod.Ref.Mod.Plugin.FunctionExists("packet_recv"))
                {
                    var res = mod.Ref.Mod.Plugin.Call("packet_recv", sp);
                    if (res.IsEmpty)
                    {
                        sp = [];
                    }
                    else
                    {
                        res.CopyTo(sp);
                    }
                }
            }

            if (!sp.IsEmpty && packetsMap.Value.TryGetValue(packetId, out var handler))
            {
                handler(sp.Slice(packetHeaderOffset, packetLen - packetHeaderOffset));
            }
        }

        network.Value.Flush();
    }
}
