// SPDX-License-Identifier: BSD-2-Clause
//
// Canonical RPC verb names. String constants live here so both ends of the
// wire (the client-side dispatcher and the CLI-side caller) can reference
// them without typos. Adding a verb here doesn't implement it — that
// happens in src/ClassicUO.Client/Agent/Handlers/.

namespace ClassicUO.Agent.Contracts;

public static class RpcVerbs
{
    // Lifecycle: connection sanity, ready signaling, shutdown.
    public const string LifecyclePing = "lifecycle.ping";
    public const string LifecycleReady = "lifecycle.ready";
    public const string LifecycleInWorld = "lifecycle.inWorld";
    public const string LifecycleShutdown = "lifecycle.shutdown";

    // World inspection. Pure reads against the ECS world from the game thread.
    public const string WorldDumpState = "world.dumpState";
    public const string WorldGetPlayer = "world.getPlayer";

    // Input. Synthesizes engine-side events; bypasses real keyboard/mouse.
    public const string InputWalk = "input.walk";
    public const string InputSendKey = "input.sendKey";
    public const string InputTarget = "input.target";

    // Login. Bypasses the GUI and emits OnLoginRequest directly.
    public const string AgentLogin = "agent.login";

    // Event subscription. Client subscribes to a named event stream
    // (journalEntry, equipmentChanged, ...); server pushes via the
    // event envelope (no `id` field) until the connection drops.
    public const string EventsSubscribe = "events.subscribe";
    public const string EventsUnsubscribe = "events.unsubscribe";

    // Gump tree introspection.
    public const string GumpTree = "gump.tree";
    public const string GumpDump = "gump.dump";
    public const string GumpClose = "gump.close";

    // Screen capture: PNG shots and MP4 recordings.
    public const string CaptureShot = "capture.shot";
    public const string CaptureRecordStart = "capture.recordStart";
    public const string CaptureRecordStop = "capture.recordStop";
}
