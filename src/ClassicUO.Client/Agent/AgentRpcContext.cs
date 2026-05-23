// SPDX-License-Identifier: BSD-2-Clause
//
// Per-call context handed to every AgentDispatcher handler on the main
// (pre-ECS) branch. Built once per inbox-drain tick from the running
// GameController and passed by `in` to each handler so handlers can:
//
//   * read/write the channels and connection slot on AgentServerState,
//   * touch the live game World via GameController.UO.World,
//   * fetch GraphicsDevice / Scene etc. via the GameController instance.
//
// Modeled as a `readonly ref struct` so handlers cannot accidentally
// capture it into a lambda, field, or async continuation — the context
// is only valid for the synchronous duration of a single DrainInbox
// iteration. Delegates take it as `in AgentRpcContext` which is fine:
// the delegate signature references the ref struct but does not store it.
//
// Handlers must remain synchronous and non-blocking — they run inside
// GameController.Update on the game thread.

#if AGENT_BUILD
#nullable enable

namespace ClassicUO.Agent;

internal readonly ref struct AgentRpcContext
{
    public readonly AgentServerState State;
    public readonly GameController Game;

    public AgentRpcContext(AgentServerState state, GameController game)
    {
        State = state;
        Game = game;
    }
}

#endif
