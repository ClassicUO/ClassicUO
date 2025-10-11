using System.Collections.Generic;
using ClassicUO.Game.Data;
using ClassicUO.IO;

namespace ClassicUO.Ecs;

internal struct OnCorpseEquipmentPacket_0x89 : IPacket
{
    internal struct CorpseItem
    {
        public Layer Layer;
        public uint Serial;
    }

    public byte Id => 0x89;

    public uint Serial { get; private set; }
    public List<CorpseItem> Items { get; private set; }

    public void Fill(StackDataReader reader)
    {
        Serial = reader.ReadUInt32BE();

        Items ??= new List<CorpseItem>();
        Items.Clear();
        Layer layer;
        while ((layer = (Layer)reader.ReadUInt8()) != Layer.Invalid)
        {
            var itemSerial = reader.ReadUInt32BE();
            if (itemSerial == 0)
            {
                break;
            }

            Items.Add(new CorpseItem
            {
                Layer = layer,
                Serial = itemSerial
            });
        }
    }
}
