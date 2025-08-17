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

[JsonDerivedType(typeof(LoginResponse), nameof(LoginResponse))]
[JsonDerivedType(typeof(ServerLoginResponse), nameof(ServerLoginResponse))]
internal interface HostMessage
{
    internal record struct MouseMove(float X, float Y) : HostMessage;
    internal record struct MouseWheel(float Delta) : HostMessage;
    internal record struct MousePressed(int Button, float X, float Y) : HostMessage;
    internal record struct MouseReleased(int Button, float X, float Y) : HostMessage;
    internal record struct MouseDoubleClick(int Button, float X, float Y) : HostMessage;
    internal record struct KeyPressed(Keys Key) : HostMessage;
    internal record struct KeyReleased(Keys Key) : HostMessage;



    internal record struct LoginResponse(CharacterListFlags Flags, IEnumerable<CharacterInfo> Characters, IEnumerable<TownInfo> Cities) : HostMessage;
    internal record struct ServerLoginResponse(byte Flags, IEnumerable<ServerInfo> Servers) : HostMessage;
}

internal record struct HostMessages(IEnumerable<HostMessage> Messages);
