#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
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
using ClassicUO.IO.Resources;
using System;
using System.Collections.Generic;

namespace ClassicUO.Game.GameObjects
{
    public partial class Mobile
    {
        private static readonly byte[,] _animAssociateTable = { { (byte)LOW_ANIMATION_GROUP.LAG_WALK, (byte)HIGHT_ANIMATION_GROUP.HAG_WALK, (byte)PEOPLE_ANIMATION_GROUP.PAG_WALK_UNARMED }, { (byte)LOW_ANIMATION_GROUP.LAG_WALK, (byte)HIGHT_ANIMATION_GROUP.HAG_WALK, (byte)PEOPLE_ANIMATION_GROUP.PAG_WALK_ARMED }, { (byte)LOW_ANIMATION_GROUP.LAG_RUN, (byte)HIGHT_ANIMATION_GROUP.HAG_FLY, (byte)PEOPLE_ANIMATION_GROUP.PAG_RUN_UNARMED }, { (byte)LOW_ANIMATION_GROUP.LAG_RUN, (byte)HIGHT_ANIMATION_GROUP.HAG_FLY, (byte)PEOPLE_ANIMATION_GROUP.PAG_RUN_ARMED }, { (byte)LOW_ANIMATION_GROUP.LAG_STAND, (byte)HIGHT_ANIMATION_GROUP.HAG_STAND, (byte)PEOPLE_ANIMATION_GROUP.PAG_STAND }, { (byte)LOW_ANIMATION_GROUP.LAG_FIDGET_1, (byte)HIGHT_ANIMATION_GROUP.HAG_FIDGET_1, (byte)PEOPLE_ANIMATION_GROUP.PAG_FIDGET_1 }, { (byte)LOW_ANIMATION_GROUP.LAG_FIDGET_2, (byte)HIGHT_ANIMATION_GROUP.HAG_FIDGET_2, (byte)PEOPLE_ANIMATION_GROUP.PAG_FIDGET_2 }, { (byte)LOW_ANIMATION_GROUP.LAG_STAND, (byte)HIGHT_ANIMATION_GROUP.HAG_STAND, (byte)PEOPLE_ANIMATION_GROUP.PAG_STAND_ONEHANDED_ATTACK }, { (byte)LOW_ANIMATION_GROUP.LAG_STAND, (byte)HIGHT_ANIMATION_GROUP.HAG_STAND, (byte)PEOPLE_ANIMATION_GROUP.PAG_STAND_TWOHANDED_ATTACK }, { (byte)LOW_ANIMATION_GROUP.LAG_EAT, (byte)HIGHT_ANIMATION_GROUP.HAG_ATTACK_3, (byte)PEOPLE_ANIMATION_GROUP.PAG_ATTACK_ONEHANDED }, { (byte)LOW_ANIMATION_GROUP.LAG_EAT, (byte)HIGHT_ANIMATION_GROUP.HAG_ATTACK_1, (byte)PEOPLE_ANIMATION_GROUP.PAG_ATTACK_UNARMED_1 }, { (byte)LOW_ANIMATION_GROUP.LAG_EAT, (byte)HIGHT_ANIMATION_GROUP.HAG_ATTACK_2, (byte)PEOPLE_ANIMATION_GROUP.PAG_ATTACK_UNARMED_2 }, { (byte)LOW_ANIMATION_GROUP.LAG_EAT, (byte)HIGHT_ANIMATION_GROUP.HAG_ATTACK_3, (byte)PEOPLE_ANIMATION_GROUP.PAG_ATTACK_TWOHANDED_DOWN }, { (byte)LOW_ANIMATION_GROUP.LAG_EAT, (byte)HIGHT_ANIMATION_GROUP.HAG_ATTACK_1, (byte)PEOPLE_ANIMATION_GROUP.PAG_ATTACK_TWOHANDED_WIDE }, { (byte)LOW_ANIMATION_GROUP.LAG_EAT, (byte)HIGHT_ANIMATION_GROUP.HAG_ATTACK_2, (byte)PEOPLE_ANIMATION_GROUP.PAG_ATTACK_TWOHANDED_JAB }, { (byte)LOW_ANIMATION_GROUP.LAG_WALK, (byte)HIGHT_ANIMATION_GROUP.HAG_WALK, (byte)PEOPLE_ANIMATION_GROUP.PAG_WALK_WARMODE }, { (byte)LOW_ANIMATION_GROUP.LAG_EAT, (byte)HIGHT_ANIMATION_GROUP.HAG_ATTACK_2, (byte)PEOPLE_ANIMATION_GROUP.PAG_CAST_DIRECTED }, { (byte)LOW_ANIMATION_GROUP.LAG_EAT, (byte)HIGHT_ANIMATION_GROUP.HAG_ATTACK_3, (byte)PEOPLE_ANIMATION_GROUP.PAG_CAST_AREA }, { (byte)LOW_ANIMATION_GROUP.LAG_EAT, (byte)HIGHT_ANIMATION_GROUP.HAG_ATTACK_1, (byte)PEOPLE_ANIMATION_GROUP.PAG_ATTACK_BOW }, { (byte)LOW_ANIMATION_GROUP.LAG_EAT, (byte)HIGHT_ANIMATION_GROUP.HAG_ATTACK_2, (byte)PEOPLE_ANIMATION_GROUP.PAG_ATTACK_CROSSBOW }, { (byte)LOW_ANIMATION_GROUP.LAG_EAT, (byte)HIGHT_ANIMATION_GROUP.HAG_GET_HIT_1, (byte)PEOPLE_ANIMATION_GROUP.PAG_GET_HIT }, { (byte)LOW_ANIMATION_GROUP.LAG_DIE_1, (byte)HIGHT_ANIMATION_GROUP.HAG_DIE_1, (byte)PEOPLE_ANIMATION_GROUP.PAG_DIE_1 }, { (byte)LOW_ANIMATION_GROUP.LAG_DIE_2, (byte)HIGHT_ANIMATION_GROUP.HAG_DIE_2, (byte)PEOPLE_ANIMATION_GROUP.PAG_DIE_2 }, { (byte)LOW_ANIMATION_GROUP.LAG_WALK, (byte)HIGHT_ANIMATION_GROUP.HAG_WALK, (byte)PEOPLE_ANIMATION_GROUP.PAG_ONMOUNT_RIDE_SLOW }, { (byte)LOW_ANIMATION_GROUP.LAG_RUN, (byte)HIGHT_ANIMATION_GROUP.HAG_FLY, (byte)PEOPLE_ANIMATION_GROUP.PAG_ONMOUNT_RIDE_FAST }, { (byte)LOW_ANIMATION_GROUP.LAG_STAND, (byte)HIGHT_ANIMATION_GROUP.HAG_STAND, (byte)PEOPLE_ANIMATION_GROUP.PAG_ONMOUNT_STAND }, { (byte)LOW_ANIMATION_GROUP.LAG_EAT, (byte)HIGHT_ANIMATION_GROUP.HAG_ATTACK_1, (byte)PEOPLE_ANIMATION_GROUP.PAG_ONMOUNT_ATTACK }, { (byte)LOW_ANIMATION_GROUP.LAG_EAT, (byte)HIGHT_ANIMATION_GROUP.HAG_ATTACK_2, (byte)PEOPLE_ANIMATION_GROUP.PAG_ONMOUNT_ATTACK_BOW }, { (byte)LOW_ANIMATION_GROUP.LAG_EAT, (byte)HIGHT_ANIMATION_GROUP.HAG_ATTACK_1, (byte)PEOPLE_ANIMATION_GROUP.PAG_ONMOUNT_ATTACK_CROSSBOW }, { (byte)LOW_ANIMATION_GROUP.LAG_EAT, (byte)HIGHT_ANIMATION_GROUP.HAG_ATTACK_2, (byte)PEOPLE_ANIMATION_GROUP.PAG_ONMOUNT_SLAP_HORSE }, { (byte)LOW_ANIMATION_GROUP.LAG_EAT, (byte)HIGHT_ANIMATION_GROUP.HAG_STAND, (byte)PEOPLE_ANIMATION_GROUP.PAG_TURN }, { (byte)LOW_ANIMATION_GROUP.LAG_WALK, (byte)HIGHT_ANIMATION_GROUP.HAG_WALK, (byte)PEOPLE_ANIMATION_GROUP.PAG_ATTACK_UNARMED_AND_WALK }, { (byte)LOW_ANIMATION_GROUP.LAG_EAT, (byte)HIGHT_ANIMATION_GROUP.HAG_STAND, (byte)PEOPLE_ANIMATION_GROUP.PAG_EMOTE_BOW }, { (byte)LOW_ANIMATION_GROUP.LAG_EAT, (byte)HIGHT_ANIMATION_GROUP.HAG_STAND, (byte)PEOPLE_ANIMATION_GROUP.PAG_EMOTE_SALUTE }, { (byte)LOW_ANIMATION_GROUP.LAG_FIDGET_1, (byte)HIGHT_ANIMATION_GROUP.HAG_FIDGET_1, (byte)PEOPLE_ANIMATION_GROUP.PAG_FIDGET_3 } };

