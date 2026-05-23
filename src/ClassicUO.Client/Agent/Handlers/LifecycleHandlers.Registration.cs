// SPDX-License-Identifier: BSD-2-Clause
//
// Partial-method implementation that wires the lifecycle handlers into
// AgentDispatcher.Routes. Adding/removing a lifecycle verb is a one-line
// change here; the dispatcher core does not need to know about it.

#if AGENT_BUILD
#nullable enable

using System.Collections.Generic;
using ClassicUO.Agent.Agent.Handlers;
using ClassicUO.Agent.Contracts;

namespace ClassicUO.Agent;

internal static partial class AgentDispatcher
{
    static partial void RegisterLifecycleRoutes(Dictionary<string, RpcHandler> routes)
    {
        routes[RpcVerbs.LifecyclePing] = LifecycleHandlers.Ping;
        routes[RpcVerbs.LifecycleInWorld] = LifecycleHandlers.InWorld;
    }
}

#endif
