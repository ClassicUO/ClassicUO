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

    // High-level state machine current value, updated each frame by
    // AgentServerPlugin.TrackGameStateSystem. Useful for scenarios that
    // need to gate on ServerSelection / CharacterSelection / GameScreen
    // without polling the engine directly.
    public static JsonRpcResponse GameState(JsonRpcRequest req, in AgentRpcContext ctx) => new()
    {
        Id = req.Id,
        Result = new JsonObject
        {
            ["state"] = ctx.State.CurrentGameState,
        },
    };

    // Cached character list from the most recent CharacterSelectionInfoEvent.
    // Empty until the first agent.login completes server selection.
    public static JsonRpcResponse Characters(JsonRpcRequest req, in AgentRpcContext ctx)
    {
        // Manual JSON build: AgentJsonContext source-gen does not register
        // System.String for AOT, so JsonArray<string> serialization throws
        // NoMetadataForType. Build the JSON literal by hand and parse it
        // back to a JsonNode so the existing JsonRpcResponse serializer
        // can pass it through verbatim.
        var names = ctx.State.LastCharacterNames ?? System.Array.Empty<string>();
        var sb = new System.Text.StringBuilder();
        sb.Append('[');
        for (var i = 0; i < names.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append('"');
            foreach (var c in names[i])
            {
                if (c == '\\' || c == '"') sb.Append('\\').Append(c);
                else if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                else sb.Append(c);
            }
            sb.Append('"');
        }
        sb.Append(']');
        return new JsonRpcResponse
        {
            Id = req.Id,
            Result = new JsonObject { ["characters"] = JsonNode.Parse(sb.ToString()) },
        };
    }

    // TODO: Ready, Shutdown.
}

#endif
