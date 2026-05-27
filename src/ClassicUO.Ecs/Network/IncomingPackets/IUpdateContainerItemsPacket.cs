using System.Collections.Generic;

namespace ClassicUO.Ecs;

internal interface IUpdateContainerItemsPacket
{
    ushort Count { get; }
    List<ContainerItem> Items { get; }
    bool HasGridIndices { get; }
}

internal struct ContainerItem
{
    public uint Serial;
    public ushort Graphic;
    public byte GraphicInc;
    public ushort Amount;
    public ushort X;
    public ushort Y;
    public byte GridIndex;
    public uint ContainerSerial;
    public ushort Hue;
}