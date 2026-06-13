using System.Collections.Generic;

namespace ClassicUO.Ecs;

// Pure numeric-argument parsing for the server-pushed gump layout DSL
// (packets 0xB0 / 0xDD). Each command is a token list ("button", "10", "20",
// …) produced by TextFileParser; the verb dispatch + entity spawning stay in
// ServerGumpPlugin.BuildGump, but the fixed-position int/ushort extraction —
// the part that runs on raw, server-controlled bytes and is the most likely
// to choke on a malformed packet — lives here so it can be unit-tested without
// an ECS world. Every TryParse mirrors its old inline guard EXACTLY: same
// minimum token count, same int vs ushort widths, same field order.
internal static class ServerGumpCommand
{
    // resizepic x y id w h
    internal readonly record struct ResizePic(int X, int Y, ushort Id, int W, int H);

    internal static bool TryResizePic(IReadOnlyList<string> gp, out ResizePic a)
    {
        a = default;
        if (gp.Count >= 6 &&
            int.TryParse(gp[1], out var x) && int.TryParse(gp[2], out var y) &&
            ushort.TryParse(gp[3], out var id) &&
            int.TryParse(gp[4], out var w) && int.TryParse(gp[5], out var h))
        {
            a = new ResizePic(x, y, id, w, h);
            return true;
        }
        return false;
    }

    // gumppic / tilepicasgumppic / gumppichued / gumppicphued: x y id [...].
    // The hue source differs per verb (named hue= scan vs positional ushort)
    // so it stays in the caller; this only lifts the x y id core.
    internal readonly record struct PicAt(int X, int Y, ushort Id);

    // gumppic / tilepicasgumppic — count >= 4.
    internal static bool TryGumpPic(IReadOnlyList<string> gp, out PicAt a)
        => TryPicAt(gp, 4, out a);

    // gumppichued / gumppicphued — count >= 5 (positional hue at gp[4]).
    internal static bool TryGumpPicHued(IReadOnlyList<string> gp, out PicAt a)
        => TryPicAt(gp, 5, out a);

    // tilepic / tilepichue — count >= 4 (optional positional hue at gp[4]).
    internal static bool TryTilePic(IReadOnlyList<string> gp, out PicAt a)
        => TryPicAt(gp, 4, out a);

    static bool TryPicAt(IReadOnlyList<string> gp, int min, out PicAt a)
    {
        a = default;
        if (gp.Count >= min &&
            int.TryParse(gp[1], out var x) && int.TryParse(gp[2], out var y) &&
            ushort.TryParse(gp[3], out var id))
        {
            a = new PicAt(x, y, id);
            return true;
        }
        return false;
    }

    // gumppictiled x y w h id
    internal readonly record struct GumpPicTiled(int X, int Y, int W, int H, ushort Id);

    internal static bool TryGumpPicTiled(IReadOnlyList<string> gp, out GumpPicTiled a)
    {
        a = default;
        if (gp.Count >= 6 &&
            int.TryParse(gp[1], out var x) && int.TryParse(gp[2], out var y) &&
            int.TryParse(gp[3], out var w) && int.TryParse(gp[4], out var h) &&
            ushort.TryParse(gp[5], out var id))
        {
            a = new GumpPicTiled(x, y, w, h, id);
            return true;
        }
        return false;
    }

    // button x y normal pressed [action] [toPage] [btnId].
    // Also the shared core of buttontileart (its tile overlay is parsed inline).
    internal readonly record struct Button(int X, int Y, ushort Normal, ushort Pressed, int Action, int ToPage, int BtnId);

    internal static bool TryButton(IReadOnlyList<string> gp, out Button a)
    {
        a = default;
        if (gp.Count >= 5 &&
            int.TryParse(gp[1], out var x) && int.TryParse(gp[2], out var y) &&
            ushort.TryParse(gp[3], out var normal) && ushort.TryParse(gp[4], out var pressed))
        {
            var action = gp.Count >= 6 ? ServerGumpPlugin.SafeInt(gp[5]) : 0;
            var toPage = gp.Count >= 7 ? ServerGumpPlugin.SafeInt(gp[6]) : 0;
            var btnId = gp.Count >= 8 ? ServerGumpPlugin.SafeInt(gp[7]) : 0;
            a = new Button(x, y, normal, pressed, action, toPage, btnId);
            return true;
        }
        return false;
    }

    // text x y hue lineId
    internal readonly record struct Text(int X, int Y, ushort Hue, int LineId);

