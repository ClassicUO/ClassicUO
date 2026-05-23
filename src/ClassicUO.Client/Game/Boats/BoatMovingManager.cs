// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Boats
{
    /// <summary>
    /// Facade over the Boats collaborators: keeps the existing public
    /// surface (<c>_world.BoatMovingManager.X</c>) and delegates to three
    /// cohesive classes. The per-boat step queue and packet clock live in
    /// <see cref="IBoatStepQueue"/>, rider bookkeeping in
    /// <see cref="IBoatEntityTracker"/>, and the per-frame interpolation
    /// loop in <see cref="IBoatUpdater"/>.
    /// </summary>
    internal sealed class BoatMovingManager
    {
        private readonly World _world;
        private readonly IBoatStepQueue _queue;
        private readonly IBoatEntityTracker _tracker;
        private readonly IBoatUpdater _updater;

        /// <summary>Production composition root. Defaults to concrete collaborators.</summary>
        public BoatMovingManager(World world)
            : this(world, new BoatStepQueue(), new BoatEntityTracker())
        {
        }

        private BoatMovingManager(World world, IBoatStepQueue queue, IBoatEntityTracker tracker)
            : this(world, queue, tracker, new BoatUpdater(queue, tracker))
        {
        }

        /// <summary>Full DI seam — inject all collaborators.</summary>
        internal BoatMovingManager(World world, IBoatStepQueue queue, IBoatEntityTracker tracker, IBoatUpdater updater)
        {
            _world = world;
            _queue = queue;
            _tracker = tracker;
            _updater = updater;
        }

        // ---- Step queue facade ----
        public void MoveRequest(Direction direction, byte speed) => _queue.MoveRequest(_world, direction, speed);

        public void AddStep
        (
            uint serial,
            byte speed,
            Direction movingDir,
            Direction facingDir,
            ushort x,
            ushort y,
            sbyte z
        )
        {
            _queue.AddStep(_world, serial, speed, movingDir, facingDir, x, y, z);
            // New step arriving means the cached rider list is stale.
            _tracker.ClearEntities(serial);
        }

        /// <summary>
        /// Drop every queued step for <paramref name="serial"/> and snap the
        /// boat <see cref="Item"/> plus its riders back to their anchor cell.
        /// </summary>
        public void ClearSteps(uint serial)
        {
            Item multiItem = _queue.ClearSteps(_world, serial);

            if (multiItem == null)
            {
                return;
            }

            multiItem.Offset.X = 0;
            multiItem.Offset.Y = 0;
            multiItem.Offset.Z = 0;

            _tracker.ResetEntityOffsets(_world, serial);
        }

        // ---- Entity tracker facade ----
        public void ClearEntities(uint serial) => _tracker.ClearEntities(serial);
        public void PushItemToList(uint serial, uint objSerial, int x, int y, int z) =>
            _tracker.PushItemToList(serial, objSerial, x, y, z);

        // ---- Updater facade ----
        public void Update() => _updater.Update(_world);
    }
}
