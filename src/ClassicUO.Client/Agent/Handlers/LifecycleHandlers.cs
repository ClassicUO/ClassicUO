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

    // High-level state string. On main this is derived from the active
    // Scene type plus the LoginScene's sub-step machine. ECS branch
    // returns the same alphabet from GameState enum names so callers
    // can target either client with one poll.
    //
    // Returns: { state: "..." }
    //   GameScreen          — in world (Scene is GameScene)
    //   ServerSelection     — LoginScene.CurrentLoginStep is ServerSelection
    //   CharacterSelection  — LoginScene.CurrentLoginStep is CharacterSelection
    //   LoginScreen         — LoginScene.CurrentLoginStep is Main
    //   <other LoginStep names> verbatim (Connecting, VerifyingAccount, ...)
    //   <other Scene names>      verbatim (MainScene, ...)
    public static JsonRpcResponse GameState(JsonRpcRequest req, in AgentRpcContext ctx)
    {
        var scene = ctx.Game.Scene;
        string state;
        if (scene is ClassicUO.Game.Scenes.GameScene)
        {
            state = "GameScreen";
        }
        else if (scene is ClassicUO.Game.Scenes.LoginScene login)
        {
            state = login.CurrentLoginStep switch
            {
                ClassicUO.Game.Scenes.LoginSteps.Main => "LoginScreen",
                _ => login.CurrentLoginStep.ToString(),
            };
        }
        else
        {
            state = scene?.GetType().Name ?? "Unknown";
        }
        return new JsonRpcResponse
        {
            Id = req.Id,
            Result = new JsonObject { ["state"] = state },
        };
    }

    // Character list cached on LoginScene once the server replies with
    // CharacterListInfo. Slots that weren't created on the account show
    // up as empty strings; we drop them so callers see only real
    // characters. Returns empty array when the LoginScene hasn't received
    // the list yet (e.g. still on login screen / not connected).
    //
    // Returns: { characters: ["name1", "name2", ...] }
    public static JsonRpcResponse Characters(JsonRpcRequest req, in AgentRpcContext ctx)
    {
        // Manual JSON build mirrors the ECS branch — AgentJsonContext
        // source-gen does not register System.String for AOT, so a
        // JsonArray<string> serialization would throw NoMetadataForType.
        var names = (ctx.Game.Scene as ClassicUO.Game.Scenes.LoginScene)?.Characters
                    ?? System.Array.Empty<string>();
        var sb = new System.Text.StringBuilder();
        sb.Append('[');
        var first = true;
        foreach (var n in names)
        {
            if (string.IsNullOrEmpty(n)) continue;
            if (!first) sb.Append(',');
            first = false;
            sb.Append('"');
            foreach (var c in n)
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
}

#endif
