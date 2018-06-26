using ClassicUO.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClassicUO.Network
{
    public class PacketHandler
    {
        public PacketHandler(Action<Packet> callback)
        {
            Callback = callback;
        }

        public Action<Packet> Callback { get; }
    }

    public class PacketHandlers
    {
        public static PacketHandlers ToClient { get; private set; }
        public static PacketHandlers ToServer { get; private set; }

        static PacketHandlers()
        {
            ToClient = new PacketHandlers();
            ToServer = new PacketHandlers();

            NetClient.PacketReceived += ToClient.OnPacket;
            NetClient.PacketSended += ToServer.OnPacket;
        }

        private readonly List<PacketHandler>[] _handlers = new List<PacketHandler>[0x100];

        private PacketHandlers()
        {
            for (int i = 0; i < _handlers.Length; i++)
                _handlers[i] = new List<PacketHandler>();
        }

        public void Add(byte id, Action<Packet> handler)
        {
            lock (_handlers)
                _handlers[id].Add(new PacketHandler(handler));
        }

        public void Remove(byte id, Action<Packet> handler)
        {
            lock (_handlers)
                _handlers[id].Remove(_handlers[id].FirstOrDefault(s => s.Callback == handler));
        }

        private void OnPacket(object sender, Packet p)
        {
            lock (_handlers)
            {
                for (int i = 0; i < _handlers[p.ID].Count; i++)
                {
                    p.MoveToData();
                    _handlers[p.ID][i].Callback(p);
                }
            }
        }





        public static void Load()
        {
            ToClient.Add(0xA8, ServerList);
            ToClient.Add(0x8C, ServerRelay);
            ToClient.Add(0x82, LoginRejectionReason);
            ToClient.Add(0xA9, CharactersList);
            ToClient.Add(0x1B, LoginConfirm);
            ToClient.Add(0xF0, NegotiateFeatures);
            ToClient.Add(0xDD, OnCompressedGump);
        }

        public static void ServerList(Packet p)
        {
            byte flag = p.ReadByte();
            int count = p.ReadUShort();

            for (int i = 0;  i < count; i++)
            {
                ushort idx = p.ReadUShort();
                string name = p.ReadASCII(32);
                byte percfull = p.ReadByte();
                byte timezone = p.ReadByte();
                uint address = p.ReadUInt();
            }
        }

        public static void ServerRelay(Packet p)
        {
           
        }

        public static void LoginRejectionReason(Packet p)
        {

        }

        public static void CharactersList(Packet p)
        {

        }

        public static void LoginConfirm(Packet p)
        {

        }

        public static void NegotiateFeatures(Packet p)
        {

        }

        public static void OnCompressedGump(Packet p)
        {
            p.Skip(4 * 4);
            uint clen = p.ReadUInt() - 4;
            uint dlen = p.ReadUInt();

            byte[] dest = new byte[dlen];
            Zlib.Decompress(p.ToArray(), p.Position, dest, (int)dlen);

            string layout = Encoding.UTF8.GetString(dest);
        }
    }
}