        public Graphic GetGraphicForAnimation()
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
            Direction dir = Direction & Direction.Up;

            if (Steps.Count > 0)
            {
                dir = (Direction)Steps.Front().Direction & Direction.Up;
            }

            return dir;
        }

        public static void GetGroupForAnimation(ANIMATION_GROUPS group, ref byte animation)
        {
            if ((sbyte)group > 0 && animation < (byte)PEOPLE_ANIMATION_GROUP.PAG_ANIMATION_COUNT)
            {
                animation = _animAssociateTable[animation, (sbyte)group - 1];
            }
        }

        public static byte GetGroupForAnimation(Mobile mobile, ushort checkGraphic = 0)
        {
            if (mobile != World.Player)
            {

            }

            Graphic graphic = checkGraphic;
            if (graphic == 0)
            {
                graphic = mobile.GetGraphicForAnimation();
            }

            ANIMATION_GROUPS groupIndex = Animations.GetGroupIndex(graphic);
            byte result = mobile.AnimationGroup;

            if (result != 0xFF && (mobile.Serial & 0x80000000) <= 0 && (!mobile.AnimationFromServer || checkGraphic > 0))
            {
                GetGroupForAnimation(groupIndex, ref result);

                if (!Animations.AnimationExists(graphic, result))
                {
                    CorrectAnimationGroup(graphic, groupIndex, ref result);
                }
            }

            bool isWalking = mobile.IsWalking;
            bool isRun = mobile.IsRunning;

            if (mobile.Steps.Count > 0)
            {
                isWalking = true;
                isRun = mobile.Steps.Front().Run;
            }

            if (groupIndex == ANIMATION_GROUPS.AG_LOW)
            {
                if (isWalking)
                {
                    if (isRun)
                    {
                        result = (byte)LOW_ANIMATION_GROUP.LAG_RUN;
                    }
                    else
                    {
                        result = (byte)LOW_ANIMATION_GROUP.LAG_WALK;
                    }
                }
                else if (mobile.AnimationGroup == 0xFF)
                {
                    result = (byte)LOW_ANIMATION_GROUP.LAG_STAND;
                    mobile.AnimIndex = 0;
                }
            }
            else if (groupIndex == ANIMATION_GROUPS.AG_HIGHT)
            {
                if (isWalking)
                {
                    result = (byte)HIGHT_ANIMATION_GROUP.HAG_WALK;
                    if (isRun)
                    {
                        if (Animations.AnimationExists(graphic, (byte)HIGHT_ANIMATION_GROUP.HAG_FLY))
                        {
                            result = (byte)HIGHT_ANIMATION_GROUP.HAG_FLY;
                        }
                    }
                }
                else if (mobile.AnimationGroup == 0xFF)
                {
                    result = (byte)HIGHT_ANIMATION_GROUP.HAG_STAND;
                    mobile.AnimIndex = 0;
                }

                if (graphic == 151)
                {
                    result++;
                }
            }
            else if (groupIndex == ANIMATION_GROUPS.AG_PEOPLE)
            {
                bool inWar = mobile.InWarMode;

                if (isWalking)
                {
                    if (isRun)
                    {
                        if (mobile.Equipment[(int)Layer.Mount] != null)
                        {
                            result = (byte)PEOPLE_ANIMATION_GROUP.PAG_ONMOUNT_RIDE_FAST;
                        }
                        else if (mobile.Equipment[(int)Layer.OneHanded] != null || mobile.Equipment[(int)Layer.TwoHanded] != null)
                        {
                            result = (byte)PEOPLE_ANIMATION_GROUP.PAG_RUN_ARMED;
                        }
                        else
                        {
                            result = (byte)PEOPLE_ANIMATION_GROUP.PAG_RUN_UNARMED;
                        }

                        if (!mobile.IsHuman && !Animations.AnimationExists(graphic, result))
                        {
                            if (mobile.Equipment[(int)Layer.Mount] != null)
                            {
                                result = (byte)PEOPLE_ANIMATION_GROUP.PAG_ONMOUNT_RIDE_SLOW;
                            }
                            else if ((mobile.Equipment[(int)Layer.TwoHanded] != null || mobile.Equipment[(int)Layer.OneHanded] != null) && !mobile.IsDead)
                            {
                                if (inWar)
                                {
                                    result = (byte)PEOPLE_ANIMATION_GROUP.PAG_WALK_WARMODE;
                                }
                                else
                                {
                                    result = (byte)PEOPLE_ANIMATION_GROUP.PAG_WALK_ARMED;
                                }
                            }
                            else if (inWar && !mobile.IsDead)
                            {
                                result = (byte)PEOPLE_ANIMATION_GROUP.PAG_WALK_WARMODE;
                            }
                            else
                            {
                                result = (byte)PEOPLE_ANIMATION_GROUP.PAG_WALK_UNARMED;
                            }
                        }
                    }
                    else
                    {
                        if (mobile.Equipment[(int)Layer.Mount] != null)
                        {
                            result = (byte)PEOPLE_ANIMATION_GROUP.PAG_ONMOUNT_RIDE_SLOW;
                        }
                        else if ((mobile.Equipment[(int)Layer.OneHanded] != null || mobile.Equipment[(int)Layer.TwoHanded] != null) && !mobile.IsDead)
                        {
                            if (inWar)
                            {
                                result = (byte)PEOPLE_ANIMATION_GROUP.PAG_WALK_WARMODE;
                            }
                            else
                            {
                                result = (byte)PEOPLE_ANIMATION_GROUP.PAG_WALK_ARMED;
                            }
                        }
                        else if (inWar && !mobile.IsDead)
                        {
                            result = (byte)PEOPLE_ANIMATION_GROUP.PAG_WALK_WARMODE;
                        }
                        else
                        {
                            result = (byte)PEOPLE_ANIMATION_GROUP.PAG_WALK_UNARMED;
                        }
                    }
                }
                else if (mobile.AnimationGroup == 0xFF)
                {
                    if (mobile.Equipment[(int)Layer.Mount] != null)
                    {
                        result = (byte)PEOPLE_ANIMATION_GROUP.PAG_ONMOUNT_STAND;
                    }
                    else if (inWar && !mobile.IsDead)
                    {
                        if (mobile.Equipment[(int)Layer.OneHanded] != null)
                        {
                            result = (byte)PEOPLE_ANIMATION_GROUP.PAG_STAND_ONEHANDED_ATTACK;
                        }
                        else if (mobile.Equipment[(int)Layer.TwoHanded] != null)
                        {
                            result = (byte)PEOPLE_ANIMATION_GROUP.PAG_STAND_TWOHANDED_ATTACK;
                        }
                        else
                        {
                            result = (byte)PEOPLE_ANIMATION_GROUP.PAG_STAND_ONEHANDED_ATTACK;
                        }
                    }
                    else
                    {
                        result = (byte)PEOPLE_ANIMATION_GROUP.PAG_STAND;
                    }

                    mobile.AnimIndex = 0;
                }

                if (mobile.Race == RaceType.GARGOYLE)
                {
                    if (mobile.IsFlying)
                    {
                        if (result == 0 || result == 1)
                        {
                            result = 62;
                        }
                        else if (result == 2 || result == 3)
                        {
                            result = 63;
                        }
                        else if (result == 4)
                        {
                            result = 64;
                        }
                        else if (result == 6)
                        {
                            result = 66;
                        }
                        else if (result == 7 || result == 8)
                        {
                            result = 65;
                        }
                        else if (result >= 9 && result <= 11)
                        {
                            result = 71;
                        }
                        else if (result >= 12 && result <= 14)
                        {
                            result = 72;
                        }
                        else if (result == 15)
                        {
                            result = 62;
                        }
                        else if (result == 20)
                        {
                            result = 77;
                        }
                        else if (result == 31)
                        {
                            result = 71;
                        }
                        else if (result == 34)
                        {
                            result = 78;
                        }
                        else if (result >= 200 && result <= 259)
                        {
                            result = 75;
                        }
                        else if (result >= 260 && result <= 270)
                        {
                            result = 75;
                        }
                    }
                }
            }

            return result;
        }


