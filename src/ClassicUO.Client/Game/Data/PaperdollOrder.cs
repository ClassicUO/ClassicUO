// SPDX-License-Identifier: BSD-2-Clause

// =============================================================================
//  PaperdollOrder.cs
//  Builds the paperdoll / in-world equipment layer draw-order used by the UO
//  Classic Client.
//
//  No coverage masks / no "hide" pass. Pure painter's algorithm:
//    1. Copy one of THREE base order tables (chosen by body/arms/torso graphic).
//    2. Apply per-graphic reorder rules (swap / move-to / move-after) keyed on
//       specific equipped item graphics.
//    3. Draw each layer's gump in that order — later index = painted on top.
//
//  The tables drive both the paperdoll gump and the in-world views. Base table T2
//  is the default order and matches the old paperdoll _layerOrder; the
//  Cloak->after->Robe rule (graphic 0x380/0x5F3) matches the old
//  _layerOrder_quiver_fix.
// =============================================================================

using System;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Data
{
    internal static class PaperdollOrder
    {
        // --- base tables, 25 layers each (leading Invalid is a sentinel) ---------
        // T1: arms drawn late
        private static readonly Layer[] T1 =
        {
            Layer.Invalid, Layer.Cloak, Layer.Shirt, Layer.Pants, Layer.Shoes, Layer.Legs,
            Layer.Torso, Layer.Tunic, Layer.Ring, Layer.Bracelet, Layer.Face, Layer.Arms,
            Layer.Gloves, Layer.Skirt, Layer.Robe, Layer.Waist, Layer.Necklace, Layer.Hair,
            Layer.Beard, Layer.Earrings, Layer.Helmet, Layer.OneHanded, Layer.TwoHanded,
            Layer.Backpack, Layer.Talisman
        };

        // T2: DEFAULT — arms early (matches the old _layerOrder)
        private static readonly Layer[] T2 =
        {
            Layer.Invalid, Layer.Cloak, Layer.Shirt, Layer.Pants, Layer.Shoes, Layer.Legs,
            Layer.Arms, Layer.Torso, Layer.Tunic, Layer.Ring, Layer.Bracelet, Layer.Face,
            Layer.Gloves, Layer.Skirt, Layer.Robe, Layer.Waist, Layer.Necklace, Layer.Hair,
            Layer.Beard, Layer.Earrings, Layer.Helmet, Layer.OneHanded, Layer.TwoHanded,
            Layer.Backpack, Layer.Talisman
        };

        // T3: torso pulled to front (female-style chest-under)
        private static readonly Layer[] T3 =
        {
            Layer.Invalid, Layer.Cloak, Layer.Torso, Layer.Shirt, Layer.Pants, Layer.Shoes,
            Layer.Legs, Layer.Tunic, Layer.Ring, Layer.Bracelet, Layer.Face, Layer.Arms,
            Layer.Gloves, Layer.Skirt, Layer.Robe, Layer.Waist, Layer.Necklace, Layer.Hair,
            Layer.Beard, Layer.Earrings, Layer.Helmet, Layer.OneHanded, Layer.TwoHanded,
            Layer.Backpack, Layer.Talisman
        };

        public const int N = 0x19; // 25 layers scanned by the reorder helpers

        /// <summary>
        /// Build the layer draw-order into <paramref name="order"/> (must be >= N).
        /// <paramref name="graphic"/> is indexed by layer (0x00..0x18); 0 = nothing
        /// equipped on that layer. <paramref name="altTorsoTable"/> gates the
        /// gargoyle/female torso variant. The result is back-to-front paint order —
        /// later index = painted on top. No heap allocation.
        /// </summary>
        public static void Build(ReadOnlySpan<ushort> graphic, bool altTorsoTable, Span<Layer> order)
        {
            // 1) base table selection --------------------------------------------
            Layer[] table = T2; // default

            uint arms = graphic[(int)Layer.Arms];
            if (arms < 0x3d0)
            {
                if (arms == 0x3cf || arms == 0x210 || arms == 0x3b3) table = T1;
            }
            else if (arms == 0x3dd)
            {
                table = T1;
            }

            uint torso = graphic[(int)Layer.Torso];
            if (torso == 0x21a) table = T1;
            else if (torso - 0x399 < 5 && altTorsoTable) table = T3;

            table.AsSpan(0, N).CopyTo(order);
            Span<Layer> o = order.Slice(0, N);

            // 2) per-graphic reorder rules ---------------------------------------
            // shirt present + pants gfx 0x398 -> move Pants to Shirt's slot
            if (graphic[(int)Layer.Shirt] != 0 && graphic[(int)Layer.Pants] == 0x398)
            {
                MoveTo(o, Layer.Pants, Layer.Shirt);
            }

            uint pants = graphic[(int)Layer.Pants];
            bool skipFinalPantsCheck = false;
            if (pants < 0x201)
            {
                if (pants == 0x200 || pants == 0x1eb || pants == 0x1fa)
                {
                    // ensure Shoes before Pants: if Pants currently precedes Shoes, swap
                    int iShoes = IndexOf(o, Layer.Shoes), iPants = IndexOf(o, Layer.Pants);
                    if (iShoes >= 0 && iPants >= 0 && iPants < iShoes)
                    {
                        o[iShoes] = Layer.Pants;
                        o[iPants] = Layer.Shoes;
                    }
                }
            }
            else if (pants - 0x513u < 2)
            {
                if (graphic[(int)Layer.Shoes] != 0) MoveTo(o, Layer.Pants, Layer.Shoes);
                skipFinalPantsCheck = true; // skip the 0x3e4 check below
            }

            if (!skipFinalPantsCheck && graphic[(int)Layer.Shoes] != 0 && graphic[(int)Layer.Pants] == 0x3e4)
            {
                MoveTo(o, Layer.Pants, Layer.Shoes);
            }

            // tunic gfx 0x238 -> Tunic after Waist; +robe in set -> Robe after Neck
            if (graphic[(int)Layer.Tunic] == 0x238)
            {
                MoveAfter(o, Layer.Tunic, Layer.Waist);
                if (graphic[(int)Layer.Robe] != 0)
                {
                    uint r = graphic[(int)Layer.Robe];
                    if (r == 0x4e8 || r == 0x4e9 || r == 0x4ea || r == 0x4eb ||
                        r == 0x5e2 || r == 0x5e3 || r == 0x5e4 || r == 0x5e5)
                    {
                        MoveTo(o, Layer.Robe, Layer.Necklace);
                    }
                }
            }

            // cloak/quiver gfx 0x380/0x5F3 -> Cloak after Robe (the quiver fix)
            uint cloak = graphic[(int)Layer.Cloak];
            if (cloak == 0x380 || cloak == 0x5f3) MoveAfter(o, Layer.Cloak, Layer.Robe);

            // helmet/neck interplay
            uint helm = graphic[(int)Layer.Helmet];
            if (helm < 0x202)
            {
                uint neck = graphic[(int)Layer.Necklace];
                if ((helm == 0x201 || helm == 0x1a9) &&
                    (neck == 0x1c8 || (neck > 0x1d6 && neck < 0x1d9)))
                {
                    MoveAfter(o, Layer.Necklace, Layer.Helmet);
                    return; // done — helmet covers the neck item
                }
            }
            else if (helm - 0x5e9u < 2 && graphic[(int)Layer.Robe] != 0 && (graphic[(int)Layer.Robe] - 0x5e2u) < 4)
            {
                MoveTo(o, Layer.Robe, Layer.Helmet);
            }
        }

        /// <summary>
        /// Fill <paramref name="gfx"/> (length >= N) with the entity's per-layer item
        /// graphics (0x00..0x18); empty layers are left at 0.
        /// </summary>
        public static void GraphicsFromEntity(Entity entity, Span<ushort> gfx)
        {
            gfx.Slice(0, N).Clear();

            for (int layer = (int)Layer.OneHanded; layer <= (int)Layer.Legs; layer++)
            {
                Item item = entity.FindItemByLayer((Layer)layer);
                if (item != null)
                {
                    gfx[layer] = item.Graphic;
                }
            }
        }

        /// <summary>
        /// Copy a raw N-entry order into <paramref name="dest"/>, dropping the sentinel
        /// and Mount. Backpack is kept only when <paramref name="includeBackpack"/> is
        /// set. Returns the entry count.
        /// </summary>
        public static int Filter(ReadOnlySpan<Layer> order, bool includeBackpack, Span<Layer> dest)
        {
            int c = 0;
            foreach (Layer layer in order)
            {
                if (layer == Layer.Invalid || layer == Layer.Mount) continue;
                if (layer == Layer.Backpack && !includeBackpack) continue;
                dest[c++] = layer;
            }

            return c;
        }

        /// <summary>
        /// One-shot helper for the in-world (animated) views: builds the order for an
        /// entity into <paramref name="dest"/> (length >= N) and repositions the cloak
        /// by facing direction. Returns the entry count. No heap allocation.
        /// </summary>
        public static int BuildInWorld(Entity entity, bool altTorsoTable, byte direction, Span<Layer> dest)
        {
            Span<ushort> gfx = stackalloc ushort[N];
            GraphicsFromEntity(entity, gfx);

            Span<Layer> order = stackalloc Layer[N];
            Build(gfx, altTorsoTable, order);

            int count = Filter(order, includeBackpack: false, dest);
            return ApplyDirectionCloak(dest, count, direction);
        }

        /// <summary>
        /// Reposition Cloak for the in-world views, which the official client keys on
        /// facing direction rather than the paperdoll cloak rules: dir 0 (North/away)
        /// draws the cloak on top, dir 3 (facing viewer) behind, every other dir places
        /// it just below the helmet. Operates in place; returns the (unchanged) count.
        /// </summary>
        public static int ApplyDirectionCloak(Span<Layer> layers, int count, byte dir)
        {
            int idx = -1;
            for (int i = 0; i < count; i++)
            {
                if (layers[i] == Layer.Cloak) { idx = i; break; }
            }

            if (idx < 0) return count;

            // remove cloak
            for (int i = idx; i < count - 1; i++) layers[i] = layers[i + 1];
            count--;

            int insert;
            if (dir == 0)
            {
                insert = count; // painted last = on top
            }
            else if (dir == 3)
            {
                insert = 0; // painted first = behind
            }
            else
            {
                insert = count;
                for (int i = 0; i < count; i++)
                {
                    if (layers[i] == Layer.Helmet) { insert = i; break; }
                }
            }

            // open a slot at 'insert' and drop cloak in
            for (int i = count; i > insert; i--) layers[i] = layers[i - 1];
            layers[insert] = Layer.Cloak;
            return count + 1;
        }

        // --- reorder primitives (exact memmove semantics) -----------------------
        private static int IndexOf(ReadOnlySpan<Layer> a, Layer v)
        {
            for (int i = 0; i < N; i++)
            {
                if (a[i] == v) return i;
            }

            return -1;
        }

        /// Relocate value A so it lands on B's index.
        private static void MoveTo(Span<Layer> a, Layer A, Layer B)
        {
            int i1 = IndexOf(a, A); if (i1 < 0) return;
            int i2 = IndexOf(a, B); if (i2 < 0 || i2 == i1) return;
            if (i2 < i1) { for (int k = i1; k > i2; k--) a[k] = a[k - 1]; a[i2] = A; }
            else { for (int k = i1; k < i2; k++) a[k] = a[k + 1]; a[i2] = A; }
        }

        /// Relocate value A so it lands immediately AFTER B.
        private static void MoveAfter(Span<Layer> a, Layer A, Layer B)
        {
            int i1 = IndexOf(a, A); if (i1 < 0) return;
            int i2 = IndexOf(a, B); if (i2 < 0 || i2 == i1) return;
            if (i2 < i1) { for (int k = i1; k > i2 + 1; k--) a[k] = a[k - 1]; a[i2 + 1] = A; }
            else { for (int k = i1; k < i2; k++) a[k] = a[k + 1]; a[i2] = A; }
        }
    }
}
