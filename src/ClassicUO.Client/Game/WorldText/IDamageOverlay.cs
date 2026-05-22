// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Renderer;

namespace ClassicUO.Game.WorldText
{
    /// <summary>
    /// Floating-damage overlay drawn on top of entities. One responsibility:
    /// accumulate damage numbers per mobile, age them out, and draw them.
    /// </summary>
    internal interface IDamageOverlay
    {
        void AddDamage(uint serial, int damage);
        void Update();
        void Draw(UltimaBatcher2D batcher, float layerDepth);
        void Clear();
    }
}