        private static void CorrectAnimationGroup(ushort graphic,  ANIMATION_GROUPS group, ref byte animation)
        {
            if (group == ANIMATION_GROUPS.AG_LOW)
            {
                switch ((LOW_ANIMATION_GROUP)animation)
                {
                    case LOW_ANIMATION_GROUP.LAG_DIE_2:
                        animation = (byte)LOW_ANIMATION_GROUP.LAG_DIE_1;
                        break;
                    case LOW_ANIMATION_GROUP.LAG_FIDGET_2:
                        animation = (byte)LOW_ANIMATION_GROUP.LAG_FIDGET_1;
                        break;
                    case LOW_ANIMATION_GROUP.LAG_ATTACK_3:
                    case LOW_ANIMATION_GROUP.LAG_ATTACK_2:
                        animation = (byte)LOW_ANIMATION_GROUP.LAG_ATTACK_1;
                        break;
                }

                if (!Animations.AnimationExists(graphic, animation))
                {
                    animation = (byte)LOW_ANIMATION_GROUP.LAG_STAND;
                }
            }
            else if (group == ANIMATION_GROUPS.AG_HIGHT)
            {
                switch ((HIGHT_ANIMATION_GROUP)animation)
                {
                    case HIGHT_ANIMATION_GROUP.HAG_DIE_2:
                        animation = (byte)HIGHT_ANIMATION_GROUP.HAG_DIE_1;
                        break;
                    case HIGHT_ANIMATION_GROUP.HAG_FIDGET_2:
                        animation = (byte)HIGHT_ANIMATION_GROUP.HAG_FIDGET_1;
                        break;
                    case HIGHT_ANIMATION_GROUP.HAG_ATTACK_3:
                    case HIGHT_ANIMATION_GROUP.HAG_ATTACK_2:
                        animation = (byte)HIGHT_ANIMATION_GROUP.HAG_ATTACK_1;
                        break;
                    case HIGHT_ANIMATION_GROUP.HAG_GET_HIT_3:
                    case HIGHT_ANIMATION_GROUP.HAG_GET_HIT_2:
                        animation = (byte)HIGHT_ANIMATION_GROUP.HAG_GET_HIT_1;
                        break;
                    case HIGHT_ANIMATION_GROUP.HAG_MISC_4:
                    case HIGHT_ANIMATION_GROUP.HAG_MISC_3:
                    case HIGHT_ANIMATION_GROUP.HAG_MISC_2:
                        animation = (byte)HIGHT_ANIMATION_GROUP.HAG_MISC_1;
                        break;
                    case HIGHT_ANIMATION_GROUP.HAG_FLY:
                        animation = (byte)HIGHT_ANIMATION_GROUP.HAG_WALK;
                        break;
                }

                if (!Animations.AnimationExists(graphic, animation))
                {
                    animation = (byte)HIGHT_ANIMATION_GROUP.HAG_STAND;
                }
            }
        }

