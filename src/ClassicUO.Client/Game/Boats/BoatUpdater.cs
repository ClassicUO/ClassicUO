// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Utility.Collections;

namespace ClassicUO.Game.Boats
{
    /// <summary>
    /// Concrete <see cref="IBoatUpdater"/>. Stateless except for a reusable
    /// <c>_toRemove</c> scratch list used to defer dictionary mutations
    /// until after the iteration completes. Delegates queue state to
    /// <see cref="IBoatStepQueue"/> and rider mutation to
    /// <see cref="IBoatEntityTracker"/>.
    /// </summary>
    internal sealed class BoatUpdater : IBoatUpdater
    {
        private readonly IBoatStepQueue _queue;
        private readonly IBoatEntityTracker _tracker;
        private readonly List<uint> _toRemove = new List<uint>();

        public BoatUpdater(IBoatStepQueue queue, IBoatEntityTracker tracker)
        {
            _queue = queue;
            _tracker = tracker;
        }

        public void Update(World world)
        {
            foreach (Deque<BoatStep> deques in _queue.AllDeques)
            {
                while (deques.Count != 0)
                {
                    ref BoatStep step = ref deques.Front();

                    Item item = world.Items.Get(step.Serial);

                    if (item == null || item.IsDestroyed)
                    {
                        _toRemove.Add(step.Serial);

                        break;
                    }

                    bool drift = step.MovingDir != step.FacingDir;
                    int maxDelay = step.TimeDiff /*- (int) Client.Game.FrameDelay[1]*/;

                    int delay = (int)Time.Ticks - (int)item.LastStepTime;
                    bool removeStep = delay >= maxDelay;
                    bool directionChange = false;

                    if ( /*step.FacingDir == step.MovingDir &&*/
                        item.X != step.X || item.Y != step.Y)
                    {
                        if (maxDelay != 0)
                        {
                            float steps = maxDelay / (float)Constants.CHARACTER_ANIMATION_DELAY;
                            float x = delay / (float)Constants.CHARACTER_ANIMATION_DELAY;
                            float y = x;
                            item.Offset.Z = (sbyte)((step.Z - item.Z) * x * (4.0f / steps));
                            MovementSpeed.GetPixelOffset((byte)step.MovingDir, ref x, ref y, steps);
                            item.Offset.X = (sbyte)x;
                            item.Offset.Y = (sbyte)y;
                        }
                    }
                    else
                    {
                        directionChange = true;
                        removeStep = true;
                    }

                    world.HouseManager.TryGetHouse(item, out House house);

                    if (removeStep)
                    {
                        item.X = step.X;
                        item.Y = step.Y;
                        item.Z = step.Z;
                        item.UpdateScreenPosition();

                        item.Offset.X = 0;
                        item.Offset.Y = 0;
                        item.Offset.Z = 0;

                        deques.RemoveFromFront();

                        if (item.TNext != null || item.TPrevious != null)
                        {
                            item.AddToTile();
                        }

                        house?.Generate(true, true, true);

                        _tracker.UpdateEntitiesInside
                        (
                            world,
                            item.Serial,
                            removeStep,
                            step.X,
                            step.Y,
                            step.Z,
                            step.MovingDir
                        );

                        item.LastStepTime = Time.Ticks;
                    }
                    else
                    {
                        if (house != null)
                        {
                            foreach (Multi c in house.Components)
                            {
                                c.Offset = item.Offset;
                            }
                        }

                        _tracker.UpdateEntitiesInside
                        (
                            world,
                            item.Serial,
                            removeStep,
                            item.X,
                            item.Y,
                            item.Z,
                            step.MovingDir
                        );
                    }

                    if (!directionChange)
                    {
                        break;
                    }
                }
            }

            if (_toRemove.Count != 0)
            {
                for (int i = 0; i < _toRemove.Count; i++)
                {
                    _queue.Remove(_toRemove[i]);
                    _tracker.Remove(_toRemove[i]);
                }

                _toRemove.Clear();
            }
        }
    }
}
