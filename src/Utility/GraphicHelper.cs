#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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

using System.Runtime.CompilerServices;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Utility
{
    internal class GraphicHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ScreenToWorldCoordinates(Rectangle viewport, ref Point in_point, ref Matrix transform, out Point result)
        {
            Matrix matrix = Matrix.Invert(transform);

            float x =   (((in_point.X - viewport.X) / ((float) viewport.Width))  * 2f) - 1f;
            float y = -((((in_point.Y - viewport.Y) / ((float) viewport.Height)) * 2f) - 1f);

            result.X = (int) ((x * matrix.M11) + (y * matrix.M21) + matrix.M41);
            result.Y = (int) ((x * matrix.M12) + (y * matrix.M22) + matrix.M42);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WorldToScreenCoordinates(Rectangle viewport, ref Point in_point, ref Matrix transform, out Point result)
        {
            float x = ((in_point.X * transform.M11) + (in_point.Y * transform.M21) + transform.M41);
            float y = ((in_point.X * transform.M12) + (in_point.Y * transform.M22) + transform.M42);

            result.X = (int) ((((x + 1f) * 0.5f) * viewport.Width) + viewport.X);
            result.Y = (int) ((((-y + 1f) * 0.5f) * viewport.Height) + viewport.Y);
        }


        /// <summary>
        ///     Splits a texture into an array of smaller textures of the specified size.
        /// </summary>
        /// <param name="original">The texture to be split into smaller textures</param>
        /// <param name="partXYplusWidthHeight">
        ///     We must specify here an array with size of 'parts' for the first dimension,
        ///     for each part, in the second dimension, we specify:
        ///     starting x and y, plus width and height for that specified part (4 as size in second dimension).
        /// </param>
        internal static UOTexture32[] SplitTexture16(UOTexture32 original, int[,] partXYplusWidthHeight)
        {
            if (partXYplusWidthHeight.GetLength(0) == 0 || partXYplusWidthHeight.GetLength(1) < 4)
                return null;

            UOTexture32[] r = new UOTexture32[partXYplusWidthHeight.GetLength(0)];
            int pwidth = original.Width; //((original.Width + 1) >> 1) << 1;
            int pheight = original.Height; //((original.Height + 1) >> 1) << 1;
            uint[] originalData = original.Data;

            int index = 0;

            for (int p = 0; p < partXYplusWidthHeight.GetLength(0); p++)
            {
                int x = partXYplusWidthHeight[p, 0], y = partXYplusWidthHeight[p, 1], width = partXYplusWidthHeight[p, 2], height = partXYplusWidthHeight[p, 3];
                UOTexture32 part = new UOTexture32(width, height);
                uint[] partData = new uint[width * height];

                for (int py = 0; py < height; py++)
                {
                    for (int px = 0; px < width; px++)
                    {
                        int partIndex = px + py * width;

                        //If a part goes outside of the source texture, then fill the overlapping part with transparent
                        if (y + py >= pheight || x + px >= pwidth)
                            partData[partIndex] = 0;
                        else
                            partData[partIndex] = originalData[x + px + (y + py) * pwidth];
                    }
                }

                part.PushData(partData);
                r[index++] = part;
            }

            return r;
        }
    }
}