// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.IO;

namespace ClassicUO.Network
{
    internal sealed partial class PacketHandlers
    {
        internal static void RegisterMovementHandlers(PacketHandlers h)
        {
            h.Add(0x15, FollowR);
            h.Add(0x20, UpdatePlayer);
            h.Add(0x21, DenyWalk);
            h.Add(0x22, ConfirmWalk);
            h.Add(0x38, Pathfinding);
            h.Add(0x97, MovePlayer);
            h.Add(0xF6, BoatMoving);
        }

        private static void FollowR(World world, ref StackDataReader p)
        {
            uint tofollow = p.ReadUInt32BE();
            uint isfollowing = p.ReadUInt32BE();
        }

        private static void UpdatePlayer(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            ushort graphic = p.ReadUInt16BE();
            byte graphic_inc = p.ReadUInt8();
            ushort hue = p.ReadUInt16BE();
            Flags flags = (Flags)p.ReadUInt8();
            ushort x = p.ReadUInt16BE();
            ushort y = p.ReadUInt16BE();
            ushort serverID = p.ReadUInt16BE();
            Direction direction = (Direction)p.ReadUInt8();
            sbyte z = p.ReadInt8();

            UpdatePlayer(world, serial, graphic, graphic_inc, hue, flags, x, y, z, serverID, direction);
        }

        private static void DenyWalk(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            byte seq = p.ReadUInt8();
            ushort x = p.ReadUInt16BE();
            ushort y = p.ReadUInt16BE();
            Direction direction = (Direction)p.ReadUInt8();
            direction &= Direction.Up;
            sbyte z = p.ReadInt8();

            world.Player.Walker.DenyWalk(seq, x, y, z);
            world.Player.Direction = direction;

            world.Weather.Reset();
        }

        private static void ConfirmWalk(World world, ref StackDataReader p)
        {
            if (world.Player == null)
            {
                return;
            }

            byte seq = p.ReadUInt8();
            byte noto = (byte)(p.ReadUInt8() & ~0x40);

            if (noto == 0 || noto >= 8)
            {
                noto = 0x01;
            }

            world.Player.NotorietyFlag = (NotorietyFlag)noto;
            world.Player.Walker.ConfirmWalk(seq);

            world.Player.AddToTile();
        }

        private static void Pathfinding(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            ushort x = p.ReadUInt16BE();
            ushort y = p.ReadUInt16BE();
            ushort z = p.ReadUInt16BE();

            world.Player.Pathfinder.WalkTo(x, y, z, 0);
        }

        private static void MovePlayer(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            Direction direction = (Direction)p.ReadUInt8();
            world.Player.Walk(direction & Direction.Mask, (direction & Direction.Running) != 0);
        }

