// SPDX-License-Identifier: BSD-2-Clause
//
// Resource shared between the background TCP thread and the per-frame ECS
// systems. Single instance, registered via scheduler.AddResource(...).
//
// All fields are intended for read-write from EITHER thread via the
// Channel<T>s and the connection slot, never via direct field access. The
// CurrentClient slot is an atomic write (Interlocked.Exchange) when a new
// connection wins the single-concurrent-connection invariant.

#if AGENT_BUILD
#nullable enable

using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using ClassicUO.Agent.Contracts;
using Microsoft.Xna.Framework.Input;

namespace ClassicUO.Agent;

// One frame of synthetic mouse state. Consumed by AdvanceSyntheticMouse,
// applied to MouseContext before its Update() runs. Each frame produces
// at most one (oldState, newState) transition — multiple state changes
// inside the same frame would coalesce in MouseContext's bookkeeping,
// so press/release sequences (especially double-click) must span N
// consecutive frames with one transition per frame.
internal struct SynthMouseFrame
{
    public int X;
    public int Y;
    public ButtonState Left;
    public ButtonState Middle;
    public ButtonState Right;
}

// Pending screenshot request. capture.shot enqueues this from the inbox
// thread; ServiceCaptureRequest (Stage.PostUpdate, after game.Tick has
// drawn the frame but before Present) reads the backbuffer and writes a
// JsonRpcResponse with the original request id directly to the outbox.
// Handlers return null on enqueue so DrainInbox skips its outbox write.
internal sealed class CaptureRequest
{
    public long? RequestId;
    public string? OutPath; // optional: write PNG to disk and return path only
}

internal sealed class AgentServerState
{
    public readonly Channel<JsonRpcRequest> Inbox = Channel.CreateUnbounded<JsonRpcRequest>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });

    public readonly Channel<object> Outbox = Channel.CreateUnbounded<object>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public readonly CancellationTokenSource Cancellation = new();

    // The currently-connected client. Single-concurrent-connection: a new
    // accept closes the previous slot. Atomic swaps via Interlocked.Exchange.
    public TcpClient? CurrentClient;
    public Stream? CurrentStream;

    public int Port; // chosen by the kernel; advertised via the sidecar file
    public string? PortAdvertisedAt; // %LOCALAPPDATA%\ClassicUO\agent\port.json

    // Synthetic mouse pacing. PendingMouseFrames is drained one-per-tick
    // by AgentServerPlugin's mouse-advance system; CurrentMouseSynth is
    // the held state when the queue is empty. ECS systems read it via
    // MouseContext after AgentSetSynthetic(...).
    public readonly Queue<SynthMouseFrame> PendingMouseFrames = new();
    public SynthMouseFrame CurrentMouseSynth;

    // Pending capture request (single in-flight). The render system fills
    // the TaskCompletionSource on the next frame after the request lands.
    public CaptureRequest? PendingCapture;

    // Typed characters waiting to be turned into CharInputEvent instances
    // by an engine-side system. Used instead of SDL_PushEvent because the
    // ECS branch uses SDL3 bindings, where SDL2-flavor event pushes don't
    // make it through to FNA's TextInputEXT pipeline.
    public readonly Queue<char> PendingTypedChars = new();

    // Auto-progress through ServerSelection and CharacterSelection after
    // an agent.login dispatch — bypasses the UI clicks the human player
    // would otherwise issue. Indices default to 0. Cleared once
    // character is selected.
    public bool AutoLoginActive;
    public int AutoServerIndex;
    public int AutoCharacterIndex;

    // Cached selection lists. Populated by AgentLoginCachePlugin from the
    // corresponding ECS events so RPC handlers can resolve a character by
    // index without re-querying the server.
    public string[]? LastCharacterNames;
    public int LastServerCount;

    // Current high-level game state surfaced via lifecycle.gameState. Set
    // by AgentLoginCachePlugin on every NextState<GameState> change.
    public string CurrentGameState = string.Empty;
}

#endif
