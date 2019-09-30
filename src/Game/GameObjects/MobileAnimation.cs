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
using System.Linq;
using System.Runtime.CompilerServices;

using ClassicUO.Game.Data;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Utility;

namespace ClassicUO.Game.GameObjects
{
    internal partial class Mobile
    {
        private static readonly ushort[] HANDS_BASE_ANIMID =
        {
            0x0263, 0x0264, 0x0265, 0x0266, 0x0267, 0x0268, 0x0269, 0x026D, 0x0270,
            0x0272, 0x0274, 0x027A, 0x027C, 0x027F, 0x0281, 0x0286, 0x0288, 0x0289,
            0x028B, 0
        };

        private static readonly ushort[] HAND2_BASE_ANIMID =
        {
            0x0240, 0x0241, 0x0242, 0x0243, 0x0244, 0x0245, 0x0246, 0x03E0, 0x03E1, 0
        };

        [MethodImpl(256)]
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

        [MethodImpl(256)]
        public Direction GetDirectionForAnimation()
        {
            return Steps.Count != 0 ? (Direction) Steps.Front().Direction : Direction;
        }


        private static void CalculateHight(ushort graphic, Mobile mobile, ANIMATION_FLAGS flags, bool isrun, bool iswalking, ref byte result)
        {
            if ((flags & ANIMATION_FLAGS.AF_CALCULATE_OFFSET_BY_PEOPLE_GROUP) != 0)
            {
                if (result == 0xFF)
                    result = 0;
            }
            else if ((flags & ANIMATION_FLAGS.AF_CALCULATE_OFFSET_BY_LOW_GROUP) != 0)
            {
                if (!iswalking)
                {
                    if (result == 0xFF)
                        result = 2;
                }
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
                    if (result == 0xFF)
                    {
                        if ((flags & ANIMATION_FLAGS.AF_IDLE_AT_8_FRAME) != 0 && FileManager.Animations.AnimationExists(graphic, 8))
                            result = 8;
                        else
                        {
                            if ((flags & ANIMATION_FLAGS.AF_USE_UOP_ANIMATION) != 0 && !mobile.InWarMode)
                            {
                                result = 25;
                            }
                            else
                                result = 1;
                        }
                    }
                }
                else if (isrun)
                {
                    if ((flags & ANIMATION_FLAGS.AF_CAN_FLYING) != 0 && FileManager.Animations.AnimationExists(graphic, 19))
                        result = 19;
                    else
                    {
                        if ((flags & ANIMATION_FLAGS.AF_USE_UOP_ANIMATION) != 0)
                            result = 24;
                        else
                            result = 0;
                    }
                }
                else
                {
                    if ((flags & ANIMATION_FLAGS.AF_USE_UOP_ANIMATION) != 0 && !mobile.InWarMode)
                    {
                        result = 22;
                    }
                    else
                        result = 0;
                }
            }
        }


