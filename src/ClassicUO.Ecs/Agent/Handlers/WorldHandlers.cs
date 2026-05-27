// SPDX-License-Identifier: BSD-2-Clause
//
// world.dumpState: snapshot of the player, nearby mobiles, open gumps,
// and recent journal lines as a single JSON document. Pure read against
// the ECS world from the game thread — no mutation, no I/O.
//
// Params: none (an empty object is accepted but ignored).
//
// Returns: AgentWorldStateDto. Error JsonRpcErrorCodes.NotInWorld if the
// player has not finished character select (GameContext.PlayerSerial == 0).
//
// Scope on impl/ecs (2026-05):
//   - Player name is not stored in any component on this branch; we leave
//     it empty (TODO below). Same for nearby mobile names.
//   - Notoriety is read off the wire but not stored per-mobile yet; we
//     return 0 (TODO).
//   - There's no open-gump registry or journal store on this branch yet;
//     both lists are returned empty (TODOs).

#if AGENT_BUILD
#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using ClassicUO.Agent.Contracts;
using ClassicUO.Agent.Contracts.Dto;
using ClassicUO.Agent.Host;
using ClassicUO.Ecs;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Agent.Agent.Handlers;

internal static class WorldHandlers
{
    // "Nearby" radius when GameContext.MaxObjectsDistance is unset (0).
    private const int DefaultNearbyRange = 18;

    public static void Register(AgentDispatcher<App> d)
    {
        d.Register(RpcVerbs.WorldDumpState, DumpState);
    }

    public static JsonRpcResponse DumpState(JsonRpcRequest req, in AgentRpcContext<App> ctx)
    {
        var world = ctx.Runtime.GetWorld();
        // TinyEcs.Bevy now distinguishes Res<T> (ref readonly) from
        // ResMut<T> (ref). DumpState is read-only so we copy GameContext
        // out by value into a local and pass it as `ref`.
        var gameCtx = ctx.Runtime.GetResource<GameContext>();
        if (gameCtx.PlayerSerial == 0)
        {
            return AgentServer.ErrorResponse(
                req.Id,
                JsonRpcErrorCodes.NotInWorld,
                "not in world");
        }

        var dto = BuildSnapshot(world, ref gameCtx);

        var json = JsonSerializer.Serialize(dto, AgentJsonContext.Default.AgentWorldStateDto);

        return new JsonRpcResponse
        {
            Id = req.Id,
            Result = JsonNode.Parse(json),
        };
    }

    private static AgentWorldStateDto BuildSnapshot(World world, ref GameContext gameCtx)
    {
        var player = BuildPlayer(world, ref gameCtx);
        var nearby = BuildNearbyMobiles(world, ref gameCtx);

        // TODO(agent): open-gump registry not implemented on impl/ecs yet.
        var openGumps = new List<AgentOpenGumpDto>();

        // TODO(agent): journal/chat history not stored on impl/ecs yet.
        var journal = new List<AgentJournalLineDto>();

        return new AgentWorldStateDto(player, nearby, openGumps, journal);
    }

