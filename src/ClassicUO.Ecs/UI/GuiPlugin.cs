using System;
using System.Collections.Generic;
using ClassicUO.Input;
using ClassicUO.Renderer;
using Clay;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;

namespace ClassicUO.Ecs;

internal readonly struct GuiPlugin : IPlugin
{
    public void Build(App app)
    {
        // Bevy.UI must already be installed by this plugin: it owns the layout
        // resources/stages and our render plugin reads from UiRenderCommands.
        app.AddPlugin(new UiPlugin
        {
            // UO bitmap font measurer. Reads dimensions from FontsLoader at
            // measure time so Clay's layout pass + the eventual DrawText
            // both agree on glyph metrics. UoFontRuntime.Fonts is null until
            // Stage.Startup runs UoFontPlugin's wiring system — measurer
            // returns a 0xfontSize fallback in that window.
            TextMeasurer = new UoFontTextMeasurer(),
            LogicalSize = new System.Numerics.Vector2(1280, 720),
            MaxElements = 8192,
        });

        app.AddPlugin<UoFontPlugin>();
        // Widget plugins from Bevy.UI. Scrollbar/Slider handle thumb dragging
        // + track-click jumps; Checkbox flips Checkbox.Checked on UiClick and
        // fires CheckboxChanged. The visible UO sprite for buttons/checkboxes
        // is swapped post-layout by UpdateUOButtonsState / UpdateUOCheckboxes.
        // Mouse-wheel scrolling on Overflow.Scroll containers is already in
        // Clay's input loop without ScrollbarPlugin — it just unlocks the
        // visible scroll-thumb widget.
        app.AddPlugin<TinyEcs.Bevy.UI.Widgets.ScrollbarPlugin>();
        app.AddPlugin<TinyEcs.Bevy.UI.Widgets.SliderPlugin>();
        app.AddPlugin<TinyEcs.Bevy.UI.Widgets.CheckboxPlugin>();

        // Generic "click the label toggles its checkbox": any clickable entity
        // carrying CheckboxLabel forwards a UiClick to its target checkbox the
        // same way CheckboxPlugin handles a direct hit (flip Checked + emit
        // CheckboxChanged on the checkbox). Lets a checkbox + caption read as a
        // single control everywhere, not per gump.
        app.AddObserver<On<UiClick>, Commands,
            Query<Data<CheckboxLabel>>,
            Query<Data<TinyEcs.Bevy.UI.Widgets.Checkbox>>>((trigger, cmd, labels, boxes) =>
        {
            if (!labels.Contains(trigger.EntityId)) return;
            var (_, lbl) = labels.Get(trigger.EntityId);
            var target = lbl.Ref.Target;
            if (!boxes.Contains(target)) return;
            var (_, cb) = boxes.Get(target);
            cb.Ref.Checked = !cb.Ref.Checked;
            cmd.Entity(target).EmitTrigger(
                new TinyEcs.Bevy.UI.Widgets.CheckboxChanged { Checked = cb.Ref.Checked }, propagate: true);
        });

        app
            .AddResource(new FocusedInput())
            .AddResource(new ImageCache())
            .AddResource(new UIScale())

            .AddSystem(Stage.Startup, (
                Commands commands,
                Res<AssetsServer> assets,
                ResMut<UiClayContext> clay,
                Query<Data<UiCustom>> customQuery,
                Query<Data<UiContainsByBounds>> boundsQuery) =>
            {
                var assetsServer = assets.Value;
                commands.InsertResource(new GumpBuilder(assetsServer));

                // Pixel-perfect hit-testing for every interactive UO gump across
                // all plugins (login, char-select, buttons, paperdoll, etc.).
                // Bevy.UI's InteractionSystem calls this per pointer-over
                // candidate; a transparent sprite pixel returns false so the
                // hover passes through to whatever is behind. Non-UO elements
                // (no UOCustomRender) stay bbox-interactive. Captures the
                // AssetsServer + the cached Query once — no per-call resource
                // fetches and no World traversal.
                // Drag / pickup run their own bbox loops and call
                // UiHitTest.PixelHit directly; this covers the Interaction path.
                clay.Value.PixelHitTest = (entityId, pos, box) =>
                {
                    // Legacy ContainsByBounds: some gump roots (healthbar, status
                    // bar) capture the whole bounding box, not pixel-perfect — so
                    // a click on a transparent bg slot still hits the window
                    // instead of falling through.
                    if (boundsQuery.Contains(entityId)) return true;
                    if (!customQuery.Contains(entityId)) return true;
                    var (_, customPtr) = customQuery.Get(entityId);
                    var bb = new ComputedNode
                    {
                        Position = new System.Numerics.Vector2(box.X, box.Y),
                        Size = new System.Numerics.Vector2(box.Width, box.Height),
                    };
                    return UiHitTest.PixelHit(assetsServer, customPtr.Ref.Render(), in bb,
                        new Microsoft.Xna.Framework.Vector2(pos.X, pos.Y));
                };

                // Debug overlay: outline the topmost element under the pointer
                // lime, occluded elements beneath it red. Flip to false to hide.
                global::Clay.Clay.SetPointerHighlightEnabled(false);
            });

        var states = Enum.GetValues<GameState>();
        foreach (var state in states)
        {
            app.AddSystem((Res<FocusedInput> focusedInput) => focusedInput.Value.Entity = 0)
                .OnExit(state)
                .Build();
        }

        // Sync FNA surface size + pointer state into Bevy.UI before the layout stage.
        Action<Res<GraphicsDevice>, Res<UoGame>, ResMut<UiSurface>> syncSurfaceFn = SyncSurface;
        Action<Res<MouseContext>, Res<UoGame>, ResMut<UiPointer>> syncPointerFn = SyncPointer;
        Action<Res<Time>, Res<MouseContext>, ResMut<UiClayContext>> syncDeltaAndScrollFn = SyncDeltaAndScroll;
        Action<Query<Data<UiCustom, UOButton, Interaction>>> updateUOButtonsStateFn = UpdateUOButtonsState;
        Action<Query<Data<UiCustom, UOCheckbox, TinyEcs.Bevy.UI.Widgets.Checkbox>>> updateUOCheckboxesFn = UpdateUOCheckboxes;
        Action<Query<Data<Text, MaskedText>>> syncMaskedTextFn = SyncMaskedText;
        Action<Res<Time>, Res<FocusedInput>, Query<Data<Text, TextCaret>>> caretBlinkFn = CaretBlink;

        app.AddSystem(Stage.First, syncSurfaceFn);
        app.AddSystem(Stage.First, syncPointerFn);
        app.AddSystem(Stage.First, syncDeltaAndScrollFn);
        // Wheel routing runs in Stage.First (before Stage.Update where
        // CameraPlugin reads WheelConsumed) and after syncDeltaAndScrollFn.
        // It locates the scrollable container under the cursor and scrolls it
        // by mutating its ScrollPosition component (the supported sync path;
        // see RouteWheelToScrollable), then consumes the wheel so it doesn't
        // also zoom the camera.
        // ResMut<MouseContext> because ConsumeWheel mutates the singleton —
        // Res<T> would skip scheduler write-tracking + risk parallel run.
        Action<
            ResMut<MouseContext>,
            ResMut<UiClayContext>,
            Res<AssetsServer>,
            Query<Data<ScrollPosition>>,
            Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>>,
            Query<Data<Node, GlobalZIndex>, Filter<With<UIMovable>>>,
            Query<Data<TinyEcs.Parent>>,
            Query<Data<GlobalZIndex>>,
            Query<Empty, With<GameScreenPlugin.GameWindowUI>>> routeWheelFn = RouteWheelToScrollable;
        app.AddSystem(Stage.First, routeWheelFn);
        // Must run after InteractionSystem.PostLayout writes Hovered/Pressed,
        // before UiRenderStage reads UOCustomRender.AssetId.
        app.AddSystem(updateUOButtonsStateFn)
            .InStage(UiPlugin.UiPostLayoutStage)
            .Build();
        // CheckboxPlugin flips Checkbox.Checked on click; this swaps the visible
        // sprite (Off/On) on the same UOCustomRender the command captured, so the
        // renderer shows the new state this frame with no one-frame lag.
        app.AddSystem(updateUOCheckboxesFn)
            .InStage(UiPlugin.UiPostLayoutStage)
            .Build();
        // Mirror MaskedText.Value into Text as mask chars before layout reads Text.
        app.AddSystem(Stage.PreUpdate, syncMaskedTextFn);
        // Blink the flex caret glyph of bespoke fields (split number box, skills
        // rename) that still use TextCaret. EditableText fields get a measured
        // caret + selection from TextEditPlugin instead.
        app.AddSystem(Stage.PreUpdate, caretBlinkFn);

        // stb_textedit-backed editor for EditableText fields (login, chat,
        // server-gump entries): caret index, selection, arrows, click/drag,
        // Ctrl+A/C/X/V/Z. Bespoke fields opt out by not carrying EditableText.
        app.AddPlugin<TextEditPlugin>();

#if AGENT_BUILD
        // Harness diagnostic: when debug.dumpLayout enables it, every left-click
        // prints the UI element stack under the cursor (topmost first) so the
        // pixel-perfect hit-test can be inspected against the real layout.
        app.AddResource(new DebugLayoutDump());
        Action<
            Res<DebugLayoutDump>,
            Res<MouseContext>,
            Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>>,
            Query<Data<TinyEcs.Parent>>,
            Query<Data<UIMovable>>> dumpFn = DumpLayoutOnClick;
        app.AddSystem(Stage.Last, dumpFn);
#endif
    }

#if AGENT_BUILD
    private static void DumpLayoutOnClick(
        Res<DebugLayoutDump> dump,
        Res<MouseContext> mouse,
        Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>> rendered,
        Query<Data<TinyEcs.Parent>> parents,
        Query<Data<UIMovable>> movables)
    {
        if (!dump.Value.DumpOnClick || !mouse.Value.IsPressedOnce(MouseButtonType.Left))
            return;

        var pt = mouse.Value.Position;
        var rows = new List<(int paint, string line)>();
        foreach (var (ent, comp, node, custom, bg, text) in rendered)
        {
            var bb = comp.Ref;
            if (pt.X < bb.Position.X || pt.Y < bb.Position.Y
                || pt.X >= bb.Position.X + bb.Size.X || pt.Y >= bb.Position.Y + bb.Size.Y)
                continue;

            ulong par = 0;
            if (parents.Contains(ent.Ref)) { var (_, p) = parents.Get(ent.Ref); par = (ulong)p.Ref.Id; }
            string kind = custom.IsValid() ? (custom.Ref.Render()?.Kind.ToString() ?? "nullData") : "noCustom";
            rows.Add((bb.PaintOrder,
                $"  ent={ent.Ref} paint={bb.PaintOrder} pos=({bb.Position.X:0},{bb.Position.Y:0}) " +
                $"size=({bb.Size.X:0},{bb.Size.Y:0}) overflow={node.Ref.Overflow} disp={node.Ref.Display} " +
                $"kind={kind} text={text.IsValid()} bg={bg.IsValid()} mov={movables.Contains(ent.Ref)} parent={par}"));
        }
        rows.Sort((a, b) => b.paint.CompareTo(a.paint)); // topmost (highest paint order) first
        Console.WriteLine($"[Layout] {rows.Count} element(s) under ({pt.X:0},{pt.Y:0}) — topmost first:");
        foreach (var r in rows) Console.WriteLine(r.line);
    }
#endif

    private static void SyncSurface(
        Res<GraphicsDevice> device,
        Res<UoGame> game,
        ResMut<UiSurface> surface)
    {
        var pp = device.Value.PresentationParameters;
        var dpi = game.Value.DpiScale;
        if (dpi <= 0f) dpi = 1f;
        // Clay lays out in LOGICAL pixels (UO's native UI grid). Render pass
        // applies CreateScale(DpiScale) so layout fills the physical backbuffer
        // — mirrors main's RenderTargets pipeline.
        surface.Value.LogicalSize = new System.Numerics.Vector2(pp.BackBufferWidth / dpi, pp.BackBufferHeight / dpi);
        surface.Value.PhysicalSize = new System.Numerics.Vector2(pp.BackBufferWidth, pp.BackBufferHeight);
    }

    private static void SyncPointer(
        Res<MouseContext> mouseCtx,
        Res<UoGame> game,
        ResMut<UiPointer> pointer)
    {
        // MouseContext.Position is already LOGICAL pixels (see MouseContext.cs).
        // Clay layouts in logical too, so feed it through unchanged for both
        // real-mouse and AGENT_BUILD synthetic paths.
        var p = mouseCtx.Value.Position;
        pointer.Value.Position = new System.Numerics.Vector2(p.X, p.Y);
        pointer.Value.Down = mouseCtx.Value.IsPressed(MouseButtonType.Left);
        // WasDown is latched by InteractionSystem.PostLayout.
    }

    private static void SyncDeltaAndScroll(
        Res<Time> time,
        Res<MouseContext> mouseCtx,
        ResMut<UiClayContext> ctx)
    {
        ctx.Value.DeltaTime = time.Value.Frame;
        ctx.Value.ScrollDelta = new System.Numerics.Vector2(0, mouseCtx.Value.Wheel * 3);
        ctx.Value.EnableDragScrolling = false;
    }

    // Route the wheel notch to the scrollable container under the cursor and
    // scroll it; consume the wheel so it doesn't also zoom the camera.
    //
    // Scroll containers (the clip+scroll wrappers built by ServerGumpPlugin's
    // SpawnWrappedText, journal, etc.) paint nothing of their own, so Clay
    // emits no render command for them and InteractionSystem writes them no
    // ComputedNode — an ECS Query<Data<ComputedNode, …>> can't find them. We
    // hit-test them through Clay's own layout bbox (TryGetElementBoundingBox)
    // and drive the scroll by mutating the ScrollPosition component, which
    // LayoutSystem syncs into Clay (the supported, tested path — native wheel
    // scroll via ScrollDelta doesn't reach floating containers here).
    private static void RouteWheelToScrollable(
        ResMut<MouseContext> mouseCtx,
        ResMut<UiClayContext> ctx,
        Res<AssetsServer> assets,
        Query<Data<ScrollPosition>> scrollPosQ,
        Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>> rendered,
        Query<Data<Node, GlobalZIndex>, Filter<With<UIMovable>>> movableQ,
        Query<Data<TinyEcs.Parent>> parentQ,
        Query<Data<GlobalZIndex>> zQ,
        Query<Empty, With<GameScreenPlugin.GameWindowUI>> gameWindowQ)
    {
        var wheel = mouseCtx.Value.Wheel;
        if (Math.Abs(wheel) < 0.001f) return;

        var pos = mouseCtx.Value.Position;

        // Topmost scrollable container under the cursor. Scroll containers
        // paint nothing → no ComputedNode/PaintOrder to rank by, so resolve
        // each candidate's stacking order from its owning window root's
        // GlobalZIndex (only the root carries one — walk the parent chain).
        // Highest z wins so the active (front) gump scrolls, not an
        // overlapping one behind it. Uses last frame's Clay layout (one-frame
        // lag invisible at 60fps).
        ulong topScroll = 0;
        float maxY = 0f;
        int topZ = int.MinValue;
        foreach (var (ent, _) in scrollPosQ)
        {
            var clayId = global::Clay.ElementId.HashNumber((uint)ent.Ref).Id;
            var data = ctx.Value.GetScrollContainerData(clayId);
            if (!data.Found || data.MaxScrollY <= 0f) continue;
            if (!ctx.Value.TryGetElementBoundingBox(ent.Ref, out var bb)) continue;
            if (pos.X < bb.X || pos.X > bb.X + bb.Width) continue;
            if (pos.Y < bb.Y || pos.Y > bb.Y + bb.Height) continue;
            var z = ResolveWindowZ(ent.Ref, parentQ, zQ);
            if (z < topZ) continue;
            topZ = z;
            topScroll = ent.Ref;
            maxY = data.MaxScrollY;
        }

        // Is the cursor over ANY UI? UiPick.Topmost scans all rendered elements
        // (window roots AND their opaque children — a paperdoll body, container
        // item, button — sitting over a window's transparent interior), so an
        // opaque child over the window's hollow region still counts as "over a
        // gump". A root-only pixel loop missed those and let the camera zoom.
        // The owning window's GlobalZIndex (via MovableRoot) is the gump z used
        // to arbitrate scroll fall-through below; a non-movable HUD hit blocks
        // zoom too but carries no window z (stays int.MinValue).
        // The game-window panel itself is a fullscreen BackgroundColor node, so
        // UiPick.Topmost reports a hit on it across the whole viewport. That hit
        // is the WORLD view, not a gump — the wheel must reach the camera there.
        // It's drawn first (lowest PaintOrder), so any real gump over it wins
        // Topmost; a hit ON it means the cursor is over bare game world.
        int topGumpZ = int.MinValue;
        var uiHit = UiPick.Topmost(pos, assets.Value, rendered, parentQ);
        bool overUi = uiHit.Found && !gameWindowQ.Contains(uiHit.Entity);
        if (overUi)
        {
            var root = UiPick.MovableRoot(uiHit.Entity, movableQ, parentQ);
            if (root != 0)
            {
                var (_, _, z) = movableQ.Get(root);
                topGumpZ = z.Ref.Value;
            }
        }

        bool overGump = overUi || topScroll != 0;
        if (!overGump) return;

        // Kill Clay's native ScrollDelta (we drive scroll via the component)
        // and mark the wheel consumed so CameraPlugin doesn't also zoom.
        ctx.Value.ScrollDelta = System.Numerics.Vector2.Zero;
        mouseCtx.Value.ConsumeWheel();

        if (topScroll == 0) return;

        // A higher gump covers the cursor — it captures the wheel (consumed
        // above) instead of letting it scroll the gump beneath.
        if (topZ < topGumpZ) return;

        var (_, sp) = scrollPosQ.Get(topScroll);
        // wheel > 0 (up) → OffsetY decreases. ~90px/notch matches Clay's
        // wheel*3 input factor × 30 internal multiplier.
        sp.Ref.OffsetY = Math.Clamp(sp.Ref.OffsetY - wheel * 90f, 0f, maxY);
    }

    // Walk up the parent chain to the nearest GlobalZIndex (carried only by the
    // window root) and return its value — the window's stacking order. Returns
    // int.MinValue when no ancestor carries one (e.g. an orphan scroll area).
    private static int ResolveWindowZ(
        ulong entity,
        Query<Data<TinyEcs.Parent>> parentQ,
        Query<Data<GlobalZIndex>> zQ)
    {
        var cur = entity;
        for (var guard = 0; guard < 64; guard++)
        {
            if (zQ.Contains(cur))
            {
                var (_, z) = zQ.Get(cur);
                return z.Ref.Value;
            }
            if (!parentQ.Contains(cur)) break;
            var (_, parent) = parentQ.Get(cur);
            cur = parent.Ref.Id;
        }
        return int.MinValue;
    }

    private static void UpdateUOButtonsState(
        Query<Data<UiCustom, UOButton, Interaction>> query)
    {
        foreach (var (custom, button, interaction) in query)
        {
            // Mutates the same UOCustomRender instance the Custom command
            // captured (reference), so the renderer sees the new graphic this
            // frame even though this runs post-layout.
            custom.Ref.Render().AssetId = interaction.Ref switch
            {
                Interaction.Pressed => button.Ref.Pressed,
                Interaction.Hovered => button.Ref.Over,
                _ => button.Ref.Normal,
            };
        }
    }

    private static void UpdateUOCheckboxes(
        Query<Data<UiCustom, UOCheckbox, TinyEcs.Bevy.UI.Widgets.Checkbox>> query)
    {
        foreach (var (custom, box, state) in query)
        {
            custom.Ref.Render().AssetId = state.Ref.Checked ? box.Ref.On : box.Ref.Off;
        }
    }

    // No Changed<MaskedText> filter: the producer (login TypeIntoFocused)
    // mutates MaskedText.Value through a random-access query Get, which does
    // not bump the change tick, so a Changed filter would never re-fire and
    // the mask chars would never reach Text. Only password fields carry both
    // Text + MaskedText, so the unconditional scan is effectively free.
    private static void SyncMaskedText(
        Query<Data<Text, MaskedText>> query)
    {
        foreach (var (text, masked) in query)
        {
            var value = masked.Ref.Value ?? string.Empty;
            text.Ref.Value = value.Length == 0
                ? string.Empty
                : new string(masked.Ref.MaskChar, value.Length);
        }
    }

    // Blink the caret glyph for any TextCaret entity: show "_" while its linked
    // input is focused and we're in the on-phase (~530ms), otherwise clear it so
    // it occupies no width. Shared by every text input (login, split, …) so a
    // focused field always shows a cursor — including an empty one, where the
    // caret entity still holds its slot in the input's flex row.
    private static void CaretBlink(
        Res<Time> time,
        Res<FocusedInput> focused,
        Query<Data<Text, TextCaret>> carets)
    {
        var on = (int)(time.Value.Total / 530f) % 2 == 0;
        var focusEnt = focused.Value.Entity;
        foreach (var (_, text, caret) in carets)
            text.Ref.Value = (caret.Ref.Target == focusEnt && on) ? "_" : string.Empty;
    }

    // Builds a focusable, bounds-hittable editable text field on top of an
    // already-spawned frame entity (the caller owns the background: a nine-patch
    // gump, a translucent bar, or nothing). Mirrors the login/split layout: a
    // content row (absolute, flex-row) holds the live glyph + a blinking caret;
    // clicking the frame OR the row focuses the glyph. The returned glyph id is
    // the focus target (FocusedInput.Entity), the entity the global
    // EditFocusedTextField edits, and where Text/MaskedText hold the value — tag
    // it as the caller needs (e.g. ServerGumpTextEntry). `decorate`, if given, is
    // applied to every spawned sub-entity (row, glyph, caret) so callers can add
    // scene/page markers (LoginScene, ServerGumpChild) that gate visibility.
    internal static ulong SpawnTextField(
        Commands commands,
        EntityCommands frame,
        Vector2 contentOffset,
        TextFont font,
        Clay.Color color,
        string initial,
        bool masked,
        System.Action<EntityCommands> decorate = null,
        char maskChar = '*',
        bool multiline = false,
        int wrapWidth = 0)
    {
        // `color` semantics follow the font kind (see UoFontRuntime): an ASCII
        // font (FontId | AsciiFlag) wants a PACKED hue (UoFontRuntime.AsciiHue);
        // a unicode font wants a literal RGB tint (white = no tint). Passing a
        // packed hue to a unicode field reads as RGB and blacks the text out.
        // UINoWindowDrag on every hittable part so a press on the field never
        // latches a window move — that frees the press+drag gesture for caret
        // placement / text selection (a field inside a UIMovable gump would
        // otherwise drag the gump instead of selecting).
        frame.Insert(Interaction.None).Insert<UiContainsByBounds>().Insert<UINoWindowDrag>();

        EntityCommands glyph;
        if (multiline)
        {
            // Multiline editable: the value lives in (and renders through) a
            // WrappedText custom that wraps at wrapWidth — TextEditPlugin reads /
            // writes custom.Render().Text and maps caret/selection through the
            // same wrap. No Bevy Text component (it would render single-line).
            glyph = commands.Spawn()
                .Insert(new Node { Width = Val.Px(wrapWidth), Height = Val.Px(font.Size) })
                .Insert(new UiCustom
                {
                    Data = new UOCustomRender
                    {
                        Kind = UOCustomKind.WrappedText,
                        Hue = Vector3.UnitZ,
                        Text = initial ?? string.Empty,
                        TextFont = (byte)font.FontId,
                        WrapWidth = wrapWidth,
                        IsHtml = true,
                        HtmlStartColor = 0x010101FF, // near-black, opaque (RGBA)
                        HtmlBg = false,
                    },
                })
                .Insert(font)
                .Insert<TextInput>()
                .Insert<EditableText>()
                .Insert<UINoWindowDrag>()
                // WrappedText has no UiHitTest.PixelHit case, so UiPick can't pixel-
                // hit the glyph and falls through to the window bg under the text —
                // a drag-to-select then moves the window instead. Bounds-hit the
                // glyph rect so UiPick returns it (UINoWindowDrag → drag yields to
                // the field's own caret/selection gesture in RouteMouse).
                .Insert<UiContainsByBounds>()
                .Insert(new MultilineText { WrapWidth = wrapWidth })
                .Insert(new TextFieldGeom { Frame = frame.Id, OffsetX = contentOffset.X });
        }
        else
        {
            glyph = commands.Spawn()
                .Insert(new Node { Width = Val.Auto, Height = Val.Auto })
                .Insert(new Text(masked ? string.Empty : (initial ?? string.Empty)))
                .Insert(font)
                .Insert(new TextColor(color))
                .Insert<TextInput>()
                .Insert<EditableText>()
                .Insert<UINoWindowDrag>()
                // Text origin for mouse caret/selection: TextEditPlugin reads the
                // frame's ComputedNode (logical = scaled / DpiScale) + OffsetX.
                .Insert(new TextFieldGeom { Frame = frame.Id, OffsetX = contentOffset.X });
            if (masked)
                glyph.Insert(new MaskedText { Value = initial ?? string.Empty, MaskChar = maskChar });
        }
        decorate?.Invoke(glyph);
        ulong glyphId = glyph.Id;

        // Selection highlight behind the glyphs and a caret bar over them, both
        // absolute and positioned each frame by TextEditPlugin.PositionOverlays
        // (measured from the cursor / selection indices). Added selection-first
        // so it paints under the text; caret last so it paints on top.
        var selection = commands.Spawn()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(0), Top = Val.Px(0),
                Width = Val.Px(0), Height = Val.Px(font.Size),
                Display = Display.None,
            })
            .Insert(new BackgroundColor(new Clay.Color(70, 110, 180, 120)))
            .Insert<UINoWindowDrag>()
            .Insert(new TextEditSelection { Target = glyphId });
        decorate?.Invoke(selection);

        var caret = commands.Spawn()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(0), Top = Val.Px(0),
                Width = Val.Px(2), Height = Val.Px(font.Size),
                Display = Display.None,
            })
            .Insert(new BackgroundColor(color))
            .Insert<UINoWindowDrag>()
            .Insert(new TextEditCaret { Target = glyphId });
        decorate?.Invoke(caret);

        // Single-line: absolute row at the content offset (overlays sit on top).
        // Multiline: a FLOWING row so it (and its growing glyph) is clipped and
        // scrolled by the frame's Overflow.Scroll — an absolute row would escape
        // the scroll clip and spill past the box.
        var rowNode = multiline
            ? new Node { FlexDirection = FlexDirection.Row, AlignItems = AlignItems.Start, Width = Val.Auto, Height = Val.Auto }
            : new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(contentOffset.X),
                Top = Val.Px(contentOffset.Y),
                FlexDirection = FlexDirection.Row,
                AlignItems = AlignItems.Center,
                Width = Val.Auto,
                Height = Val.Auto,
            };
        var row = commands.Spawn()
            .Insert(rowNode)
            .Insert(Interaction.None)
            .Insert<UiContainsByBounds>()
            .Insert<UINoWindowDrag>();
        decorate?.Invoke(row);
        // Press records the click for caret placement (TextEditPlugin applies it
        // after focus syncs); the raw button is consumed by the focus interaction
        // before a plain mouse system would see it.
        row.Observe((On<UiPointerDown> t, ResMut<FocusedInput> focused, ResMut<ActiveTextEdit> edit) =>
        {
            focused.Value.Entity = glyphId;
            edit.Value.PendingClickEntity = glyphId;
            edit.Value.PendingClickX = t.Event.Position.X;
            edit.Value.PendingClickY = t.Event.Position.Y;
        });
        row.AddChild(selection);
        row.AddChild(glyph);
        row.AddChild(caret);

        frame.Observe((On<UiPointerDown> t, ResMut<FocusedInput> focused, ResMut<ActiveTextEdit> edit) =>
        {
            focused.Value.Entity = glyphId;
            edit.Value.PendingClickEntity = glyphId;
            edit.Value.PendingClickX = t.Event.Position.X;
            edit.Value.PendingClickY = t.Event.Position.Y;
        });
        frame.AddChild(row);

        return glyphId;
    }
}

