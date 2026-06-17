// Software UO art cursor — port of legacy Game/GameCursor.cs (the
// !RunMouseInASeparateThread draw path). The OS cursor is hidden (Boot sets
// MouseVisible = false) and this plugin draws the UO cursor sprite at the
// mouse each frame, picking the graphic from state exactly like main's
// GameCursor.AssignGraphicByState:
//
//   targeting          -> handled by TargetingPlugin (reticle = _cursorData[war,12])
//   over the game world -> directional hand by GetMouseDirection (index 0..7)
//   anywhere else       -> neutral hand (index 9)
//   war mode            -> the 0x2053-row graphics instead of 0x206A-row
//   felucca (Map != 0)  -> normal graphics tinted with hue 0x0033
//
// Drag-hand (index 8) and text-input (14) are wired (DragGate / the TextInput
// marker on editable fields). Loading (13) is not tracked in the ECS yet and
// falls back to the neutral hand.
//
// Render ordering mirrors CursorPlugin/TargetingPlugin: UiRenderStage,
// .After("cuo:gui_rendering"), single-threaded, before Stage.Last (Present).

using System;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;

namespace ClassicUO.Ecs;

internal readonly struct GameCursorPlugin : IPlugin
{
    // main's GameCursor._cursorData. Row 0 = normal, row 1 = war. Row 2
    // (felucca) is identical to row 0 — felucca is expressed as a hue on the
    // normal graphics in the software path, not a separate row — so the table
    // only needs the two graphic rows here.
    private static readonly ushort[,] s_cursorData =
    {
        {
            0x206A, 0x206B, 0x206C, 0x206D, 0x206E, 0x206F, 0x2070, 0x2071,
            0x2072, 0x2073, 0x2074, 0x2075, 0x2076, 0x2077, 0x2078, 0x2079,
        },
        {
            0x2053, 0x2054, 0x2055, 0x2056, 0x2057, 0x2058, 0x2059, 0x205A,
            0x205B, 0x205C, 0x205D, 0x205E, 0x205F, 0x2060, 0x2061, 0x2062,
        },
    };

    public void Build(App app)
    {
        app.AddResource(new GameCursorState());

        var renderFn = RenderGameCursor;

        app
            .AddSystem(renderFn)
            .InStage(UiPlugin.UiRenderStage)
            .SingleThreaded()
            .After("cuo:gui_rendering")
            .Build();
    }

    private static void RenderGameCursor(
        Res<UltimaBatcher2D> batch,
        Res<MouseContext> mouseCtx,
        Res<AssetsServer> assets,
        Res<UoGame> game,
        Res<Camera> camera,
        Res<GameContext> gameCtx,
        Res<State<GameState>> state,
        Res<DragGate> dragGate,
        Res<TargetingState> targeting,
        ResMut<GameCursorState> cursorState,
        Query<Data<ServerFlags>, With<Player>> playerFlagsQ,
        Query<Data<GameScreenPlugin.GameWindowUI>> gameWindowQ,
        Query<Data<TextInput>> textInputQ,
        // Without<UiMovable>: a window root carries Interaction (so it captures
        // clicks) and Clay flags its whole bbox Hovered, but IsTextInput walks
        // the WHOLE subtree — a gump that embeds a text field (trade gold entry)
        // would then show the I-beam over the entire window, not just the field.
        // The genuine field frame is a non-movable child that gets flagged
        // Hovered when the cursor is actually over it, so excluding roots fixes
        // the false positive without missing the real case.
        Query<Data<Interaction>, Filter<Without<UiMovable>>> interactiveQ,
        Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>,
            Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>> rendered,
        Query<Data<TinyEcs.Children>> children)
    {
        bool inGame = state.Value.Current == GameState.GameScreen;

        bool warMode = false;
        if (inGame)
        {
            foreach (var (_, sf) in playerFlagsQ)
            {
                warMode = (sf.Ref.Value & Flags.WarMode) != 0;
                break;
            }
        }

        var mousePos = mouseCtx.Value.Position;

        // Topmost UI element under the cursor — drives the over-world test below.
        // Same pixel-perfect hit-test every gump gesture uses.
        var hit = UiPick.Topmost(mousePos, assets.Value, rendered);

        // MouseOverControl analog for the text-input test. The editable field's
        // frame is a hollow nine-patch (transparent interior) made hittable with
        // UiContainsByBounds — Clay's InteractionSystem honours that and flags the
        // single topmost interactive element Hovered/Pressed each frame, whereas
        // UiPick.Topmost is pixel-perfect and would fall straight through the
        // transparent middle to the window behind. So text-input keys off Hovered,
        // exactly like legacy AssignGraphicByState reads UIManager.MouseOverControl.
        ulong hovered = 0;
        foreach (var (e, inter) in interactiveQ)
        {
            if (inter.Ref == Interaction.Hovered || inter.Ref == Interaction.Pressed)
            {
                hovered = e.Ref;
                break;
            }
        }

        // State -> cursor index, mirroring main's GameCursor.AssignGraphicByState
        // priority: targeting > dragging > text-input > world-direction > neutral
        // hand. (Loading is not tracked in ECS.)
        // Drag-hand latch: a window-drag gesture latches gate.Mode on the press
        // edge (mutex against other drag handlers), but legacy only shows the
        // grab hand once the cursor has actually MOVED from the press origin
        // (UIManager.AttemptDragControl: _isDraggingControl flips only when
        // delta != Point.Zero). Mirror that — hold the neutral/direction cursor
        // on a stationary press, switch to the hand once movement begins, and
        // keep it latched until the gesture ends (DraggingOffset can read zero
        // mid-drag when the mouse pauses).
        bool gateDragging = dragGate.Value.Mode != ActiveDrag.None;
        if (!gateDragging)
            cursorState.Value.DragHandLatched = false;
        else if (mouseCtx.Value.DraggingOffset != Vector2.Zero)
            cursorState.Value.DragHandLatched = true;

        // Over the game world (no gump on top of the viewport) -> directional
        // hand. The GameWindowUI viewport node itself is a UI hit (it carries
        // BackgroundColor), so a hit on IT still counts as "over world".
        bool overWorld = inGame
                      && camera.Value.Bounds.Contains((int)mousePos.X, (int)mousePos.Y)
                      && (!hit.Found || gameWindowQ.Contains(hit.Entity));
        int worldDirection = 9;
        if (overWorld)
        {
            int cx = camera.Value.Bounds.X + (camera.Value.Bounds.Width >> 1);
            int cy = camera.Value.Bounds.Y + (camera.Value.Bounds.Height >> 1);
            worldDirection = GameCursor.GetMouseDirection(cx, cy, (int)mousePos.X, (int)mousePos.Y, 1);
        }

        // House design with a tool armed shows the targeting reticle (legacy
        // SetTargetingMulti puts the cursor in MultiPlacement) without arming the
        // real TargetingState — design placement owns the click, not 0x6C. The
        // flag is set by HouseCustomizationPlugin (the cursor system is already at
        // the 16-param delegate limit, so it can't take the resource directly).
        int index = PickCursorIndex(
            targeting: targeting.Value.IsTargeting || cursorState.Value.DesignReticle,
            dragging: gateDragging && cursorState.Value.DragHandLatched,
            textInput: hovered != 0 && IsTextInput(hovered, textInputQ, children),
            overWorld: overWorld,
            worldDirectionIndex: worldDirection);

        ushort graphic = CursorGraphic(warMode, index);
        DrawCursor(batch.Value, assets.Value, game.Value, cursorState.Value, gameCtx.Value, mousePos, graphic, warMode, inGame);
    }

    // Legacy GameCursor.AssignGraphicByState priority: targeting reticle (12) >
    // drag/grab hand (8) > text-input I-beam (14) > over-world directional hand
    // (0..7) > neutral hand (9). Loading (13) isn't tracked in the ECS. Pure so
    // the precedence is unit-tested without a render context.
    internal static int PickCursorIndex(bool targeting, bool dragging, bool textInput, bool overWorld, int worldDirectionIndex)
    {
        if (targeting) return 12;
        if (dragging) return 8;
        if (textInput) return 14;
        if (overWorld) return worldDirectionIndex;
        return 9;
    }

    // Resolve the cursor art id: war-mode picks the 0x2053-row, peace the
    // 0x206A-row; index is the AssignGraphicByState slot above.
    internal static ushort CursorGraphic(bool warMode, int index)
        => s_cursorData[warMode ? 1 : 0, index];

    // `hovered` is the interactive element Clay flagged this frame — the editable
    // field's hittable frame (the nine-patch input box / split-menu value row that
    // carries UiContainsByBounds). The TextInput marker, though, sits on the
    // text-glyph entity nested under that frame (field -> content row -> glyph),
    // so a field is "text input" when the hovered frame, or anything in its
    // subtree, carries TextInput. Subtrees here are tiny (row + glyph + caret);
    // depth-capped against a malformed child link.
    private static bool IsTextInput(
        ulong entity,
        Query<Data<TextInput>> textInputQ,
        Query<Data<TinyEcs.Children>> children,
        int depth = 0)
    {
        if (textInputQ.Contains(entity)) return true;
        if (depth >= 8 || !children.TryGet(entity, out var childrenRow)) return false;
        var (_, kids) = childrenRow;
        foreach (var cid in kids.Ref)
            if (IsTextInput(cid, textInputQ, children, depth + 1)) return true;
        return false;
    }

    private static void DrawCursor(
        UltimaBatcher2D b,
        AssetsServer assets,
        UoGame game,
        GameCursorState cursorState,
        GameContext gameCtx,
        Vector2 mousePos,
        ushort graphic,
        bool warMode,
        bool inGame)
    {
        var (texture, hotX, hotY) = cursorState.Cursor(assets.Arts, graphic);
        if (texture == null)
            return;

        // Trammel-ruleset tint (main: Map != 0 && !war -> hue 0x0033). Use
        // GetHueVector — it subtracts 1 from the hue index for the shader, so a
        // hand-built Vector3(0x0033, ...) is off by one and renders the wrong
        // colour.
        var hue = inGame && gameCtx.Map != 0 && !warMode
            ? ShaderHueTranslator.GetHueVector(0x0033)
            : Vector3.UnitZ;

        var dpi = game.DpiScale;
        if (dpi <= 0f) dpi = 1f;
        b.Begin(null, Matrix.CreateScale(dpi));
        // PointClamp keeps the art crisp at fractional DPI. The texture is already
        // marker-cleaned (CreateCursorTexture strips the green/black/edge ring),
        // so it's drawn whole — no UV inset, no edge-bleed line to suppress.
        b.SetSampler(SamplerState.PointClamp);

        b.Draw(
            texture,
            new Vector2(mousePos.X - hotX, mousePos.Y - hotY),
            new Rectangle(0, 0, texture.Width, texture.Height),
            hue,
            0f,
            Vector2.Zero,
            1f,
            SpriteEffects.None,
            0f
        );

        b.SetSampler(null);
        b.End();
    }
}

