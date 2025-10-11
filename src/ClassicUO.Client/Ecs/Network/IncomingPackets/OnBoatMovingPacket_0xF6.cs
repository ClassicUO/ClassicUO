using System.Collections.Generic;
using ClassicUO.Game.Data;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnBoatMovingPacket_0xF6 : IPacket
{
    internal struct BoatPassenger
    {
        public uint Serial;
        public ushort X;
        public ushort Y;
        public ushort Z;
    }

    public byte Id => 0xF6;

    public uint Serial { get; private set; }
    public byte Speed { get; private set; }
    public Direction MovingDirection { get; private set; }
    public Direction FacingDirection { get; private set; }
    public ushort X { get; private set; }
    public ushort Y { get; private set; }
    public ushort Z { get; private set; }
    public ushort PassengerCount { get; private set; }
    public List<BoatPassenger> Passengers { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        Speed = reader.ReadUInt8();
        MovingDirection = (Direction)reader.ReadUInt8();
        FacingDirection = (Direction)reader.ReadUInt8();
        X = reader.ReadUInt16BE();
        Y = reader.ReadUInt16BE();
        Z = reader.ReadUInt16BE();

        PassengerCount = reader.ReadUInt16BE();
        Passengers ??= new List<BoatPassenger>();
        Passengers.Clear();
        Passengers.Capacity = PassengerCount;
        for (var i = 0; i < PassengerCount; ++i)
        {
            Passengers.Add(new BoatPassenger
            {
                Serial = reader.ReadUInt32BE(),
                X = reader.ReadUInt16BE(),
                Y = reader.ReadUInt16BE(),
                Z = reader.ReadUInt16BE()
            });
        }
    }
}
