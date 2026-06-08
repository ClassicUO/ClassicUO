using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ClassicUO.Ecs.Modding.Guest;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(LoginRequest), nameof(LoginRequest))]
[JsonDerivedType(typeof(ServerLoginRequest), nameof(ServerLoginRequest))]
[JsonDerivedType(typeof(CustomAction), nameof(CustomAction))]
internal interface PluginMessage
{
    internal record struct LoginRequest(string Username, string Password) : PluginMessage;
    internal record struct ServerLoginRequest(byte Index) : PluginMessage;

    // Generic mod→host action channel (mirror of HostMessage.Custom). Topic names
    // the intent (e.g. "demo.confirm.ok"); Json carries the mod-defined payload.
    // Routed in ModdingPlugin.ReadModEvents. Actions that map to a UO packet can
    // skip this and call cuo_send_to_server directly; Custom is for client-local
    // intents the host must service.
    internal record struct CustomAction(string Topic, string Json) : PluginMessage;
}

internal record struct PluginMessages(List<PluginMessage> Messages);