// FontStashSharp-backed text measurer for Bevy.UI / Clay.
internal sealed class FontStashTextMeasurer : ITextMeasurer
{
    private const FontSystemEffect FONT_EFFECT = FontSystemEffect.Stroked;
    private const int FONT_EFFECT_AMOUNT = 1;

    public Dimensions MeasureText(ReadOnlySpan<char> text, ushort fontId, ushort fontSize, ushort letterSpacing, float maxWidth = 0)
    {
        if (text.IsEmpty)
            return new Dimensions(0, fontSize);

        var font = FontCache.GetFont(fontId);
        var dynFont = font.GetFont(fontSize);
        // FontStashSharp's MeasureString doesn't take ReadOnlySpan<char>; pay the
        // allocation here. Layout pass calls this once per text node per frame.
        var size = dynFont.MeasureString(
            text.ToString(),
            characterSpacing: letterSpacing,
            effect: FONT_EFFECT, effectAmount: FONT_EFFECT_AMOUNT);
        return new Dimensions(size.X, size.Y);
    }
}

// UO-specific render marker stored as a component on a UI entity. The render
// system maps Clay's element id -> entity at render time and pulls this struct.
internal enum UOCustomKind : byte
{
    Gump,
    GumpNinePatch,
    GumpTiled,
    Art,
    Land,
    Animation,
    // Minimap radar window: renderer draws the bg gump (AssetId) then the
    // per-frame baked radar+dots texture carried in UOCustomRender.Dynamic.
    MiniMap,
    // Renders nothing, but still emits a Custom command so the element gets a
    // ComputedNode and a solid (bbox) hit-test. Used as an invisible drag/close
    // surface for gump roots that have no background sprite of their own.
    None,
    // Wrapped (and optionally HTML) text block drawn glyph-by-glyph from the
    // shared font atlas at the node bbox. The node is sized at spawn to the
    // measured content so the scroll container clips/scrolls correctly. Text
    // params ride in the UOCustomRender.Text* fields.
    WrappedText,
    // A sub-rectangle of a gump sprite (legacy GumpPicTexture). Src* select the
    // source slice; SliceTiled tiles it across the node box (false = draw the
    // slice once at the box origin). Used by the resizable vendor panels
    // (top / middle-tiled / bottom slices of one tall gump).
    GumpSlice,
    // Dye-window hue palette (legacy ColorPickerBox): a GridRows x GridCols grid
    // of CellW x CellH solid swatches drawn from the precomputed GridColors, with
    // a 2px white selection dot over SelectedIndex. Colors are baked by the owning
    // plugin (it has HuesLoader); the renderer just blits quads.
    HueGrid,
}

