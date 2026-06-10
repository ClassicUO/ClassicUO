// SPDX-License-Identifier: BSD-2-Clause
//
// Agent dev-loop server, ECS plugin entry point. Wired into the App
// from src/ClassicUO.Ecs/Boot.cs only when the AGENT_BUILD constant is
// defined (see ClassicUO.Agent.Settings.props).
//
// Split of execution between threads:
//   1. AgentServer (background Task started during plugin Build) owns the
//      TcpListener, reads JSON-RPC frames, pushes requests into the inbox
//      Channel, drains the outbox Channel and writes responses/events back.
//   2. DrainInbox system (per-frame, on the game thread) reads from inbox,
//      routes via AgentDispatcher<App> to handlers that touch ECS state
//      via the App resource bag and the underlying World, and enqueues
//      responses to outbox.
//
// All engine reads happen on the game thread. The background thread only
// ever touches the Channels and the socket.

#if AGENT_BUILD
#nullable enable

using System;
using ClassicUO.Agent.Agent.Handlers;
using ClassicUO.Agent.Host;
using ClassicUO.Configuration;
using ClassicUO.Ecs;
using ClassicUO.Network;
using Microsoft.Xna.Framework.Graphics;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.Input;
using TinyEcs.Bevy.UI;

namespace ClassicUO.Agent;

