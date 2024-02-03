#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Collections.Generic;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Network;
using ClassicUO.Utility.Collections;

namespace ClassicUO.Game.Managers
{
    internal sealed class BoatMovingManager
    {
        private const int SLOW_INTERVAL = 1000;
        private const int NORMAL_INTERVAL = 500;
        private const int FAST_INTERVAL = 250;


        private readonly Dictionary<uint, Deque<BoatStep>> _steps = new Dictionary<uint, Deque<BoatStep>>();
        private readonly List<uint> _toRemove = new List<uint>();
        private readonly Dictionary<uint, FastList<ItemInside>> _items = new Dictionary<uint, FastList<ItemInside>>();

        private uint _timePacket;
        private readonly World _world;

        public BoatMovingManager(World world)
        {
            _world = world;
        }


        private int GetVelocity(byte speed)
        {
            switch (speed)
            {
                case 0x02: return SLOW_INTERVAL;

                default:
                case 0x03: return NORMAL_INTERVAL;

                case 0x04: return FAST_INTERVAL;

                case > 0x04: return speed * 10;
            }
        }

        public void MoveRequest(Direction direciton, byte speed)
        {
            NetClient.Socket.Send_MultiBoatMoveRequest(_world.Player, direciton, speed);
            _timePacket = Time.Ticks;
        }


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
            Item item = _world.Items.Get(serial);

            if (item == null || item.IsDestroyed)
            {
                return;
            }

            if (!_steps.TryGetValue(serial, out Deque<BoatStep> deque))
            {
                deque = new Deque<BoatStep>();
                _steps[serial] = deque;
            }

            bool empty = deque.Count == 0;

            while (deque.Count > 5)
            {
                deque.RemoveFromFront();
            }

            //deque.Clear();


            //GetEndPosition(
            //    item,
            //    deque,
            //    out ushort currX,
            //    out ushort currY,
            //    out sbyte currZ,
            //    out Direction endDir);

            //if (currX == x && currY == y && currZ == z && endDir == movingDir)
            //{
            //    return;
            //}

            if (empty)
            {
                item.LastStepTime = Time.Ticks;
            }

            //Direction moveDir = DirectionHelper.CalculateDirection(currX, currY, x, y);

            BoatStep step = new BoatStep();
            step.Serial = serial;
            step.TimeDiff = _timePacket == 0 || empty ? GetVelocity(speed) : (int) (Time.Ticks - _timePacket);

            step.Speed = speed;
            step.X = x;
            step.Y = y;
            step.Z = z;
            step.MovingDir = movingDir;
            deque.AddToBack(step);

            ClearEntities(serial);
            _timePacket = Time.Ticks;
            Console.WriteLine("CURRENT PACKET TIME: {0}", _timePacket);
        }

        public void ClearSteps(uint serial)
        {
            if (_steps.TryGetValue(serial, out Deque<BoatStep> deque) && deque.Count != 0)
            {
                Item multiItem = _world.Items.Get(serial);

                if (multiItem != null)
                {
                    multiItem.Offset.X = 0;
                    multiItem.Offset.Y = 0;
                    multiItem.Offset.Z = 0;
                }

                if (_items.TryGetValue(serial, out var list))
                {
                    for (int i = 0; i < list.Length; i++)
                    {
                        ref var it = ref list.Buffer[i];

                        Entity ent = _world.Get(it.Serial);

                        if (ent == null)
                        {
                            continue;
                        }

                        ent.Offset.X = 0;
                        ent.Offset.Y = 0;
                        ent.Offset.Z = 0;
                    }

                    list.Clear();
                }

                deque.Clear();
            }
        }

        public void ClearEntities(uint serial)
        {
            _items.Remove(serial);

            //if (_items.TryGetValue(serial, out var list))
            //{
            //    list.Clear();
            //}
        }


        public void PushItemToList(uint serial, uint objSerial, int x, int y, int z)
        {
            if (!_items.TryGetValue(serial, out var list))
            {
                list = new FastList<ItemInside>();

                _items[serial] = list;
            }

            for (int i = 0; i < list.Length; i++)
            {
                ref var item = ref list.Buffer[i];

                if (!SerialHelper.IsValid(item.Serial))
                {
                    break;
                }

                if (item.Serial == objSerial)
                {
                    item.X = x;
                    item.Y = y;
                    item.Z = z;

                    return;
                }
            }

            list.Add
            (
                new ItemInside
                {
                    Serial = objSerial,
                    X = x,
                    Y = y,
                    Z = z
                }
            );
        }