    internal static bool TryText(IReadOnlyList<string> gp, out Text a)
    {
        a = default;
        if (gp.Count >= 5 &&
            int.TryParse(gp[1], out var x) && int.TryParse(gp[2], out var y) &&
            ushort.TryParse(gp[3], out var hue) && int.TryParse(gp[4], out var lid))
        {
            a = new Text(x, y, hue, lid);
            return true;
        }
        return false;
    }

    // croppedtext x y w h hue lineId
    internal readonly record struct CroppedText(int X, int Y, int W, int H, ushort Hue, int LineId);

    internal static bool TryCroppedText(IReadOnlyList<string> gp, out CroppedText a)
    {
        a = default;
        if (gp.Count >= 7 &&
            int.TryParse(gp[1], out var x) && int.TryParse(gp[2], out var y) &&
            int.TryParse(gp[3], out var w) && int.TryParse(gp[4], out var h) &&
            ushort.TryParse(gp[5], out var hue) && int.TryParse(gp[6], out var lid))
        {
            a = new CroppedText(x, y, w, h, hue, lid);
            return true;
        }
        return false;
    }

    // htmlgump x y w h lineId [hasBg] [hasScroll] — the coord/flag core shared
    // with xmfhtmlgump*/xmfhtmltok (their cliloc + colour resolution is inline).
    internal readonly record struct HtmlBlock(int X, int Y, int W, int H, bool HasBg, bool HasScroll);

    // htmlgump / xmfhtmlgump / xmfhtmlgumpcolor: flags at gp[6]/gp[7].
    internal static bool TryHtmlBlock(IReadOnlyList<string> gp, out HtmlBlock a)
        => TryHtmlBlockAt(gp, 6, 6, 7, out a);

    // xmfhtmltok: count >= 9, flags at gp[5]/gp[6] (no count guard — required).
    internal static bool TryXmfHtmlTok(IReadOnlyList<string> gp, out HtmlBlock a)
        => TryHtmlBlockAt(gp, 9, 5, 6, out a);

    static bool TryHtmlBlockAt(IReadOnlyList<string> gp, int min, int bgIdx, int scrollIdx, out HtmlBlock a)
    {
        a = default;
        if (gp.Count >= min &&
            int.TryParse(gp[1], out var x) && int.TryParse(gp[2], out var y) &&
            int.TryParse(gp[3], out var w) && int.TryParse(gp[4], out var h))
        {
            bool hasBg = gp.Count > bgIdx && gp[bgIdx] == "1";
            bool hasScroll = gp.Count > scrollIdx && gp[scrollIdx] != "0";
            a = new HtmlBlock(x, y, w, h, hasBg, hasScroll);
            return true;
        }
        return false;
    }

    // checkbox / radio: x y uncheckedId checkedId [initialState]
    internal readonly record struct Check(int X, int Y, ushort UncheckedId, ushort CheckedId, bool Initial);

    internal static bool TryCheck(IReadOnlyList<string> gp, out Check a)
    {
        a = default;
        if (gp.Count >= 5 &&
            int.TryParse(gp[1], out var x) && int.TryParse(gp[2], out var y) &&
            ushort.TryParse(gp[3], out var uncheckedId) && ushort.TryParse(gp[4], out var checkedId))
        {
            var initial = gp.Count >= 6 && ServerGumpPlugin.SafeInt(gp[5]) != 0;
            a = new Check(x, y, uncheckedId, checkedId, initial);
            return true;
        }
        return false;
    }

    // picinpic x y id sx sy sw sh — dest size at gp[6]/gp[7] (crop unsupported).
    internal readonly record struct PicInPic(int X, int Y, ushort Id, int W, int H);

    internal static bool TryPicInPic(IReadOnlyList<string> gp, out PicInPic a)
    {
        a = default;
        if (gp.Count >= 8 &&
            int.TryParse(gp[1], out var x) && int.TryParse(gp[2], out var y) &&
            ushort.TryParse(gp[3], out var id) &&
            int.TryParse(gp[6], out var w) && int.TryParse(gp[7], out var h))
        {
            a = new PicInPic(x, y, id, w, h);
            return true;
        }
        return false;
    }

    // checkertrans x y w h — also the textentry coord core.
    internal readonly record struct Rect(int X, int Y, int W, int H);

    internal static bool TryRect(IReadOnlyList<string> gp, out Rect a)
    {
        a = default;
        if (gp.Count >= 5 &&
            int.TryParse(gp[1], out var x) && int.TryParse(gp[2], out var y) &&
            int.TryParse(gp[3], out var w) && int.TryParse(gp[4], out var h))
        {
            a = new Rect(x, y, w, h);
            return true;
        }
        return false;
    }
}
