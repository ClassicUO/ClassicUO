// Host implementation of the cuo:modding `net` interface (the cuo-specific
// imports a mod consumes for UO networking, separate from the generic
// tinyecs:modding `app`). One concrete subclass per WIT resource, sharing the
// per-mod ModHostContext — same pattern as GuestBridge.
//
// send → NetClient (raw passthrough, mod owns the framing). block/unblock →
// ModNetTap.Blocked, which NetworkPlugin.PacketReader honors before dispatch.

using System;
using ClassicUO.Network;
using TinyEcs.Bevy.Modding;
using N = Wit.Cuo.Modding.GuestImports;

namespace ClassicUO.Ecs.Modding;

internal sealed class CuoNetBridge(ModHostContext ctx) : N
{
    public override N.INet NewNet() => new NetImpl(ctx);
    public override N.IUi NewUi() => new UiImpl(ctx);
}

// cuo:modding/ui — asset/font/cliloc helpers + host-window triggers a mod needs
// to faithfully reproduce a host gump. Returns are primitives/strings only (no
// record/variant RETURNS — those hit the fork's native-ownership crash). A struct
// (the fork boxes it once when stored in the handle table).
internal struct UiImpl(ModHostContext ctx) : N.IUi
{
    // Gump dimensions packed (width << 16) | height — mirrors SpawnUOGump's size.
    // 0 when assets aren't loaded (headless test host has no AssetsServer).
    public uint GumpSize(ushort id)
    {
        if (!ctx.App.HasResource<AssetsServer>()) return 0;
        ref readonly var info = ref ctx.App.GetResource<AssetsServer>().Gumps.GetGump(id);
        return ((uint)info.UV.Width << 16) | (uint)(ushort)info.UV.Height;
    }

    // Rendered width in px for a TextFont id (bit 0x80 = the UO ASCII set), the
    // same metric the host StatusGump uses to center TS_CENTER columns.
    public uint MeasureText(ushort fontId, string text)
    {
        var fonts = UoFontRuntime.Fonts;
        if (fonts == null || string.IsNullOrEmpty(text)) return 0;
        var (font, ascii) = UoFontRuntime.Resolve(fontId);
        return (uint)(ascii ? fonts.GetWidthASCII(font, text) : fonts.GetWidthUnicode(font, text));
    }

    public string ResolveCliloc(uint id)
        => ctx.App.HasResource<ClassicUO.Assets.UOFileManager>()
            ? ctx.App.GetResource<ClassicUO.Assets.UOFileManager>().Clilocs.GetString((int)id, true) ?? string.Empty
            : string.Empty;

    public void Dispose() { }
}

internal struct NetImpl(ModHostContext ctx) : N.INet
{
    public void Send(ReadOnlySpan<byte> packet)
    {
        // Raw, fully-framed send (mod owns the length field). ignorePlugin: a
        // legacy passthrough flag, a no-op in the ECS NetClient.
        if (ctx.App != null && ctx.App.HasResource<NetClient>())
            ctx.App.GetResource<NetClient>().Send(packet.ToArray(), ignorePlugin: true);
    }

    public void BlockPacket(byte id)
    {
        if (ctx.App != null && ctx.App.HasResource<ModNetTap>())
            ctx.App.GetResource<ModNetTap>().Blocked.Add(id);
    }

    public void UnblockPacket(byte id)
    {
        if (ctx.App != null && ctx.App.HasResource<ModNetTap>())
            ctx.App.GetResource<ModNetTap>().Blocked.Remove(id);
    }

    public void Dispose() { }
}
