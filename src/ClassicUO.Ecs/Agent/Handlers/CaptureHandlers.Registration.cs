// SPDX-License-Identifier: BSD-2-Clause
//
// Partial-method implementation that wires capture.* handlers into the
// AgentDispatcher route table.

#if AGENT_BUILD
#nullable enable

using System.Collections.Generic;
using ClassicUO.Agent.Agent.Handlers;
using ClassicUO.Agent.Contracts;

namespace ClassicUO.Agent;

internal static partial class AgentDispatcher
{
    static partial void RegisterCaptureRoutes(Dictionary<string, RpcHandler> routes)
    {
        routes[RpcVerbs.CaptureShot] = (req, in ctx) => CaptureHandlers.Shot(req, in ctx);
    }
}

#endif
