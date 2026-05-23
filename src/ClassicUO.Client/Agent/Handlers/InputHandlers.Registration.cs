// SPDX-License-Identifier: BSD-2-Clause
//
// Partial-method implementation that wires input.* handlers into the
// AgentDispatcher route table. Verb names match RpcVerbs.cs.

#if AGENT_BUILD
#nullable enable

using System.Collections.Generic;
using ClassicUO.Agent.Agent.Handlers;
using ClassicUO.Agent.Contracts;

namespace ClassicUO.Agent;

internal static partial class AgentDispatcher
{
    static partial void RegisterInputRoutes(Dictionary<string, RpcHandler> routes)
    {
        routes["input.mouseMove"]        = (req, in ctx) => InputHandlers.MouseMove(req, in ctx);
        routes["input.mouseDown"]        = (req, in ctx) => InputHandlers.MouseDown(req, in ctx);
        routes["input.mouseUp"]          = (req, in ctx) => InputHandlers.MouseUp(req, in ctx);
        routes["input.mouseClick"]       = (req, in ctx) => InputHandlers.MouseClick(req, in ctx);
        routes["input.mouseDoubleClick"] = (req, in ctx) => InputHandlers.MouseDoubleClick(req, in ctx);
        routes["input.mouseHold"]        = (req, in ctx) => InputHandlers.MouseHold(req, in ctx);
        routes["input.mouseRelease"]     = (req, in ctx) => InputHandlers.MouseRelease(req, in ctx);
        routes["input.clear"]            = (req, in ctx) => InputHandlers.InputClear(req, in ctx);
    }
}

#endif
