// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Network;
using ClassicUO.Utility.Collections;

namespace ClassicUO.Game.Boats
{
    /// <summary>
    /// Concrete <see cref="IBoatStepQueue"/>. Stores a <see cref="Deque{T}"/>
    /// of <see cref="BoatStep"/> per boat serial and tracks the wall-clock
    /// timestamp of the last outgoing/incoming packet to translate server
    /// pacing into per-step delays.
    /// </summary>
    internal sealed class BoatStepQueue : IBoatStepQueue
    {
        private const int SLOW_INTERVAL = 1000;
        private const int NORMAL_INTERVAL = 500;
        private const int FAST_INTERVAL = 250;

        private readonly Dictionary<uint, Deque<BoatStep>> _steps = new Dictionary<uint, Deque<BoatStep>>();
        private uint _timePacket;

        public IEnumerable<Deque<BoatStep>> AllDeques => _steps.Values;

        public void MoveRequest(World world, Direction direction, byte speed)
        {
            NetClient.Socket.Send_MultiBoatMoveRequest(world.Player, direction, speed);
            _timePacket = Time.Ticks;
        }

        public void AddStep
        (
            World world,
            uint serial,
            byte speed,
            Direction movingDir,
            Direction facingDir,
            ushort x,
            ushort y,
            sbyte z
        )
        {
            Item item = world.Items.Get(serial);

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

            if (empty)
            {
                item.LastStepTime = Time.Ticks;
            }

            BoatStep step = new BoatStep();
            step.Serial = serial;
            step.TimeDiff = _timePacket == 0 || empty ? GetVelocity(speed) : (int)(Time.Ticks - _timePacket);
            step.Speed = speed;
            step.X = x;
            step.Y = y;
            step.Z = z;
            step.MovingDir = movingDir;
            deque.AddToBack(step);

            _timePacket = Time.Ticks;
            Console.WriteLine("CURRENT PACKET TIME: {0}", _timePacket);
        }

        public Item ClearSteps(World world, uint serial)
        {
            if (!_steps.TryGetValue(serial, out Deque<BoatStep> deque) || deque.Count == 0)
            {
                return null;
            }

            Item multiItem = world.Items.Get(serial);
            deque.Clear();
            return multiItem;
        }

        public void Remove(uint serial)
        {
            _steps.Remove(serial);
        }

        private static int GetVelocity(byte speed)
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
    }
}
