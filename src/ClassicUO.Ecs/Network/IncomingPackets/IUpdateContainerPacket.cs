namespace ClassicUO.Ecs;

internal interface IUpdateContainerPacket
{
    uint Serial { get; }
    ushort Graphic { get; }
    sbyte GraphicIncrement { get; }
    ushort Amount { get; }
    ushort X { get; }
    ushort Y { get; }
    bool HasGridIndex { get; }
    byte GridIndex { get; }
    uint ContainerSerial { get; }
    ushort Hue { get; }
}
