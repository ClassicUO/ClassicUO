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
                    color = 61;

                    break;

                case 0x19BB:
                case 0x1F2B:
                    color = 40;

                    break;
            }

            ;

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
                                                    if (id >= 0x1ECD && id <= 0x1ECF ||
                                                        id >= 0x1ED0 && id <= 0x1ED2)
                                                        color = 1;
                                                }
                                                else
                                                    color = 60;
                                            }
                                            else
                                                color = 60;
                                        }
                                        else
                                            color = 61;
                                    }
                                    else
                                        color = 61;
                                }
                                else
                                    color = 31;
                            }
                            else
                                color = 666;
                        }
                        else
                            color = 666;
                    }
                    else
                        color = 666;
                }
                else
                    color = 666;
            }
            else
                color = 30;

            if (id == 0x1FD4 || id == 0x0F6C) color = 2;

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

                                                                        if (id >= 0x983B &&
                                                                            id <= 0x983D ||
                                                                            id >= 0x983F && id <= 0x9841)
                                                                            color = 30;

                                                                        break;
                                                                }
                                                            }
                                                            else
                                                                color = 31;
                                                        }
                                                        else
                                                            color = 1;
                                                    }
                                                    else
                                                        color = 31;
                                                }
                                                else
                                                    color = 6;
                                            }
                                            else
                                                color = 6;
                                        }
                                        else
                                            color = 1;
                                    }
                                    else
                                        color = 31;
                                }
                                else
                                    color = 31;
                            }
                            else
                                color = 31;
                        }
                        else
                            color = 31;
                    }
                    else
                        color = 6;
                }
                else
                    color = 40;
            }
            else
                color = 62;

            return color;
        }
    }
}