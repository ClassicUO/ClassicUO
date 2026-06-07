using ClassicUO.IO;

namespace ClassicUO.Ecs;

// Secure-trade control packet. One id, five sub-actions selected by Type:
//   0 = open trade window (Id1/Id2 are the two trade containers, Name = other
//       player's name)
//   1 = close / cancel the trade window
//   2 = accept state changed (Id1 = my accept flag, Id2 = his accept flag)
//   3 = his gold/platinum update
//   4 = my gold/platinum update
// Mirrors legacy PacketHandlers.SecureTrading. Variable length, so the payload
// handed to Fill already has the id + length header stripped.
internal struct OnSecureTradePacket_0x6F : IPacket
{
    public byte Id => 0x6F;

    public byte Type { get; private set; }
    public uint Serial { get; private set; }
    // Type 0: the two trade containers. Type 2: accept flags (Id1 = mine, Id2 = his).
    public uint Id1 { get; private set; }
    public uint Id2 { get; private set; }
    public bool HasName { get; private set; }
    public string Name { get; private set; }
    // Type 3/4: gold + platinum amounts.
    public uint Gold { get; private set; }
    public uint Platinum { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Type = reader.ReadUInt8();
        Serial = reader.ReadUInt32BE();
        Name = string.Empty;

        switch (Type)
        {
            case 0:
                Id1 = reader.ReadUInt32BE();
                Id2 = reader.ReadUInt32BE();
                HasName = reader.ReadBool();
                if (HasName && reader.Position < reader.Length)
                    Name = reader.ReadASCII();
                break;

            case 2:
                Id1 = reader.ReadUInt32BE();
                Id2 = reader.ReadUInt32BE();
                break;

            case 3:
            case 4:
                Gold = reader.ReadUInt32BE();
                Platinum = reader.ReadUInt32BE();
                break;
        }
    }
}
