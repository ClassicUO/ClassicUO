namespace ClassicUO.Ecs;

/// Gump sprite dimension accessors shared by the gump plugins (was a private
/// GumpW/GumpH pair copy-pasted into 12 files).
internal static class UiGumpHelpers
{
    public static int GumpW(AssetsServer a, ushort id) { ref readonly var g = ref a.Gumps.GetGump(id); return g.UV.Width; }
    public static int GumpH(AssetsServer a, ushort id) { ref readonly var g = ref a.Gumps.GetGump(id); return g.UV.Height; }
}