        private static void BoatMoving(World world, ref StackDataReader p)
        {
            if (!world.InGame)
            {
                return;
            }

            uint serial = p.ReadUInt32BE();
            byte boatSpeed = p.ReadUInt8();
            Direction movingDirection = (Direction)p.ReadUInt8() & Direction.Mask;
            Direction facingDirection = (Direction)p.ReadUInt8() & Direction.Mask;
            ushort x = p.ReadUInt16BE();
            ushort y = p.ReadUInt16BE();
            ushort z = p.ReadUInt16BE();

            Item multi = world.Items.Get(serial);

            if (multi == null)
            {
                return;
            }

            //multi.LastX = x;
            //multi.LastY = y;

            //if (World.HouseManager.TryGetHouse(serial, out var house))
            //{
            //    foreach (Multi component in house.Components)
            //    {
            //        component.LastX = (ushort) (x + component.MultiOffsetX);
            //        component.LastY = (ushort) (y + component.MultiOffsetY);
            //    }
            //}

            bool smooth =
                ProfileManager.CurrentProfile != null
                && ProfileManager.CurrentProfile.UseSmoothBoatMovement;

            if (smooth)
            {
                world.BoatMovingManager.AddStep(
                    serial,
                    boatSpeed,
                    movingDirection,
                    facingDirection,
                    x,
                    y,
                    (sbyte)z
                );
            }
            else
            {
                //UpdateGameObject(serial,
                //                 multi.Graphic,
                //                 0,
                //                 multi.Amount,
                //                 x,
                //                 y,
                //                 (sbyte) z,
                //                 facingDirection,
                //                 multi.Hue,
                //                 multi.Flags,
                //                 0,
                //                 2,
                //                 1);

                multi.SetInWorldTile(x, y, (sbyte)z);

                if (world.HouseManager.TryGetHouse(serial, out House house))
                {
                    house.Generate(true, true, true);
                }
            }

            int count = p.ReadUInt16BE();

            for (int i = 0; i < count; i++)
            {
                uint cSerial = p.ReadUInt32BE();
                ushort cx = p.ReadUInt16BE();
                ushort cy = p.ReadUInt16BE();
                ushort cz = p.ReadUInt16BE();

                if (cSerial == world.Player)
                {
                    world.RangeSize.X = cx;
                    world.RangeSize.Y = cy;
                }

                Entity ent = world.Get(cSerial);

                if (ent == null)
                {
                    continue;
                }

                //if (SerialHelper.IsMobile(cSerial))
                //{
                //    Mobile m = (Mobile) ent;

                //    if (m.Steps.Count != 0)
                //    {
                //        ref var step = ref m.Steps.Back();

                //        step.X = cx;
                //        step.Y = cy;
                //    }
                //}

                //ent.LastX = cx;
                //ent.LastY = cy;

                if (smooth)
                {
                    world.BoatMovingManager.PushItemToList(
                        serial,
                        cSerial,
                        x - cx,
                        y - cy,
                        (sbyte)(z - cz)
                    );
                }
                else
                {
                    if (cSerial == world.Player)
                    {
                        UpdatePlayer(
                            world,
                            cSerial,
                            ent.Graphic,
                            0,
                            ent.Hue,
                            ent.Flags,
                            cx,
                            cy,
                            (sbyte)cz,
                            0,
                            world.Player.Direction
                        );
                    }
                    else
                    {
                        UpdateGameObject(
                            world,
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

        private static void UpdatePlayer(
            World world,
            uint serial,
            ushort graphic,
            byte graph_inc,
            ushort hue,
            Flags flags,
            ushort x,
            ushort y,
            sbyte z,
            ushort serverID,
            Direction direction
        )
        {
            if (serial == world.Player)
            {
                world.RangeSize.X = x;
                world.RangeSize.Y = y;

                bool olddead = world.Player.IsDead;
                ushort old_graphic = world.Player.Graphic;

                world.Player.CloseBank();
                world.Player.Walker.WalkingFailed = false;
                world.Player.Graphic = graphic;
                world.Player.Direction = direction & Direction.Mask;
                world.Player.FixHue(hue);
                world.Player.Flags = flags;
                world.Player.Walker.DenyWalk(0xFF, -1, -1, -1);

                GameScene gs = Client.Game.GetScene<GameScene>();

                if (gs != null)
                {
                    world.Weather.Reset();
                    gs.UpdateDrawPosition = true;
                }

                // std client keeps the target open!
                /*if (old_graphic != 0 && old_graphic != world.Player.Graphic)
                {
                    if (world.Player.IsDead)
                    {
                        TargetManager.Reset();
                    }
                }*/

                if (olddead != world.Player.IsDead)
                {
                    if (world.Player.IsDead)
                    {
                        world.ChangeSeason(Game.Managers.Season.Desolation, 42);
                    }
                    else
                    {
                        world.ChangeSeason(world.OldSeason, world.OldMusicIndex);
                    }
                }

                world.Player.Walker.ResendPacketResync = false;
                world.Player.CloseRangedGumps();
                world.Player.SetInWorldTile(x, y, z);
                world.Player.UpdateAbilities();
            }
        }
    }
}
