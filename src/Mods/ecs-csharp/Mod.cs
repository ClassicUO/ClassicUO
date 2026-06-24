// C# mod (wit-bindgen-dotnet + NativeAOT-LLVM) — proof the .NET guest toolchain
// drives the same tinyecs:modding surface as the Rust/jco mods.
//
// A movable window with a title, a CLICK button and a counter label. Clicking the
// button bumps the counter and rewrites the label text. Exercises: setup +
// add-systems, typed spawn, the clicked/name query path, and insert-typed.
//
// The generated bindings (wit-bindgen-dotnet) put the world's exports on a partial
// `Wit.Mod.Csharp.CsharpMod`; the shared tinyecs:modding types live under
// `Wit.Tinyecs.Modding.App` (aliased `AppNs`). Variants are constructed via Create*
// factories; records are plain structs (object-initializer, no ctor); resource
// handles are classes with instance methods.

using System.Text.Json;
using AppNs = Wit.Tinyecs.Modding.App;

namespace Wit.Mod.Csharp;

public static partial class CsharpMod
{
    const float WinW = 240f, WinH = 120f;
    const ushort UiFont = 1; // unicode UI font (RGB-tinted), like the Rust mods

    // Guest instance is single-threaded — plain statics, no locks.
    static bool _spawned;
    static int _count;

    public static partial void Setup(AppNs.AppResource app)
    {
        var init = new AppNs.System("mod-csharp-init");
        init.AddCommands();
        app.AddSystems(AppNs.Schedule.CreateUpdate(), [init]);

        var tick = new AppNs.System("mod-csharp-tick");
        tick.AddCommands();
        // clicked: only entities tagged this frame; name read at index 0.
        tick.AddQuery([
            AppNs.QueryFor.CreateWith("cuo:ui/clicked"),
            AppNs.QueryFor.CreateRef("cuo:ui/name"),
        ]);
        // named: every named entity (to locate the counter label).
        tick.AddQuery([AppNs.QueryFor.CreateRef("cuo:ui/name")]);
        app.AddSystems(AppNs.Schedule.CreateUpdate(), [tick]);

        System.Console.WriteLine("[csharp] setup");
    }

    public static partial void ModCsharpInit(AppNs.Commands commands)
    {
        if (_spawned) return; // one-shot
        _spawned = true;
        BuildWindow(commands);
        System.Console.WriteLine("[csharp] window spawned");
    }

    public static partial void ModCsharpTick(AppNs.Commands commands, AppNs.Query clicked, AppNs.Query named)
    {
        // 1) did our button get clicked this frame? (label + button share the action)
        var hit = false;
        for (var row = clicked.Iter(); row != null; row = clicked.Iter())
        {
            if (NameOf(row.Component(0).Get()).StartsWith("csharp.btn"))
                hit = true;
        }
        if (!hit) return;

        _count++;

        // 2) find the counter label and rewrite its text.
        for (var row = named.Iter(); row != null; row = named.Iter())
        {
            if (NameOf(row.Component(0).Get()) == "csharp.label")
            {
                commands.Entity(row.Entity()).InsertTyped([
                    AppNs.ComponentValue.CreateText(new AppNs.TextRec { Value = $"clicks: {_count}" }),
                ]);
                break;
            }
        }
    }

