// SPDX-License-Identifier: BSD-2-Clause
//
// Custom ISystemParam that exposes the TinyEcs World plus the owning App
// to a system. The generic AddSystem<T1..TN> extensions constrain every
// type parameter to ISystemParam (with a default ctor), so passing the
// raw `World` or `App` to a system function fails to bind. AppParam wraps
// both into a single system param so AgentServerPlugin.DrainInboxSystem
// can forward the App to AgentDispatcher<App>.DrainInbox(state, app).
//
// Access: no resource reads/writes (an empty SystemParamAccess) — World
// and App are the shared engine state, not tracked resources.

#if AGENT_BUILD
#nullable enable

using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Agent;

internal sealed class AppParam : ISystemParam
{
    public App App { get; private set; } = null!;
    public World World { get; private set; } = null!;

    public void Initialize(App app)
    {
        App = app;
        World = app.GetWorld();
    }

    public void Fetch(App app)
    {
        App = app;
        World = app.GetWorld();
    }

    public SystemParamAccess GetAccess() => new();
}

#endif
