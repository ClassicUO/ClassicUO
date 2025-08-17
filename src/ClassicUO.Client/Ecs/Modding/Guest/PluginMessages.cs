using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ClassicUO.Ecs.Modding.Guest;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(LoginRequest), nameof(LoginRequest))]
[JsonDerivedType(typeof(ServerLoginRequest), nameof(ServerLoginRequest))]
internal interface PluginMessage
{
    internal record struct LoginRequest(string Username, string Password) : PluginMessage;
    internal record struct ServerLoginRequest(byte Index) : PluginMessage;
}

internal record struct PluginMessages(List<PluginMessage> Messages);