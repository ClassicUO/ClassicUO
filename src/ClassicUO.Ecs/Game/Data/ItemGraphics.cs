namespace ClassicUO.Ecs;

// Coin rendering rules, shared by container / world / cursor draw paths so they
// stay in sync (legacy Item.IsCoin + Item.DisplayedGraphic).
internal static class ItemGraphics
{
    // Gold/silver coin base graphics (legacy Item.IsCoin).
    public static bool IsCoin(ushort graphic)
        => graphic == 0x0EEA || graphic == 0x0EED || graphic == 0x0EF0;

    // The sprite actually drawn for a stack of coins: base+1 for 2..5, base+2
    // for >5 (the small/large pile art). Non-coins draw their own graphic.
    public static ushort Displayed(ushort graphic, int amount)
    {
        if (IsCoin(graphic))
        {
            if (amount > 5) return (ushort)(graphic + 2);
            if (amount > 1) return (ushort)(graphic + 1);
        }
        return graphic;
    }

    // Coins show quantity through the pile art, NOT the +5/+5 double-draw — they
    // are excluded from the stacked-sprite path (legacy `!IsCoin`).
    public static bool DrawStacked(ushort graphic, int amount, bool isStackable)
        => isStackable && amount > 1 && !IsCoin(graphic);
}
