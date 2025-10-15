using ClassicUO.IO;
using System.Collections.Generic;

namespace ClassicUO.Ecs;

internal struct OnUpdateContainerItemsPacket_0x3C_Pre6017 : IPacket, IUpdateContainerItemsPacket
{
    public byte Id => 0x3C;

    public ushort Count { get; private set; }
    public List<ContainerItem> Items { get; private set; }
    public bool HasGridIndices => false;


    public void Fill(StackDataReader reader)
    {
        Count = reader.ReadUInt16BE();
        Items ??= new List<ContainerItem>();
        Items.Clear();

        for (int i = 0; i < Count; i++)
        {
            var item = new ContainerItem
            {
                Serial = reader.ReadUInt32BE(),
                Graphic = reader.ReadUInt16BE(),
                GraphicInc = reader.ReadUInt8(),
                Amount = reader.ReadUInt16BE(),
                X = reader.ReadUInt16BE(),
                Y = reader.ReadUInt16BE(),
                GridIndex = 0,
                ContainerSerial = reader.ReadUInt32BE(),
                Hue = reader.ReadUInt16BE()
            };
            Items.Add(item);
        }    
    }
}
