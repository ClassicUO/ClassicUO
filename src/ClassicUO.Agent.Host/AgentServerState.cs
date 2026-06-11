// SPDX-License-Identifier: BSD-2-Clause
//
// Resource shared between the background TCP thread and the per-frame
// engine systems. Single instance, owned by the consumer-side bootstrap
// (AgentBootstrap on the OOP runtime, AgentServerPlugin on the ECS one).
//
// All fields are intended for read-write from EITHER thread via the
// Channel<T>s and the connection slot, never via direct field access. The
// CurrentClient slot is an atomic write (Interlocked.Exchange) when a new
// connection wins the single-concurrent-connection invariant.

#nullable enable

using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using ClassicUO.Agent.Contracts;
using Microsoft.Xna.Framework.Input;

namespace ClassicUO.Agent.Host;

// One frame of synthetic mouse state. Consumed by the runtime-side
// per-tick advance system, applied to the runtime's mouse input layer
// before its update runs. Each frame produces at most one
// (oldState, newState) transition — multiple state changes inside the
// same frame would coalesce in the mouse bookkeeping, so press/release
// sequences (especially double-click) must span N consecutive frames
// with one transition per frame.
public struct SynthMouseFrame
{
    public int X;
    public int Y;
    public ButtonState Left;
    public ButtonState Middle;
    public ButtonState Right;
    // Scroll-wheel notches to apply on the frame this is drained (+up / -down).
    // 0 for ordinary move/click frames.
    public int Wheel;
}

// Pending screenshot request. capture.shot enqueues this from the inbox
// thread; a runtime-side service system (after the frame has been drawn
// but before Present) reads the backbuffer and writes a JsonRpcResponse
// with the original request id directly to the outbox. Handlers return
// null on enqueue so DrainInbox skips its outbox write.
public sealed class CaptureRequest
{
    public long? RequestId;
    public string? OutPath; // optional: write PNG to disk and return path only
}

public sealed class AgentServerState
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
    // by the runtime's mouse-advance system; CurrentMouseSynth is the
    // held state when the queue is empty.
    public readonly Queue<SynthMouseFrame> PendingMouseFrames = new();
    public SynthMouseFrame CurrentMouseSynth;

    // Pending capture request (single in-flight). The render system fills
    // the response on the next frame after the request lands.
    public CaptureRequest? PendingCapture;

    // Typed characters waiting to be turned into engine-side text input
    // events by a per-frame system. Used on the ECS branch instead of
    // SDL_PushEvent because that runtime uses SDL3 bindings, where SDL2-
    // flavor event pushes don't make it through to FNA's TextInputEXT
    // pipeline. Unused on the OOP runtime (which pushes SDL events
    // directly) but kept here for shared-struct simplicity.
    public readonly Queue<char> PendingTypedChars = new();

    // Synthetic key presses (one frame down, released next) waiting to be
    // drained into the ECS runtime's KeyboardContext. Lets the harness drive
    // IsPressedOnce paths (Enter to submit chat, Escape) that the CharInput
    // text channel can't reach. Unused on the OOP runtime.
    public readonly Queue<Keys> PendingKeyPresses = new();

    // Auto-progress through ServerSelection and CharacterSelection after
    // an agent.login dispatch — bypasses the UI clicks the human player
    // would otherwise issue. Indices default to 0. Cleared once
    // character is selected.
    public bool AutoLoginActive;
    public int AutoServerIndex;
    public int AutoCharacterIndex;

    // Cached selection lists. Populated by runtime-side systems from the
    // corresponding events so RPC handlers can resolve a character by
    // index without re-querying the server.
    public string[]? LastCharacterNames;
    public int LastServerCount;

    // Current high-level game state surfaced via lifecycle.gameState.
    // Set by runtime-side systems on every state change.
    public string CurrentGameState = string.Empty;
}
