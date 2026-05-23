// SPDX-License-Identifier: BSD-2-Clause
//
// Wire DTOs matching the schema in classicuo-wasm/web/packages/ai/src/world.ts.
// Field naming (camelCase via the source-gen context) MUST stay in sync so
// the same seed scripts and assertion shapes work on both loops.

using System.Text.Json.Serialization;

namespace ClassicUO.Agent.Contracts.Dto;

public readonly record struct AgentPositionDto(int X, int Y, int Z, int Map);

public readonly record struct AgentStatRangeDto(int Cur, int Max);

public sealed record AgentPlayerDto(
    uint Serial,
    string Name,
    AgentPositionDto Position,
    AgentStatRangeDto Hp,
    AgentStatRangeDto Stam,
    AgentStatRangeDto Mana);

public sealed record AgentNearbyMobileDto(
    uint Serial,
    string Name,
    int Graphic,
    int Hue,
    AgentPositionDto Position,
    int Notoriety,
    AgentStatRangeDto Hp);

public sealed record AgentOpenGumpDto(int Serial, int GumpId);

public sealed record AgentJournalLineDto(
    int Id,
    string Text,
    string Speaker,
    int Hue,
    long Timestamp);

public sealed record AgentWorldStateDto(
    AgentPlayerDto Player,
    List<AgentNearbyMobileDto> NearbyMobiles,
    List<AgentOpenGumpDto> OpenGumps,
    List<AgentJournalLineDto> LastJournalLines);

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Default,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(AgentWorldStateDto))]
[JsonSerializable(typeof(AgentPlayerDto))]
[JsonSerializable(typeof(AgentNearbyMobileDto))]
[JsonSerializable(typeof(AgentOpenGumpDto))]
[JsonSerializable(typeof(AgentJournalLineDto))]
[JsonSerializable(typeof(JsonRpcRequest))]
[JsonSerializable(typeof(JsonRpcResponse))]
[JsonSerializable(typeof(JsonRpcEvent))]
[JsonSerializable(typeof(JsonRpcError))]
public sealed partial class AgentJsonContext : JsonSerializerContext
{
}
