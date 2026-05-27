// SPDX-License-Identifier: BSD-2-Clause
//
// world.dumpState: snapshot of the player, nearby mobiles, open gumps,
// and recent journal lines as a single JSON document. Pure read against
// the main client's World / UIManager — no mutation, no I/O.
//
// Params: none (an empty object is accepted but ignored).
//
// Returns: AgentWorldStateDto. Error JsonRpcErrorCodes.NotInWorld if
// World.Player is null or its serial is 0 (still on login/charlist).
//
// Field parity with the ECS branch (cuo-agents/Ecs/Agent/Handlers/
// WorldHandlers.cs): same DTO surface, so the same scripts and
// assertions work on both loops.

#if AGENT_BUILD
#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using ClassicUO.Agent.Contracts;
using ClassicUO.Agent.Contracts.Dto;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Agent.Agent.Handlers;

internal static class WorldHandlers
{
    // "Nearby" radius. Main has no MaxObjectsDistance setting on the
    // World; pick the same 18-tile default the ECS branch uses so
    // dumpState returns comparable mobile sets on both loops.
    private const int DefaultNearbyRange = 18;

    public static JsonRpcResponse DumpState(JsonRpcRequest req, in AgentRpcContext ctx)
    {
        var world = ctx.Game.UO.World;
        var player = world.Player;
        if (player is null || player.Serial == 0)
        {
            return AgentServer.ErrorResponse(
                req.Id,
                JsonRpcErrorCodes.NotInWorld,
                "not in world");
        }

        var dto = BuildSnapshot(world);

        var json = JsonSerializer.Serialize(dto, AgentJsonContext.Default.AgentWorldStateDto);

        return new JsonRpcResponse
        {
            Id = req.Id,
            Result = JsonNode.Parse(json),
        };
    }

    private static AgentWorldStateDto BuildSnapshot(ClassicUO.Game.World world)
    {
        var playerDto = BuildPlayer(world.Player!, world.MapIndex);
        var nearby = BuildNearbyMobiles(world);

        // TODO(agent): open-gump registry / journal store not wired into
        // this dump on main yet. Return empty lists for now — matches the
        // ECS branch's TODO state, callers should not rely on them.
        var openGumps = new List<AgentOpenGumpDto>();
        var journal = new List<AgentJournalLineDto>();

        return new AgentWorldStateDto(playerDto, nearby, openGumps, journal);
    }

    private static AgentPlayerDto BuildPlayer(PlayerMobile player, int map)
    {
        return new AgentPlayerDto(
            Serial: player.Serial,
            Name: player.Name ?? string.Empty,
            Position: new AgentPositionDto(player.X, player.Y, player.Z, map),
            Hp:   new AgentStatRangeDto(player.Hits,    player.HitsMax),
            Stam: new AgentStatRangeDto(player.Stamina, player.StaminaMax),
            Mana: new AgentStatRangeDto(player.Mana,    player.ManaMax));
    }

    private static List<AgentNearbyMobileDto> BuildNearbyMobiles(ClassicUO.Game.World world)
    {
        var results = new List<AgentNearbyMobileDto>();
        var player = world.Player!;
        var range = DefaultNearbyRange;
        var map = world.MapIndex;

        foreach (var m in world.Mobiles.Values)
        {
            if (m is null || m.Serial == player.Serial)
                continue;
            if (m.IsDestroyed)
                continue;

            var dx = m.X - player.X;
            var dy = m.Y - player.Y;
            if (Math.Abs(dx) > range || Math.Abs(dy) > range)
                continue;

            results.Add(new AgentNearbyMobileDto(
                Serial: m.Serial,
                Name: m.Name ?? string.Empty,
                Graphic: m.Graphic,
                Hue: m.Hue,
                Position: new AgentPositionDto(m.X, m.Y, m.Z, map),
                Notoriety: (int)m.NotorietyFlag,
                Hp: new AgentStatRangeDto(m.Hits, m.HitsMax)));
        }

        return results;
    }
}

#endif
