// SPDX-License-Identifier: BSD-2-Clause
//
// Partial-method implementation that wires the agent.login handler into
// AgentDispatcher.Routes. See AgentDispatcher.cs for the pattern.

#if AGENT_BUILD
#nullable enable

using System.Collections.Generic;
using ClassicUO.Agent.Agent.Handlers;
using ClassicUO.Agent.Contracts;

namespace ClassicUO.Agent;

internal static partial class AgentDispatcher
{
    static partial void RegisterAgentLoginRoutes(Dictionary<string, RpcHandler> routes)
    {
        routes[RpcVerbs.AgentLogin] = AgentLoginHandlers.Login;
    }
}

#endif
