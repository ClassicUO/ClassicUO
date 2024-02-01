#region license

// Copyright (c) 2021, andreakarasho
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

using ClassicUO.Assets;
using ClassicUO.Game.Data;
using ClassicUO.Utility;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ClassicUO.Game.GameObjects
{
    public partial class Mobile
    {
        private static readonly ushort[] HANDS_BASE_ANIMID =
        {
            0x0263,
            0x0264,
            0x0265,
            0x0266,
            0x0267,
            0x0268,
            0x0269,
            0x026D,
            0x0270,
            0x0272,
            0x0274,
            0x027A,
            0x027C,
            0x027F,
            0x0281,
            0x0286,
            0x0288,
            0x0289,
            0x028B,
            0
        };

        private static readonly ushort[] HAND2_BASE_ANIMID =
        {
            0x0240,
            0x0241,
            0x0242,
            0x0243,
            0x0244,
            0x0245,
            0x0246,
            0x03E0,
            0x03E1,
            0
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override ushort GetGraphicForAnimation()
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

                case 0x02B6:
                    g = 667;

                    break;

                case 0x02B7:
                    g = 666;

                    break;
            }

            return g;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Direction GetDirectionForAnimation()
        {
            if (Steps.Count != 0)
            {
                ref Step step = ref Steps.Front();

                return (Direction)step.Direction;
            }

            return Direction;
        }

        private static void CalculateHight(
            ushort graphic,
            Mobile mobile,
            AnimationFlags flags,
            bool isrun,
            bool iswalking,
            ref byte result
        )
        {
            if ((flags & AnimationFlags.CalculateOffsetByPeopleGroup) != 0)
            {
                if (result == 0xFF)
                {
                    result = 0;
                }
            }
            else if ((flags & AnimationFlags.CalculateOffsetByLowGroup) != 0)
            {
                if (!iswalking)
                {
                    if (result == 0xFF)
                    {
                        result = 2;
                    }
                }
                else if (isrun)
                {
                    result = 1;
                }
                else
                {
                    result = 0;
                }
            }
            else
            {
                if (mobile.IsFlying)
                {
                    result = 19;
                }
                else if (!iswalking)
                {
                    if (result == 0xFF)
                    {
                        if (
                            (flags & AnimationFlags.IdleAt8Frame) != 0
                            && Client.Game.Animations.AnimationExists(graphic, 8)
                        )
                        {
                            result = 8;
                        }
                        else
                        {
                            if (
                                (flags & AnimationFlags.UseUopAnimation) != 0
                                && !mobile.InWarMode
                            )
                            {
                                result = 25;
                            }
                            else
                            {
                                result = 1;
                            }
                        }
                    }
                }
                else if (isrun)
                {
                    if (
                        (flags & AnimationFlags.CanFlying) != 0
                        && Client.Game.Animations.AnimationExists(graphic, 19)
                    )
                    {
                        result = 19;
                    }
                    else
                    {
                        if ((flags & AnimationFlags.UseUopAnimation) != 0)
                        {
                            result = 24;
                        }
                        else
                        {
                            result = 0;
                        }
                    }
                }
                else
                {
                    if ((flags & AnimationFlags.UseUopAnimation) != 0 && !mobile.InWarMode)
                    {
                        result = 22;
                    }
                    else
                    {
                        result = 0;
                    }
                }
            }
        }

        private static void LABEL_222(AnimationFlags flags, ref ushort v13)
        {
            if ((flags & AnimationFlags.CalculateOffsetLowGroupExtended) != 0)
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

                        if ((flags & AnimationFlags.IdleAt8Frame) != 0)
                        {
                            v13 = 4;
                        }
                        else
                        {
                            v13 = (ushort)(6 - (RandomHelper.GetValue() % 2 != 0 ? 1 : 0));
                        }

                        goto LABEL_243;

                    case 8:
                        v13 = 2;

                        goto LABEL_243;

                    case 9:
                        v13 = 17;

                        goto LABEL_243;

                    case 10:
                        v13 = 18;

                        if ((flags & AnimationFlags.IdleAt8Frame) != 0)
                        {
                            v13--;
                        }

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
                if ((flags & AnimationFlags.CalculateOffsetByLowGroup) != 0)
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
            v13 = (ushort)(v13 & 0x7F);

            //if (v13 > 34)
            //    v13 = 0;
        }

        private static void LABEL_190(AnimationFlags flags, ref ushort v13)
        {
            if ((flags & AnimationFlags.Unknown80) != 0 && v13 == 4)
            {
                v13 = 5;
            }

            if ((flags & AnimationFlags.Unknown200) != 0)
            {
                if (v13 - 7 > 9)
                {
                    if (v13 == 19)
                    {
                        //LABEL_196
                        v13 = 0;
                    }
                    else if (v13 > 19)
                    {
                        v13 = 1;
                    }

                    LABEL_222(flags, ref v13);

                    return;
                }
            }
            else
            {
                if ((flags & AnimationFlags.Unknown100) != 0)
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

                if ((flags & AnimationFlags.Unknown1) != 0)
                {
                    if (v13 == 21)
                    {
                        v13 = 10;
                    }

                    LABEL_222(flags, ref v13);

                    return;
                }

                if ((flags & AnimationFlags.CalculateOffsetByPeopleGroup) == 0)
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

        public static byte GetGroupForAnimation(
            Mobile mobile,
            ushort checkGraphic = 0,
            bool isParent = false
        )
        {
            ushort graphic = checkGraphic;

            if (graphic == 0)
            {
                graphic = mobile.GetGraphicForAnimation();
            }

            if (graphic >= Client.Game.Animations.MaxAnimationCount)
            {
                return 0;
            }

            AnimationGroupsType originalType = Client.Game.Animations.GetAnimType(graphic);
            Client.Game.Animations.ConvertBodyIfNeeded(ref graphic, isParent);
            AnimationGroupsType type = Client.Game.Animations.GetAnimType(graphic);
            AnimationFlags flags = Client.Game.Animations.GetAnimFlags(graphic);

            bool uop = (flags & AnimationFlags.UseUopAnimation) != 0;

            if (mobile.AnimationFromServer && mobile._animationGroup != 0xFF)
            {
                ushort v13 = mobile._animationGroup;

                if (v13 == 12)
                {
                    if (
                        !(
                            type == AnimationGroupsType.Human
                            || type == AnimationGroupsType.Equipment
                            || (flags & AnimationFlags.Unknown1000) != 0
                        )
                    )
                    {
                        if (type != AnimationGroupsType.Monster)
                        {
                            if (
                                type == AnimationGroupsType.Human
                                || type == AnimationGroupsType.Equipment
                            )
                            {
                                v13 = 16;
                            }
                            else
                            {
                                v13 = 5;
                            }
                        }
                        else
                        {
                            v13 = 4;
                        }
                    }
                }

                if (type != AnimationGroupsType.Monster)
                {
                    if (type != AnimationGroupsType.SeaMonster)
                    {
                        if (type == AnimationGroupsType.Animal)
                        {
                            if (IsReplacedObjectAnimation(0, v13))
                            {
                                originalType = AnimationGroupsType.Unknown;
                            }

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

                                        if (
                                            !Client.Game.Animations.AnimationExists(graphic, 26)
                                            || (
                                                mobile.InWarMode
                                                && Client.Game.Animations.AnimationExists(
                                                    graphic,
                                                    9
                                                )
                                            )
                                        )
                                        {
                                            v13 = 9;
                                        }

                                        break;

                                    case 28:

                                        v13 = (ushort)(
                                            Client.Game.Animations.AnimationExists(graphic, 10)
                                                ? 10
                                                : 5
                                        );

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

                                return (byte)v13;
                            }
                        }
                    }
                    else
                    {
                        if (IsReplacedObjectAnimation(3, v13))
                        {
                            originalType = AnimationGroupsType.Unknown;
                        }

                        if (v13 > 8)
                        {
                            v13 = 2;
                        }
                    }
                }
                else
                {
                    if (IsReplacedObjectAnimation(2, v13))
                    {
                        originalType = AnimationGroupsType.Unknown;
                    }

                    if (!Client.Game.Animations.AnimationExists(graphic, (byte)v13))
                    {
                        v13 = 1;
                    }

                    if ((flags & AnimationFlags.UseUopAnimation) != 0)
                    {
                        // do nothing?
                    }
                    else if (v13 > 21)
                    {
                        v13 = 1;
                    }
                }

                if (originalType == AnimationGroupsType.Unknown)
                {
                    LABEL_190(flags, ref v13);

                    return (byte)v13;
                }

                if (originalType != 0)
                {
                    if (
                        originalType == AnimationGroupsType.Animal
                        && type == AnimationGroupsType.Monster
                    )
                    {
                        switch (v13)
                        {
                            case 0:
                                v13 = 0;
                                LABEL_190(flags, ref v13);

                                return (byte)v13;

                            case 1:
                                v13 = 19;
                                LABEL_190(flags, ref v13);

                                return (byte)v13;

                            case 3:
                                v13 = 11;
                                LABEL_190(flags, ref v13);

                                return (byte)v13;

                            case 5:
                                v13 = 4;
                                LABEL_190(flags, ref v13);

                                return (byte)v13;

                            case 6:
                                v13 = 5;
                                LABEL_190(flags, ref v13);

                                return (byte)v13;

                            case 7:
                            case 11:
                                v13 = 10;
                                LABEL_190(flags, ref v13);

                                return (byte)v13;

                            case 8:
                                v13 = 2;
                                LABEL_190(flags, ref v13);

                                return (byte)v13;

                            case 9:
                                v13 = 17;
                                LABEL_190(flags, ref v13);

                                return (byte)v13;

                            case 10:
                                v13 = 18;
                                LABEL_190(flags, ref v13);

                                return (byte)v13;

                            case 12:
                                v13 = 3;
                                LABEL_190(flags, ref v13);

                                return (byte)v13;
                        }

                        // LABEL_187
                        v13 = 1;
                    }

                    LABEL_190(flags, ref v13);

                    return (byte)v13;
                }

                switch (type)
                {
                    case AnimationGroupsType.Human:

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

                                return (byte)v13;

                            case 18:
                                v13 = 6;
                                LABEL_190(flags, ref v13);

                                return (byte)v13;

                            case 19:
                                v13 = 1;
                                LABEL_190(flags, ref v13);

                                return (byte)v13;
                        }

                        //LABEL_161:
                        v13 = 4;

                        goto LABEL_189;

                    case AnimationGroupsType.Animal:

                        switch (v13)
                        {
                            case 0:
                                v13 = 0;

                                goto LABEL_189;

                            case 2:
                                v13 = 8;
                                LABEL_190(flags, ref v13);

                                return (byte)v13;

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

                                return (byte)v13;

                            case 5:
                                v13 = 6;
                                LABEL_190(flags, ref v13);

                                return (byte)v13;

                            case 10:
                            case 21:
                                v13 = 7;
                                LABEL_190(flags, ref v13);

                                return (byte)v13;

                            case 11:
                                v13 = 3;
                                LABEL_190(flags, ref v13);

                                return (byte)v13;

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

                                return (byte)v13;
                        }

                        v13 = 2;
                        LABEL_190(flags, ref v13);

                        return (byte)v13;

                    case AnimationGroupsType.SeaMonster:

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

                                return (byte)v13;
                        }

                        v13 = 2;
                        LABEL_190(flags, ref v13);

                        return (byte)v13;

                    default:
                        LABEL_189:

                        LABEL_190(flags, ref v13);

                        return (byte)v13;
                }

                //// LABEL_188
                //v13 = 2;

                //LABEL_190(flags, ref v13);

                //return (byte)v13;
            }

            byte result = mobile._animationGroup;

            bool isWalking = mobile.IsWalking;
            bool isRun = mobile.IsRunning;

            if (mobile.Steps.Count != 0)
            {
                isWalking = true;
                ref Step step = ref mobile.Steps.Front();
                isRun = step.Run;
            }

            switch (type)
            {
                case AnimationGroupsType.Animal:

                    if ((flags & AnimationFlags.CalculateOffsetLowGroupExtended) != 0)
                    {
                        CalculateHight(graphic, mobile, flags, isRun, isWalking, ref result);
                    }
                    else
                    {
                        if (!isWalking)
                        {
                            if (result == 0xFF)
                            {
                                if ((flags & AnimationFlags.UseUopAnimation) != 0)
                                {
                                    if (
                                        mobile.InWarMode
                                        && Client.Game.Animations.AnimationExists(graphic, 1)
                                    )
                                    {
                                        result = 1;
                                    }
                                    else
                                    {
                                        result = 25;
                                    }
                                }
                                else
                                {
                                    result = 2;
                                }
                            }
                        }
                        else if (isRun)
                        {
                            if ((flags & AnimationFlags.UseUopAnimation) != 0)
                            {
                                result = 24;
                            }
                            else
                            {
                                result = Client.Game.Animations.AnimationExists(graphic, 1)
                                    ? (byte)1
                                    : (byte)2;
                            }
                        }
                        else if (
                            (flags & AnimationFlags.UseUopAnimation) != 0
                            && (
                                !mobile.InWarMode
                                || !Client.Game.Animations.AnimationExists(graphic, 0)
                            )
                        )
                        {
                            result = 22;
                        }
                        else
                        {
                            result = 0;
                        }
                    }

                    break;

                case AnimationGroupsType.Monster:
                    CalculateHight(graphic, mobile, flags, isRun, isWalking, ref result);

                    break;

                case AnimationGroupsType.SeaMonster:

                    if (!isWalking)
                    {
                        if (result == 0xFF)
                        {
                            result = 2;
                        }
                    }
                    else if (isRun)
                    {
                        result = 1;
                    }
                    else
                    {
                        result = 0;
                    }

                    break;

                default:
                    {
                        Item hand2 = mobile.FindItemByLayer(Layer.TwoHanded);

                        if (!isWalking)
                        {
                            if (result == 0xFF)
                            {
                                bool haveLightAtHand2 =
                                    hand2 != null
                                    && hand2.ItemData.IsLight
                                    && hand2.ItemData.AnimID == graphic;

                                if (mobile.IsMounted)
                                {
                                    if (haveLightAtHand2)
                                    {
                                        result = 28;
                                    }
                                    else
                                    {
                                        result = 25;
                                    }
                                }
                                else if (mobile.IsGargoyle && mobile.IsFlying) // TODO: what's up when it is dead?
                                {
                                    if (mobile.InWarMode)
                                    {
                                        result = 65;
                                    }
                                    else
                                    {
                                        result = 64;
                                    }
                                }
                                else if (!mobile.InWarMode || mobile.IsDead)
                                {
                                    if (haveLightAtHand2)
                                    {
                                        // TODO: UOP EQUIPMENT ?
                                        result = 0;
                                    }
                                    else
                                    {
                                        if (
                                            uop
                                            && type == AnimationGroupsType.Equipment
                                            && Client.Game.Animations.AnimationExists(graphic, 37)
                                        )
                                        {
                                            result = 37;
                                        }
                                        else
                                        {
                                            result = 4;
                                        }
                                    }
                                }
                                else if (haveLightAtHand2)
                                {
                                    // TODO: UOP EQUIPMENT ?

                                    result = 2;
                                }
                                else
                                {
                                    unsafe
                                    {
                                        ushort* handAnimIDs = stackalloc ushort[2];
                                        Item hand1 = mobile.FindItemByLayer(Layer.OneHanded);

                                        if (hand1 != null)
                                        {
                                            handAnimIDs[0] = hand1.ItemData.AnimID;
                                        }

                                        if (hand2 != null)
                                        {
                                            handAnimIDs[1] = hand2.ItemData.AnimID;
                                        }

                                        if (hand1 == null)
                                        {
                                            if (hand2 != null)
                                            {
                                                if (
                                                    uop
                                                    && type == AnimationGroupsType.Equipment
                                                    && !Client.Game.Animations.AnimationExists(
                                                        graphic,
                                                        7
                                                    )
                                                )
                                                {
                                                    result = 8;
                                                }
                                                else
                                                {
                                                    result = 7;
                                                }

                                                for (int i = 0; i < 2; i++)
                                                {
                                                    if (
                                                        handAnimIDs[i] >= 0x0263
                                                        && handAnimIDs[i] <= 0x028B
                                                    )
                                                    {
                                                        for (
                                                            int k = 0;
                                                            k < HANDS_BASE_ANIMID.Length;
                                                            k++
                                                        )
                                                        {
                                                            if (handAnimIDs[i] == HANDS_BASE_ANIMID[k])
                                                            {
                                                                result = 8;
                                                                i = 2;

                                                                break;
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            else if (mobile.IsGargoyle && mobile.IsFlying)
                                            {
                                                result = 64;
                                            }
                                            else
                                            {
                                                result = 7;
                                            }
                                        }
                                        else
                                        {
                                            result = 7;
                                        }
                                    }
                                }
                            }
                        }
                        else if (mobile.IsMounted)
                        {
                            if (isRun)
                            {
                                result = 24;
                            }
                            else
                            {
                                result = 23;
                            }
                        }
                        //else if (EquippedGraphic0x3E96)
                        //{

                        //}
                        else if (isRun || !mobile.InWarMode || mobile.IsDead)
                        {
                            if ((flags & AnimationFlags.UseUopAnimation) != 0)
                            {
                                // i'm not sure here if it's necessary the isgargoyle
                                if (mobile.IsGargoyle && mobile.IsFlying)
                                {
                                    if (isRun)
                                    {
                                        result = 63;
                                    }
                                    else
                                    {
                                        result = 62;
                                    }
                                }
                                else
                                {
                                    if (isRun && Client.Game.Animations.AnimationExists(graphic, 24))
                                    {
                                        result = 24;
                                    }
                                    else
                                    {
                                        if (isRun)
                                        {
                                            if (
                                                uop
                                                && type == AnimationGroupsType.Equipment
                                                && !Client.Game.Animations.AnimationExists(graphic, 2)
                                            )
                                            {
                                                result = 3;
                                            }
                                            else
                                            {
                                                result = 2;

                                                if (mobile.IsGargoyle)
                                                {
                                                    hand2 = mobile.FindItemByLayer(Layer.OneHanded);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (
                                                uop
                                                && type == AnimationGroupsType.Equipment
                                                && !Client.Game.Animations.AnimationExists(graphic, 0)
                                            )
                                            {
                                                result = 1;
                                            }
                                            else
                                            {
                                                result = 0;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (isRun)
                                {
                                    result = (byte)(hand2 != null ? 3 : 2);
                                }
                                else
                                {
                                    result = (byte)(hand2 != null ? 1 : 0);
                                }
                            }

                            if (hand2 != null)
                            {
                                ushort hand2Graphic = hand2.ItemData.AnimID;

                                if (hand2Graphic < 0x0240 || hand2Graphic > 0x03E1)
                                {
                                    if (mobile.IsGargoyle && mobile.IsFlying)
                                    {
                                        if (isRun)
                                        {
                                            result = 63;
                                        }
                                        else
                                        {
                                            result = 62;
                                        }
                                    }
                                    else
                                    {
                                        if (isRun)
                                        {
                                            result = 3;
                                        }
                                        else
                                        {
                                            result = 1;
                                        }
                                    }
                                }
                                else
                                {
                                    for (int i = 0; i < HAND2_BASE_ANIMID.Length; i++)
                                    {
                                        if (HAND2_BASE_ANIMID[i] == hand2Graphic)
                                        {
                                            if (mobile.IsGargoyle && mobile.IsFlying)
                                            {
                                                if (isRun)
                                                {
                                                    result = 63;
                                                }
                                                else
                                                {
                                                    result = 62;
                                                }
                                            }
                                            else
                                            {
                                                if (isRun)
                                                {
                                                    result = 3;
                                                }
                                                else
                                                {
                                                    result = 1;
                                                }
                                            }

                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        else if (mobile.IsGargoyle && mobile.IsFlying)
                        {
                            result = 62;
                        }
                        else
                        {
                            result = 15;
                        }

                        break;
                    }
            }

            return result;
        }

        public static bool IsReplacedObjectAnimation(byte anim, ushort v13)
        {
            if (anim < AnimationsLoader.Instance.GroupReplaces.Length)
            {
                foreach (var tuple in AnimationsLoader.Instance.GroupReplaces[anim])
                {
                    if (tuple.Item1 == v13)
                    {
                        return tuple.Item2 != 0xFF;
                    }
                }
            }

            return false;
        }

        public static byte GetReplacedObjectAnimation(ushort graphic, ushort index)
        {
            ushort getReplacedGroup(List<(ushort, byte)> list, ushort idx, ushort walkIdx)
            {
                foreach (var item in list)
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

            AnimationGroups group = AnimationsLoader.Instance.GetGroupIndex(
                graphic,
                Client.Game.Animations.GetAnimType(graphic)
            );

            if (group == AnimationGroups.Low)
            {
                return (byte)(
                    getReplacedGroup(
                        AnimationsLoader.Instance.GroupReplaces[0],
                        index,
                        (ushort)LowAnimationGroup.Walk
                    ) % (ushort)LowAnimationGroup.AnimationCount
                );
            }

            if (group == AnimationGroups.People)
            {
                return (byte)(
                    getReplacedGroup(
                        AnimationsLoader.Instance.GroupReplaces[1],
                        index,
                        (ushort)PeopleAnimationGroup.WalkUnarmed
                    ) % (ushort)PeopleAnimationGroup.AnimationCount
                );
            }

            return (byte)(index % (ushort)HighAnimationGroup.AnimationCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte GetObjectNewAnimation(
            Mobile mobile,
            ushort type,
            ushort action,
            byte mode
        )
        {
            if (mobile.Graphic >= Client.Game.Animations.MaxAnimationCount)
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
                    return GetObjectNewAnimationType_9(mobile, action, mode);
                case 10:
                    return GetObjectNewAnimationType_10(mobile, action, mode);

                case 11:
                    return GetObjectNewAnimationType_11(mobile, action, mode);
            }

            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TestStepNoChangeDirection(Mobile mob, byte group)
        {
            switch ((PeopleAnimationGroup)group)
            {
                case PeopleAnimationGroup.OnmountRideFast:
                case PeopleAnimationGroup.RunUnarmed:
                case PeopleAnimationGroup.RunArmed:
                case PeopleAnimationGroup.OnmountRideSlow:
                case PeopleAnimationGroup.WalkWarmode:
                case PeopleAnimationGroup.WalkArmed:
                case PeopleAnimationGroup.WalkUnarmed:

                    if (mob.Steps.Count != 0)
                    {
                        ref Step s = ref mob.Steps.Front();

                        if (s.X != mob.X || s.Y != mob.Y)
                        {
                            return true;
                        }
                    }

                    break;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte GetObjectNewAnimationType_0(Mobile mobile, ushort action, byte mode)
        {
            if (action <= 10)
            {
                AnimationFlags flags = Client.Game.Animations.GetAnimFlags(mobile.Graphic);
                AnimationGroupsType type = AnimationGroupsType.Monster;

                if ((flags & AnimationFlags.Found) != 0)
                {
                    type = Client.Game.Animations.GetAnimType(mobile.Graphic);
                }

                if (type == AnimationGroupsType.Monster)
                {
                    switch (mode % 4)
                    {
                        case 1:
                            return 5;

                        case 2:
                            return 6;

                        case 3:

                            if ((flags & AnimationFlags.Unknown1) != 0)
                            {
                                return 12;
                            }

                            goto case 0;

                        case 0:
                            return 4;
                    }
                }
                else if (type == AnimationGroupsType.SeaMonster)
                {
                    if (mode % 2 != 0)
                    {
                        return 6;
                    }

                    return 5;
                }
                else if (type != AnimationGroupsType.Animal)
                {
                    if (mobile.IsMounted)
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
                            if (
                                mobile.IsGargoyle
                                && mobile.IsFlying
                                && Client.Game.Animations.AnimationExists(mobile.Graphic, 71)
                            )
                            {
                                return 71;
                            }
                            else if (Client.Game.Animations.AnimationExists(mobile.Graphic, 31))
                            {
                                return 31;
                            }

                            break;

                        case 1:
                            return 18;

                        case 2:
                            return 19;

                        case 6:
                            return 12;

                        case 7:
                            if (
                                mobile.IsGargoyle
                                && mobile.IsFlying
                                && Client.Game.Animations.AnimationExists(mobile.Graphic, 72)
                            )
                            {
                                return 72;
                            }

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

                if ((flags & AnimationFlags.Use2IfHittedWhileRunning) != 0)
                {
                    return 2;
                }

                if (mode % 2 != 0 && Client.Game.Animations.AnimationExists(mobile.Graphic, 6))
                {
                    return 6;
                }

                return 5;
            }

            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte GetObjectNewAnimationType_1_2(Mobile mobile, ushort action, byte mode)
        {
            AnimationFlags flags = Client.Game.Animations.GetAnimFlags(mobile.Graphic);
            AnimationGroupsType type = AnimationGroupsType.Monster;

            if ((flags & AnimationFlags.Found) != 0)
            {
                type = Client.Game.Animations.GetAnimType(mobile.Graphic);
            }

            if (type != AnimationGroupsType.Monster)
            {
                if (type <= AnimationGroupsType.Animal || mobile.IsMounted)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte GetObjectNewAnimationType_3(Mobile mobile, ushort action, byte mode)
        {
            AnimationFlags flags = Client.Game.Animations.GetAnimFlags(mobile.Graphic);
            AnimationGroupsType type = AnimationGroupsType.Monster;

            if ((flags & AnimationFlags.Found) != 0)
            {
                type = Client.Game.Animations.GetAnimType(mobile.Graphic);
            }

            if (type != AnimationGroupsType.Monster)
            {
                if (type == AnimationGroupsType.SeaMonster)
                {
                    return 8;
                }

                if (type == AnimationGroupsType.Animal)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte GetObjectNewAnimationType_4(Mobile mobile, ushort action, byte mode)
        {
            AnimationFlags flags = Client.Game.Animations.GetAnimFlags(mobile.Graphic);
            AnimationGroupsType type = AnimationGroupsType.Monster;

            if ((flags & AnimationFlags.Found) != 0)
            {
                type = Client.Game.Animations.GetAnimType(mobile.Graphic);
            }

            if (type != AnimationGroupsType.Monster)
            {
                if (type > AnimationGroupsType.Animal)
                {
                    if (
                        mobile.IsGargoyle
                        && mobile.IsFlying
                        && Client.Game.Animations.AnimationExists(mobile.Graphic, 77)
                    )
                    {
                        return 77;
                    }

                    if (mobile.IsMounted)
                    {
                        return 0xFF;
                    }

                    return 20;
                }

                return 7;
            }

            return 10;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte GetObjectNewAnimationType_5(Mobile mobile, ushort action, byte mode)
        {
            AnimationFlags flags = Client.Game.Animations.GetAnimFlags(mobile.Graphic);
            AnimationGroupsType type = AnimationGroupsType.Monster;

            if ((flags & AnimationFlags.Found) != 0)
            {
                type = Client.Game.Animations.GetAnimType(mobile.Graphic);
            }

            if (type <= AnimationGroupsType.SeaMonster)
            {
                if (mode % 2 != 0)
                {
                    return 18;
                }

                return 17;
            }

            if (type != AnimationGroupsType.Animal)
            {
                if (mobile.IsMounted)
                {
                    return 0xFF;
                }

                if (mode % 2 != 0)
                {
                    return 6;
                }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte GetObjectNewAnimationType_6_14(Mobile mobile, ushort action, byte mode)
        {
            AnimationFlags flags = Client.Game.Animations.GetAnimFlags(mobile.Graphic);
            AnimationGroupsType type = AnimationGroupsType.Monster;

            if ((flags & AnimationFlags.Found) != 0)
            {
                type = Client.Game.Animations.GetAnimType(mobile.Graphic);
            }

            if (type != AnimationGroupsType.Monster)
            {
                if (type != AnimationGroupsType.SeaMonster)
                {
                    if (type == AnimationGroupsType.Animal)
                    {
                        return 3;
                    }

                    if (mobile.IsMounted)
                    {
                        return 0xFF;
                    }

                    return 34;
                }

                return 5;
            }

            return 11;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte GetObjectNewAnimationType_7(Mobile mobile, ushort action, byte mode)
        {
            if (mobile.IsMounted)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte GetObjectNewAnimationType_8(Mobile mobile, ushort action, byte mode)
        {
            AnimationFlags flags = Client.Game.Animations.GetAnimFlags(mobile.Graphic);
            AnimationGroupsType type = AnimationGroupsType.Monster;

            if ((flags & AnimationFlags.Found) != 0)
            {
                type = Client.Game.Animations.GetAnimType(mobile.Graphic);
            }

            if (type != AnimationGroupsType.Monster)
            {
                if (type != AnimationGroupsType.SeaMonster)
                {
                    if (type == AnimationGroupsType.Animal)
                    {
                        return 9;
                    }

                    return mobile.IsMounted ? (byte)0xFF : (byte)33;
                }

                return 3;
            }

            return 11;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte GetObjectNewAnimationType_9(Mobile mobile, ushort action, byte mode)
        {
            AnimationFlags flags = Client.Game.Animations.GetAnimFlags(mobile.Graphic);
            AnimationGroupsType type = AnimationGroupsType.Monster;

            if ((flags & AnimationFlags.Found) != 0)
            {
                type = Client.Game.Animations.GetAnimType(mobile.Graphic);
            }

            if (type != AnimationGroupsType.Monster)
            {
                if (mobile.IsGargoyle)
                {
                    if (action == 0)
                    {
                        return 60;
                    }
                }

                return 0xFF;
            }

            return 20;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte GetObjectNewAnimationType_10(Mobile mobile, ushort action, byte mode)
        {
            AnimationFlags flags = Client.Game.Animations.GetAnimFlags(mobile.Graphic);
            AnimationGroupsType type = AnimationGroupsType.Monster;

            if ((flags & AnimationFlags.Found) != 0)
            {
                type = Client.Game.Animations.GetAnimType(mobile.Graphic);
            }

            if (type != AnimationGroupsType.Monster)
            {
                if (mobile.IsGargoyle)
                {
                    if (action == 0)
                    {
                        return 61;
                    }
                }

                return 0xFF;
            }

            return 20;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte GetObjectNewAnimationType_11(Mobile mobile, ushort action, byte mode)
        {
            AnimationFlags flags = Client.Game.Animations.GetAnimFlags(mobile.Graphic);
            AnimationGroupsType type = AnimationGroupsType.Monster;

            if ((flags & AnimationFlags.Found) != 0)
            {
                type = Client.Game.Animations.GetAnimType(mobile.Graphic);
            }

            if (type != AnimationGroupsType.Monster)
            {
                if (type >= AnimationGroupsType.Animal)
                {
                    if (mobile.IsMounted)
                    {
                        return 0xFF;
                    }

                    switch (action)
                    {
                        case 1:
                        case 2:
                            if (mobile.IsGargoyle && mobile.IsFlying)
                            {
                                return 76;
                            }

                            return 17;
                    }

                    if (mobile.IsGargoyle && mobile.IsFlying)
                    {
                        return 75;
                    }

                    return 16;
                }

                return 5;
            }

            return 12;
        }
    }
}
