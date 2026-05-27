// SPDX-License-Identifier: BSD-2-Clause
//
// Per-frame system that drains the inbox and routes each request to a
// handler. Runs on the game thread, so handlers may freely touch ECS
// world state and managers — they just can't block.
//
// Adding a new verb: drop a partial implementation of one of the
// Register*Routes(Dictionary<string, RpcHandler>) hooks below into a
// separate file under Agent/Handlers/. Each Register*Routes method is
// declared here as a `static partial void` so its existence is optional;
// the compiler elides any partial whose implementation does not exist
// (no runtime cost, no link error). This file therefore never needs to
// change when new verbs are added.
//
// Handler signature is (JsonRpcRequest, in AgentRpcContext) =>
// JsonRpcResponse. AgentRpcContext is a readonly ref struct that bundles
// the AgentServerState plus the ECS bits handlers most often want
// (World, GameContext, Settings, OnLoginRequest writer). Handlers that
// need additional resources should extend AgentRpcContext rather than
// re-fetching from the ECS world directly.

#if AGENT_BUILD
#nullable enable

using System;
using System.Collections.Generic;
using ClassicUO.Agent.Contracts;
using ClassicUO.Configuration;
using ClassicUO.Ecs;
using ClassicUO.Network;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Agent;

internal static partial class AgentDispatcher
{
    // Handlers may return null to defer the response: another system
    // (e.g. ServiceCaptureRequest after the frame is rendered) will write
    // the JsonRpcResponse directly to AgentServerState.Outbox with the
    // original request id. DrainInbox skips its outbox write on null.
    internal delegate JsonRpcResponse? RpcHandler(JsonRpcRequest req, in AgentRpcContext ctx);

    private static readonly Dictionary<string, RpcHandler> Routes = BuildRoutes();

    private static Dictionary<string, RpcHandler> BuildRoutes()
    {
        var routes = new Dictionary<string, RpcHandler>();
        RegisterLifecycleRoutes(routes);
        RegisterAgentLoginRoutes(routes);
        RegisterWorldRoutes(routes);
        RegisterInputRoutes(routes);
        RegisterGumpRoutes(routes);
        RegisterCaptureRoutes(routes);
        return routes;
    }

    // Each category gets its own partial-method hook. Handler files
    // implement the ones they own; unimplemented hooks are elided by
    // the compiler.
    static partial void RegisterLifecycleRoutes(Dictionary<string, RpcHandler> routes);
    static partial void RegisterAgentLoginRoutes(Dictionary<string, RpcHandler> routes);
    static partial void RegisterWorldRoutes(Dictionary<string, RpcHandler> routes);
    static partial void RegisterInputRoutes(Dictionary<string, RpcHandler> routes);
    static partial void RegisterGumpRoutes(Dictionary<string, RpcHandler> routes);
    static partial void RegisterCaptureRoutes(Dictionary<string, RpcHandler> routes);

    public static void DrainInbox(
        Res<AgentServerState> stateRes,
        World world,
        Res<GameContext> gameCtx,
        Res<Settings> settings,
        Commands commands,
        MouseContext mouseCtx,
        NetClient network)
    {
        var state = stateRes.Value!;
        var ctx = new AgentRpcContext(state, world, gameCtx, settings, commands, mouseCtx, network);

        while (state.Inbox.Reader.TryRead(out var req))
        {
            JsonRpcResponse? resp;
            try
            {
                if (Routes.TryGetValue(req.Method, out var handler))
                {
                    resp = handler(req, in ctx);
                }
                else
                {
                    resp = AgentServer.ErrorResponse(
                        req.Id,
                        JsonRpcErrorCodes.MethodNotFound,
                        $"unknown method '{req.Method}'");
                }
            }
            catch (Exception ex)
            {
                resp = AgentServer.ErrorResponse(
                    req.Id,
                    JsonRpcErrorCodes.InternalError,
                    ex.Message);
            }

            // Null response means the handler deferred — another system
            // will write the JsonRpcResponse to the outbox later (this
            // frame or a future frame). Skip the outbox write here.
            if (resp is not null)
            {
                state.Outbox.Writer.TryWrite(resp);
            }
        }
    }
}

#endif