// Reference type (NOT an ECS component) — the instance lives in UiCustom.Data
// and is what flows into Clay's Custom render command (cmd.Custom.CustomData).
// Because it's a reference, the post-layout button system can mutate AssetId on
// the same object the command captured, so the renderer reads the current
// graphic with no entity lookup and no one-frame lag. Hit-test / drag / pickup
// read it off UiCustom.Data the same way.
internal sealed class UOCustomRender
{
    public UOCustomKind Kind;
    public uint AssetId;
    public Vector3 Hue;
    // When true the renderer draws the same sprite a second time at +5/+5
    // to mirror legacy ItemGump.Draw's stacked-item visual (Amount > 1 &&
    // ItemData.IsStackable).
    public bool Stacked;
    // For UOCustomKind.Art in a fixed slot box (paperdoll equipment): crop the
    // source to the art's real (non-transparent) bounds and centre that region
    // in the node box, drawing at native size when it fits — mirrors OOP
    // PaperdollGump.ItemGumpFixed (Arts.GetRealArtBounds). Without it the full
    // sprite incl. transparent margins is fit to the box, so the visible art
    // sits off-centre.
    public bool ArtRealBounds;
    // For UOCustomKind.MiniMap: the per-frame baked radar+dots texture. Owned
    // and updated by MiniMapPlugin's bake system; the renderer just draws it
    // over the bg gump. Null until the first bake.
    public Microsoft.Xna.Framework.Graphics.Texture2D Dynamic;

