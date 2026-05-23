// SPDX-License-Identifier: BSD-2-Clause
//
// Lifecycle verbs on the main (pre-ECS) branch: ping (sanity), inWorld
// (whether the player is past character select), shutdown (TODO).
// Player presence is checked off GameController.UO.World.Player on this
// branch; the ECS branch uses GameContext.PlayerSerial instead.

#if AGENT_BUILD
#nullable enable

using System.Text.Json.Nodes;
using ClassicUO.Agent.Contracts;

namespace ClassicUO.Agent.Agent.Handlers;

internal static class LifecycleHandlers
{
    public static JsonRpcResponse Ping(JsonRpcRequest req, in AgentRpcContext ctx) => new()
    {
        Id = req.Id,
        Result = new JsonObject
        {
            ["pong"] = true,
            ["port"] = ctx.State.Port,
        },
    };

    public static JsonRpcResponse InWorld(JsonRpcRequest req, in AgentRpcContext ctx) => new()
    {
        Id = req.Id,
        Result = new JsonObject
        {
            ["inWorld"] = ctx.Game.UO.World.InGame,
        },
    };
}

#endif