    private static AgentPlayerDto BuildPlayer(World world, ref GameContext gameCtx)
    {
        var serial = gameCtx.PlayerSerial;
        var pos = new AgentPositionDto(gameCtx.CenterX, gameCtx.CenterY, gameCtx.CenterZ, gameCtx.Map);
        var hp = default(AgentStatRangeDto);
        var stam = default(AgentStatRangeDto);
        var mana = default(AgentStatRangeDto);

        // Walk the Player-tagged entity to pick up current stat values and
        // the authoritative WorldPosition (CenterX/Y/Z trails behind during
        // smoothed walks).
        var query = world.QueryBuilder().With<Player>().Build();
        var it = query.Iter();
        while (it.Next())
        {
            var entities = it.Entities();
            for (var i = 0; i < entities.Length; ++i)
            {
                var id = entities[i].ID;

                if (world.Has<WorldPosition>(id))
                {
                    ref var wp = ref world.Get<WorldPosition>(id);
                    pos = new AgentPositionDto(wp.X, wp.Y, wp.Z, gameCtx.Map);
                }

                if (world.Has<Hits>(id))
                {
                    ref var h = ref world.Get<Hits>(id);
                    hp = new AgentStatRangeDto(h.Value, h.MaxValue);
                }

                if (world.Has<Stamina>(id))
                {
                    ref var s = ref world.Get<Stamina>(id);
                    stam = new AgentStatRangeDto(s.Value, s.MaxValue);
                }

                if (world.Has<Mana>(id))
                {
                    ref var m = ref world.Get<Mana>(id);
                    mana = new AgentStatRangeDto(m.Value, m.MaxValue);
                }
            }
        }

        // TODO(agent): character name not stored in ECS on impl/ecs;
        // Settings.Username is the account, not the character. Leave blank
        // until a Name component (or PlayerData.Name) lands upstream.
        return new AgentPlayerDto(
            Serial: serial,
            Name: string.Empty,
            Position: pos,
            Hp: hp,
            Stam: stam,
            Mana: mana);
    }

    private static List<AgentNearbyMobileDto> BuildNearbyMobiles(World world, ref GameContext gameCtx)
    {
        var results = new List<AgentNearbyMobileDto>();

        var playerSerial = gameCtx.PlayerSerial;
        var range = gameCtx.MaxObjectsDistance > 0 ? gameCtx.MaxObjectsDistance : DefaultNearbyRange;

        // Resolve the player's authoritative tile position from its
        // WorldPosition component if available, otherwise fall back to
        // the camera centre.
        int px = gameCtx.CenterX;
        int py = gameCtx.CenterY;

        var playerQuery = world.QueryBuilder().With<Player>().Build();
        var playerIt = playerQuery.Iter();
        while (playerIt.Next())
        {
            var entities = playerIt.Entities();
            if (entities.Length == 0)
                continue;

            var id = entities[0].ID;
            if (world.Has<WorldPosition>(id))
            {
                ref var wp = ref world.Get<WorldPosition>(id);
                px = wp.X;
                py = wp.Y;
            }
            break;
        }

        // Mobiles carry the Mobiles tag and a NetworkSerial; WorldPosition
        // is set once the mobile has been placed on the map. Filter out
        // mobiles still inside a container.
        var query = world.QueryBuilder()
            .With<Mobiles>()
            .With<NetworkSerial>()
            .With<WorldPosition>()
            .Without<ContainedInto>()
            .Build();

        var it = query.Iter();
        while (it.Next())
        {
            var entities = it.Entities();
            for (var i = 0; i < entities.Length; ++i)
            {
                var id = entities[i].ID;

                ref var serial = ref world.Get<NetworkSerial>(id);
                if (serial.Value == playerSerial)
                    continue;

                ref var wp = ref world.Get<WorldPosition>(id);
                var dx = wp.X - px;
                var dy = wp.Y - py;
                if (Math.Abs(dx) > range || Math.Abs(dy) > range)
                    continue;

                ushort graphic = 0;
                if (world.Has<Graphic>(id))
                    graphic = world.Get<Graphic>(id).Value;

                ushort hue = 0;
                if (world.Has<Hue>(id))
                    hue = world.Get<Hue>(id).Value;

                var hp = default(AgentStatRangeDto);
                if (world.Has<Hits>(id))
                {
                    ref var h = ref world.Get<Hits>(id);
                    hp = new AgentStatRangeDto(h.Value, h.MaxValue);
                }

                // TODO(agent): per-mobile name and notoriety are not stored
                // as components on impl/ecs (only read off the wire).
                results.Add(new AgentNearbyMobileDto(
                    Serial: serial.Value,
                    Name: string.Empty,
                    Graphic: graphic,
                    Hue: hue,
                    Position: new AgentPositionDto(wp.X, wp.Y, wp.Z, gameCtx.Map),
                    Notoriety: 0,
                    Hp: hp));
            }
        }

        return results;
    }
}

#endif
