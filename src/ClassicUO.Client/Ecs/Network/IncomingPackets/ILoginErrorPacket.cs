namespace ClassicUO.Ecs;

internal interface ILoginErrorPacket : IPacket
{
    byte Code { get; }
}