        public void Update()
        {
            foreach (Deque<BoatStep> deques in _steps.Values)
            {
                while (deques.Count != 0)
                {
                    ref BoatStep step = ref deques.Front();

                    Item item = _world.Items.Get(step.Serial);

                    if (item == null || item.IsDestroyed)
                    {
                        _toRemove.Add(step.Serial);

                        break;
                    }

                    bool drift = step.MovingDir != step.FacingDir;
                    int maxDelay = step.TimeDiff /*- (int) Client.Game.FrameDelay[1]*/;

                    int delay = (int) Time.Ticks - (int) item.LastStepTime;
                    bool removeStep = delay >= maxDelay;
                    bool directionChange = false;


                    if ( /*step.FacingDir == step.MovingDir &&*/
                        item.X != step.X || item.Y != step.Y)
                    {
                        if (maxDelay != 0)
                        {
                            float steps = maxDelay / (float) Constants.CHARACTER_ANIMATION_DELAY;
                            float x = delay / (float) Constants.CHARACTER_ANIMATION_DELAY;
                            float y = x;
                            item.Offset.Z = (sbyte) ((step.Z - item.Z) * x * (4.0f / steps));
                            MovementSpeed.GetPixelOffset((byte) step.MovingDir, ref x, ref y, steps);
                            item.Offset.X = (sbyte) x;
                            item.Offset.Y = (sbyte) y;
                        }
                    }
                    else
                    {
                        directionChange = true;
                        removeStep = true;
                    }

                    //item.BoatDirection = step.MovingDir;

                    _world.HouseManager.TryGetHouse(item, out House house);

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

                        UpdateEntitiesInside
                        (
                            item,
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

                        UpdateEntitiesInside
                        (
                            item,
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
                    _steps.Remove(_toRemove[i]);
                    _items.Remove(_toRemove[i]);
                }

                _toRemove.Clear();
            }
        }

        private void UpdateEntitiesInside
        (
            uint serial,
            bool removeStep,
            int x,
            int y,
            int z,
            Direction direction
        )
        {
            if (_items.TryGetValue(serial, out var list))
            {
                Item item = _world.Items.Get(serial);

                for (int i = 0; i < list.Length; i++)
                {
                    ref var it = ref list.Buffer[i];

                    //if (!SerialHelper.IsValid(it.Serial))
                    //    break;

                    Entity entity = _world.Get(it.Serial);

                    if (entity == null || entity.IsDestroyed)
                    {
                        continue;
                    }

                    //entity.BoatDirection = direction;

                    if (removeStep)
                    {
                        entity.X = (ushort) (x - it.X);
                        entity.Y = (ushort) (y - it.Y);
                        entity.Z = (sbyte) (z - it.Z);
                        entity.UpdateScreenPosition();

                        entity.Offset.X = 0;
                        entity.Offset.Y = 0;
                        entity.Offset.Z = 0;

                        if (entity.TPrevious != null || entity.TNext != null)
                        {
                            entity.AddToTile();
                        }
                    }
                    else
                    {
                        if (item != null)
                        {
                            entity.Offset = item.Offset;
                        }
                    }
                }
            }
        }

        private void GetEndPosition
        (
            Item item,
            Deque<BoatStep> deque,
            out ushort x,
            out ushort y,
            out sbyte z,
            out Direction dir
        )
        {
            if (deque.Count == 0)
            {
                x = item.X;
                y = item.Y;
                z = item.Z;
                dir = item.Direction & Direction.Up;
                dir &= Direction.Running;
            }
            else
            {
                ref BoatStep s = ref deque.Back();
                x = s.X;
                y = s.Y;
                z = s.Z;
                dir = s.MovingDir;
            }
        }

        private struct BoatStep
        {
            public uint Serial;
            public int TimeDiff;
            public ushort X, Y;
            public sbyte Z;
            public byte Speed;
            public Direction MovingDir, FacingDir;
        }

        private struct ItemInside
        {
            public uint Serial;
            public int X, Y, Z;
        }
    }
}
