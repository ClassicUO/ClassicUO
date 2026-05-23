// SPDX-License-Identifier: BSD-2-Clause
//
// Agent dev-loop server, ECS plugin entry point. Wired into the App
// from src/ClassicUO.Client/Ecs/Boot.cs only when the AGENT_BUILD
// constant is defined (see ClassicUO.Agent.Settings.props).
//
// Split of execution between threads:
//   1. AgentServer (background Task started during plugin Build) owns the
//      TcpListener, reads JSON-RPC frames, pushes requests into the inbox
//      Channel, drains the outbox Channel and writes responses/events back.
//   2. DrainInbox system (per-frame, on the game thread) reads from inbox,
//      routes via AgentDispatcher to handlers that touch ECS state via
//      Commands, and enqueues responses to outbox.
//
// All engine reads happen on the game thread. The background thread only
// ever touches the Channels and the socket.

#if AGENT_BUILD
#nullable enable

using System;
using ClassicUO.Configuration;
using ClassicUO.Ecs;
using Microsoft.Xna.Framework.Graphics;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;

namespace ClassicUO.Agent;

internal readonly struct AgentServerPlugin : IPlugin
{
    // Capture must run after the UI is drawn (UiRenderStage) but before
    // Present (Stage.Last) so the backbuffer still holds the rendered
    // frame. PostUpdate alone is not late enough — the world is rendered
    // there, but the UI runs in UiRenderStage which comes after PostUpdate.
    public static readonly Stage AgentCaptureStage = Stage.Custom("AgentCapture");

    public void Build(App app)
    {
        app.AddStage(AgentCaptureStage)
            .After(UiPlugin.UiRenderStage)
            .Before(Stage.Last);

        var state = new AgentServerState();
        app.AddResource(state);

        // Start the listener IMMEDIATELY during plugin registration so the
        // socket binds before any other plugin's Stage.Startup system can
        // crash. This lets the CLI ping the listener even when downstream
        // engine initialisation (assets, network, etc.) blows up — the
        // ping fast-path in ReadFramesAsync answers without the dispatcher.
        AgentServer.StartAcceptLoop(state);

        // DrainInbox runs on the game thread. Stage.First so handlers and
        // any downstream observer triggers they emit via Commands (e.g.
        // agent.login → OnLoginRequest → HandleLoginRequests) fire before
        // gameplay systems read state this frame. SingleThreaded because
        // handlers mutate ECS state through Commands.
        //
        // Method-group form so the compiler binds to the generic
        // AddSystem<T1..T5>(this App, Action<T1..T5>) extension instead
        // of the instance AddSystem(ISystem) overload.
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

    private static void DrainInboxSystem(
        Res<AgentServerState> stateRes,
        WorldParam world,
        Res<GameContext> gameCtx,
        Res<Settings> settings,
        Commands commands,
        Res<MouseContext> mouseCtx)
    {
        AgentDispatcher.DrainInbox(stateRes, world.World, gameCtx, settings, commands, mouseCtx.Value!);
    }
}

#endif
