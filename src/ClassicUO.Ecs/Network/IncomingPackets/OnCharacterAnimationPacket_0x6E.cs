using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnCharacterAnimationPacket_0x6E : IPacket
{
    public byte Id => 0x6E;

    public uint Serial { get; private set; }
    public ushort Action { get; private set; }
    public ushort FrameCount { get; private set; }
    public ushort RepeatForNTimes { get; private set; }
    public bool Backward { get; private set; }
    public bool Loop { get; private set; }
    public byte Delay { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        Action = reader.ReadUInt16BE();
        FrameCount = reader.ReadUInt16BE();
        RepeatForNTimes = reader.ReadUInt16BE();
        Backward = reader.ReadBool();
        Loop = reader.ReadBool();
        Delay = reader.ReadUInt8();
    }
}
