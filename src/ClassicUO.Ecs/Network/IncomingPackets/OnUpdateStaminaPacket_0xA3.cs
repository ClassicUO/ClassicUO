using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnUpdateStaminaPacket_0xA3 : IPacket
{
    public byte Id => 0xA3;

    public uint Serial { get; private set; }
    public ushort StaminaMax { get; private set; }
    public ushort Stamina { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        StaminaMax = reader.ReadUInt16BE();
        Stamina = reader.ReadUInt16BE();
    }
}
