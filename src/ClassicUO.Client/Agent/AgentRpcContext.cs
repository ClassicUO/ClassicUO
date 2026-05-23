// SPDX-License-Identifier: BSD-2-Clause
//
// Per-call context handed to every AgentDispatcher handler. Built once
// per DrainInbox tick from the surrounding ECS system parameters and
// passed by `in` to each handler so handlers can:
//
//   * read/write the channels and connection slot on AgentServerState,
//   * touch ECS world state (entities, components, queries),
//   * read engine resources (GameContext, Settings),
//   * raise engine-side events (e.g. emit an OnLoginRequest trigger).
//
// Modeled as a `readonly ref struct` so handlers cannot accidentally
// capture it into a lambda, field, or async continuation — the context
// is only valid for the synchronous duration of a single DrainInbox
// iteration. Delegates take it as `in AgentRpcContext` which is fine:
// the delegate signature references the ref struct but does not store it.
//
// Handlers must remain synchronous and non-blocking. ECS state changes
// route through Commands, which TinyEcs.Bevy flushes after the system
// completes — observer-style triggers like OnLoginRequest therefore fire
// the same frame, with no inbox/outbox ordering hazard.

#if AGENT_BUILD
#nullable enable

using ClassicUO.Configuration;
using ClassicUO.Ecs;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Agent;

internal readonly ref struct AgentRpcContext
{
    public readonly AgentServerState State;
    public readonly World World;
    public readonly Res<GameContext> GameCtx;
    public readonly Res<Settings> Settings;
    public readonly Commands Commands;
    public readonly MouseContext MouseCtx;

    public AgentRpcContext(
        AgentServerState state,
        World world,
        Res<GameContext> gameCtx,
        Res<Settings> settings,
        Commands commands,
        MouseContext mouseCtx)
    {
        State = state;
        World = world;
        GameCtx = gameCtx;
        Settings = settings;
        Commands = commands;
        MouseCtx = mouseCtx;
    }
}

#endif
