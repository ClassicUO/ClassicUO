using System.Text.Json.Serialization;
using ClassicUO.Ecs.Modding.Guest;
using ClassicUO.Ecs.Modding.Host;
using ClassicUO.Ecs.Modding.UI;

namespace ClassicUO.Ecs.Modding;


[JsonSourceGenerationOptions(IncludeFields = true, PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
// components
[JsonSerializable(typeof(WorldPosition), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(Graphic), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(Hue), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(Facing), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(EquipmentSlots), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(Hits), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(Mana), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(Stamina), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(MobAnimation), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(ServerFlags), GenerationMode = JsonSourceGenerationMode.Default)]

[JsonSerializable(typeof(UINodes), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(UIEvent), GenerationMode = JsonSourceGenerationMode.Default)]

[JsonSerializable(typeof(HostMessages), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(PluginMessages), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(TimeProxy), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(WasmPluginVersion), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(PacketHandlerInfo), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(SpriteDescription), GenerationMode = JsonSourceGenerationMode.Default)]
internal partial class ModdingJsonContext : JsonSerializerContext { }
