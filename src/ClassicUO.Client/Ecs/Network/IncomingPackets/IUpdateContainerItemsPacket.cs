namespace ClassicUO.Ecs;

internal interface IUpdateContainerItemsPacket
{
    ushort Count { get; }
    byte[] ItemsData { get; }
    bool HasGridIndices { get; }
}
