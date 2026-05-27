using System.Collections.Generic;
using System.Text.Json.Serialization;
using ClassicUO.Game.Data;
using Microsoft.Xna.Framework.Input;

namespace ClassicUO.Ecs.Modding.Host;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(MouseMove), nameof(MouseMove))]
[JsonDerivedType(typeof(MouseWheel), nameof(MouseWheel))]
[JsonDerivedType(typeof(MousePressed), nameof(MousePressed))]
[JsonDerivedType(typeof(MouseReleased), nameof(MouseReleased))]
[JsonDerivedType(typeof(MouseDoubleClick), nameof(MouseDoubleClick))]
[JsonDerivedType(typeof(KeyPressed), nameof(KeyPressed))]
[JsonDerivedType(typeof(KeyReleased), nameof(KeyReleased))]

[JsonDerivedType(typeof(GameStateChanged), nameof(GameStateChanged))]

[JsonDerivedType(typeof(LoginResponse), nameof(LoginResponse))]
[JsonDerivedType(typeof(ServerLoginResponse), nameof(ServerLoginResponse))]

[JsonDerivedType(typeof(WorldEntered), nameof(WorldEntered))]
[JsonDerivedType(typeof(FacetChanged), nameof(FacetChanged))]
[JsonDerivedType(typeof(MessageReceived), nameof(MessageReceived))]

[JsonDerivedType(typeof(ContainerOpened), nameof(ContainerOpened))]
[JsonDerivedType(typeof(ContainerClosed), nameof(ContainerClosed))]
[JsonDerivedType(typeof(ContainerItemAdded), nameof(ContainerItemAdded))]
internal interface HostMessage
{
    internal record struct MouseMove(float X, float Y) : HostMessage;
    internal record struct MouseWheel(float Delta) : HostMessage;
    internal record struct MousePressed(int Button, float X, float Y) : HostMessage;
    internal record struct MouseReleased(int Button, float X, float Y) : HostMessage;
    internal record struct MouseDoubleClick(int Button, float X, float Y) : HostMessage;
    internal record struct KeyPressed(Keys Key) : HostMessage;
    internal record struct KeyReleased(Keys Key) : HostMessage;


    internal record struct GameStateChanged(GameState State) : HostMessage;

    internal record struct LoginResponse(CharacterListFlags Flags, IEnumerable<CharacterInfo> Characters, IEnumerable<TownInfo> Cities) : HostMessage;
    internal record struct ServerLoginResponse(byte Flags, IEnumerable<ServerInfo> Servers) : HostMessage;

    internal record struct WorldEntered() : HostMessage;
    internal record struct FacetChanged(int Index) : HostMessage;
    internal record struct MessageReceived(MessageType MessageType, string Text, string Name, uint Serial, ushort Hue, byte Font) : HostMessage;

    internal record struct ContainerOpened(uint Serial, ushort Graphic) : HostMessage;
    internal record struct ContainerClosed(uint Serial) : HostMessage;
    internal record struct ContainerItemAdded(uint ContainerSerial, uint Serial, ushort Graphic, int Amount, int X, int Y, int GridIndex, ushort Hue) : HostMessage;
}

internal record struct HostMessages(IEnumerable<HostMessage> Messages);
