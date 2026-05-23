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

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SDL2;

namespace ClassicUO.Agent;

internal static class AgentBootstrap
{
    private static AgentServerState? s_state;
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
        AgentDispatcher.DrainInbox(state, game);
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
        EmitEdge(s_lastApplied.Left, f.Left, (byte)SDL.SDL_BUTTON_LEFT, f.X, f.Y);
        EmitEdge(s_lastApplied.Middle, f.Middle, (byte)SDL.SDL_BUTTON_MIDDLE, f.X, f.Y);
        EmitEdge(s_lastApplied.Right, f.Right, (byte)SDL.SDL_BUTTON_RIGHT, f.X, f.Y);
        s_lastApplied = f;
    }

    private static void EmitEdge(ButtonState prev, ButtonState curr, byte sdlButton, int x, int y)
    {
        if (prev == curr) return;

        var evt = default(SDL.SDL_Event);
        evt.type = curr == ButtonState.Pressed
            ? SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN
            : SDL.SDL_EventType.SDL_MOUSEBUTTONUP;
        evt.button.type = evt.type;
        evt.button.button = sdlButton;
        evt.button.state = curr == ButtonState.Pressed ? SDL.SDL_PRESSED : (byte)0;
        evt.button.clicks = 1;
        evt.button.x = x;
        evt.button.y = y;
        SDL.SDL_PushEvent(ref evt);
    }
}

#endif