    // For UOCustomKind.WrappedText: the wrapped (optionally HTML) text run plus
    // the parse params. The atlas drawer reproduces the same layout that
    // sized this node at spawn time.
    public string Text;
    public byte TextFont;
    public ushort TextHue;
    public int WrapWidth;
    public bool IsHtml;
    // Render the wrapped run with the UO ASCII gump fonts (FontsLoader.ASCII)
    // instead of the unicode set — matches OOP Label(isunicode: false). The hue
    // is baked per pixel from TextHue.
    public bool TextAscii;
    public uint HtmlStartColor;
    public bool HtmlBg;
    // Center each wrapped line within WrapWidth (legacy tooltip uses TS_CENTER).
    public bool TextCenter;
    // Text-selection highlight range (char indices into Text) for an editable
    // WrappedText field. SelEnd > SelStart draws a per-visual-line highlight rect
    // behind the glyphs — set by TextEditPlugin only on the focused multiline
    // field, -1/-1 otherwise. Multi-row selections render here (one rect per line)
    // because the single Bevy.UI overlay node can only cover one row.
    public int SelStart = -1;
    public int SelEnd = -1;
    // Caret bar for an editable WrappedText field, drawn in-content (so it scrolls
    // and clips with the glyphs when the field is in an Overflow.Scroll box, where
    // an absolute overlay node would escape the clip). Set by TextEditPlugin on the
    // focused multiline field only; CaretOn carries the blink phase.
    public bool CaretOn;
    public float CaretX, CaretY, CaretH;

