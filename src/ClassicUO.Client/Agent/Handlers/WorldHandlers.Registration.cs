// SPDX-License-Identifier: BSD-2-Clause
//
// Wires world.* verbs into AgentDispatcher.Routes.

#if AGENT_BUILD
#nullable enable

using System.Collections.Generic;
using ClassicUO.Agent.Agent.Handlers;
using ClassicUO.Agent.Contracts;

namespace ClassicUO.Agent;

internal static partial class AgentDispatcher
{
    static partial void RegisterWorldRoutes(Dictionary<string, RpcHandler> routes)
    {
        routes[RpcVerbs.WorldDumpState] = WorldHandlers.DumpState;
    }
}

#endif
