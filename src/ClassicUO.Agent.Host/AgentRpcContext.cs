// SPDX-License-Identifier: BSD-2-Clause
//
// Per-call context handed to every RPC handler. Built once per
// DrainInbox tick from the per-runtime bootstrap and passed by `in` to
// each handler so handlers can:
//
//   * read/write the channels and connection slot on AgentServerState,
//   * reach into runtime-specific state via the TRuntime field
//     (GameController on the OOP runtime, TinyEcs.Bevy App on the ECS one).
//
// Modeled as a `readonly ref struct` so handlers cannot accidentally
// capture it into a lambda, field, or async continuation — the context
// is only valid for the synchronous duration of a single DrainInbox
// iteration. Delegates take it as `in AgentRpcContext<TRuntime>` which is
// fine: the delegate signature references the ref struct but does not
// store it.
//
// Handlers must remain synchronous and non-blocking — they run inside
// the engine's per-frame update on the game thread.

#nullable enable

namespace ClassicUO.Agent.Host;

public readonly ref struct AgentRpcContext<TRuntime> where TRuntime : class
{
    public readonly AgentServerState State;
    public readonly TRuntime Runtime;

    public AgentRpcContext(AgentServerState state, TRuntime runtime)
    {
        State = state;
        Runtime = runtime;
    }
}
