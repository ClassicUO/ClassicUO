// SPDX-License-Identifier: BSD-2-Clause
//
// Custom ISystemParam that exposes the TinyEcs World to systems. The
// generic AddSystem<T1..TN> extensions constrain every type parameter to
// ISystemParam (with a default ctor), so passing the raw `World` to a
// system function fails to bind. WorldParam wraps the World pointer in
// a system param so handlers like AgentDispatcher.DrainInbox can still
// query the ECS world directly without going through Commands.
//
// Access: no resource reads/writes (an empty SystemParamAccess) — World
// itself is the shared ECS state, not a tracked resource.

#if AGENT_BUILD
#nullable enable

using System.Collections.Generic;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Agent;

internal sealed class WorldParam : ISystemParam
{
    public World World { get; private set; } = null!;

    public void Initialize(App app) => World = app.GetWorld();

    public void Fetch(App app) => World = app.GetWorld();

    public SystemParamAccess GetAccess() => new();
}

#endif
