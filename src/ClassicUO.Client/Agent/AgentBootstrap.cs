// SPDX-License-Identifier: BSD-2-Clause
//
// Agent dev-loop entry point on the main (pre-ECS) branch. Replaces the
// ECS-side AgentServerPlugin: GameController calls the static hooks
// directly at known phases of the loop:
//
//   * GameController constructor / LoadContent:
//       AgentBootstrap.Start() — spin up TCP listener.
//   * GameController.Update (BEFORE Mouse.Update):
//       AgentBootstrap.OnFrameUpdateBefore() — drain one synth mouse
//       frame so the next Mouse.Update sees a single press/release edge.
//       AgentBootstrap.DrainInbox(game) — route any queued RPC calls.
//   * GameController.Draw (AFTER Scene.Draw, BEFORE Present):
//       AgentBootstrap.ServiceCapture(device) — read backbuffer for any
//       pending capture.shot request and write the deferred response.
//   * GameController.Draw end:
//       AgentBootstrap.FlushOutbox() — push responses to the socket.
//
// All members are no-ops without -p:AGENT_BUILD=true; this file is
// excluded from prod builds via #if AGENT_BUILD.

#if AGENT_BUILD
#nullable enable

using ClassicUO.Agent.Agent.Handlers;
using ClassicUO.Agent.Host;
using ClassicUO.Game.Scenes;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SDL2;

namespace ClassicUO.Agent;

internal static class AgentBootstrap
{
    // Per-runtime dispatcher. Routes are registered exactly once during
    // Start(); the per-frame DrainInbox call forwards the live
    // GameController instance so handlers can reach Scene / UO.World /
    // GraphicsDevice via the TRuntime field on AgentRpcContext.
    private static readonly AgentDispatcher<GameController> Dispatcher = new();

    private static AgentServerState? s_state;
    private static bool s_routesRegistered;

    // Track the previous applied state so EmitEdge can detect transitions.
    // Initialized to all-Released so the first applied frame with a
    // Pressed button correctly emits SDL_MOUSEBUTTONDOWN.
    private static SynthMouseFrame s_lastApplied = new()
    {
        Left = ButtonState.Released,
        Middle = ButtonState.Released,
        Right = ButtonState.Released,
    };

    public static AgentServerState? State => s_state;

    public static void Start()
    {
        if (s_state is not null) return;
        s_state = new AgentServerState();

        if (!s_routesRegistered)
        {
            LifecycleHandlers.Register(Dispatcher);
            AgentLoginHandlers.Register(Dispatcher);
            WorldHandlers.Register(Dispatcher);
            InputHandlers.Register(Dispatcher);
            CaptureHandlers.Register(Dispatcher);
            s_routesRegistered = true;
        }

        AgentServer.StartAcceptLoop(s_state);
    }

    public static void OnFrameUpdateBefore()
    {
        var state = s_state;
        if (state is null) return;

        // Drain one queued synth-mouse frame per tick so press/release
        // transitions are visible across consecutive Mouse.Update() calls.
        // When the queue is empty, hold the last applied state.
        if (state.PendingMouseFrames.Count > 0)
        {
            state.CurrentMouseSynth = state.PendingMouseFrames.Dequeue();
            ApplySynthFrame(state.CurrentMouseSynth);
        }
        else if (ClassicUO.Input.Mouse.AgentSyntheticActive)
        {
            ApplySynthFrame(state.CurrentMouseSynth);
        }
    }

    public static void DrainInbox(GameController game)
    {
        var state = s_state;
        if (state is null) return;
        Dispatcher.DrainInbox(state, game);
        AdvanceAutoLogin(state, game);
    }

    // After agent.login dispatches LoginScene.Connect, the scene cycles
    // through ServerSelection → CharacterSelection states as packets land.
    // Auto-click each gate by calling SelectServer / SelectCharacter when
    // the scene reports it is parked on that step with the data loaded.
    private static void AdvanceAutoLogin(AgentServerState state, GameController game)
    {
        if (!state.AutoLoginActive) return;
        var login = game.GetScene<LoginScene>();
        if (login is null)
        {
            // Scene transitioned to GameScene — login flow is done.
            state.AutoLoginActive = false;
            return;
        }

        switch (login.CurrentLoginStep)
        {
            case LoginSteps.ServerSelection:
                var servers = login.Servers;
                if (servers is null || servers.Length == 0) return;
                var sIdx = state.AutoServerIndex;
                if (sIdx < 0 || sIdx >= servers.Length) sIdx = 0;
                login.SelectServer((byte)servers[sIdx].Index);
                break;

            case LoginSteps.CharacterSelection:
                var chars = login.Characters;
                if (chars is null || chars.Length == 0) return;
                var cIdx = state.AutoCharacterIndex;
                if (cIdx < 0 || cIdx >= chars.Length) cIdx = 0;
                login.SelectCharacter((uint)cIdx);
                // SelectCharacter advances to EnteringBritania; stop here
                // so we don't re-fire on the next tick before the scene
                // tears down to GameScene.
                state.AutoLoginActive = false;
                break;
        }
    }

    public static void ServiceCapture(GraphicsDevice device)
    {
        var state = s_state;
        if (state is null) return;
        var req = state.PendingCapture;
        if (req is null) return;
        state.PendingCapture = null;

        var resp = AgentCaptureService.Run(device, req);
        state.Outbox.Writer.TryWrite(resp);
    }

    public static void FlushOutbox()
    {
        var state = s_state;
        if (state is null) return;
        AgentServer.FlushOutbox(state);
    }

    private static void ApplySynthFrame(SynthMouseFrame f)
    {
        ClassicUO.Input.Mouse.AgentSetSyntheticPosition(f.X, f.Y);
        ClassicUO.Input.Mouse.AgentSetSyntheticButtons(
            f.Left == ButtonState.Pressed,
            f.Middle == ButtonState.Pressed,
            f.Right == ButtonState.Pressed);

        // Synthesize SDL button events on edge transitions so HandleSdlEvent
        // in GameController fires Scene.OnMouseDown / UIManager.OnMouseButtonDown,
        // which are the actual click pathways on the main branch (the
        // engine reads button state only as a held-mode bit; clicks come
        // from SDL events).
        // SDL mouse-button constants are stable across SDL2/SDL3: 1=L, 2=M, 3=R.
        EmitEdge(s_lastApplied.Left,   f.Left,   sdlButton: 1, f.X, f.Y);
        EmitEdge(s_lastApplied.Middle, f.Middle, sdlButton: 2, f.X, f.Y);
        EmitEdge(s_lastApplied.Right,  f.Right,  sdlButton: 3, f.X, f.Y);
        s_lastApplied = f;
    }

    private static void EmitEdge(ButtonState prev, ButtonState curr, byte sdlButton, int x, int y)
    {
        if (prev == curr) return;

        var kind = curr == ButtonState.Pressed
            ? SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN
            : SDL.SDL_EventType.SDL_MOUSEBUTTONUP;
        var evt = default(SDL.SDL_Event);
        evt.type = kind;
        evt.button.type = kind;
        evt.button.button = sdlButton;
        evt.button.state = curr == ButtonState.Pressed ? (byte)1 : (byte)0;
        evt.button.clicks = 1;
        evt.button.x = x;
        evt.button.y = y;
        SDL.SDL_PushEvent(ref evt);
    }
}

#endif
