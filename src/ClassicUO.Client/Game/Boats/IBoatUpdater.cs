// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Boats
{
    /// <summary>
    /// Drives the per-frame boat movement loop: walks the
    /// <see cref="IBoatStepQueue"/>, interpolates each boat
    /// <see cref="GameObjects.Item"/> toward its current target step, runs
    /// the rider translation via <see cref="IBoatEntityTracker"/> and
    /// retires steps that have elapsed.
    /// </summary>
    internal interface IBoatUpdater
    {
        /// <summary>
        /// Advance all queued boat steps by one frame. Items that have been
        /// destroyed are unregistered from both the queue and the entity
        /// tracker.
        /// </summary>
        void Update(World world);
    }
}