    // For UOCustomKind.GumpSlice: source sub-rectangle within the gump sprite,
    // and whether to tile it across the node box (vs draw once at the origin).
    public ushort SrcX, SrcY, SrcW, SrcH;
    public bool SliceTiled;

    // For UOCustomKind.HueGrid: the swatch grid geometry + state. GridColors is
    // the per-cell packed RGBA (row-major, length GridRows*GridCols), baked by
    // ColorPickerPlugin from the live graduation; the renderer blits one quad per
    // entry. SelectedIndex is the 2px-dot cell. (GridHues — the actual UO hue
    // values — live on the plugin's ColorPickerState, not here; the renderer only
    // needs colors.) A reference array so a graduation change mutates in place.
    public uint[] GridColors;
    public int GridRows, GridCols, CellW, CellH, SelectedIndex;

    // Tooltip serial for elements that aren't backed by a world/container item
    // but still want the hover tooltip (e.g. a dragged-out spell cast button).
    // TooltipPlugin reads this off the topmost hit element's UOCustomRender and
    // renders the OPL entry pre-seeded under this synthetic serial. 0 = none.
    public uint TooltipSerial;
}

// The UO render payload lives in UiCustom.Data (a reference, so it threads into
// Clay's Custom command and stays live). This pulls it back out.
internal static class UiCustomRenderExtensions
{
    public static UOCustomRender Render(this in UiCustom c) => c.Data as UOCustomRender;
}

