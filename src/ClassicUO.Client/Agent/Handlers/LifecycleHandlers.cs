// SPDX-License-Identifier: BSD-2-Clause
//
// Lifecycle verbs: ping (sanity check), ready (set by the engine once
// asset loading + first frame complete; CLI polls this during `up`),
// inWorld (whether the player has past character select), shutdown
// (graceful exit; engine teardown happens on the next frame).
//
// Routes are wired in via a partial implementation of
// AgentDispatcher.RegisterLifecycleRoutes — see bottom of this file.

#if AGENT_BUILD
#nullable enable

using System.Collections.Generic;
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

    // inWorld: true once the player serial has been assigned by the
    // server (i.e. we're past character select and into the game world).
    // PlayerSerial == 0 covers both the pre-connect state and the
    // login/charlist screens.
    public static JsonRpcResponse InWorld(JsonRpcRequest req, in AgentRpcContext ctx) => new()
    {
        Id = req.Id,
        Result = new JsonObject
        {
            ["inWorld"] = ctx.GameCtx.Value!.PlayerSerial != 0,
        },
    };

    // TODO: Ready, Shutdown.
}

#endif
