using ClassicUO.Renderer;

namespace ClassicUO.Utility
{
    internal class GraphicHelper
    {
        /// <summary>
        ///     Splits a texture into an array of smaller textures of the specified size.
        /// </summary>
        /// <param name="original">The texture to be split into smaller textures</param>
        /// <param name="partXYplusWidthHeight">
        ///     We must specify here an array with size of 'parts' for the first dimension,
        ///     for each part, in the second dimension, we specify:
        ///     starting x and y, plus width and height for that specified part (4 as size in second dimension).
        /// </param>
        internal static UOTexture16[] SplitTexture16(UOTexture original, int[,] partXYplusWidthHeight)
        {
            if (partXYplusWidthHeight.GetLength(0) == 0 || partXYplusWidthHeight.GetLength(1) < 4)
                return null;

            UOTexture16[] r = new UOTexture16[partXYplusWidthHeight.GetLength(0)];
            int pwidth = ((original.Width + 1) >> 1) << 1;
            int pheight = ((original.Height + 1) >> 1) << 1;
            ushort[] originalData = new ushort[pwidth * pheight];
            original.GetData(originalData, 0, pwidth * pheight);

            int index = 0;

            for (int p = 0; p < partXYplusWidthHeight.GetLength(0); p++)
            {
                int x = partXYplusWidthHeight[p, 0], y = partXYplusWidthHeight[p, 1], width = partXYplusWidthHeight[p, 2], height = partXYplusWidthHeight[p, 3];
                UOTexture16 part = new UOTexture16(width, height);
                ushort[] partData = new ushort[width * height];

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