// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Assets;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    internal sealed class MovingEffect : GameEffect
    {
        public MovingEffect
        (
            World world,
            EffectManager manager,
            uint src,
            uint trg,
            ushort xSource,
            ushort ySource,
            sbyte zSource,
            ushort xTarget,
            ushort yTarget,
            sbyte zTarget,
            ushort graphic,
            ushort hue,
            bool fixedDir,
            int duration,
            byte speed,
            byte layer = 0xFF
        ) : base(world, manager, graphic, hue, 0, speed)
        {
            FixedDir = fixedDir;

            // we override interval time with speed
            var d = Constants.ITEM_EFFECT_ANIMATION_DELAY * 2;

            IntervalInMs = (uint)(d + (speed * d));

            // moving effects want a +22 to the X
            Offset.X += 22;

            Entity source = World.Get(src);

            if (SerialHelper.IsValid(src) && source != null)
            {
                SetSource(source);
            }
            else
            {
                SetSource(xSource, ySource, zSource);
            }

            // Apply hand offset for projectiles (ranged weapons + directed spells)
            if (source is Mobile mobile)
            {
                ApplyProjectileOffset(mobile, layer);
            }

            Entity target = World.Get(trg);

            if (SerialHelper.IsValid(trg) && target != null)
            {
                SetTarget(target);
            }
            else
            {
                SetTarget(xTarget, yTarget, zTarget);
            }
        }

        public readonly bool FixedDir;

        private void ApplyProjectileOffset(Mobile mobile, byte layer)
        {
            var dir = mobile.Direction & Direction.Mask;
            var animGroup = mobile.AnimationGroup;
            float ox, oy;

            switch (layer)
            {
                default: return;

                // Explicit RightHand from 0xC7 packet
                case 1:

                // No layer info (0xC0 packet) — heuristic
                case 0xFF:
                    (ox, oy) = animGroup switch
                    {
                        (byte)PeopleAnimationGroup.AttackBow => GetRangedAttackOffset(dir, false),
                        (byte)PeopleAnimationGroup.OnmountAttackBow => GetRangedAttackOffset(dir, true),
                        (byte)PeopleAnimationGroup.AttackCrossbow => GetRangedXBowAttackOffset(dir, false),
                        (byte)PeopleAnimationGroup.OnmountAttackCrossbow => GetRangedXBowAttackOffset(dir, true),
                        (byte)PeopleAnimationGroup.CastDirected => GetSpellCastOffset(dir),
                        _ => (0, 0)
                    };
                    break;
            }

            Offset.X += ox;
            Offset.Y += oy;
        }

        private static (float x, float y) GetRangedXBowAttackOffset(Direction dir, bool mounted)
        {
            if (mounted)
            {
                return dir switch
                {
                    Direction.North => (-10, -57),
                    Direction.Right => (-17, -62),
                    Direction.East  => (-16, -68),
                    Direction.Down  => (-5, -73),
                    Direction.South => (8, -73),
                    Direction.Left  => (5, -73),
                    Direction.West  => (16, -68),
                    Direction.Up    => (17, -62),
                    _ => (0, 0)
                };
            }

            return dir switch
            {
                Direction.North => (-1, -36),
                Direction.Right => (-10, -39),
                Direction.East  => (-13, -43),
                Direction.Down  => (-9, -48),
                Direction.South => (1, -50),
                Direction.Left  => (9, -48),
                Direction.West  => (13, -43),
                Direction.Up    => (10, -39),
                _ => (0, 0)
            };
        }

        private static (float x, float y) GetRangedAttackOffset(Direction dir, bool mounted)
        {
            if (mounted)
            {
                return dir switch
                {
                    Direction.North => (4, -61),
                    Direction.Right => (-8, -52),
                    Direction.East  => (-23, -57),
                    Direction.Down  => (-25, -68),
                    Direction.South => (-11, -75),
                    Direction.Left  => (25, -68),
                    Direction.West  => (23, -57),
                    Direction.Up    => (8, -52),
                    _ => (0, 0)
                };
            }

            return dir switch
            {
                Direction.North => (-3, -40),
                Direction.Right => (-15, -39),
                Direction.East  => (-20, -44),
                Direction.Down  => (-12, -48),
                Direction.South => (4, -50),
                Direction.Left  => (12, -48),
                Direction.West  => (20, -44),
                Direction.Up    => (15, -39),
                _ => (0, 0)
            };
        }

        private static (float x, float y) GetSpellCastOffset(Direction dir)
        {
            return dir switch
            {
                Direction.North => (0, -15),
                Direction.Right => (-28, -23),  // NE
                Direction.East  => (-38, -36),
                Direction.Down  => (-25, -48),  // SE
                Direction.South => (1, -50),
                Direction.Left  => (25, -48),   // SW
                Direction.West  => (38, -36),
                Direction.Up    => (28, -23),   // NW
                _ => (0, 0)
            };
        }

        public override void Update()
        {
            base.Update();
            UpdateOffset();
        }


        private void UpdateOffset()
        {
            if (Target != null && Target.IsDestroyed)
            {
                TargetX = Target.X;
                TargetY = Target.Y;
                TargetZ = Target.Z;
            }

            int playerX = World.Player.X;
            int playerY = World.Player.Y;
            int playerZ = World.Player.Z;

            (int sX, int sY, int sZ) = GetSource();
            int offsetSourceX = sX - playerX;
            int offsetSourceY = sY - playerY;
            int offsetSourceZ = sZ - playerZ;

            (int tX, int tY, int tZ) = GetTarget();
            int offsetTargetX = tX - playerX;
            int offsetTargetY = tY - playerY;
            int offsetTargetZ = tZ - playerZ;

            Vector2 source = new Vector2((offsetSourceX - offsetSourceY) * 22, (offsetSourceX + offsetSourceY) * 22 - offsetSourceZ * 4);

            source.X += Offset.X;
            source.Y += Offset.Y;

            Vector2 target = new Vector2((offsetTargetX - offsetTargetY) * 22, (offsetTargetX + offsetTargetY) * 22 - offsetTargetZ * 4);

            var offset = target - source;
            var distance = offset.Length();
            var frameIndependentSpeed = IntervalInMs * Time.Delta;
            Vector2 s0;

            if (distance > frameIndependentSpeed)
            {
                offset.Normalize();
                s0 = offset * frameIndependentSpeed;
            }
            else
            {
                s0 = target;
            }


            if (distance <= 22)
            {
                RemoveMe();

                return;
            }

            int newOffsetX = (int) (source.X / 22f);
            int newOffsetY = (int) (source.Y / 22f);

            TileOffsetOnMonitorToXY(ref newOffsetX, ref newOffsetY, out int newCoordX, out int newCoordY);

            int newX = playerX + newCoordX;
            int newY = playerY + newCoordY;

            if (newX == tX && newY == tY)
            {
                RemoveMe();

                return;
            }


            IsPositionChanged = true;
            AngleToTarget = (float) Math.Atan2(-offset.Y, -offset.X);

            if (newX != sX || newY != sY)
            {
                // TODO: Z is wrong. We have to calculate an average
                SetSource((ushort) newX, (ushort) newY, (sbyte)sZ);

                Vector2 nextSource = new Vector2((newCoordX - newCoordY) * 22, (newCoordX + newCoordY) * 22 - offsetSourceZ * 4);

                Offset.X = source.X - nextSource.X;
                Offset.Y = source.Y - nextSource.Y;
            }

            Offset.X += s0.X;
            Offset.Y += s0.Y;
        }


        private void RemoveMe()
        {
            CreateExplosionEffect();

            Destroy();
        }

        private static void TileOffsetOnMonitorToXY(ref int ofsX, ref int ofsY, out int x, out int y)
        {
            y = 0;

            if (ofsX == 0)
            {
                x = y = ofsY >> 1;
            }
            else if (ofsY == 0)
            {
                x = ofsX >> 1;
                y = -x;
            }
            else
            {
                int absX = Math.Abs(ofsX);
                int absY = Math.Abs(ofsY);
                x = ofsX;

                if (ofsY > ofsX)
                {
                    if (ofsX < 0 && ofsY < 0)
                    {
                        y = absX - absY;
                    }
                    else if (ofsX > 0 && ofsY > 0)
                    {
                        y = absY - absX;
                    }
                }
                else if (ofsX > ofsY)
                {
                    if (ofsX < 0 && ofsY < 0)
                    {
                        y = -(absY - absX);
                    }
                    else if (ofsX > 0 && ofsY > 0)
                    {
                        y = -(absX - absY);
                    }
                }

                if (y == 0 && ofsY != ofsX)
                {
                    if (ofsY < 0)
                    {
                        y = -(absX + absY);
                    }
                    else
                    {
                        y = absX + absY;
                    }
                }

                y /= 2;
                x += y;
            }
        }
    }
}