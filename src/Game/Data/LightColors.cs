﻿#region license

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

namespace ClassicUO.Game.Data
{
    internal static class LightColors
    {
        public static ushort GetHue(ushort id)
        {
            ushort color = 0;

            if (id < 0x3E27)
            {
                //color = ???;
            }
            else
            {
                color = 666;

                //if (id > 0x3E3A)
                //	color = ???;
            }

            switch (id)
            {
                case 0x088C:
                    color = 31;

                    break;

                case 0x0FAC:
                    color = 30;

                    break;

                case 0x0FB1:
                    color = 60;

                    break;

                case 0x1647:
                    color = 51;

                    break;

                case 0x19BB:
                case 0x1F2B:
                    color = 40;

                    break;

                case 0x9F66:
                    color = 0;

                    break;
            }


            if (id < 0x09FB || id > 0x0A14)
            {
                if (id < 0x0A15 || id > 0x0A29)
                {
                    if (id < 0x0B1A || id > 0x0B1F)
                    {
                        if (id < 0x0B20 || id > 0x0B25)
                        {
                            if (id < 0x0B26 || id > 0x0B28)
                            {
                                if (id < 0x0DE1 || id > 0x0DEA)
                                {
                                    if (id < 0x1849 || id > 0x1850)
                                    {
                                        if (id < 0x1853 || id > 0x185A)
                                        {
                                            if (id < 0x197A || id > 0x19A9)
                                            {
                                                if (id < 0x19AB || id > 0x19B6)
                                                {
                                                    if (id >= 0x1ECD && id <= 0x1ECF || id >= 0x1ED0 && id <= 0x1ED2)
                                                    {
                                                        color = 62;
                                                    }
                                                }
                                                else
                                                {
                                                    color = 30;
                                                }
                                            }
                                            else
                                            {
                                                color = 51;
                                            }
                                        }
                                        else
                                        {
                                            color = 61;
                                        }
                                    }
                                    else
                                    {
                                        color = 61;
                                    }
                                }
                                else
                                {
                                    color = 41;
                                }
                            }
                            else
                            {
                                color = 0;
                            }
                        }
                        else
                        {
                            color = 0;
                        }
                    }
                    else
                    {
                        color = 0;
                    }
                }
                else
                {
                    color = 0;
                }
            }
            else
            {
                color = 40;
            }

            if (id == 0x1FD4 || id == 0x0F6C)
            {
                color = 2;
            }

            if (id < 0x0E2D || id > 0x0E30)
            {
                if (id < 0x0E31 || id > 0x0E33)
                {
                    if (id < 0x0E5C || id > 0x0E6A)
                    {
                        if (id < 0x12EE || id > 0x134D)
                        {
                            if (id < 0x306A || id > 0x329B)
                            {
                                if (id < 0x343B || id > 0x346C)
                                {
                                    if (id < 0x3547 || id > 0x354C)
                                    {
                                        if (id < 0x3914 || id > 0x3929)
                                        {
                                            if (id < 0x3946 || id > 0x3964)
                                            {
                                                if (id < 0x3967 || id > 0x397A)
                                                {
                                                    if (id < 0x398C || id > 0x399F)
                                                    {
                                                        if (id < 0x3E02 || id > 0x3E0B)
                                                        {
                                                            if (id < 0x3E27 || id > 0x3E3A)
                                                            {
                                                                switch (id)
                                                                {
                                                                    case 0x40FE:
                                                                        color = 40;

                                                                        break;

                                                                    case 0x40FF:
                                                                        color = 10;

                                                                        break;

                                                                    case 0x4100:
                                                                        color = 20;

                                                                        break;

                                                                    case 0x4101:
                                                                        color = 32;

                                                                        break;

                                                                    default:

                                                                        if (id >= 0x983B && id <= 0x983D || id >= 0x983F && id <= 0x9841)
                                                                        {
                                                                            color = 30;
                                                                        }

                                                                        break;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                color = 31;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            color = 1;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        color = 31;
                                                    }
                                                }
                                                else
                                                {
                                                    color = 6;
                                                }
                                            }
                                            else
                                            {
                                                color = 6;
                                            }
                                        }
                                        else
                                        {
                                            color = 1;
                                        }
                                    }
                                    else
                                    {
                                        color = 31;
                                    }
                                }
                                else
                                {
                                    color = 31;
                                }
                            }
                            else
                            {
                                color = 31;
                            }
                        }
                        else
                        {
                            color = 40;
                        }
                    }
                    else
                    {
                        color = 6;
                    }
                }
                else
                {
                    color = 40;
                }
            }
            else
            {
                color = 0;
            }

            return color;
        }
    }
}