        public static byte GetReplacedObjectAnimation(Mobile mobile,  ushort index)
        {
            ushort getReplacedGroup(IReadOnlyList<Tuple<ushort, byte>> list,  ushort idx,  ushort walkIdx)
            {
                foreach (Tuple<ushort, byte> item in list)
                {
                    if (item.Item1 == idx)
                    {
                        if (item.Item2 == 0xFF)
                        {
                            return walkIdx;
                        }

                        return item.Item2;
                    }
                }

                return idx;
            }

            ANIMATION_GROUPS group = Animations.GetGroupIndex(mobile.Graphic);

            if (group == ANIMATION_GROUPS.AG_LOW)
            {
                return (byte)(getReplacedGroup(Animations.GroupReplaces[0], index, (ushort)LOW_ANIMATION_GROUP.LAG_WALK) % (ushort)LOW_ANIMATION_GROUP.LAG_ANIMATION_COUNT);
            }

            if (group == ANIMATION_GROUPS.AG_PEOPLE)
            {
                return (byte)(getReplacedGroup(Animations.GroupReplaces[1], index, (ushort)PEOPLE_ANIMATION_GROUP.PAG_WALK_UNARMED) % (ushort)PEOPLE_ANIMATION_GROUP.PAG_ANIMATION_COUNT);
            }

            return (byte)(index % (ushort)HIGHT_ANIMATION_GROUP.HAG_ANIMATION_COUNT);
        }