    // ── window construction ────────────────────────────────────────────────────
    static void BuildWindow(AppNs.Commands commands)
    {
        var root = commands.SpawnTyped([
            AppNs.ComponentValue.CreateNode(AbsNode(40, 140, WinW, WinH)),
            AppNs.ComponentValue.CreateBgColor(Rgba(18, 18, 26, 235)),
            AppNs.ComponentValue.CreateMovable(),
            AppNs.ComponentValue.CreateContainsByBounds(),
            AppNs.ComponentValue.CreateGlobalZ(new AppNs.ZRec { Value = 100 }),
            AppNs.ComponentValue.CreateUiName(new AppNs.NameRec { Value = "csharp.root" }),
        ]);

        var title = commands.SpawnTyped([
            AppNs.ComponentValue.CreateNode(AbsNode(10, 6, WinW - 20, 18)),
            AppNs.ComponentValue.CreateText(new AppNs.TextRec { Value = "C# MOD (wit-bindgen-dotnet)" }),
            AppNs.ComponentValue.CreateTextFont(new AppNs.TextFontRec { FontId = UiFont, Size = 15 }),
            AppNs.ComponentValue.CreateTextColor(Rgba(235, 235, 245, 255)),
        ]);
        root.AddChild(title.Id(), uint.MaxValue);

        // Button + its label. The label is an absolute (Clay-floating) child so it
        // captures the pointer; give it its own Interaction + NoWindowDrag and route
        // its name to the same action (mirrors the Rust mods).
        var btn = commands.SpawnTyped([
            AppNs.ComponentValue.CreateNode(AbsNode(10, 36, 120, 24)),
            AppNs.ComponentValue.CreateBgColor(Rgba(58, 58, 92, 255)),
            AppNs.ComponentValue.CreateInteraction(0),
            AppNs.ComponentValue.CreateNoWindowDrag(),
            AppNs.ComponentValue.CreateUiName(new AppNs.NameRec { Value = "csharp.btn" }),
        ]);
        var btnLbl = commands.SpawnTyped([
            AppNs.ComponentValue.CreateNode(AbsNode(12, 4, 100, 16)),
            AppNs.ComponentValue.CreateText(new AppNs.TextRec { Value = "CLICK ME" }),
            AppNs.ComponentValue.CreateTextFont(new AppNs.TextFontRec { FontId = UiFont, Size = 13 }),
            AppNs.ComponentValue.CreateTextColor(Rgba(235, 235, 245, 255)),
            AppNs.ComponentValue.CreateInteraction(0),
            AppNs.ComponentValue.CreateNoWindowDrag(),
            AppNs.ComponentValue.CreateUiName(new AppNs.NameRec { Value = "csharp.btn.label" }),
        ]);
        btn.AddChild(btnLbl.Id(), uint.MaxValue);
        root.AddChild(btn.Id(), uint.MaxValue);

        var label = commands.SpawnTyped([
            AppNs.ComponentValue.CreateNode(AbsNode(10, 72, WinW - 20, 18)),
            AppNs.ComponentValue.CreateText(new AppNs.TextRec { Value = "clicks: 0" }),
            AppNs.ComponentValue.CreateTextFont(new AppNs.TextFontRec { FontId = UiFont, Size = 13 }),
            AppNs.ComponentValue.CreateTextColor(Rgba(120, 230, 140, 255)),
            AppNs.ComponentValue.CreateUiName(new AppNs.NameRec { Value = "csharp.label" }),
        ]);
        root.AddChild(label.Id(), uint.MaxValue);
    }

    // ── helpers ─────────────────────────────────────────────────────────────────
    static AppNs.Val Auto() => new() { Kind = 0, Value = 0f };
    static AppNs.Val Px(float v) => new() { Kind = 1, Value = v };
    static AppNs.UiRect ZeroRect() => new() { Left = Px(0), Right = Px(0), Top = Px(0), Bottom = Px(0) };
    static AppNs.Color Rgba(float r, float g, float b, float a) => new() { R = r, G = g, B = b, A = a };

    static AppNs.NodeRec BaseNode() => new()
    {
        Display = AppNs.Display.Flex,
        PositionType = AppNs.PositionType.Relative,
        Overflow = AppNs.Overflow.Visible,
        FlexDirection = AppNs.FlexDirection.Row,
        JustifyContent = AppNs.JustifyContent.Start,
        AlignItems = AppNs.AlignItems.Start,
        Width = Auto(), Height = Auto(),
        MinWidth = Auto(), MinHeight = Auto(), MaxWidth = Auto(), MaxHeight = Auto(),
        Left = Auto(), Top = Auto(), Right = Auto(), Bottom = Auto(),
        Padding = ZeroRect(), Border = ZeroRect(),
        Gap = Px(0), AspectRatio = 0f,
    };

    static AppNs.NodeRec AbsNode(float left, float top, float w, float h)
    {
        var n = BaseNode();
        n.PositionType = AppNs.PositionType.Absolute;
        n.Left = Px(left);
        n.Top = Px(top);
        n.Width = Px(w);
        n.Height = Px(h);
        return n;
    }

    static string NameOf(string json)
    {
        // cuo:ui/name serializes to {"Value":"…"}.
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("Value", out var v) ? v.GetString() ?? "" : "";
    }
}
