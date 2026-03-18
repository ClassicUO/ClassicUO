// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Assets;
using System.Runtime.CompilerServices;

namespace ClassicUO.Game.Data
{
    public static class AnimationHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetSittingAnimDirection(ref byte dir, ref bool mirror, ref int x, ref int y)
        {
            switch (dir)
            {
                case 0:
                    mirror = true;
                    dir = 3;

                    break;

                case 2:
                    mirror = true;
                    dir = 1;

                    break;

                case 4:
                    mirror = false;
                    dir = 1;

                    break;

                case 6:
                    mirror = false;
                    dir = 3;

                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FixSittingDirection(
            ref byte direction,
            ref bool mirror,
            ref int x,
            ref int y,
            ref AnimationsLoader.SittingInfoData data
        )
        {
            switch (direction)
            {
                case 7:
                case 0:
                    {
                        if (data.Direction1 == -1)
                        {
                            if (direction == 7)
                            {
                                direction = (byte)data.Direction4;
                            }
                            else
                            {
                                direction = (byte)data.Direction2;
                            }
                        }
                        else
                        {
                            direction = (byte)data.Direction1;
                        }

                        break;
                    }

                case 1:
                case 2:
                    {
                        if (data.Direction2 == -1)
                        {
                            if (direction == 1)
                            {
                                direction = (byte)data.Direction1;
                            }
                            else
                            {
                                direction = (byte)data.Direction3;
                            }
                        }
                        else
                        {
                            direction = (byte)data.Direction2;
                        }

                        break;
                    }

                case 3:
                case 4:
                    {
                        if (data.Direction3 == -1)
                        {
                            if (direction == 3)
                            {
                                direction = (byte)data.Direction2;
                            }
                            else
                            {
                                direction = (byte)data.Direction4;
                            }
                        }
                        else
                        {
                            direction = (byte)data.Direction3;
                        }

                        break;
                    }

                case 5:
                case 6:
                    {
                        if (data.Direction4 == -1)
                        {
                            if (direction == 5)
                            {
                                direction = (byte)data.Direction3;
                            }
                            else
                            {
                                direction = (byte)data.Direction1;
                            }
                        }
                        else
                        {
                            direction = (byte)data.Direction4;
                        }

                        break;
                    }
            }

            GetSittingAnimDirection(ref direction, ref mirror, ref x, ref y);

            const int SITTING_OFFSET_X = 8;

            int offsX = SITTING_OFFSET_X;

            if (mirror)
            {
                if (direction == 3)
                {
                    y += 25 + data.MirrorOffsetY;
                    x += offsX - 4;
                }
                else
                {
                    y += data.OffsetY + 9;
                }
            }
            else
            {
                if (direction == 3)
                {
                    y += 23 + data.MirrorOffsetY;
                    x -= 3;
                }
                else
                {
                    y += 10 + data.OffsetY;
                    x -= offsX + 1;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AnimationGroups GetGroupIndex(ushort graphic, AnimationGroupsType animType)
        {
            switch (animType)
            {
                case AnimationGroupsType.Animal:
                    return AnimationGroups.Low;

                case AnimationGroupsType.Monster:
                case AnimationGroupsType.SeaMonster:
                    return AnimationGroups.High;

                case AnimationGroupsType.Human:
                case AnimationGroupsType.Equipment:
                    return AnimationGroups.People;
            }

            return AnimationGroups.High;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte GetDeathAction(
            ushort animID,
            AnimationFlags animFlags,
            AnimationGroupsType animType,
            bool second,
            bool isRunning = false
        )
        {
            //ConvertBodyIfNeeded(ref animID);

            if (animFlags.HasFlag(AnimationFlags.CalculateOffsetByLowGroup))
            {
                animType = AnimationGroupsType.Animal;
            }

            if (animFlags.HasFlag(AnimationFlags.CalculateOffsetLowGroupExtended))
            {
                animType = AnimationGroupsType.Monster;
            }

            switch (animType)
            {
                case AnimationGroupsType.Animal:

                    if (
                        (animFlags & AnimationFlags.Use2IfHittedWhileRunning) != 0
                        || (animFlags & AnimationFlags.CanFlying) != 0
                    )
                    {
                        return 2;
                    }

                    if ((animFlags & AnimationFlags.UseUopAnimation) != 0)
                    {
                        return (byte)(second ? 3 : 2);
                    }

                    return (byte)(
                        second ? LowAnimationGroup.Die2 : LowAnimationGroup.Die1
                    );

                case AnimationGroupsType.SeaMonster:
                    {
                        if (!isRunning)
                        {
                            return 8;
                        }

                        goto case AnimationGroupsType.Monster;
                    }

                case AnimationGroupsType.Monster:

                    if ((animFlags & AnimationFlags.UseUopAnimation) != 0)
                    {
                        return (byte)(second ? 3 : 2);
                    }

                    return (byte)(
                        second ? HighAnimationGroup.Die2 : HighAnimationGroup.Die1
                    );

                case AnimationGroupsType.Human:
                case AnimationGroupsType.Equipment:
                    return (byte)(
                        second ? PeopleAnimationGroup.Die2 : PeopleAnimationGroup.Die1
                    );
            }

            return 0;
        }
    }
}