        public static byte GetObjectNewAnimation(Mobile mobile,  ushort type,  ushort action,  byte mode)
        {
            if (mobile.Graphic >= Animations.MAX_ANIMATIONS_DATA_INDEX_COUNT)
            {
                return 0;
            }

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

        private static byte GetObjectNewAnimationType_0(Mobile mobile,  ushort action,  byte mode)
        {
            if (action <= 10)
            {
                ref IndexAnimation ia = ref Animations.DataIndex[mobile.Graphic];
                ANIMATION_GROUPS_TYPE type = ANIMATION_GROUPS_TYPE.MONSTER;

                if ((ia.Flags & 0x80000000) != 0)
                {
                    type = ia.Type;
                }

                if (type == ANIMATION_GROUPS_TYPE.MONSTER)
                {
                    switch (mode % 4)
                    {
                        case 1:
                            return 5;
                        case 2:
                            return 6;
                        case 3:
                            if ((ia.Flags & 1) != 0)
                            {
                                return 12;
                            }

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
                    if (mobile.Equipment[(int)Layer.Mount] != null)
                    {
                        if (action > 0)
                        {
                            if (action == 1)
                            {
                                return 27;
                            }

                            if (action == 2)
                            {
                                return 28;
                            }

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

        private static byte GetObjectNewAnimationType_1_2(Mobile mobile,  ushort action,  byte mode)
        {
            ref IndexAnimation ia = ref Animations.DataIndex[mobile.Graphic];
            ANIMATION_GROUPS_TYPE type = ANIMATION_GROUPS_TYPE.MONSTER;

            if ((ia.Flags & 0x80000000) != 0)
                type = ia.Type;

            if (type != ANIMATION_GROUPS_TYPE.MONSTER)
            {
                if (type <= ANIMATION_GROUPS_TYPE.ANIMAL || mobile.Equipment[(int)Layer.Mount] != null)
                {
                    return 0xFF;
                }

                return 30;
            }

            if (mode % 2 != 0)
            {
                return 15;
            }

            return 16;
        }

        private static byte GetObjectNewAnimationType_3(Mobile mobile,  ushort action,  byte mode)
        {
            ref IndexAnimation ia = ref Animations.DataIndex[mobile.Graphic];
            ANIMATION_GROUPS_TYPE type = ANIMATION_GROUPS_TYPE.MONSTER;

            if ((ia.Flags & 0x80000000) != 0)
            {
                type = ia.Type;
            }

            if (type != ANIMATION_GROUPS_TYPE.MONSTER)
            {
                if (type == ANIMATION_GROUPS_TYPE.SEA_MONSTER)
                {
                    return 8;
                }

                if (type == ANIMATION_GROUPS_TYPE.ANIMAL)
                {
                    if (mode % 2 != 0)
                    {
                        return 21;
                    }

                    return 22;
                }

                if (mode % 2 != 0)
                {
                    return 8;
                }

                return 12;
            }

            if (mode % 2 != 0)
            {
                return 2;
            }

            return 3;
        }

        private static byte GetObjectNewAnimationType_4(Mobile mobile,  ushort action,  byte mode)
        {
            ref IndexAnimation ia = ref Animations.DataIndex[mobile.Graphic];
            ANIMATION_GROUPS_TYPE type = ANIMATION_GROUPS_TYPE.MONSTER;

            if ((ia.Flags & 0x80000000) != 0)
            {
                type = ia.Type;
            }

            if (type != ANIMATION_GROUPS_TYPE.MONSTER)
            {
                if (type > ANIMATION_GROUPS_TYPE.ANIMAL)
                {
                    if (mobile.Equipment[(int)Layer.Mount] != null)
                    {
                        return 0xFF;
                    }

                    return 20;
                }

                return 7;
            }

            return 10;
        }

        private static byte GetObjectNewAnimationType_5(Mobile mobile,  ushort action,  byte mode)
        {
            ref IndexAnimation ia = ref Animations.DataIndex[mobile.Graphic];
            ANIMATION_GROUPS_TYPE type = ANIMATION_GROUPS_TYPE.MONSTER;

            if ((ia.Flags & 0x80000000) != 0)
            {
                type = ia.Type;
            }

            if (type <= ANIMATION_GROUPS_TYPE.SEA_MONSTER)
            {
                if (mode % 2 != 0)
                {
                    return 18;
                }

                return 17;
            }
            else if (type != ANIMATION_GROUPS_TYPE.ANIMAL)
            {
                if (mobile.Equipment[(int)Layer.Mount] != null)
                {
                    return 0xFF;
                }

                if (mode % 2 != 0)
                {
                    return 6;
                }

                return 5;
            }
            else
            {
                switch (mode % 3)
                {
                    case 1:
                        return 10;
                    case 2:
                        return 3;
                }
            }
            return 9;
        }

        private static byte GetObjectNewAnimationType_6_14(Mobile mobile,  ushort action,  byte mode)
        {
            ref IndexAnimation ia = ref Animations.DataIndex[mobile.Graphic];
            ANIMATION_GROUPS_TYPE type = ANIMATION_GROUPS_TYPE.MONSTER;

            if ((ia.Flags & 0x80000000) != 0)
            {
                type = ia.Type;
            }

            if (type != ANIMATION_GROUPS_TYPE.MONSTER)
            {
                if (type != ANIMATION_GROUPS_TYPE.SEA_MONSTER)
                {
                    if (type == ANIMATION_GROUPS_TYPE.ANIMAL)
                    {
                        return 3;
                    }

                    if (mobile.Equipment[(int)Layer.Mount] != null)
                    {
                        return 0xFF;
                    }

                    return 34;
                }

                return 5;
            }

            return 11;
        }

        private static byte GetObjectNewAnimationType_7(Mobile mobile,  ushort action,  byte mode)
        {
            if (mobile.Equipment[(int)Layer.Mount] != null)
            {
                return 0xFF;
            }

            if (action > 0)
            {
                if (action == 1)
                {
                    return 33;
                }
            }
            else
            {
                return 32;
            }

            return 0;
        }

        private static byte GetObjectNewAnimationType_8(Mobile mobile,  ushort action,  byte mode)
        {
            ref IndexAnimation ia = ref Animations.DataIndex[mobile.Graphic];
            ANIMATION_GROUPS_TYPE type = ANIMATION_GROUPS_TYPE.MONSTER;

            if ((ia.Flags & 0x80000000) != 0)
            {
                type = ia.Type;
            }

            if (type != ANIMATION_GROUPS_TYPE.MONSTER)
            {
                if (type != ANIMATION_GROUPS_TYPE.SEA_MONSTER)
                {
                    if (type == ANIMATION_GROUPS_TYPE.ANIMAL)
                    {
                        return 9;
                    }

                    return mobile.Equipment[(int)Layer.Mount] != null ? (byte)0xFF : (byte)33;
                }

                return 3;
            }

            return 11;
        }

        private static byte GetObjectNewAnimationType_9_10(Mobile mobile,  ushort action,  byte mode)
        {
            ref IndexAnimation ia = ref Animations.DataIndex[mobile.Graphic];
            ANIMATION_GROUPS_TYPE type = ANIMATION_GROUPS_TYPE.MONSTER;

            if ((ia.Flags & 0x80000000) != 0)
            {
                type = ia.Type;
            }

            return type != ANIMATION_GROUPS_TYPE.MONSTER ? (byte)0xFF : (byte)20;
        }

        private static byte GetObjectNewAnimationType_11(Mobile mobile,  ushort action,  byte mode)
        {
            ref IndexAnimation ia = ref Animations.DataIndex[mobile.Graphic];
            ANIMATION_GROUPS_TYPE type = ANIMATION_GROUPS_TYPE.MONSTER;

            if ((ia.Flags & 0x80000000) != 0)
            {
                type = ia.Type;
            }

            if (type != ANIMATION_GROUPS_TYPE.MONSTER)
            {
                if (type >= ANIMATION_GROUPS_TYPE.ANIMAL)
                {
                    if (mobile.Equipment[(int)Layer.Mount] != null)
                    {
                        return 0xFF;
                    }

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