// Marker for the UO button widget. UpdateUOButtonsState rewrites the visible
// asset id on the entity's UOCustomRender based on Interaction state.
internal struct UOButton
{
    public ushort Normal, Pressed, Over;
}

// Marker for the UO checkbox widget. Bevy.UI's CheckboxPlugin flips the sibling
// Checkbox.Checked on click; UpdateUOCheckboxes rewrites the visible asset id
// (Off/On gump) on the entity's UOCustomRender based on that state.
internal struct UOCheckbox
{
    public ushort Off, On;
}

// Marker tags carried over from the old plugin.
internal struct UIMovable;
// On every editable field's glyph entity: the I-beam cursor (GameCursorPlugin)
// and "a field is focused, chat back off" both key off this. Present on ALL
// fields including ones with bespoke editors (split-stack number box, skills
// group rename).
internal struct TextInput;
// Opt-in: the shared global editor (GuiPlugin.EditFocusedTextField) appends /
// backspaces typed chars into THIS entity's Text (or MaskedText) when it holds
// keyboard focus. Login, chat and server-gump text entries carry it; fields
// with their own char readers (split number box, skills rename) deliberately
// do NOT, so the global editor leaves them to their bespoke filtering.
internal struct EditableText;

// Opt-in on an editable glyph: it's a MULTILINE field — the value renders through
// a WrappedText custom (not Bevy Text), the stb editor runs non-single-line so
// Enter inserts '\n', and the caret/selection map through the WrapWidth wrap.
internal struct MultilineText { public int WrapWidth; }

