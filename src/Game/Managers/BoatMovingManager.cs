using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Utility.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.Managers
{
    static class BoatMovingManager
    {   
        struct BoatStep
        {
            public uint LastStepTime;
            public uint Serial;
            public ushort X, Y;
            public sbyte Z;
            public byte Speed;
            public Direction MovingDir, FacingDir;
        }

        struct ItemInside
        {
            public uint Serial;
            public ushort X, Y;
            public sbyte Z;
        }

        private static readonly Dictionary<uint, Deque<BoatStep>> _steps = new Dictionary<uint, Deque<BoatStep>>();
        private static readonly List<uint> _toRemove = new List<uint>();
        private static readonly Dictionary<uint, RawList<ItemInside>> _items = new Dictionary<uint, RawList<ItemInside>>();


        public static void AddStep(
            uint serial, 
            byte speed, 
            Direction movingDir,
            Direction facingDir,
            ushort x, 
            ushort y,
            sbyte z)
        {


            Item item = World.Items.Get(serial);
            if (item == null || item.IsDestroyed) 
            {
                return;
            }

            item.LastStepTime = Time.Ticks;

            if (!_steps.TryGetValue(serial, out var deque))
            {
                deque = new Deque<BoatStep>();
                _steps[serial] = deque;
            }


            BoatStep step = new BoatStep()
            {
                Serial = serial,
                Speed = speed,
                MovingDir = movingDir,
                FacingDir = facingDir,
                X = x,
                Y = y,
                Z = z,
            };

            deque.AddToBack(step);
        }

        public static void PushItemToList(
            uint serial,
            uint objSerial,
            ushort x,
            ushort y,
            sbyte z)
        {
            if (!_items.TryGetValue(serial, out var list))
            {
                list = new RawList<ItemInside>();

                _items[serial] = list;
            }

            int i = 0;
            for (; i < list.Count; i++)
            {
                ref var item = ref list[i];

                if (item.Serial == objSerial)
                {
                    item.X = x;
                    item.Y = y;
                    item.Z = z;
                    return;
                }
            }

            list.Add(new ItemInside()
            {
                Serial = objSerial,
                X = x,
                Y = y,
                Z = z
            });
        }

        public static void Update()
        {
            foreach (Deque<BoatStep> deques in _steps.Values)
            {
                LABEL:
                if (deques.Count != 0)
                {
                    ref var step = ref deques.Front();

                    Item item = World.Items.Get(step.Serial);

                    if (item == null || item.IsDestroyed)
                    {
                        _toRemove.Add(step.Serial);
                        UpdateEntitiesInside(step.Serial, true);
                        continue;
                    }

                    int maxDelay = 150; // MovementSpeed.TimeToCompleteMovement(this, step.Run) - (int) Client.Game.FrameDelay[1];
                    int delay = (int) Time.Ticks - (int) item.LastStepTime;
                    bool removeStep = delay >= maxDelay;
                    bool directionChange = false;

                    const int DELAY = 100;

                    if (item.X != step.X || item.Y != step.Y)
                    {
                        float steps = maxDelay / (float) DELAY;
                        float x = delay / (float) DELAY;
                        float y = x;
                        item.Offset.Z = (sbyte) ((step.Z - item.Z) * x * (4.0f / steps));
                        MovementSpeed.GetPixelOffset((byte) step.MovingDir, ref x, ref y, steps);
                        item.Offset.X = (sbyte) x;
                        item.Offset.Y = (sbyte) y;
                    }
                    else
                    {
                        directionChange = true;
                        removeStep = true;
                    }


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

                        if (directionChange)
                        {
                            goto LABEL;
                        }

                        if (item.Right != null || item.Left != null)
                            item.AddToTile();

                        item.LastStepTime = Time.Ticks;
                    }


                    UpdateEntitiesInside(item, removeStep);


                    if (World.HouseManager.TryGetHouse(item, out House house))
                        house.Generate(true);

                }
            }


            if (_toRemove.Count != 0)
            {
                for (int i = 0; i < _toRemove.Count; i++)
                {
                    _steps.Remove(_toRemove[i]);
                }

                _toRemove.Clear();
            }
        }

        private static void UpdateEntitiesInside(uint serial, bool removeStep)
        {
            if (_items.TryGetValue(serial, out var list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    ref var it = ref list[i];

                    Entity entity = World.Get(it.Serial);
                    if (entity == null || entity.IsDestroyed)
                    {
                        list.RemoveAt((uint) i--);
                        continue;
                    }

                    if (removeStep)
                    {
                        if (entity == World.Player)
                        {
                            World.RangeSize.X = it.X;
                            World.RangeSize.Y = it.Y;
                        }
                        entity.X = it.X;
                        entity.Y = it.Y;
                        entity.Z = it.Z;
                        entity.Offset.X = 0;
                        entity.Offset.Y = 0;
                        entity.Offset.Z = 0;
                        entity.UpdateScreenPosition();
                        entity.AddToTile();
                    }
                    else
                    {
                        Item item = World.Items.Get(serial);
                        if (item != null)
                            entity.Offset = item.Offset;
                    }

                }
            }
        }

        public static void Remove(uint serial)
        {

        }
    }
}
