using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Network;
using ClassicUO.Utility;
using TinyEcs;
using ClassicUO.Network.Encryption;
using ClassicUO.Assets;
using System.Runtime.InteropServices;
using ClassicUO.Game.Data;
using ClassicUO.IO;
using TinyEcs.Bevy;

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

internal sealed class PacketsMap2 : Dictionary<byte, Func<IPacket>>;


internal interface IPacket
{
    byte Id { get; }
    void Fill(StackDataReader reader);
}


internal struct OnEnterWorldPacket() : IPacket
{
    public byte Id { get; } = 0x1B;

    public uint Serial { get; private set; }
    public uint Unused0 { get; private set; }
    public ushort Graphic { get; private set; }
    public (ushort X, ushort Y, sbyte Z) Position { get; private set; }
    public Direction Direction { get; private set; }
    public uint Unused1 { get; private set; }
    public uint Unused2 { get; private set; }
    public byte Unused3 { get; private set; }
    public ushort MapWidth { get; private set; }
    public ushort MapHeight { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        Unused0 = reader.ReadUInt32BE();
        Graphic = reader.ReadUInt16BE();
        Position = (reader.ReadUInt16BE(), reader.ReadUInt16BE(), (sbyte)reader.ReadUInt16BE());
        Direction = (Direction) reader.ReadUInt8();
        Unused1 = reader.ReadUInt32BE();
        Unused2 = reader.ReadUInt32BE();
        Unused3 = reader.ReadUInt8();
        MapWidth = reader.ReadUInt16BE();
        MapHeight = reader.ReadUInt16BE();
    }
}

readonly struct NetworkPlugin : IPlugin
{
    public void Build(App app)
    {
        var setupSocketFn = SetupSocket;
        var handleLoginRequestsFn = HandleLoginRequests;
        var packetReaderFn = PacketReader;

        app.AddResource(new CircularBuffer());
        app.AddResource(new PacketsMap());
        app.AddResource(new PacketsMap2());

        app
            .AddSystem(Stage.Startup, setupSocketFn)

            .AddSystem(Stage.Startup, (Res<PacketsMap2> packetsMap) =>
            {
                void create<T>() where T : IPacket, new()
                {
                    var stack = new Stack<IPacket>();
                    var packet = new T();
                    stack.Push(packet);
                    var fn = () => (IPacket)new T();
                    packetsMap.Value.Add(packet.Id, fn);
                }

                create<OnEnterWorldPacket>();
            })

            .AddSystem((EventReader<IPacket> reader) =>
            {
                foreach (var packet in reader.Read())
                {
                    if (packet is OnEnterWorldPacket enterWorld)
                    {
                        Console.WriteLine(">> OnEnterWorld: Serial 0x{0:X8} | Graphic 0x{1:X4} | Pos {2},{3},{4} | Dir {5} | MapSize {6}x{7}",
                            enterWorld.Serial,
                            enterWorld.Graphic,
                            enterWorld.Position.X,
                            enterWorld.Position.Y,
                            enterWorld.Position.Z,
                            enterWorld.Direction,
                            enterWorld.MapWidth,
                            enterWorld.MapHeight
                        );
                    }
                }
            })
            .InStage(Stage.Update)
            .RunIf((EventReader<IPacket> reader) => reader.HasEvents)
            .Build()

            .AddPlugin<LoginPacketsPlugin>()
            .AddPlugin<InGamePacketsPlugin>()

            .AddSystem((Res<NetClient> network, Res<CircularBuffer> buffer, ResMut<GameContext> gameCtx) =>
            {
                gameCtx.Value.Map = -1;
                gameCtx.Value.PlayerSerial = 0;
                network.Value.Disconnect();
                buffer.Value.Clear();
            })
            .OnExit(GameState.GameScreen)
            .Build()

            .AddSystem((Res<NetClient> network) => network.Value.Send_Ping(0xFF))
            .InStage(Stage.Update)
            .RunIf((Res<GameContext> gameCtx, Res<NetClient> network) => network.Value!.IsConnected && gameCtx.Value.PlayerSerial != 0)
            .RunIf((Res<Time> time, Local<float> updateTime) =>
            {
                if (updateTime.Value >= time.Value.Total)
                    return false;

                updateTime.Value = time.Value.Total + 1000f;
                return true;
            })
            .Build()

            .AddSystem(handleLoginRequestsFn)
            .InStage(Stage.Update)
            .RunIf((EventReader<OnLoginRequest> loginRequests) => loginRequests.HasEvents)
            .Build()

            .AddSystem(packetReaderFn)
            .InStage(Stage.Update)
            .RunIf((Res<NetClient> network) => network.Value!.IsConnected)
            .Build();
    }

    void SetupSocket(Res<Settings> settings, Res<NetClient> socket, Res<UOFileManager> fileManager, Commands commands)
    {
        settings.Value.Encryption = (byte)socket.Value.Load(fileManager.Value.Version, (EncryptionType)settings.Value.Encryption);
    }

    void HandleLoginRequests(
        EventReader<OnLoginRequest> loginRequests,
        Res<NetClient> network,
        Res<GameContext> gameCtx,
        Res<Settings> settings
    )
    {
        foreach (var request in loginRequests.Read())
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
    }

    sealed class PacketBuffer
    {
        public PacketBuffer() => Buffer = new byte[1024 * 4];

        public byte[] Buffer;
    }

    void PacketReader(
        Query<Data<WasmMod>> queryMods,
        Res<NetClient> network,
        Res<PacketsMap> packetsMap,
        Res<PacketsMap2> packetsMap2,
        Res<CircularBuffer> buffer,
        Local<PacketBuffer> packetBuffer,
        EventWriter<IPacket> queuePackets
    )
    {
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

            while (packetLen > packetBuffer.Value.Buffer.Length)
                Array.Resize(ref packetBuffer.Value.Buffer, packetBuffer.Value.Buffer.Length * 2);

            _ = buffer.Value.Dequeue(packetBuffer.Value.Buffer, 0, packetLen);

            // Console.WriteLine(">> packet-in: ID 0x{0:X2} | Len: {1}", packetId, packetLen);

            var sp = packetBuffer.Value.Buffer.AsSpan(0, packetLen + packetHeaderOffset);

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
                if (packetsMap2.Value.TryGetValue(packetId, out var fn))
                {
                    var data = sp.Slice(packetHeaderOffset, packetLen - packetHeaderOffset);
                    var reader = new StackDataReader(data);
                    var packet = fn();
                    packet.Fill(reader);
                    queuePackets.Send(packet);
                }

                handler(sp.Slice(packetHeaderOffset, packetLen - packetHeaderOffset));
            }
        }

        network.Value.Flush();
    }
}