// A caret glyph entity linked to a focusable text input (Target). CaretBlink
// fills/clears its Text each frame based on focus + blink phase. Place it as a
// flex-row sibling after the input's Text so it follows the typed text.
internal struct TextCaret { public ulong Target; }

// Marks a clickable entity (e.g. a checkbox's caption) as a proxy for a
// checkbox (Target). GuiPlugin's global UiClick observer toggles that checkbox
// when this entity is clicked. The entity must be hittable itself
// (Interaction + UiContainsByBounds / a sprite).
internal struct CheckboxLabel { public ulong Target; }

#if AGENT_BUILD
// Harness diagnostic toggle: when DumpOnClick is set (via debug.dumpLayout),
// GuiPlugin.DumpLayoutOnClick prints the UI element stack under each left-click.
internal sealed class DebugLayoutDump
{
    public bool DumpOnClick;
}
#endif

// Suppresses ONLY the drag gesture on a UIMovable window (UO `nomove` flag).
// The window is still a window: right-click-close, click-capture-to-world,
// pickup/cursor awareness and z-stack all key off UIMovable and keep working —
// WindowDragPlugin.Drag is the one system that yields to this tag.
internal struct UINoDrag;

// Real text storage for masked fields (passwords). SyncMaskedText keeps
// the sibling `Text` component populated with `MaskChar` repeated for the
// length of `Value`. Editors and consumers (e.g. login) read/write Value;
// the renderer only ever sees the masked Text.
internal struct MaskedText
{
    public string Value;
    public char MaskChar;
}

internal sealed class FocusedInput
{
    public ulong Entity { get; set; }
}

internal sealed class ImageCache : Dictionary<nint, Texture2D>;
