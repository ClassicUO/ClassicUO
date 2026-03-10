// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using Microsoft.Xna.Framework;

using MathF = System.MathF;

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

            Entity target = World.Get(trg);

            if (SerialHelper.IsValid(trg) && target != null)
            {
                SetTarget(target);
            }
            else
            {
                SetTarget(xTarget, yTarget, zTarget);
            }

            // Apply hand offset for projectiles (ranged weapons + directed spells).
            // Must be after SetTarget so we can compute the actual angle to target.
            if (source is Mobile mobile)
            {
                ushort tgtX = target != null ? target.X : xTarget;
                ushort tgtY = target != null ? target.Y : yTarget;
                ApplyProjectileOffset(mobile, layer, tgtX, tgtY);
            }

            // Compute target center offset so the arrow flies to chest level
            // rather than feet. MobileView draws at RSP.Y + 19, so target center
            // in effect space = 19 - (Height + CenterY) / 2.
            if (target is Mobile targetMobile)
            {
                Client.Game.UO.Animations.GetAnimationDimensions(
                    0,
                    targetMobile.GetGraphicForAnimation(),
                    0, 0,
                    targetMobile.IsMounted,
                    0,
                    out _,
                    out int tCenterY,
                    out _,
                    out int tHeight
                );
                
                _targetOffsetY = 19 - (tHeight + tCenterY) / 2f;
            }
        }

        public readonly bool FixedDir;

        // Y offset applied to the target position so the arrow flies to
        // the target's center/chest rather than their feet (tile anchor).
        private float _targetOffsetY;

        private void ApplyProjectileOffset(Mobile mobile, byte layer, ushort targetX, ushort targetY)
        {
            if (layer != 1 && layer != 0xFF)
                return;

            var dir = mobile.Direction & Direction.Mask;
            float ax, ay;

            // Graphic-based detection (animation group is unreliable because
            // the animation packet arrives after the effect packet).
            switch (Graphic)
            {
                case 0x36E4: // Magic Arrow
                case 0x36D4: // Fireball
                case 0x379F: // Energy Bolt
                    (ax, ay) = GetSpellCastOffset(dir);
                    break;

                case 0xF42:  // Arrow (all bows)
                case 0x1BFE: // Bolt (all crossbows)
                {
                    bool isBow = Graphic == 0xF42;

                    if (isBow)
                        (ax, ay) = GetRangedAttackOffset(dir, mobile.IsMounted);
                    else
                        (ax, ay) = GetRangedXBowAttackOffset(dir, mobile.IsMounted);

                    // Rotation compensation: place the sprite reference point at
                    // the grip. The sprite rotates around its top-left anchor, so
                    // the reference ends up at anchor + rotated(refX, refY).
                    // Solve: anchor = grip - rotated(refX, refY).
                    //
                    // Reference point in unrotated sprite coords (44x44).
                    // At angle=0 (Left dir) the arrowhead is at (0,22).
                    const float refX = 0f;
                    const float refY = 22f;

                    // Actual angle from source→target so compensation stays
                    // accurate even when targets move off-angle.
                    float dTileX = targetX - mobile.X;
                    float dTileY = targetY - mobile.Y;
                    float screenDX = (dTileX - dTileY) * 22f;
                    float screenDY = (dTileX + dTileY) * 22f;
                    float angle = MathF.Atan2(-screenDY, -screenDX);

                    float rotRefX = refX * MathF.Cos(angle) - refY * MathF.Sin(angle);
                    float rotRefY = refX * MathF.Sin(angle) + refY * MathF.Cos(angle);
                    ax -= rotRefX;
                    ay -= rotRefY;
                    break;
                }

                default:
                    // Unknown projectile — fall back to generic head anchor.
                    Client.Game.UO.Animations.GetAnimationDimensions(
                        0,
                        mobile.GetGraphicForAnimation(),
                        0, 0,
                        mobile.IsMounted,
                        0,
                        out _,
                        out int headCenterY,
                        out _,
                        out int headHeight
                    );

                    ax = 0;
                    ay = -(headHeight + headCenterY);
                    break;
            }

            Offset.X += ax;
            Offset.Y += ay;
        }

        /// <summary>
        /// Per-direction (X, Y) offsets for crossbow/heavy-xbow projectiles.
        /// Same NOCK-BASED convention as bow offsets.
        /// Computed analytically: ax = gripX, ay = gripY + 19.
        /// </summary>
        private static (float x, float y) GetRangedXBowAttackOffset(Direction dir, bool mounted)
        {
            if (mounted)
            {
                // MountedShootXBow (group 28)
                return dir switch
                {
                    Direction.North => (25, -64),
                    Direction.Right => (36, -49),
                    Direction.East  => (27, -23),
                    Direction.Down  => (-10, -18),
                    Direction.South => (-37, -28),
                    Direction.Left  => (-36, -49),
                    Direction.West  => (-25, -64),
                    Direction.Up    => (8, -54),
                    _ => (0, 0)
                };
            }

            // ShootXBow (group 19)
            return dir switch
            {
                Direction.North => (9, -29),
                Direction.Right => (13, -24),
                Direction.East  => (10, -20),
                Direction.Down  => (-1, 3),
                Direction.South => (-10, -20),
                Direction.Left  => (-13, -24),
                Direction.West  => (-9, -29),
                Direction.Up    => (1, -31),
                _ => (0, 0)
            };
        }

        /// <summary>
        /// Per-direction (X, Y) offsets for bow projectiles.
        /// NOCK-BASED: (ax, ay) = desired screen position of the arrow nock relative
        /// to the source tile anchor. Rotation compensation applied automatically.
        /// Computed analytically: ax = gripX, ay = gripY + 19 (MobileView gap).
        /// Grip data from AnimFrameAnalyzer averaged across all bow types.
        /// CUO dir → Analyzer dir remap: (d+5)%8.
        /// Mirror pairs: North↔West, Right↔Left, East↔South (negate X, same Y).
        /// </summary>
        private static (float x, float y) GetRangedAttackOffset(Direction dir, bool mounted)
        {
            if (mounted)
            {
                // MountedShootBow (group 27)
                return dir switch
                {
                    Direction.North => (25, -49),
                    Direction.Right => (23, -38),
                    Direction.East  => (8, -33),
                    Direction.Down  => (9, -22),
                    Direction.South => (-8, -33),
                    Direction.Left  => (-23, -38),
                    Direction.West  => (-25, -49),
                    Direction.Up    => (-11, -56),
                    _ => (0, 0)
                };
            }

            // ShootBow (group 18)
            return dir switch
            {
                Direction.North => (12, -29),
                Direction.Right => (20, -25),
                Direction.East  => (15, -20),
                Direction.Down  => (-3, 4),
                Direction.South => (-15, -20),
                Direction.Left  => (-20, -25),
                Direction.West  => (-12, -29),
                Direction.Up    => (4, -31),
                _ => (0, 0)
            };
        }

        private static (float x, float y) GetSpellCastOffset(Direction dir)
        {
            // CastDirected (group 16) — remapped: CUO dir → Analyzer dir (d+5)%8
            return dir switch
            {
                Direction.North => (25, -48),
                Direction.Right => (38, -36),
                Direction.East  => (28, -23),
                Direction.Down  => (0, -15),
                Direction.South => (-28, -23),
                Direction.Left  => (-38, -36),
                Direction.West  => (-25, -48),
                Direction.Up    => (1, -50),
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

            target.Y += _targetOffsetY;

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