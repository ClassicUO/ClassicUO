// SPDX-License-Identifier: BSD-2-Clause
//
// Per-frame request router. The per-runtime bootstrap holds an instance
// of AgentDispatcher<TRuntime>, registers handlers at startup, and calls
// DrainInbox(state, runtime) once per tick to route any queued RPC
// requests.
//
// Adding a new verb: add a public static class under the consumer's
// Agent/Handlers/ directory with a `Register(AgentDispatcher<TRuntime>)`
// method, then call it from the bootstrap's startup. The dispatcher
// itself is verb-agnostic.
//
// Handler signature is (JsonRpcRequest, in AgentRpcContext<TRuntime>) =>
// JsonRpcResponse?. Returning null defers the response — another
// engine-side path will write a JsonRpcResponse with the same request
// id directly to AgentServerState.Outbox (e.g. capture.shot waits for
// the next Draw frame).

#nullable enable

using System;
using System.Collections.Generic;
using ClassicUO.Agent.Contracts;

namespace ClassicUO.Agent.Host;

public delegate JsonRpcResponse? RpcHandler<TRuntime>(JsonRpcRequest req, in AgentRpcContext<TRuntime> ctx)
    where TRuntime : class;

public sealed class AgentDispatcher<TRuntime> where TRuntime : class
{
    private readonly Dictionary<string, RpcHandler<TRuntime>> _routes = new();

    public void Register(string verb, RpcHandler<TRuntime> handler) => _routes[verb] = handler;

    public void DrainInbox(AgentServerState state, TRuntime runtime)
    {
        var ctx = new AgentRpcContext<TRuntime>(state, runtime);

        while (state.Inbox.Reader.TryRead(out var req))
        {
            JsonRpcResponse? resp;
            try
            {
                if (_routes.TryGetValue(req.Method, out var handler))
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