        private static void LABEL_222(ANIMATION_FLAGS flags, ref ushort v13)
        {
            if ((flags & ANIMATION_FLAGS.AF_CALCULATE_OFFSET_LOW_GROUP_EXTENDED) != 0)
            {
                switch (v13)
                {
                    case 0:
                        v13 = 0;

                        goto LABEL_243;

                    case 1:
                        v13 = 19;

                        goto LABEL_243;

                    case 5:
                    case 6:

                        if ((flags & ANIMATION_FLAGS.AF_IDLE_AT_8_FRAME) != 0)
                            v13 = 4;
                        else
                            v13 = (ushort) (6 - (RandomHelper.GetValue() % 2 != 0 ? 1 : 0));

                        goto LABEL_243;

                    case 8:
                        v13 = 2;

                        goto LABEL_243;

                    case 9:
                        v13 = 17;

                        goto LABEL_243;

                    case 10:
                        v13 = 18;

                        if ((flags & ANIMATION_FLAGS.AF_IDLE_AT_8_FRAME) != 0)
                            v13--;

                        goto LABEL_243;

                    case 12:
                        v13 = 3;

                        goto LABEL_243;
                }

                // LABEL_241
                v13 = 1;
            }
            else
            {
                if ((flags & ANIMATION_FLAGS.AF_CALCULATE_OFFSET_BY_LOW_GROUP) != 0)
                {
                    switch (v13)
                    {
                        case 0:
                            // LABEL_232
                            v13 = 0;

                            break;

                        case 2:
                            v13 = 8;

                            break;

                        case 3:
                            v13 = 12;

                            break;

                        case 4:
                        case 6:
                        case 7:
                        case 8:
                        case 9:
                        case 12:
                        case 13:
                        case 14:
                            v13 = 5;

                            break;

                        case 5:
                            v13 = 6;

                            break;

                        case 10:
                        case 21:
                            v13 = 7;

                            break;

                        case 11:
                            //LABEL_238:
                            v13 = 3;

                            break;

                        case 17:
                            v13 = 9;

                            break;

                        case 18:
                            v13 = 10;

                            break;

                        case 19:

                            v13 = 1;

                            break;

                        default:
                            //LABEL_242:
                            v13 = 2;

                            break;
                    }
                }
            }

            LABEL_243:
            v13 = (ushort) (v13 & 0x7F);

            if (v13 > 34)
                v13 = 0;

            //return (byte)v13;
        }

        private static void LABEL_190(ANIMATION_FLAGS flags, ref ushort v13)
        {
            if ((flags & ANIMATION_FLAGS.AF_UNKNOWN_80) != 0 && v13 == 4)
                v13 = 5;

            if ((flags & ANIMATION_FLAGS.AF_UNKNOWN_200) != 0)
            {
                if (v13 - 7 > 9)
                {
                    if (v13 == 19)
                    {
                        //LABEL_196
                        v13 = 0;
                    }
                    else if (v13 > 19)
                        v13 = 1;

                    LABEL_222(flags, ref v13);

                    return;
                }
            }
            else
            {
                if ((flags & ANIMATION_FLAGS.AF_UNKNOWN_100) != 0)
                {
                    switch (v13)
                    {
                        case 10:
                        case 15:
                        case 16:
                            v13 = 1;
                            LABEL_222(flags, ref v13);

                            return;

                        case 11:
                            v13 = 17;
                            LABEL_222(flags, ref v13);

                            return;
                    }

                    LABEL_222(flags, ref v13);

                    return;
                }

                if ((flags & ANIMATION_FLAGS.AF_UNKNOWN_1) != 0)
                {
                    if (v13 == 21)
                        v13 = 10;

                    LABEL_222(flags, ref v13);

                    return;
                }

                if ((flags & ANIMATION_FLAGS.AF_CALCULATE_OFFSET_BY_PEOPLE_GROUP) == 0)
                {
                    //LABEL_222:
                    LABEL_222(flags, ref v13);

                    return;
                }

                switch (v13)
                {
                    case 0:
                        v13 = 0;

                        break;

                    case 2:
                        v13 = 21;
                        LABEL_222(flags, ref v13);

                        return;

                    case 3:
                        v13 = 22;
                        LABEL_222(flags, ref v13);

                        return;

                    case 4:
                    case 9:
                        v13 = 9;
                        LABEL_222(flags, ref v13);

                        return;

                    case 5:
                        v13 = 11;
                        LABEL_222(flags, ref v13);

                        return;

                    case 6:
                        v13 = 13;
                        LABEL_222(flags, ref v13);

                        return;

                    case 7:
                        v13 = 18;
                        LABEL_222(flags, ref v13);

                        return;

                    case 8:
                        v13 = 19;
                        LABEL_222(flags, ref v13);

                        return;

                    case 10:
                    case 21:
                        v13 = 20;
                        LABEL_222(flags, ref v13);

                        return;

                    case 11:
                        v13 = 3;
                        LABEL_222(flags, ref v13);

                        return;

                    case 12:
                    case 14:
                        v13 = 16;
                        LABEL_222(flags, ref v13);

                        return;

                    case 13:
                        //LABEL_202:
                        v13 = 17;
                        LABEL_222(flags, ref v13);

                        return;

                    case 15:
                    case 16:
                        v13 = 30;
                        LABEL_222(flags, ref v13);

                        return;

                    case 17:
                        v13 = 5;
                        LABEL_222(flags, ref v13);

                        return;

                    case 18:
                        v13 = 6;
                        LABEL_222(flags, ref v13);

                        return;

                    case 19:
                        //LABEL_201:
                        v13 = 1;
                        LABEL_222(flags, ref v13);

                        return;
                }
            }

            v13 = 4;

            LABEL_222(flags, ref v13);
        }



