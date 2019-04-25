#region license
//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//      
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion
using System;
using System.Collections.Generic;

using ClassicUO.Game.Data;
using ClassicUO.IO;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.GameObjects
{
    internal partial class Mobile
    {
        public override Graphic GetGraphicForAnimation()
        {
            ushort g = Graphic;

            switch (g)
            {
                case 0x0192:
                case 0x0193:

                {
                    g -= 2;

                    break;
                }
            }

            return g;
        }

        public Direction GetDirectionForAnimation()
        {
            return Steps.Count != 0 ? (Direction) Steps.Front().Direction : Direction;
        }


        private static void CalculateHight(Mobile mobile, ANIMATION_FLAGS flags, bool isrun, bool iswalking, ref byte result)
        {
            if (mobile.AnimationGroup != 0xFF)
            {
                result = mobile.AnimationGroup;
                return;
            }

            if ((flags & ANIMATION_FLAGS.AF_CALCULATE_OFFSET_BY_PEOPLE_GROUP) != 0)
            {
                result = 0;
            }
            else if ((flags & ANIMATION_FLAGS.AF_CALCULATE_OFFSET_BY_LOW_GROUP) != 0)
            {
                if (!iswalking)
                    result = 2;
                else if (isrun)
                    result = 1;
                else
                    result = 0;
            }
            else
            {
                if (mobile.IsFlying)
                    result = 19;
                else if (!iswalking)
                {
                    if ((flags & ANIMATION_FLAGS.AF_IDLE_AT_8_FRAME) != 0)
                        result = 8;
                    else
                        result = 1;
                }
                else if (isrun)
                {
                    if ((flags & ANIMATION_FLAGS.AF_CAN_FLYING) != 0)
                        result = 19;
                    else
                        result = 0;
                }
                else
                {
                    result = 0;
                }
            }
        }

        public static byte GetGroupForAnimation(Mobile mobile, ushort checkGraphic = 0, bool isParent = false)
        {
            Graphic graphic = checkGraphic;
            if (graphic == 0)
                graphic = mobile.GetGraphicForAnimation();

            ANIMATION_GROUPS_TYPE type = FileManager.Animations.DataIndex[graphic].Type;

            if (FileManager.Animations.DataIndex[graphic].IsUOP && (isParent || !FileManager.Animations.DataIndex[graphic].IsValidMUL))
            {
                // do nothing ?
            }
            else
            {
                if (!FileManager.Animations.DataIndex[graphic].HasBodyReplaced)
                {
                    ushort newGraphic = FileManager.Animations.DataIndex[graphic].Graphic;

                    if (graphic != newGraphic)
                    {
                        graphic = newGraphic;
                        ANIMATION_GROUPS_TYPE newType = FileManager.Animations.DataIndex[graphic].Type;

                        if (newType != type)
                        {
                            type = newType;
                        }
                    }
                }
            }

           

            ANIMATION_FLAGS flags = (ANIMATION_FLAGS) FileManager.Animations.DataIndex[graphic].Flags;

            //ANIMATION_GROUPS groupIndex = FileManager.Animations.GetGroupIndex(graphic, isequip);
            byte result = 0; // mobile.AnimationGroup;


            //if (result != 0xFF && (mobile.Serial & 0x80000000) == 0 && (!mobile.AnimationFromServer || checkGraphic != 0))
            //{
            //    GetGroupForAnimation(groupIndex, ref result);

            //    if (!FileManager.Animations.AnimationExists(graphic, result, isequip))
            //        CorrectAnimationGroup(graphic, groupIndex, ref result);
            //}

            bool isWalking = mobile.IsWalking;
            bool isRun = mobile.IsRunning;

            if (mobile.Steps.Count != 0)
            {
                isWalking = true;
                isRun = mobile.Steps.Front().Run;
            }

            switch (type)
            {
                case ANIMATION_GROUPS_TYPE.ANIMAL:

                    if (mobile.AnimationGroup != 0xFF)
                    {
                        result = mobile.AnimationGroup;
                        break;
                    }

                    if ((flags & ANIMATION_FLAGS.AF_CALCULATE_OFFSET_LOW_GROUP_EXTENDED) != 0)
                    {
                        CalculateHight(mobile, flags, isRun, isWalking, ref result);
                    }
                    else
                    {
                        if (!isWalking)
                            result = 2;
                        else if (isRun)
                            result = 1;
                        else
                            result = 0;
                    }
                    break;
                case ANIMATION_GROUPS_TYPE.MONSTER:
                    CalculateHight(mobile, flags, isRun, isWalking, ref result);
                    break;
                case ANIMATION_GROUPS_TYPE.SEA_MONSTER:
                    if (!isWalking)
                        result = 2;
                    else if (isRun)
                        result = 1;
                    else
                        result = 0;
                    break;
                default:
                {
                    bool inWar = mobile.InWarMode;

                    if (isWalking)
                    {
                        if (isRun)
                        {
                            if (mobile.IsMounted)
                                result = (byte) PEOPLE_ANIMATION_GROUP.PAG_ONMOUNT_RIDE_FAST;
                            else if (mobile.Equipment[(int) Layer.OneHanded] != null || mobile.Equipment[(int) Layer.TwoHanded] != null)
                                result = (byte) PEOPLE_ANIMATION_GROUP.PAG_RUN_ARMED;
                            else
                                result = (byte) PEOPLE_ANIMATION_GROUP.PAG_RUN_UNARMED;

                            if (!mobile.IsHuman && !FileManager.Animations.AnimationExists(graphic, result))
                            {
                                if (mobile.IsMounted)
                                    result = (byte) PEOPLE_ANIMATION_GROUP.PAG_ONMOUNT_RIDE_SLOW;
                                else if ((mobile.Equipment[(int) Layer.TwoHanded] != null || mobile.Equipment[(int) Layer.OneHanded] != null) && !mobile.IsDead)
                                {
                                    if (inWar)
                                        result = (byte) PEOPLE_ANIMATION_GROUP.PAG_WALK_WARMODE;
                                    else
                                        result = (byte) PEOPLE_ANIMATION_GROUP.PAG_WALK_ARMED;
                                }
                                else if (inWar && !mobile.IsDead)
                                    result = (byte) PEOPLE_ANIMATION_GROUP.PAG_WALK_WARMODE;
                                else
                                    result = (byte) PEOPLE_ANIMATION_GROUP.PAG_WALK_UNARMED;
                            }
                        }
                        else
                        {
                            if (mobile.IsMounted)
                                result = (byte) PEOPLE_ANIMATION_GROUP.PAG_ONMOUNT_RIDE_SLOW;
                            else if ((mobile.Equipment[(int) Layer.OneHanded] != null || mobile.Equipment[(int) Layer.TwoHanded] != null) && !mobile.IsDead)
                            {
                                if (inWar)
                                    result = (byte) PEOPLE_ANIMATION_GROUP.PAG_WALK_WARMODE;
                                else
                                    result = (byte) PEOPLE_ANIMATION_GROUP.PAG_WALK_ARMED;
                            }
                            else if (inWar && !mobile.IsDead)
                                result = (byte) PEOPLE_ANIMATION_GROUP.PAG_WALK_WARMODE;
                            else
                                result = (byte) PEOPLE_ANIMATION_GROUP.PAG_WALK_UNARMED;
                        }
                    }
                    else if (mobile.AnimationGroup == 0xFF)
                    {
                        if (mobile.IsMounted)
                            result = (byte) PEOPLE_ANIMATION_GROUP.PAG_ONMOUNT_STAND;
                        else if (inWar && !mobile.IsDead)
                        {
                            if (mobile.Equipment[(int) Layer.OneHanded] != null)
                                result = (byte) PEOPLE_ANIMATION_GROUP.PAG_STAND_ONEHANDED_ATTACK;
                            else if (mobile.Equipment[(int) Layer.TwoHanded] != null)
                                result = (byte) PEOPLE_ANIMATION_GROUP.PAG_STAND_TWOHANDED_ATTACK;
                            else
                                result = (byte) PEOPLE_ANIMATION_GROUP.PAG_STAND_ONEHANDED_ATTACK;
                        }
                        else
                            result = (byte) PEOPLE_ANIMATION_GROUP.PAG_STAND;

                        mobile.AnimIndex = 0;
                    }
                    else
                    {
                        result = mobile.AnimationGroup;
                    }

               

                    if (mobile.Race == RaceType.GARGOYLE)
                    {
                        if (mobile.IsFlying)
                        {
                            if (result == 0 || result == 1)
                                result = 62;
                            else if (result == 2 || result == 3)
                                result = 63;
                            else if (result == 4)
                                result = 64;
                            else if (result == 6)
                                result = 66;
                            else if (result == 7 || result == 8)
                                result = 65;
                            else if (result >= 9 && result <= 11)
                                result = 71;
                            else if (result >= 12 && result <= 14)
                                result = 72;
                            else if (result == 15)
                                result = 62;
                            else if (result == 20)
                                result = 77;
                            else if (result == 31)
                                result = 71;
                            else if (result == 34)
                                result = 78;
                            else if (result >= 200 && result <= 259)
                                result = 75;
                            else if (result >= 260 && result <= 270) result = 75;


                            return result;
                        }
                    }

                    break;
                }
            }

            //if (!isequip)
            //    CorretAnimationByAnimSequence(groupIndex, graphic, ref result);

            return result;
        }

        private static void CorretAnimationByAnimSequence(ANIMATION_GROUPS type, ushort graphic, ref byte result)
        {
            if (FileManager.Animations.IsReplacedByAnimationSequence(graphic, out byte t))
            {

                switch (type)
                {
                    case ANIMATION_GROUPS.AG_LOW:

                       
                        break;
                    case ANIMATION_GROUPS.AG_HIGHT:

                        if (result == 1)
                        {
                            result = 25;
                            return;
                        }
                       
                        break;
                    case ANIMATION_GROUPS.AG_PEOPLE:
                        if (result == 1)
                        {
                            result = result;
                            return;
                        }
                        break;
                }


                if (result == 4) // people stand
                    result = 25;
                else if (
                        result == 0 || // people walk un armed / high walk
                        result == 1 || // walk armed / high stand
                        result == 15)  // walk warmode
                        result = 22; // 22
                else if (
                        result == 2 || // people run unarmed
                        result == 3 || // people run armed
                        result == 19)  // high fly
                    result = 24;
            }
        }
        public static byte GetReplacedObjectAnimation(Graphic graphic, ushort index)
        {
            ushort getReplacedGroup(IReadOnlyList<Tuple<ushort, byte>> list, ushort idx, ushort walkIdx)
            {
                foreach (Tuple<ushort, byte> item in list)
                {
                    if (item.Item1 == idx)
                    {
                        if (item.Item2 == 0xFF) return walkIdx;

                        return item.Item2;
                    }
                }

                return idx;
            }

            ANIMATION_GROUPS group = FileManager.Animations.GetGroupIndex(graphic);

            if (group == ANIMATION_GROUPS.AG_LOW)
                return (byte) (getReplacedGroup(FileManager.Animations.GroupReplaces[0], index, (ushort) LOW_ANIMATION_GROUP.LAG_WALK) % (ushort) LOW_ANIMATION_GROUP.LAG_ANIMATION_COUNT);

            if (group == ANIMATION_GROUPS.AG_PEOPLE)
                return (byte) (getReplacedGroup(FileManager.Animations.GroupReplaces[1], index, (ushort) PEOPLE_ANIMATION_GROUP.PAG_WALK_UNARMED) % (ushort) PEOPLE_ANIMATION_GROUP.PAG_ANIMATION_COUNT);

            return (byte) (index % (ushort) HIGHT_ANIMATION_GROUP.HAG_ANIMATION_COUNT);
        }

        public static byte GetObjectNewAnimation(Mobile mobile, ushort type, ushort action, byte mode)
        {
            if (mobile.Graphic >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT) return 0;

            switch (type)
            {
                case 0:

                    return GetObjectNewAnimationType_0(mobile, action, mode);
                case 1:
                case 2:

                    return GetObjectNewAnimationType_1_2(mobile, action, mode);
                case 3:

                    return GetObjectNewAnimationType_3(mobile, action, mode);
                case 4:

                    return GetObjectNewAnimationType_4(mobile, action, mode);
                case 5:

                    return GetObjectNewAnimationType_5(mobile, action, mode);
                case 6:
                case 14:

                    return GetObjectNewAnimationType_6_14(mobile, action, mode);
                case 7:

                    return GetObjectNewAnimationType_7(mobile, action, mode);
                case 8:

                    return GetObjectNewAnimationType_8(mobile, action, mode);
                case 9:
                case 10:

                    return GetObjectNewAnimationType_9_10(mobile, action, mode);
                case 11:

                    return GetObjectNewAnimationType_11(mobile, action, mode);
            }

            return 0;
        }

        private static bool TestStepNoChangeDirection( Mobile mob, byte group)
        {
            switch ( (PEOPLE_ANIMATION_GROUP) group)
            {
                case PEOPLE_ANIMATION_GROUP.PAG_ONMOUNT_RIDE_FAST:
                case PEOPLE_ANIMATION_GROUP.PAG_RUN_UNARMED:
                case PEOPLE_ANIMATION_GROUP.PAG_RUN_ARMED:
                case PEOPLE_ANIMATION_GROUP.PAG_ONMOUNT_RIDE_SLOW:
                case PEOPLE_ANIMATION_GROUP.PAG_WALK_WARMODE:
                case PEOPLE_ANIMATION_GROUP.PAG_WALK_ARMED:
                case PEOPLE_ANIMATION_GROUP.PAG_WALK_UNARMED:

                    if (mob.IsMoving)
                    {
                        var s = mob.Steps.Front();

                        if (s.X != mob.X || s.Y != mob.Y)
                            return true;
                    }

                    break;
            }

            return false;
        }

        private static byte GetObjectNewAnimationType_0(Mobile mobile, ushort action, byte mode)
        {
            if (action <= 10)
            {
                ref IndexAnimation ia = ref FileManager.Animations.DataIndex[mobile.Graphic];
                ANIMATION_GROUPS_TYPE type = ANIMATION_GROUPS_TYPE.MONSTER;
                if ((ia.Flags & 0x80000000) != 0) type = ia.Type;

                if (type == ANIMATION_GROUPS_TYPE.MONSTER)
                {
                    switch (mode % 4)
                    {
                        case 1:

                            return 5;
                        case 2:

                            return 6;
                        case 3:

                            if ((ia.Flags & 1) != 0) return 12;
                            goto case 0;
                        case 0:

                            return 4;
                    }
                }
                else if (type == ANIMATION_GROUPS_TYPE.SEA_MONSTER)
                {
                    if (mode % 2 != 0)
                        return 6;

                    return 5;
                }
                else if (type != ANIMATION_GROUPS_TYPE.ANIMAL)
                {
                    if (mobile.Equipment[(int) Layer.Mount] != null)
                    {
                        if (action > 0)
                        {
                            if (action == 1) return 27;
                            if (action == 2) return 28;

                            return 26;
                        }

                        return 29;
                    }

                    switch (action)
                    {
                        default:

                            return 31;
                        case 1:

                            return 18;
                        case 2:

                            return 19;
                        case 6:

                            return 12;
                        case 7:

                            return 13;
                        case 8:

                            return 14;
                        case 3:

                            return 11;
                        case 4:

                            return 9;
                        case 5:

                            return 10;
                    }
                }

                if (mode % 2 != 0)
                    return 6;

                return 5;
            }

            return 0;
        }

        private static byte GetObjectNewAnimationType_1_2(Mobile mobile, ushort action, byte mode)
        {
            ref IndexAnimation ia = ref FileManager.Animations.DataIndex[mobile.Graphic];
            ANIMATION_GROUPS_TYPE type = ANIMATION_GROUPS_TYPE.MONSTER;

            if ((ia.Flags & 0x80000000) != 0)
                type = ia.Type;

            if (type != ANIMATION_GROUPS_TYPE.MONSTER)
            {
                if (type <= ANIMATION_GROUPS_TYPE.ANIMAL || mobile.Equipment[(int) Layer.Mount] != null) return 0xFF;

                return 30;
            }

            if (mode % 2 != 0) return 15;

            return 16;
        }

        private static byte GetObjectNewAnimationType_3(Mobile mobile, ushort action, byte mode)
        {
            ref IndexAnimation ia = ref FileManager.Animations.DataIndex[mobile.Graphic];
            ANIMATION_GROUPS_TYPE type = ANIMATION_GROUPS_TYPE.MONSTER;
            if ((ia.Flags & 0x80000000) != 0) type = ia.Type;

            if (type != ANIMATION_GROUPS_TYPE.MONSTER)
            {
                if (type == ANIMATION_GROUPS_TYPE.SEA_MONSTER) return 8;

                if (type == ANIMATION_GROUPS_TYPE.ANIMAL)
                {
                    if (mode % 2 != 0) return 21;

                    return 22;
                }

                if (mode % 2 != 0) return 8;

                return 12;
            }

            if (mode % 2 != 0) return 2;

            return 3;
        }

        private static byte GetObjectNewAnimationType_4(Mobile mobile, ushort action, byte mode)
        {
            ref IndexAnimation ia = ref FileManager.Animations.DataIndex[mobile.Graphic];
            ANIMATION_GROUPS_TYPE type = ANIMATION_GROUPS_TYPE.MONSTER;
            if ((ia.Flags & 0x80000000) != 0) type = ia.Type;

            if (type != ANIMATION_GROUPS_TYPE.MONSTER)
            {
                if (type > ANIMATION_GROUPS_TYPE.ANIMAL)
                {
                    if (mobile.Equipment[(int) Layer.Mount] != null) return 0xFF;

                    return 20;
                }

                return 7;
            }

            return 10;
        }

        private static byte GetObjectNewAnimationType_5(Mobile mobile, ushort action, byte mode)
        {
            ref IndexAnimation ia = ref FileManager.Animations.DataIndex[mobile.Graphic];
            ANIMATION_GROUPS_TYPE type = ANIMATION_GROUPS_TYPE.MONSTER;
            if ((ia.Flags & 0x80000000) != 0) type = ia.Type;

            if (type <= ANIMATION_GROUPS_TYPE.SEA_MONSTER)
            {
                if (mode % 2 != 0) return 18;

                return 17;
            }

            if (type != ANIMATION_GROUPS_TYPE.ANIMAL)
            {
                if (mobile.Equipment[(int) Layer.Mount] != null) return 0xFF;
                if (mode % 2 != 0) return 6;

                return 5;
            }

            switch (mode % 3)
            {
                case 1:

                    return 10;
                case 2:

                    return 3;
            }

            return 9;
        }

        private static byte GetObjectNewAnimationType_6_14(Mobile mobile, ushort action, byte mode)
        {
            ref IndexAnimation ia = ref FileManager.Animations.DataIndex[mobile.Graphic];
            ANIMATION_GROUPS_TYPE type = ANIMATION_GROUPS_TYPE.MONSTER;
            if ((ia.Flags & 0x80000000) != 0) type = ia.Type;

            if (type != ANIMATION_GROUPS_TYPE.MONSTER)
            {
                if (type != ANIMATION_GROUPS_TYPE.SEA_MONSTER)
                {
                    if (type == ANIMATION_GROUPS_TYPE.ANIMAL) return 3;
                    if (mobile.Equipment[(int) Layer.Mount] != null) return 0xFF;

                    return 34;
                }

                return 5;
            }

            return 11;
        }

        private static byte GetObjectNewAnimationType_7(Mobile mobile, ushort action, byte mode)
        {
            if (mobile.Equipment[(int) Layer.Mount] != null) return 0xFF;

            if (action > 0)
            {
                if (action == 1) return 33;
            }
            else
                return 32;

            return 0;
        }

        private static byte GetObjectNewAnimationType_8(Mobile mobile, ushort action, byte mode)
        {
            ref IndexAnimation ia = ref FileManager.Animations.DataIndex[mobile.Graphic];
            ANIMATION_GROUPS_TYPE type = ANIMATION_GROUPS_TYPE.MONSTER;
            if ((ia.Flags & 0x80000000) != 0) type = ia.Type;

            if (type != ANIMATION_GROUPS_TYPE.MONSTER)
            {
                if (type != ANIMATION_GROUPS_TYPE.SEA_MONSTER)
                {
                    if (type == ANIMATION_GROUPS_TYPE.ANIMAL) return 9;

                    return mobile.Equipment[(int) Layer.Mount] != null ? (byte) 0xFF : (byte) 33;
                }

                return 3;
            }

            return 11;
        }

        private static byte GetObjectNewAnimationType_9_10(Mobile mobile, ushort action, byte mode)
        {
            ref IndexAnimation ia = ref FileManager.Animations.DataIndex[mobile.Graphic];
            ANIMATION_GROUPS_TYPE type = ANIMATION_GROUPS_TYPE.MONSTER;
            if ((ia.Flags & 0x80000000) != 0) type = ia.Type;

            return type != ANIMATION_GROUPS_TYPE.MONSTER ? (byte) 0xFF : (byte) 20;
        }

        private static byte GetObjectNewAnimationType_11(Mobile mobile, ushort action, byte mode)
        {
            ref IndexAnimation ia = ref FileManager.Animations.DataIndex[mobile.Graphic];
            ANIMATION_GROUPS_TYPE type = ANIMATION_GROUPS_TYPE.MONSTER;
            if ((ia.Flags & 0x80000000) != 0) type = ia.Type;

            if (type != ANIMATION_GROUPS_TYPE.MONSTER)
            {
                if (type >= ANIMATION_GROUPS_TYPE.ANIMAL)
                {
                    if (mobile.Equipment[(int) Layer.Mount] != null) return 0xFF;

                    switch (action)
                    {
                        case 1:
                        case 2:

                            return 17;
                    }

                    return 16;
                }

                return 5;
            }

            return 12;
        }
    }
}