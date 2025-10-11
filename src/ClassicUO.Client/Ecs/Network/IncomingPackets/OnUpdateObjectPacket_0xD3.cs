using System;
using System.Collections.Generic;
using ClassicUO.Game.Data;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnUpdateObjectPacket_0xD3 : IPacket
{
    internal struct EquipmentEntry
    {
        public uint Serial;
        public ushort Graphic;
        public Layer Layer;
        public ushort Hue;
    }

    public byte Id => 0xD3;

    public uint Serial { get; private set; }
    public ushort Graphic { get; private set; }
    public ushort X { get; private set; }
    public ushort Y { get; private set; }
    public sbyte Z { get; private set; }
    public Direction Direction { get; private set; }
    public ushort Hue { get; private set; }
    public Flags Flags { get; private set; }
    public NotorietyFlag Notoriety { get; private set; }
    public ushort[] Reserved { get; private set; }
    public List<EquipmentEntry> Equipment { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();
        Graphic = reader.ReadUInt16BE();
        X = reader.ReadUInt16BE();
        Y = reader.ReadUInt16BE();
        Z = reader.ReadInt8();
        Direction = (Direction)reader.ReadUInt8();
        Hue = reader.ReadUInt16BE();
        Flags = (Flags)reader.ReadUInt8();
        Notoriety = (NotorietyFlag)reader.ReadUInt8();

        Reserved = new ushort[3];
        for (var i = 0; i < Reserved.Length; ++i)
        {
            Reserved[i] = reader.ReadUInt16BE();
        }

        Equipment ??= new List<EquipmentEntry>();
        Equipment.Clear();
        uint itemSerial;
        while ((itemSerial = reader.ReadUInt32BE()) != 0)
        {
            var itemGraphic = reader.ReadUInt16BE();
            var layer = (Layer)reader.ReadUInt8();
            ushort itemHue = 0;

            if (reader.Remaining >= 2)
            {
                if ((itemGraphic & 0x8000) != 0)
                {
                    itemGraphic &= 0x7FFF;
                    itemHue = reader.ReadUInt16BE();
                }
                else
                {
                    itemHue = reader.ReadUInt16BE();
                }
            }

            Equipment.Add(new EquipmentEntry
            {
                Serial = itemSerial,
                Graphic = itemGraphic,
                Layer = layer,
                Hue = itemHue
            });
        }
    }
}
