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

using System.Runtime.CompilerServices;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Utility
{
    internal class GraphicHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ScreenToWorldCoordinates(Rectangle viewport, ref Point in_point, ref Matrix transform, out Point result)
        {
            Matrix matrix = Matrix.Invert(transform);

            float x = (in_point.X - viewport.X) / (float) viewport.Width * 2f - 1f;
            float y = -((in_point.Y - viewport.Y) / (float) viewport.Height * 2f - 1f);

            result.X = (int) (x * matrix.M11 + y * matrix.M21 + matrix.M41);
            result.Y = (int) (x * matrix.M12 + y * matrix.M22 + matrix.M42);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WorldToScreenCoordinates(Rectangle viewport, ref Point in_point, ref Matrix transform, out Point result)
        {
            float x = in_point.X * transform.M11 + in_point.Y * transform.M21 + transform.M41;
            float y = in_point.X * transform.M12 + in_point.Y * transform.M22 + transform.M42;

            result.X = (int) ((x + 1f) * 0.5f * viewport.Width + viewport.X);
            result.Y = (int) ((-y + 1f) * 0.5f * viewport.Height + viewport.Y);
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
        internal static UOTexture[] SplitTexture16(UOTexture original, int[,] partXYplusWidthHeight)
        {
            if (partXYplusWidthHeight.GetLength(0) == 0 || partXYplusWidthHeight.GetLength(1) < 4)
            {
                return null;
            }

            UOTexture[] r = new UOTexture[partXYplusWidthHeight.GetLength(0)];
            int pwidth = original.Width;   //((original.Width + 1) >> 1) << 1;
            int pheight = original.Height; //((original.Height + 1) >> 1) << 1;
            uint[] originalData = System.Buffers.ArrayPool<uint>.Shared.Rent(pwidth * pheight);

            original.GetData(originalData, 0, pwidth * pheight);

            try
            {
                int index = 0;

                for (int p = 0; p < partXYplusWidthHeight.GetLength(0); p++)
                {
                    int x = partXYplusWidthHeight[p, 0], y = partXYplusWidthHeight[p, 1], width = partXYplusWidthHeight[p, 2], height = partXYplusWidthHeight[p, 3];

                    uint[] partData = System.Buffers.ArrayPool<uint>.Shared.Rent(width * height);

                    try
                    {
                        UOTexture part = new UOTexture(width, height);

                        for (int py = 0; py < height; py++)
                        {
                            for (int px = 0; px < width; px++)
                            {
                                int partIndex = px + py * width;

                                //If a part goes outside of the source texture, then fill the overlapping part with transparent
                                if (y + py >= pheight || x + px >= pwidth)
                                {
                                    partData[partIndex] = 0;
                                }
                                else
                                {
                                    partData[partIndex] = originalData[x + px + (y + py) * pwidth];
                                }
                            }
                        }

                        part.SetData(partData, 0, width * height);
                        r[index++] = part;
                    }
                    finally
                    {
                        System.Buffers.ArrayPool<uint>.Shared.Return(partData, true);
                    }
                }
            }
            finally
            {
                System.Buffers.ArrayPool<uint>.Shared.Return(originalData, true);
            }

            return r;
        }
    }
}