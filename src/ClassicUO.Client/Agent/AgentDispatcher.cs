// SPDX-License-Identifier: BSD-2-Clause
//
// Per-frame entry point that drains the inbox and routes each request
// to a handler. Called from GameController.Update on the game thread,
// so handlers may freely touch World / Scene / UIManager state — they
// just can't block.
//
// Adding a new verb: drop a partial implementation of one of the
// Register*Routes(Dictionary<string, RpcHandler>) hooks below into a
// separate file under Agent/Handlers/. Each Register*Routes method is
// declared here as a `static partial void` so its existence is optional;
// the compiler elides any partial whose implementation does not exist.
//
// Handler signature is (JsonRpcRequest, in AgentRpcContext) =>
// JsonRpcResponse?. Returning null defers the response — another
// engine-side path will write a JsonRpcResponse with the same request
// id directly to AgentServerState.Outbox (e.g. capture.shot waits for
// the next Draw frame).

#if AGENT_BUILD
#nullable enable

using System;
using System.Collections.Generic;
using ClassicUO.Agent.Contracts;

namespace ClassicUO.Agent;

internal static partial class AgentDispatcher
{
    internal delegate JsonRpcResponse? RpcHandler(JsonRpcRequest req, in AgentRpcContext ctx);

    private static readonly Dictionary<string, RpcHandler> Routes = BuildRoutes();

    private static Dictionary<string, RpcHandler> BuildRoutes()
    {
        var routes = new Dictionary<string, RpcHandler>();
        RegisterLifecycleRoutes(routes);
        RegisterInputRoutes(routes);
        RegisterCaptureRoutes(routes);
        return routes;
    }

    static partial void RegisterLifecycleRoutes(Dictionary<string, RpcHandler> routes);
    static partial void RegisterInputRoutes(Dictionary<string, RpcHandler> routes);
    static partial void RegisterCaptureRoutes(Dictionary<string, RpcHandler> routes);

    public static void DrainInbox(AgentServerState state, GameController game)
    {
        var ctx = new AgentRpcContext(state, game);

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

            if (resp is not null)
            {
                state.Outbox.Writer.TryWrite(resp);
            }
        }
    }
}

#endif
