// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Utility.Collections;

namespace ClassicUO.Game.Boats
{
    /// <summary>
    /// Owns the per-boat queue of pending movement steps and the
    /// last-packet timestamp used to derive step durations. A "step" is a
    /// server-authoritative target (x/y/z + direction) that the updater
    /// interpolates the boat toward; multiple steps may be queued so the
    /// client can smooth them over time.
    /// </summary>
    internal interface IBoatStepQueue
    {
        /// <summary>
        /// Sends a multi-boat move request packet for the local player and
        /// stamps the packet clock used by <see cref="AddStep"/> to compute
        /// inter-step delays.
        /// </summary>
        void MoveRequest(World world, Direction direction, byte speed);

        /// <summary>
        /// Appends a server-sent step for <paramref name="serial"/>. Caps the
        /// queue at 5 entries, stamps <c>LastStepTime</c> on the first step,
        /// and derives <c>TimeDiff</c> from either the speed table (for the
        /// first step / fresh packet clock) or the wall-clock delta since
        /// the previous packet.
        /// </summary>
        void AddStep
        (
            World world,
            uint serial,
            byte speed,
            Direction movingDir,
            Direction facingDir,
            ushort x,
            ushort y,
            sbyte z
        );

        /// <summary>
        /// Drops every queued step for <paramref name="serial"/>. Returns
        /// the boat <see cref="Item"/> the caller may need for follow-up
        /// offset cleanup, or <c>null</c> if there were no queued steps.
        /// </summary>
        Item ClearSteps(World world, uint serial);

        /// <summary>Remove all bookkeeping for a boat (called after destroy).</summary>
        void Remove(uint serial);

        /// <summary>Per-boat step deques, exposed for the updater loop.</summary>
        IEnumerable<Deque<BoatStep>> AllDeques { get; }
    }

    /// <summary>
    /// A single queued boat movement step. Mirrors the fields the server
    /// sends in the multi-boat move packet.
    /// </summary>
    internal struct BoatStep
    {
        public uint Serial;
        public int TimeDiff;
        public ushort X, Y;
        public sbyte Z;
        public byte Speed;
        public Direction MovingDir, FacingDir;
    }
}
