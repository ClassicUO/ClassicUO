// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace ClassicUO.ECS
{
    /// <summary>
    /// Records enqueued commands with timing metadata for deterministic replay.
    ///
    /// Usage:
    ///   1. Call StartCapture() to begin recording
    ///   2. Call RecordCommand() from EnqueueCommand (automatically if enabled)
    ///   3. Call RecordCheckpoint() at key frames
    ///   4. Call StopCapture() to finalize
    ///   5. Use SaveToFile() / LoadFromFile() for persistence
    ///   6. Use GetRecordedCommands() to replay
    /// </summary>
    public sealed class ReplayCapture
    {
        private readonly List<ReplayEntry> _entries = new();
        private readonly List<ParityCheckpoint> _checkpoints = new();
        private bool _capturing;
        private uint _startTick;

        /// <summary>Whether capture is currently active.</summary>
        public bool IsCapturing => _capturing;

        /// <summary>Number of recorded entries.</summary>
        public int EntryCount => _entries.Count;

        /// <summary>Number of recorded checkpoints.</summary>
        public int CheckpointCount => _checkpoints.Count;

        /// <summary>Start recording commands.</summary>
        public void StartCapture(uint currentTick)
        {
            _entries.Clear();
            _checkpoints.Clear();
            _startTick = currentTick;
            _capturing = true;
        }

        /// <summary>Stop recording.</summary>
        public void StopCapture()
        {
            _capturing = false;
        }

        /// <summary>
        /// Record a command with its type tag and binary payload.
        /// Called from EcsRuntimeHost.EnqueueCommand when capture is active.
        /// </summary>
        public void RecordCommand<T>(uint tick, T command) where T : unmanaged
        {
            if (!_capturing)
                return;

            int size = Marshal.SizeOf<T>();
            byte[] payload = new byte[size];
            unsafe
            {
                fixed (byte* ptr = payload)
                {
                    *(T*)ptr = command;
                }
            }

            _entries.Add(new ReplayEntry(
                tick - _startTick,
                typeof(T).Name,
                payload
            ));
        }

        /// <summary>Record a parity checkpoint at the current frame.</summary>
        public void RecordCheckpoint(ParityCheckpoint checkpoint)
        {
            if (!_capturing)
                return;

            _checkpoints.Add(checkpoint);
        }

        /// <summary>Get all recorded entries for replay.</summary>
        public IReadOnlyList<ReplayEntry> GetRecordedEntries() => _entries;

        /// <summary>Get all recorded checkpoints for comparison.</summary>
        public IReadOnlyList<ParityCheckpoint> GetCheckpoints() => _checkpoints;

        /// <summary>
        /// Save capture to a binary file.
        /// Format: [entryCount][entries...][checkpointCount][checkpoints...]
        /// </summary>
        public void SaveToFile(string path)
        {
            using var fs = File.Create(path);
            using var bw = new BinaryWriter(fs);

            // Header
            bw.Write((byte)'C');
            bw.Write((byte)'U');
            bw.Write((byte)'O');
            bw.Write((byte)'R'); // "CUOR" magic
            bw.Write((int)1);    // version
            bw.Write(_startTick);

            // Entries
            bw.Write(_entries.Count);
            foreach (var entry in _entries)
            {
                bw.Write(entry.RelativeTick);
                bw.Write(entry.CommandTypeName);
                bw.Write(entry.Payload.Length);
                bw.Write(entry.Payload);
            }

            // Checkpoints
            bw.Write(_checkpoints.Count);
            foreach (var cp in _checkpoints)
            {
                bw.Write(cp.Tick);
                bw.Write(cp.MobileCount);
                bw.Write(cp.ItemCount);
                bw.Write(cp.EffectCount);
                bw.Write(cp.PlayerX);
                bw.Write(cp.PlayerY);
                bw.Write(cp.PlayerZ);
                bw.Write(cp.PlayerHits);
                bw.Write(cp.PlayerMana);
                bw.Write(cp.PlayerStamina);
            }
        }

        /// <summary>Load a previously saved capture.</summary>
        public static ReplayCapture LoadFromFile(string path)
        {
            var capture = new ReplayCapture();

            using var fs = File.OpenRead(path);
            using var br = new BinaryReader(fs);

            // Header
            byte c = br.ReadByte(), u = br.ReadByte(), o = br.ReadByte(), r = br.ReadByte();
            if (c != 'C' || u != 'U' || o != 'O' || r != 'R')
                throw new InvalidDataException("Invalid replay file magic");

            int version = br.ReadInt32();
            if (version != 1)
                throw new InvalidDataException($"Unsupported replay version: {version}");

            capture._startTick = br.ReadUInt32();

            // Entries
            int entryCount = br.ReadInt32();
            for (int i = 0; i < entryCount; i++)
            {
                uint relTick = br.ReadUInt32();
                string typeName = br.ReadString();
                int payloadLen = br.ReadInt32();
                byte[] payload = br.ReadBytes(payloadLen);
                capture._entries.Add(new ReplayEntry(relTick, typeName, payload));
            }

            // Checkpoints
            int cpCount = br.ReadInt32();
            for (int i = 0; i < cpCount; i++)
            {
                capture._checkpoints.Add(new ParityCheckpoint(
                    Tick: br.ReadUInt32(),
                    MobileCount: br.ReadInt32(),
                    ItemCount: br.ReadInt32(),
                    EffectCount: br.ReadInt32(),
                    PlayerX: br.ReadUInt16(),
                    PlayerY: br.ReadUInt16(),
                    PlayerZ: br.ReadSByte(),
                    PlayerHits: br.ReadUInt16(),
                    PlayerMana: br.ReadUInt16(),
                    PlayerStamina: br.ReadUInt16()
                ));
            }

            return capture;
        }
    }

    /// <summary>A single recorded command entry.</summary>
    public readonly struct ReplayEntry
    {
        /// <summary>Tick offset from capture start.</summary>
        public readonly uint RelativeTick;

        /// <summary>Command struct type name for dispatch.</summary>
        public readonly string CommandTypeName;

        /// <summary>Binary payload of the command struct.</summary>
        public readonly byte[] Payload;

        public ReplayEntry(uint relativeTick, string commandTypeName, byte[] payload)
        {
            RelativeTick = relativeTick;
            CommandTypeName = commandTypeName;
            Payload = payload;
        }
    }
}
