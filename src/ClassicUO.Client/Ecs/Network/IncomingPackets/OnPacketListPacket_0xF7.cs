using System.Collections.Generic;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnPacketListPacket_0xF7 : IPacket
{
    public byte Id => 0xF7;

    public ushort Count { get; private set; }
    public List<byte> PacketIds { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Count = reader.ReadUInt16BE();
        PacketIds ??= new List<byte>();
        PacketIds.Clear();
        PacketIds.Capacity = Count;
        for (var i = 0; i < Count; ++i)
        {
            PacketIds.Add(reader.ReadUInt8());
        }
    }
}