internal readonly struct AgentServerPlugin : IPlugin
{
    // Capture must run after the UI is drawn (UiRenderStage) but before
    // Present (Stage.Last) so the backbuffer still holds the rendered
    // frame. PostUpdate alone is not late enough — the world is rendered
    // there, but the UI runs in UiRenderStage which comes after PostUpdate.
    public static readonly Stage AgentCaptureStage = Stage.Custom("AgentCapture");

    // Per-runtime dispatcher instance. Handler registration runs once
    // during plugin Build; DrainInboxSystem then calls Dispatcher.DrainInbox
    // each frame. Static-on-the-plugin-struct keeps the dispatcher reachable
    // from both Build (registers routes) and the per-frame system without
    // threading it through an additional resource.
    internal static readonly AgentDispatcher<App> Dispatcher = new();
    private static bool s_routesRegistered;

    public void Build(App app)
    {
        app.AddStage(AgentCaptureStage)
            .After(UiPlugin.UiRenderStage)
            .Before(Stage.Last);

        var state = new AgentServerState();
        app.AddResource(state);

        // Wire every *Handlers.Register(Dispatcher) once. Plugin Build can
        // be called multiple times in test rigs that build several Apps
        // back-to-back; the dispatcher is process-static so a second call
        // would double-register.
        if (!s_routesRegistered)
        {
            LifecycleHandlers.Register(Dispatcher);
            AgentLoginHandlers.Register(Dispatcher);
            WorldHandlers.Register(Dispatcher);
            InputHandlers.Register(Dispatcher);
            CaptureHandlers.Register(Dispatcher);
            s_routesRegistered = true;
        }

        // Start the listener IMMEDIATELY during plugin registration so the
        // socket binds before any other plugin's Stage.Startup system can
        // crash. This lets the CLI ping the listener even when downstream
        // engine initialisation (assets, network, etc.) blows up — the
        // ping fast-path in ReadFramesAsync answers without the dispatcher.
        AgentServer.StartAcceptLoop(state);

        // DrainInbox runs on the game thread. Stage.First so handlers and
        // any downstream observer triggers they emit (e.g. agent.login →
        // OnLoginRequest → HandleLoginRequests) fire before gameplay
        // systems read state this frame. SingleThreaded because handlers
        // mutate ECS state through the world directly.
        var drainFn = DrainInboxSystem;
        app.AddSystem(drainFn)
            .InStage(Stage.First)
            .SingleThreaded()
            .Build();

        // Stage.First: drain one frame of synthetic mouse state into
        // MouseContext. Sits in the same stage as MouseContext.Update; the
        // worst-case one-frame ordering lag is acceptable because input
        // sequences are paced one frame each anyway.
        var advanceMouseFn = AdvanceSyntheticMouseSystem;
        app.AddSystem(advanceMouseFn)
            .InStage(Stage.First)
            .SingleThreaded()
            .Build();

        // Stage.PostUpdate: after game.Tick has Drawn the frame and before
        // Present (Stage.Last), service any pending screenshot request by
        // reading the backbuffer and writing a JsonRpcResponse with the
        // original request id directly to the outbox.
        var captureFn = ServiceCaptureRequestSystem;
        app.AddSystem(captureFn)
            .InStage(AgentCaptureStage)
            .SingleThreaded()
            .Build();

        // Drain queued synthetic-keyboard text into CharInput so
        // gameplay subscribers (chat, login-screen text input) see the
        // chars on the same channel real keyboard input uses.
        Action<Res<AgentServerState>, EventWriter<CharInput>> drainTypedFn = DrainTypedCharsSystem;
        app.AddSystem(drainTypedFn)
            .InStage(Stage.First)
            .SingleThreaded()
            .Build();

        // Auto-progress through ServerSelection / CharacterSelection after
        // agent.login when state.AutoLoginActive is set. Sends the same
        // Send_SelectServer / Send_SelectCharacter the UI clicks emit.
        Action<Res<AgentServerState>, EventReader<ServerSelectionInfoEvent>, Res<NetClient>>
            serverSelFn = AutoServerSelectSystem;
        app.AddSystem(serverSelFn)
            .InStage(Stage.Update)
            .RunIf((EventReader<ServerSelectionInfoEvent> r) => r.HasEvents)
            .SingleThreaded()
            .Build();

        Action<Res<AgentServerState>, EventReader<CharacterSelectionInfoEvent>,
            Res<NetClient>, Res<GameContext>> charSelFn = AutoCharacterSelectSystem;
        app.AddSystem(charSelFn)
            .InStage(Stage.Update)
            .RunIf((EventReader<CharacterSelectionInfoEvent> r) => r.HasEvents)
            .SingleThreaded()
            .Build();

        // Track current high-level GameState so lifecycle.gameState can
        // return a stable string without poking ECS internals from the
        // handler thread.
        Action<Res<AgentServerState>, Res<State<GameState>>> trackStateFn = TrackGameStateSystem;
        app.AddSystem(trackStateFn)
            .InStage(Stage.Last)
            .SingleThreaded()
            .Build();

        // Stage.Last: flush responses after handler systems and any
        // engine-side event emitters have written to the outbox this tick.
        Action<Res<AgentServerState>> flushFn = static s => AgentServer.FlushOutbox(s.Value!);
        app.AddSystem(flushFn)
            .InStage(Stage.Last)
            .SingleThreaded()
            .Build();
    }

    private static void AdvanceSyntheticMouseSystem(
        Res<AgentServerState> stateRes,
        Res<MouseContext> mouseCtxRes)
    {
        var state = stateRes.Value!;
        var mouseCtx = mouseCtxRes.Value!;
        // Drain one queued frame per tick so press/release transitions
        // are visible across MouseContext.Update's oldState/newState
        // diff. When the queue is empty hold the last applied state.
        if (state.PendingMouseFrames.Count > 0)
        {
            state.CurrentMouseSynth = state.PendingMouseFrames.Dequeue();
            var c = state.CurrentMouseSynth;
            mouseCtx.AgentSetSynthetic(c.X, c.Y, c.Left, c.Middle, c.Right);
            if (c.Wheel != 0)
                mouseCtx.AgentAddSyntheticWheel(c.Wheel);
        }
        else if (mouseCtx.AgentSyntheticActive)
        {
            // Re-apply the held state every frame. Avoids drift if some
            // other system inadvertently flips _agentSynthEnabled off
            // between ticks (defensive; nothing currently does this).
            var c = state.CurrentMouseSynth;
            mouseCtx.AgentSetSynthetic(c.X, c.Y, c.Left, c.Middle, c.Right);
        }
    }

    private static void DrainTypedCharsSystem(
        Res<AgentServerState> stateRes,
        EventWriter<CharInput> writer)
    {
        var state = stateRes.Value!;
        while (state.PendingTypedChars.Count > 0)
        {
            var ch = state.PendingTypedChars.Dequeue();
            writer.Send(new CharInput { Value = ch });
        }
    }

    private static void ServiceCaptureRequestSystem(
        Res<AgentServerState> stateRes,
        Res<GraphicsDevice> deviceRes)
    {
        var state = stateRes.Value!;
        var req = state.PendingCapture;
        if (req is null) return;
        state.PendingCapture = null;

        var resp = AgentCaptureService.Run(deviceRes.Value!, req);
        state.Outbox.Writer.TryWrite(resp);
    }

    private static void AutoServerSelectSystem(
        Res<AgentServerState> stateRes,
        EventReader<ServerSelectionInfoEvent> reader,
        Res<NetClient> net)
    {
        var state = stateRes.Value!;
        foreach (var ev in reader.Read())
        {
            state.LastServerCount = ev.Servers?.Count ?? 0;
            if (!state.AutoLoginActive) continue;
            if (state.LastServerCount == 0) continue;
            var idx = state.AutoServerIndex;
            if (idx < 0 || idx >= state.LastServerCount) idx = 0;
            net.Value.Send_SelectServer((byte)idx);
        }
    }

    private static void AutoCharacterSelectSystem(
        Res<AgentServerState> stateRes,
        EventReader<CharacterSelectionInfoEvent> reader,
        Res<NetClient> net,
        Res<GameContext> gameCtx)
    {
        var state = stateRes.Value!;
        foreach (var ev in reader.Read())
        {
            var chars = ev.Characters;
            if (chars is null || chars.Count == 0)
            {
                state.LastCharacterNames = Array.Empty<string>();
                continue;
            }

            var names = new string[chars.Count];
            for (var i = 0; i < chars.Count; i++) names[i] = chars[i].Name;
            state.LastCharacterNames = names;

            if (!state.AutoLoginActive) continue;
            var idx = state.AutoCharacterIndex;
            if (idx < 0 || idx >= chars.Count) idx = 0;
            net.Value.Send_SelectCharacter(
                chars[idx].Index, chars[idx].Name,
                net.Value.LocalIP, gameCtx.Value.Protocol);
            // One-shot: clear so a subsequent login flow can opt out.
            state.AutoLoginActive = false;
        }
    }

    private static void TrackGameStateSystem(
        Res<AgentServerState> stateRes,
        Res<State<GameState>> state)
    {
        var s = stateRes.Value!;
        var name = state.Value.Current.ToString();
        if (s.CurrentGameState != name) s.CurrentGameState = name;
    }

    // The dispatcher takes the App as its TRuntime, so we capture the App
    // from the AppParam system param. AppParam exposes the engine's App
    // instance (TinyEcs.Bevy injects it automatically as an ISystemParam).
    private static void DrainInboxSystem(
        Res<AgentServerState> stateRes,
        AppParam appParam)
    {
        Dispatcher.DrainInbox(stateRes.Value!, appParam.App);
    }
}

#endif