        public static byte GetGroupForAnimation(Mobile mobile, ushort checkGraphic = 0, bool isParent = false)
        {
            Graphic graphic = checkGraphic;

            if (graphic == 0)
                graphic = mobile.GetGraphicForAnimation();

            if (graphic >= Constants.MAX_ANIMATIONS_DATA_INDEX_COUNT)
                return 0;

            ANIMATION_GROUPS_TYPE type = FileManager.Animations.DataIndex[graphic].Type;
            ANIMATION_GROUPS_TYPE originalType = ANIMATION_GROUPS_TYPE.UNKNOWN;

            if (FileManager.Animations.DataIndex[graphic].IsUOP && (isParent || !FileManager.Animations.DataIndex[graphic].IsValidMUL))
            {
                // do nothing ?
            }
            else
            {
                if (!FileManager.Animations.DataIndex[graphic].HasBodyConversion)
                {
                    ushort newGraphic = FileManager.Animations.DataIndex[graphic].Graphic;

                    if (graphic != newGraphic)
                    {
                        graphic = newGraphic;
                        ANIMATION_GROUPS_TYPE newType = FileManager.Animations.DataIndex[graphic].Type;

                        if (newType != type)
                        {
                            originalType = type;
                            type = newType;
                        }
                    }
                }
            }


            ANIMATION_FLAGS flags = (ANIMATION_FLAGS) FileManager.Animations.DataIndex[graphic].Flags;

            if (mobile.AnimationFromServer && mobile.AnimationGroup != 0xFF)
            {
                ushort v13 = mobile.AnimationGroup;

                if (v13 == 12)
                {
                    if (!(type == ANIMATION_GROUPS_TYPE.HUMAN || type == ANIMATION_GROUPS_TYPE.EQUIPMENT || (flags & ANIMATION_FLAGS.AF_UNKNOWN_1000) != 0))
                    {
                        if (type != ANIMATION_GROUPS_TYPE.MONSTER)
                        {
                            if (type == ANIMATION_GROUPS_TYPE.HUMAN || type == ANIMATION_GROUPS_TYPE.EQUIPMENT)
                                v13 = 16;
                            else
                                v13 = 5;
                        }
                        else
                            v13 = 4;
                    }
                }

                if (type != ANIMATION_GROUPS_TYPE.MONSTER)
                {
                    if (type != ANIMATION_GROUPS_TYPE.SEA_MONSTER)
                    {
                        if (type == ANIMATION_GROUPS_TYPE.ANIMAL)
                        {
                            if (IsReplacedObjectAnimation(0, v13))
                                originalType = ANIMATION_GROUPS_TYPE.UNKNOWN;

                            if (v13 > 12)
                            {
                                switch (v13)
                                {
                                    case 23:
                                        v13 = 0;

                                        break;

                                    case 24:
                                        v13 = 1;

                                        break;

                                    case 26:
                                        v13 = 9;

                                        break;

                                    case 28:
                                        v13 = 10;

                                        break;

                                    default:
                                        v13 = 2;

                                        break;
                                }
                            }

                            //if (v13 > 12)
                            //    v13 = 0; // 2
                        }
                        else
                        {
                            if (IsReplacedObjectAnimation(1, v13))
                            {
                                // LABEL_190:

                                LABEL_190(flags, ref v13);

                                return (byte) v13;
                            }
                        }
                    }
                    else
                    {
                        if (IsReplacedObjectAnimation(3, v13))
                            originalType = ANIMATION_GROUPS_TYPE.UNKNOWN;

                        if (v13 > 8)
                            v13 = 2;
                    }
                }
                else
                {
                    if (IsReplacedObjectAnimation(2, v13))
                        originalType = ANIMATION_GROUPS_TYPE.UNKNOWN;


                    if (!FileManager.Animations.AnimationExists(graphic, (byte) v13))
                        v13 = 1;

                    if ((flags & ANIMATION_FLAGS.AF_USE_UOP_ANIMATION) != 0)
                    {
                        // do nothing?
                    }
                    else if (v13 > 21)
                        v13 = 1;
                }


                if (originalType == ANIMATION_GROUPS_TYPE.UNKNOWN)
                {
                    LABEL_190(flags, ref v13);

                    return (byte) v13;
                }

                if (originalType != 0)
                {
                    if (originalType == ANIMATION_GROUPS_TYPE.ANIMAL && type == ANIMATION_GROUPS_TYPE.MONSTER)
                    {
                        switch (v13)
                        {
                            case 0:
                                v13 = 0;
                                LABEL_190(flags, ref v13);

                                return (byte) v13;

                            case 1:
                                v13 = 19;
                                LABEL_190(flags, ref v13);

                                return (byte) v13;

                            case 3:
                                v13 = 11;
                                LABEL_190(flags, ref v13);

                                return (byte) v13;

                            case 5:
                                v13 = 4;
                                LABEL_190(flags, ref v13);

                                return (byte) v13;

                            case 6:
                                v13 = 5;
                                LABEL_190(flags, ref v13);

                                return (byte) v13;

                            case 7:
                            case 11:
                                v13 = 10;
                                LABEL_190(flags, ref v13);

                                return (byte) v13;

                            case 8:
                                v13 = 2;
                                LABEL_190(flags, ref v13);

                                return (byte) v13;

                            case 9:
                                v13 = 17;
                                LABEL_190(flags, ref v13);

                                return (byte) v13;

                            case 10:
                                v13 = 18;
                                LABEL_190(flags, ref v13);

                                return (byte) v13;

                            case 12:
                                v13 = 3;
                                LABEL_190(flags, ref v13);

                                return (byte) v13;
                        }

                        // LABEL_187
                        v13 = 1;
                    }

                    LABEL_190(flags, ref v13);

                    return (byte) v13;
                }

                switch (type)
                {
                    case ANIMATION_GROUPS_TYPE.HUMAN:

                        switch (v13)
                        {
                            case 0:
                                v13 = 0;

                                goto LABEL_189;

                            case 2:
                                v13 = 21;

                                goto LABEL_189;

                            case 3:
                                v13 = 22;

                                goto LABEL_189;

                            case 4:
                            case 9:
                                v13 = 9;

                                goto LABEL_189;

                            case 5:
                                //LABEL_163:
                                v13 = 11;

                                goto LABEL_189;

                            case 6:
                                v13 = 13;

                                goto LABEL_189;

                            case 7:
                                //LABEL_165:
                                v13 = 18;

                                goto LABEL_189;

                            case 8:
                                //LABEL_172:
                                v13 = 19;

                                goto LABEL_189;

                            case 10:
                            case 21:
                                v13 = 20;

                                goto LABEL_189;

                            case 12:
                            case 14:
                                v13 = 16;

                                goto LABEL_189;

                            case 13:
                                //LABEL_164:
                                v13 = 17;

                                goto LABEL_189;

                            case 15:
                            case 16:
                                v13 = 30;

                                goto LABEL_189;

                            case 17:
                                v13 = 5;
                                LABEL_190(flags, ref v13);

                                return (byte) v13;

                            case 18:
                                v13 = 6;
                                LABEL_190(flags, ref v13);

                                return (byte) v13;

                            case 19:
                                v13 = 1;
                                LABEL_190(flags, ref v13);

                                return (byte) v13;
                        }

                        //LABEL_161:
                        v13 = 4;

                        goto LABEL_189;

                    case ANIMATION_GROUPS_TYPE.ANIMAL:

                        switch (v13)
                        {
                            case 0:
                                v13 = 0;

                                goto LABEL_189;

                            case 2:
                                v13 = 8;
                                LABEL_190(flags, ref v13);

                                return (byte) v13;

                            case 3:
                                v13 = 12;

                                goto LABEL_189;

                            case 4:
                            case 6:
                            case 7:
                            case 8:
                            case 9:
                            case 12:
                            case 13:
                            case 14:
                                v13 = 5;
                                LABEL_190(flags, ref v13);

                                return (byte) v13;

                            case 5:
                                v13 = 6;
                                LABEL_190(flags, ref v13);

                                return (byte) v13;

                            case 10:
                            case 21:
                                v13 = 7;
                                LABEL_190(flags, ref v13);

                                return (byte) v13;

                            case 11:
                                v13 = 3;
                                LABEL_190(flags, ref v13);

                                return (byte) v13;

                            case 17:
                                //LABEL_170:
                                v13 = 9;

                                goto LABEL_189;

                            case 18:
                                //LABEL_162:
                                v13 = 10;

                                goto LABEL_189;

                            case 19:
                                v13 = 1;
                                LABEL_190(flags, ref v13);

                                return (byte) v13;
                        }

                        v13 = 2;
                        LABEL_190(flags, ref v13);

                        return (byte) v13;

                    case ANIMATION_GROUPS_TYPE.SEA_MONSTER:

                        switch (v13)
                        {
                            case 0:
                                //LABEL_182:
                                v13 = 0;

                                goto LABEL_189;

                            case 2:
                            case 3:
                                //LABEL_178:
                                v13 = 8;

                                goto LABEL_189;

                            case 4:
                            case 6:
                            case 7:
                            case 8:
                            case 9:
                            case 12:
                            case 13:
                            case 14:
                                //LABEL_183:
                                v13 = 5;

                                goto LABEL_189;

                            case 5:
                                //LABEL_184:
                                v13 = 6;

                                goto LABEL_189;

                            case 10:
                            case 21:
                                //LABEL_185:
                                v13 = 7;

                                goto LABEL_189;

                            case 17:
                                //LABEL_186:
                                v13 = 3;

                                goto LABEL_189;

                            case 18:
                                v13 = 4;

                                goto LABEL_189;

                            case 19:
                                LABEL_190(flags, ref v13);

                                return (byte) v13;
                        }

                        v13 = 2;
                        LABEL_190(flags, ref v13);

                        return (byte) v13;

                    default:
                        LABEL_189:

                        LABEL_190(flags, ref v13);

                        return (byte) v13;
                }

                // LABEL_188
                v13 = 2;

                LABEL_190(flags, ref v13);

                return (byte) v13;
            }


            byte result = mobile.AnimationGroup;


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

                    if ((flags & ANIMATION_FLAGS.AF_CALCULATE_OFFSET_LOW_GROUP_EXTENDED) != 0)
                        CalculateHight(graphic, mobile, flags, isRun, isWalking, ref result);
                    else
                    {
                        if (!isWalking)
                        {
                            if (result == 0xFF)
                            {
                                if ((flags & ANIMATION_FLAGS.AF_USE_UOP_ANIMATION) != 0)
                                {
                                    if (mobile.InWarMode)
                                        result = 1;
                                    else
                                        result = 25;
                                }
                                else
                                    result = 2;
                            }
                        }
                        else if (isRun)
                        {
                            if ((flags & ANIMATION_FLAGS.AF_USE_UOP_ANIMATION) != 0)
                            {
                                result = 24;
                            }
                            else
                                result = FileManager.Animations.AnimationExists(graphic, 1) ? (byte) 1 : (byte) 2;
                        }
                        else if ((flags & ANIMATION_FLAGS.AF_USE_UOP_ANIMATION) != 0 && !mobile.InWarMode)
                        {
                            result = 22;
                        }
                        else
                            result = 0;
                    }

                    break;

                case ANIMATION_GROUPS_TYPE.MONSTER:
                    CalculateHight(graphic, mobile, flags, isRun, isWalking, ref result);

                    break;

                case ANIMATION_GROUPS_TYPE.SEA_MONSTER:

                    if (!isWalking)
                    {
                        if (result == 0xFF)
                            result = 2;
                    }
                    else if (isRun)
                        result = 1;
                    else
                        result = 0;

                    break;

                default:

                {
                    Item hand2 = mobile.HasEquipment ? mobile.Equipment[(int) Layer.TwoHanded] : null;

                    if (!isWalking)
                    {
                        if (result == 0xFF)
                        {
                            bool haveLightAtHand2 = hand2 != null && hand2.ItemData.IsLight && hand2.ItemData.AnimID == graphic;

                            if (mobile.IsMounted)
                                result = !haveLightAtHand2 ? (byte) 25 : (byte) 28;
                            else if (!mobile.InWarMode || mobile.IsDead)
                                result = !haveLightAtHand2 ? (byte) 4 : (byte) 0;
                            else if (haveLightAtHand2)
                                result = 2;
                            else
                            {
                                ushort[] handAnimIDs = {0, 0};
                                Item hand1 = mobile.HasEquipment ? mobile.Equipment[(int) Layer.OneHanded] : null;

                                if (hand1 != null)
                                    handAnimIDs[0] = hand1.ItemData.AnimID;

                                if (hand2 != null)
                                    handAnimIDs[1] = hand2.ItemData.AnimID;


                                if (hand1 == null)
                                {
                                    if (hand2 != null)
                                    {
                                        result = handAnimIDs.Where(handAnimID => handAnimID >= 0x0263 && handAnimID <= 0x028B)
                                                            .Any(handBaseGraphic => HANDS_BASE_ANIMID.Any(s => s == handBaseGraphic))
                                                     ? (byte) 8
                                                     : (byte) 7;
                                    }
                                    else
                                        result = 7;
                                }
                                else
                                    result = 7;
                            }
                        }
                    }
                    else if (mobile.IsMounted)
                    {
                        if (isRun)
                            result = 24;
                        else
                            result = 23;
                    }
                    //else if (EquippedGraphic0x3E96)
                    //{

                    //}
                    else if (isRun || !mobile.InWarMode || mobile.IsDead)
                    {
                        if ((flags & ANIMATION_FLAGS.AF_USE_UOP_ANIMATION) != 0 && isRun && FileManager.Animations.AnimationExists(graphic, 24))
                        {
                            result = 24;
                        }
                        else
                            result = (byte) (isRun ? 2 : 0);

                        if (hand2 != null)
                        {
                            ushort hand2Graphic = hand2.ItemData.AnimID;

                            if (hand2Graphic < 0x0240 || hand2Graphic > 0x03E1)
                                result = (byte) (isRun ? 3 : 1);
                            else
                            {
                                if (HAND2_BASE_ANIMID.Any(s => s == hand2Graphic)) result = (byte) (isRun ? 3 : 1);
                            }
                        }
                    }
                    else
                        result = 15;

                    break;
                }
            }
            return result;
        }

        public static bool IsReplacedObjectAnimation(byte anim, ushort v13)
        {
            if (anim < FileManager.Animations.GroupReplaces.Length)
            {
                foreach (Tuple<ushort, byte> tuple in FileManager.Animations.GroupReplaces[anim])
                {
                    if (tuple.Item1 == v13)
                        return tuple.Item2 != 0xFF;
                }
            }

            return false;
        }

        public static byte GetReplacedObjectAnimation(Graphic graphic, ushort index)
        {
            ushort getReplacedGroup(List<Tuple<ushort, byte>> list, ushort idx, ushort walkIdx)
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

        [MethodImpl(256)]
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

        [MethodImpl(256)]
        private static bool TestStepNoChangeDirection(Mobile mob, byte group)
        {
            switch ((PEOPLE_ANIMATION_GROUP) group)
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

        [MethodImpl(256)]
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
                    if (mobile.IsMounted)
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

                            if (FileManager.Animations.AnimationExists(mobile.Graphic, 31))
                                return 31;
                            break;

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

        [MethodImpl(256)]
        private static byte GetObjectNewAnimationType_1_2(Mobile mobile, ushort action, byte mode)
        {
            ref IndexAnimation ia = ref FileManager.Animations.DataIndex[mobile.Graphic];
            ANIMATION_GROUPS_TYPE type = ANIMATION_GROUPS_TYPE.MONSTER;

            if ((ia.Flags & 0x80000000) != 0)
                type = ia.Type;

            if (type != ANIMATION_GROUPS_TYPE.MONSTER)
            {
                if (type <= ANIMATION_GROUPS_TYPE.ANIMAL || mobile.IsMounted) return 0xFF;

                return 30;
            }

            if (mode % 2 != 0) return 15;

            return 16;
        }

        [MethodImpl(256)]
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

        [MethodImpl(256)]
        private static byte GetObjectNewAnimationType_4(Mobile mobile, ushort action, byte mode)
        {
            ref IndexAnimation ia = ref FileManager.Animations.DataIndex[mobile.Graphic];
            ANIMATION_GROUPS_TYPE type = ANIMATION_GROUPS_TYPE.MONSTER;
            if ((ia.Flags & 0x80000000) != 0) type = ia.Type;

            if (type != ANIMATION_GROUPS_TYPE.MONSTER)
            {
                if (type > ANIMATION_GROUPS_TYPE.ANIMAL)
                {
                    if (mobile.IsMounted) return 0xFF;

                    return 20;
                }

                return 7;
            }

            return 10;
        }

        [MethodImpl(256)]
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
                if (mobile.IsMounted)
                    return 0xFF;
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

        [MethodImpl(256)]
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
                    if (mobile.IsMounted) return 0xFF;

                    return 34;
                }

                return 5;
            }

            return 11;
        }

        [MethodImpl(256)]
        private static byte GetObjectNewAnimationType_7(Mobile mobile, ushort action, byte mode)
        {
            if (mobile.IsMounted) return 0xFF;

            if (action > 0)
            {
                if (action == 1) return 33;
            }
            else
                return 32;

            return 0;
        }

        [MethodImpl(256)]
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

                    return mobile.IsMounted ? (byte) 0xFF : (byte) 33;
                }

                return 3;
            }

            return 11;
        }

        [MethodImpl(256)]
        private static byte GetObjectNewAnimationType_9_10(Mobile mobile, ushort action, byte mode)
        {
            ref IndexAnimation ia = ref FileManager.Animations.DataIndex[mobile.Graphic];
            ANIMATION_GROUPS_TYPE type = ANIMATION_GROUPS_TYPE.MONSTER;
            if ((ia.Flags & 0x80000000) != 0) type = ia.Type;

            return type != ANIMATION_GROUPS_TYPE.MONSTER ? (byte) 0xFF : (byte) 20;
        }

        [MethodImpl(256)]
        private static byte GetObjectNewAnimationType_11(Mobile mobile, ushort action, byte mode)
        {
            ref IndexAnimation ia = ref FileManager.Animations.DataIndex[mobile.Graphic];
            ANIMATION_GROUPS_TYPE type = ANIMATION_GROUPS_TYPE.MONSTER;
            if ((ia.Flags & 0x80000000) != 0) type = ia.Type;

            if (type != ANIMATION_GROUPS_TYPE.MONSTER)
            {
                if (type >= ANIMATION_GROUPS_TYPE.ANIMAL)
                {
                    if (mobile.IsMounted) return 0xFF;

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