// Per-cursor texture cache. CreateCursorTexture touches raw art pixels and
// allocates a Texture2D, so it's done once per graphic and memoised.
// 0x2053..0x2079 is the full cursor range; the dictionary stays tiny
// (<= 32 entries) and lives for the app lifetime.
internal sealed class GameCursorState
{
    private readonly System.Collections.Generic.Dictionary<ushort, (Texture2D Texture, int X, int Y)> _cursors = new();

    // Latched true once a drag gesture has actually moved the cursor (see
    // RenderGameCursor); gates the grab-hand graphic so a stationary press
    // keeps the neutral/direction cursor, matching legacy UIManager.IsDragging.
    public bool DragHandLatched;

    // Set each frame by HouseCustomizationPlugin: true while design mode has a
    // tool armed, so RenderGameCursor shows the targeting reticle.
    public bool DesignReticle;

    public (Texture2D Texture, int X, int Y) Cursor(ClassicUO.Renderer.Arts.Art arts, ushort graphic)
    {
        if (!_cursors.TryGetValue(graphic, out var c))
        {
            // customHue 0: felucca tint is applied by the shader at draw time,
            // not baked, so one texture per graphic covers trammel + felucca.
            var tex = arts.CreateCursorTexture(graphic, 0, out int hx, out int hy);
            c = (tex, hx, hy);
            _cursors[graphic] = c;
        }

        return c;
    }
}
