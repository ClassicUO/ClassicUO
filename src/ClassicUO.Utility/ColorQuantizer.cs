using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassicUO.Utility
{
    public interface IColorQuantizer
    {
        /// <summary>
        ///     Adds the color to quantizer.
        /// </summary>
        /// <param name="color">The color to be added.</param>
        void AddColor(uint color);

        /// <summary>
        ///     Gets the palette with specified count of the colors.
        /// </summary>
        /// <param name="colorCount">The color count.</param>
        /// <returns></returns>
        List<uint> GetPalette(int colorCount);

        /// <summary>
        ///     Gets the index of the palette for specific color.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <returns></returns>
        int GetPaletteIndex(uint color);

        /// <summary>
        ///     Gets the color count.
        /// </summary>
        /// <returns></returns>
        int GetColorCount();

        /// <summary>
        ///     Clears this instance.
        /// </summary>
        void Clear();
    }

    static class ArgbHelper
    {
        public static uint FromArgb(int alpha, int red, int green, int blue)
        {
            return ((uint)alpha << 24) | ((uint)red << 16) | ((uint)green << 8) | (uint)blue;
        }

        public static int GetAlpha(uint color)
        {
            return (int)(color >> 24) & 0xFF;
        }

        public static int GetRed(uint color)
        {
            return (int)(color >> 16) & 0xFF;
        }

        public static int GetGreen(uint color)
        {
            return (int)(color >> 8) & 0xFF;
        }

        public static int GetBlue(uint color)
        {
            return (int)color & 0xFF;
        }

        public static float GetHue(uint color)
        {
            // Same logic as Color.GetHue but using uint
            int r = GetRed(color);
            int g = GetGreen(color);
            int b = GetBlue(color);

            int max = Math.Max(r, Math.Max(g, b));
            int min = Math.Min(r, Math.Min(g, b));

            float hue = 0f;
            if (max == min)
            {
                hue = 0f;
            }
            else
            {
                float delta = max - min;
                if (r == max)
                {
                    hue = (g - b) / delta;
                }
                else if (g == max)
                {
                    hue = 2f + (b - r) / delta;
                }
                else if (b == max)
                {
                    hue = 4f + (r - g) / delta;
                }
                hue *= 60f;
                if (hue < 0f)
                {
                    hue += 360f;
                }
            }
            return hue;
        }

        public static float GetSaturation(uint color)
        {
            // Same logic as Color.GetSaturation but using uint
            int r = GetRed(color);
            int g = GetGreen(color);
            int b = GetBlue(color);

            int max = Math.Max(r, Math.Max(g, b));
            int min = Math.Min(r, Math.Min(g, b));

            if (max == 0) return 0f;

            return 1f - (1f * min / max);
        }

        public static float GetBrightness(uint color)
        {
            // Same logic as Color.GetBrightness but using uint
            int r = GetRed(color);
            int g = GetGreen(color);
            int b = GetBlue(color);

            int max = Math.Max(r, Math.Max(g, b));
            return max / 255f;
        }
    }

    public class QuantizationHelper
    {
        private static readonly uint _BackgroundColor;
        private static readonly double[] _Factors;

        static QuantizationHelper()
        {
            _BackgroundColor = 0;
            _Factors = PrecalculateFactors();
        }

        /// <summary>
        ///     Precalculates the alpha-fix values for all the possible alpha values (0-255).
        /// </summary>
        private static double[] PrecalculateFactors()
        {
            var result = new double[256];

            for (var value = 0; value < 256; value++) result[value] = value / 255.0;

            return result;
        }

        /// <summary>
        ///     Converts the alpha blended color to a non-alpha blended color.
        /// </summary>
        /// <param name="color">The alpha blended color (ARGB).</param>
        /// <returns>The non-alpha blended color (RGB).</returns>
        internal static uint ConvertAlpha(uint color)
        {
            var result = color;

            if (ArgbHelper.GetAlpha(color) < 255)
            {
                // performs a alpha blending (second color is BackgroundColor, by default a Control color)
                var colorFactor = _Factors[ArgbHelper.GetAlpha(color)];
                var backgroundFactor = _Factors[255 - ArgbHelper.GetAlpha(color)];
                var red = (int)(ArgbHelper.GetRed(color) * colorFactor + ArgbHelper.GetRed(_BackgroundColor) * backgroundFactor);
                var green = (int)(ArgbHelper.GetGreen(color) * colorFactor + ArgbHelper.GetGreen(_BackgroundColor) * backgroundFactor);
                var blue = (int)(ArgbHelper.GetBlue(color) * colorFactor + ArgbHelper.GetBlue(_BackgroundColor) * backgroundFactor);
                result = ArgbHelper.FromArgb(255, red, green, blue);
            }

            return result;
        }

        /// <summary>
        ///     Finds the closest color match in a given palette using Euclidean distance.
        /// </summary>
        /// <param name="color">The color to be matched.</param>
        /// <param name="palette">The palette to search in.</param>
        /// <returns>The palette index of the closest match.</returns>
        internal static int GetNearestColor(uint color, IList<uint> palette)
        {
            // initializes the best difference, set it for worst possible, it can only get better
            var bestIndex = 0;
            var leastDifference = int.MaxValue;

            // goes thru all the colors in the palette, looking for the best match
            for (var index = 0; index < palette.Count; index++)
            {
                var targetColor = palette[index];

                // calculates a difference for all the color components
                var deltaA = ArgbHelper.GetAlpha(color) - ArgbHelper.GetAlpha(targetColor);
                var deltaR = ArgbHelper.GetRed(color) - ArgbHelper.GetRed(targetColor);
                var deltaG = ArgbHelper.GetGreen(color) - ArgbHelper.GetGreen(targetColor);
                var deltaB = ArgbHelper.GetBlue(color) - ArgbHelper.GetBlue(targetColor);

                // calculates a power of two
                var factorA = deltaA * deltaA;
                var factorR = deltaR * deltaR;
                var factorG = deltaG * deltaG;
                var factorB = deltaB * deltaB;

                // calculates the Euclidean distance, a square-root is not need
                // as we're only comparing distance, not measuring it
                var difference = factorA + factorR + factorG + factorB;

                // if a difference is zero, we're good because it won't get better
                if (difference == 0)
                {
                    bestIndex = index;
                    break;
                }

                // if a difference is the best so far, stores it as our best candidate
                if (difference < leastDifference)
                {
                    leastDifference = difference;
                    bestIndex = index;
                }
            }

            // returns the palette index of the most similar color
            return bestIndex;
        }
    }

    internal struct ColorInfo
    {
        /// <summary>
        ///     The original color.
        /// </summary>
        public uint Color { get; }

        /// <summary>
        ///     The pixel presence count in the image.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        ///     A hue component of the color.
        /// </summary>
        public float Hue { get; }

        /// <summary>
        ///     A saturation component of the color.
        /// </summary>
        public float Saturation { get; }

        /// <summary>
        ///     A brightness component of the color.
        /// </summary>
        public float Brightness { get; }

        /// <summary>
        ///     A cached hue hashcode.
        /// </summary>
        public int HueHashCode { get; }

        /// <summary>
        ///     A cached saturation hashcode.
        /// </summary>
        public int SaturationHashCode { get; }

        /// <summary>
        ///     A cached brightness hashcode.
        /// </summary>
        public int BrightnessHashCode { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ColorInfo" /> struct.
        /// </summary>
        /// <param name="color">The color.</param>
        public ColorInfo(uint color) : this()
        {
            Color = color;
            Count = 1;

            Hue = ArgbHelper.GetHue(color);
            Saturation = ArgbHelper.GetSaturation(color);
            Brightness = ArgbHelper.GetBrightness(color);

            HueHashCode = Hue.GetHashCode();
            SaturationHashCode = Saturation.GetHashCode();
            BrightnessHashCode = Brightness.GetHashCode();
        }

        /// <summary>
        ///     Increases the count of pixels of this color.
        /// </summary>
        public void IncreaseCount()
        {
            Count++;
        }
    }

    public class OctreeQuantizer : IColorQuantizer
    {
        private readonly List<OctreeNode>[] _levels;
        private OctreeNode _root;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Octree" /> class.
        /// </summary>
        public OctreeQuantizer()
        {
            // initializes the octree level lists
            _levels = new List<OctreeNode>[7];

            // creates the octree level lists
            for (var level = 0; level < 7; level++) _levels[level] = new List<OctreeNode>();

            // creates a root node
            _root = new OctreeNode(0, this);
        }

        #region | Calculated properties |

        /// <summary>
        ///     Gets the leaf nodes only (recursively).
        /// </summary>
        /// <value>All the tree leaves.</value>
        internal IEnumerable<OctreeNode> Leaves
        {
            get { return _root.ActiveNodes.Where(node => node.IsLeaf); }
        }

        #endregion | Calculated properties |

        #region | Methods |

        /// <summary>
        ///     Adds the node to a level node list.
        /// </summary>
        /// <param name="level">The depth level.</param>
        /// <param name="octreeNode">The octree node to be added.</param>
        internal void AddLevelNode(int level, OctreeNode octreeNode)
        {
            _levels[level].Add(octreeNode);
        }

        #endregion | Methods |

        #region << IColorQuantizer >>

        /// <summary>
        ///     Adds the color to quantizer.
        /// </summary>
        /// <param name="color">The color to be added.</param>
        public void AddColor(uint color)
        {
            color = QuantizationHelper.ConvertAlpha(color);
            _root.AddColor(color, 0, this);
        }

        /// <summary>
        ///     Gets the palette with specified count of the colors.
        /// </summary>
        /// <param name="colorCount">The color count.</param>
        /// <returns></returns>
        public List<uint> GetPalette(int colorCount)
        {
            var result = new List<uint>();
            var leafCount = Leaves.Count();
            var paletteIndex = 0;

            // goes thru all the levels starting at the deepest, and goes upto a root level
            for (var level = 6; level >= 0; level--)
            {
                // if level contains any node
                if (_levels[level].Count > 0)
                {
                    // orders the level node list by pixel presence (those with least pixels are at the top)
                    IEnumerable<OctreeNode> sortedNodeList = _levels[level].OrderBy(node => node.ActiveNodesPixelCount);

                    // removes the nodes unless the count of the leaves is lower or equal than our requested color count
                    foreach (var node in sortedNodeList)
                    {
                        // removes a node
                        leafCount -= node.RemoveLeaves();

                        // if the count of leaves is lower then our requested count terminate the loop
                        if (leafCount <= colorCount) break;
                    }

                    // if the count of leaves is lower then our requested count terminate the level loop as well
                    if (leafCount <= colorCount) break;

                    // otherwise clear whole level, as it is not needed anymore
                    _levels[level].Clear();
                }
            }

            // goes through all the leaves that are left in the tree (there should now be less or equal than requested)
            foreach (var node in Leaves)
            {
                // adds then to a palette
                result.Add(node.Color);

                // and marks the node with a palette index
                node.SetPaletteIndex(paletteIndex++);
            }

            // we're unable to reduce the Octree with enough precision, and the leaf count is zero
            if (result.Count == 0)
            {
                throw new NotSupportedException("The Octree contains after the reduction 0 colors, it may happen for 1-16 colors because it reduces by 1-8 nodes at time. Should be used on 8 or above to ensure the correct functioning.");
            }

            // returns the palette
            return result;
        }

        /// <summary>
        ///     Gets the index of the palette for specific color.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <returns></returns>
        public int GetPaletteIndex(uint color)
        {
            color = QuantizationHelper.ConvertAlpha(color);

            // retrieves a palette index
            return _root.GetPaletteIndex(color, 0);
        }

        /// <summary>
        ///     Gets the color count.
        /// </summary>
        /// <returns></returns>
        public int GetColorCount()
        {
            // calculates the number of leaves, by parsing the whole tree
            return Leaves.Count();
        }

        /// <summary>
        ///     Clears this instance.
        /// </summary>
        public void Clear()
        {
            // clears all the node list levels
            foreach (var level in _levels) level.Clear();

            // creates a new root node (thus throwing away the old tree)
            _root = new OctreeNode(0, this);
        }

        #endregion << IColorQuantizer >>
    }

    internal class OctreeNode
    {
        private static readonly byte[] _Mask = { 0x80, 0x40, 0x20, 0x10, 0x08, 0x04, 0x02, 0x01 };

        private readonly OctreeNode[] _nodes;
        private int _blue;
        private int _green;
        private int _paletteIndex;

        private int _pixelCount;

        private int _red;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OctreeNode" /> class.
        /// </summary>
        public OctreeNode(int level, OctreeQuantizer parent)
        {
            _nodes = new OctreeNode[8];

            if (level < 7) parent.AddLevelNode(level, this);
        }

        #region | Calculated properties |

        /// <summary>
        ///     Gets a value indicating whether this node is a leaf.
        /// </summary>
        /// <value><c>true</c> if this node is a leaf; otherwise, <c>false</c>.</value>
        public bool IsLeaf => _pixelCount > 0;

        /// <summary>
        ///     Gets the averaged leaf color.
        /// </summary>
        /// <value>The leaf color.</value>
        public uint Color
        {
            get
            {
                uint result;

                // determines a color of the leaf
                if (IsLeaf)
                {
                    if (_pixelCount == 1)
                    {
                        // if a pixel count for this color is 1 than this node contains our color already
                        result = ArgbHelper.FromArgb(255, _red, _green, _blue);
                    }
                    else
                    {
                        // otherwise calculates the average color (without rounding)
                        result = ArgbHelper.FromArgb(255, _red / _pixelCount, _green / _pixelCount, _blue / _pixelCount);
                    }
                }
                else
                    throw new InvalidOperationException("Cannot retrieve a color for other node than leaf.");

                return result;
            }
        }

        /// <summary>
        ///     Gets the active nodes pixel count.
        /// </summary>
        /// <value>The active nodes pixel count.</value>
        public int ActiveNodesPixelCount
        {
            get
            {
                var result = _pixelCount;

                // sums up all the pixel presence for all the active nodes
                for (var index = 0; index < 8; index++)
                {
                    var node = _nodes[index];

                    if (node != null) result += node._pixelCount;
                }

                return result;
            }
        }

        /// <summary>
        ///     Enumerates only the leaf nodes.
        /// </summary>
        /// <value>The enumerated leaf nodes.</value>
        public IEnumerable<OctreeNode> ActiveNodes
        {
            get
            {
                var result = new List<OctreeNode>();

                // adds all the active sub-nodes to a list
                for (var index = 0; index < 8; index++)
                {
                    var node = _nodes[index];

                    if (node != null)
                    {
                        if (node.IsLeaf)
                            result.Add(node);
                        else
                            result.AddRange(node.ActiveNodes);
                    }
                }

                return result;
            }
        }

        #endregion | Calculated properties |

        #region | Methods |

        /// <summary>
        ///     Adds the color.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <param name="level">The level.</param>
        /// <param name="parent">The parent.</param>
        public void AddColor(uint color, int level, OctreeQuantizer parent)
        {
            // if this node is a leaf, then increase a color amount, and pixel presence
            if (level == 8)
            {
                _red += ArgbHelper.GetRed(color);
                _green += ArgbHelper.GetGreen(color);
                _blue += ArgbHelper.GetBlue(color);
                _pixelCount++;
            }
            else if (level < 8) // otherwise goes one level deeper
            {
                // calculates an index for the next sub-branch
                var index = GetColorIndexAtLevel(color, level);

                // if that branch doesn't exist, grows it
                if (_nodes[index] == null) _nodes[index] = new OctreeNode(level, parent);

                // adds a color to that branch
                _nodes[index].AddColor(color, level + 1, parent);
            }
        }

        /// <summary>
        ///     Gets the index of the palette.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <param name="level">The level.</param>
        /// <returns></returns>
        public int GetPaletteIndex(uint color, int level)
        {
            int result;

            // if a node is leaf, then we've found are best match already
            if (IsLeaf)
                result = _paletteIndex;
            else // otherwise continue in to the lower depths
            {
                var index = GetColorIndexAtLevel(color, level);
                result = _nodes[index].GetPaletteIndex(color, level + 1);
            }

            return result;
        }

        /// <summary>
        ///     Removes the leaves by summing all it's color components and pixel presence.
        /// </summary>
        /// <returns></returns>
        public int RemoveLeaves()
        {
            var result = 0;

            // scans thru all the active nodes
            for (var index = 0; index < 8; index++)
            {
                var node = _nodes[index];

                if (node != null)
                {
                    // sums up their color components
                    _red += node._red;
                    _green += node._green;
                    _blue += node._blue;

                    // and pixel presence
                    _pixelCount += node._pixelCount;

                    // then deactivates the node
                    _nodes[index] = null;

                    // increases the count of reduced nodes
                    result++;
                }
            }

            // returns a number of reduced sub-nodes, minus one because this node becomes a leaf
            return result - 1;
        }

        #endregion | Methods |

        #region | Helper methods |

        /// <summary>
        ///     Calculates the color component bit (level) index.
        /// </summary>
        /// <param name="color">The color for which the index will be calculated.</param>
        /// <param name="level">The bit index to be used for index calculation.</param>
        /// <returns>The color index at a certain depth level.</returns>
        private static int GetColorIndexAtLevel(uint color, int level)
        {
            return ((ArgbHelper.GetRed(color) & _Mask[level]) == _Mask[level] ? 4 : 0) | ((ArgbHelper.GetGreen(color) & _Mask[level]) == _Mask[level] ? 2 : 0) | ((ArgbHelper.GetBlue(color) & _Mask[level]) == _Mask[level] ? 1 : 0);
        }

        /// <summary>
        ///     Sets a palette index to this node.
        /// </summary>
        /// <param name="index">The palette index.</param>
        internal void SetPaletteIndex(int index)
        {
            _paletteIndex = index;
        }

        #endregion | Helper methods |
    }
}