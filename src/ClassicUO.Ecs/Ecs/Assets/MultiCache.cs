using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ClassicUO.Assets;
using Microsoft.Xna.Framework;

namespace ClassicUO.Ecs;

internal sealed class MultiCache
{
    private readonly MultiLoader _multiLoader;
    private readonly Dictionary<ushort, MultiDescription> _cache = new();

    public MultiCache(MultiLoader multiLoader)
    {
        _multiLoader = multiLoader;
    }

    public MultiDescription GetMulti(ushort id)
    {
        if (_cache.TryGetValue(id, out var multi))
        {
            return multi;
        }

        var info = _multiLoader.GetMultis(id);

        var bounds = Rectangle.Empty;
        ushort multiId = id;

        var span = CollectionsMarshal.AsSpan(info);

        for (int i = 0; i < span.Length; i++)
        {
            ref var block = ref span[i];

            if (block.X < bounds.X)
                bounds.X = block.X;

            if (block.Y < bounds.Y)
                bounds.Y = block.Y;

            if (block.X > bounds.Width)
                bounds.Width = block.X;

            if (block.Y > bounds.Height)
                bounds.Height = block.Y;

            if (!block.IsVisible && i == 0)
            {
                multiId = block.ID;
            }
        }

        multi = new MultiDescription(multiId, bounds, info);
        _cache.Add(id, multi);
        return multi;
    }
}

internal record struct MultiDescription(
    ushort Id,
    Rectangle Bounds,
    List<MultiInfo> Blocks
);
