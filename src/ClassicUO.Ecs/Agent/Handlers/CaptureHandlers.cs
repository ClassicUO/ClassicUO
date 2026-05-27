// SPDX-License-Identifier: BSD-2-Clause
//
// capture.shot: queue a backbuffer screenshot. Response is deferred —
// the handler returns null after enqueueing the request, and
// ServiceCaptureRequestSystem at Stage.PostUpdate writes the actual
// JsonRpcResponse to the outbox after reading the backbuffer.
//
// Params: { path } — server-side filesystem path to write the PNG to.
// The full path is returned in the response result.

#if AGENT_BUILD
#nullable enable

using System.Text.Json;
using ClassicUO.Agent.Contracts;
using ClassicUO.Agent.Host;
using TinyEcs.Bevy;

namespace ClassicUO.Agent.Agent.Handlers;

internal static class CaptureHandlers
{
    public static void Register(AgentDispatcher<App> d)
    {
        d.Register(RpcVerbs.CaptureShot, Shot);
    }

    public static JsonRpcResponse? Shot(JsonRpcRequest req, in AgentRpcContext<App> ctx)
    {
        if (req.Params is not JsonElement p || p.ValueKind != JsonValueKind.Object)
        {
            return AgentServer.ErrorResponse(req.Id, JsonRpcErrorCodes.InvalidParams,
                "capture.shot expects { path }");
        }

        if (!p.TryGetProperty("path", out var pathEl) ||
            pathEl.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(pathEl.GetString()))
        {
            return AgentServer.ErrorResponse(req.Id, JsonRpcErrorCodes.InvalidParams,
                "capture.shot: 'path' is required and must be a non-empty string");
        }

        if (ctx.State.PendingCapture is not null)
        {
            return AgentServer.ErrorResponse(req.Id, JsonRpcErrorCodes.CaptureUnavailable,
                "a capture request is already in flight; retry after it completes");
        }

        ctx.State.PendingCapture = new CaptureRequest
        {
            RequestId = req.Id,
            OutPath = pathEl.GetString(),
        };

        // Defer the response — ServiceCaptureRequestSystem will write the
        // JsonRpcResponse for this request id once the backbuffer has
        // been read and encoded.
        return null;
    }
}

#endif
