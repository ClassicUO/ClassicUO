// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.Boats
{
    internal sealed class BoatMovementHandler : IEventListener
    {
        private readonly World _world;

        public BoatMovementHandler(World world) => _world = world;

        public void Subscribe()
        {
            EventSink.BoatMovingReceived += OnBoatMovingReceived;
        }

        public void Unsubscribe()
        {
            EventSink.BoatMovingReceived -= OnBoatMovingReceived;
        }

        private void OnBoatMovingReceived(BoatMovingReceivedArgs e)
        {
            if (!_world.InGame)
            {
                return;
            }

            uint serial = e.Serial;
            ushort x = e.X;
            ushort y = e.Y;
            ushort z = e.Z;
            Direction movingDirection = (Direction)e.MovingDirection;
            Direction facingDirection = (Direction)e.FacingDirection;

            Item multi = _world.Items.Get(serial);

            if (multi == null)
            {
                return;
            }

            bool smooth =
                ProfileManager.CurrentProfile != null
                && ProfileManager.CurrentProfile.UseSmoothBoatMovement;

            if (smooth)
            {
                _world.BoatMovingManager.AddStep(
                    serial,
                    e.Speed,
                    movingDirection,
                    facingDirection,
                    x,
                    y,
                    (sbyte)z
                );
            }
            else
            {
                multi.SetInWorldTile(x, y, (sbyte)z);

                if (_world.HouseManager.TryGetHouse(serial, out House house))
                {
                    house.Generate(true, true, true);
                }
            }

            var passengers = e.Passengers;
            int count = passengers?.Count ?? 0;

            for (int i = 0; i < count; i++)
            {
                BoatPassenger passenger = passengers[i];
                uint cSerial = passenger.Serial;
                ushort cx = passenger.X;
                ushort cy = passenger.Y;
                ushort cz = passenger.Z;

                if (cSerial == _world.Player)
                {
                    _world.RangeSize.X = cx;
                    _world.RangeSize.Y = cy;
                }

                Entity ent = _world.Get(cSerial);

                if (ent == null)
                {
                    continue;
                }

                if (smooth)
                {
                    _world.BoatMovingManager.PushItemToList(
                        serial,
                        cSerial,
                        x - cx,
                        y - cy,
                        (sbyte)(z - cz)
                    );
                }
                else
                {
                    if (cSerial == _world.Player)
                    {
                        _world.UpdatePlayer(
                            cSerial,
                            ent.Graphic,
                            0,
                            ent.Hue,
                            ent.Flags,
                            cx,
                            cy,
                            (sbyte)cz,
                            0,
                            _world.Player.Direction
                        );
                    }
                    else
                    {
                        _world.UpdateGameObject(
                            cSerial,
                            ent.Graphic,
                            0,
                            (ushort)(ent.Graphic == 0x2006 ? ((Item)ent).Amount : 0),
                            cx,
                            cy,
                            (sbyte)cz,
                            SerialHelper.IsMobile(ent) ? ent.Direction : 0,
                            ent.Hue,
                            ent.Flags,
                            0,
                            0,
                            1
                        );
                    }
                }
            }
        }
    }
}
