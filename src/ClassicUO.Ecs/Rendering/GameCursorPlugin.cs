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
// Drag-hand (index 8), loading (13) and text-input (14) cursors are not wired
// yet — those states aren't tracked in the ECS. They fall back to the neutral
// hand, which is what the cursor showed before this plugin existed.
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
        Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>,
            Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>> rendered,
        Query<Data<TinyEcs.Parent>> parents)
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

        int war = warMode ? 1 : 0;
        var mousePos = mouseCtx.Value.Position;

        // Topmost UI element under the cursor — drives text-input and over-world
        // tests below. Same pixel-perfect hit-test every gump gesture uses.
        var hit = UiPick.Topmost(mousePos, assets.Value, rendered, parents);

        // State -> cursor index, mirroring main's GameCursor.AssignGraphicByState
        // priority: targeting > dragging > text-input > world-direction > neutral
        // hand. (Loading is not tracked in ECS.)
        int index;
        if (targeting.Value.IsTargeting)
        {
            index = 12; // targeting reticle
        }
        else if (dragGate.Value.Mode != ActiveDrag.None)
        {
            index = 8; // drag/grab hand
        }
        else if (hit.Found && IsTextInput(hit.Entity, textInputQ, parents))
        {
            index = 14; // text-input I-beam
        }
        else if (inGame
                 && camera.Value.Bounds.Contains((int)mousePos.X, (int)mousePos.Y)
                 && (!hit.Found || gameWindowQ.Contains(hit.Entity)))
        {
            // Over the game world (no gump on top of the viewport) -> directional
            // hand. The GameWindowUI viewport node itself is a UI hit (it carries
            // BackgroundColor), so a hit on IT still counts as "over world".
            int cx = camera.Value.Bounds.X + (camera.Value.Bounds.Width >> 1);
            int cy = camera.Value.Bounds.Y + (camera.Value.Bounds.Height >> 1);
            index = GameCursor.GetMouseDirection(cx, cy, (int)mousePos.X, (int)mousePos.Y, 1);
        }
        else
        {
            index = 9; // neutral hand
        }

        ushort graphic = s_cursorData[war, index];
        DrawCursor(batch.Value, assets.Value, game.Value, cursorState.Value, gameCtx.Value, mousePos, graphic, warMode, inGame);
    }

    // The cursor hit may land on a child of the text field (its text glyphs or
    // the blinking caret), not the node that carries the TextInput marker —
    // walk up the parent chain like UiPick.MovableRoot does.
    private static bool IsTextInput(
        ulong entity,
        Query<Data<TextInput>> textInputQ,
        Query<Data<TinyEcs.Parent>> parents)
    {
        ulong cur = entity;
        for (int i = 0; i < 32 && cur != 0; i++)
        {
            if (textInputQ.Contains(cur)) return true;
            if (!parents.Contains(cur)) return false;
            var (_, parent) = parents.Get(cur);
            cur = (ulong)parent.Ref.Id;
        }
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
        ref readonly var sprite = ref assets.Arts.GetArt(graphic);
        if (sprite.Texture == null)
            return;

        var (hotX, hotY) = cursorState.Hotspot(assets.Arts, graphic);

        // Trammel-ruleset tint (main: Map != 0 && !war -> hue 0x0033). Use
        // GetHueVector — it subtracts 1 from the hue index for the shader, so a
        // hand-built Vector3(0x0033, ...) is off by one and renders the wrong
        // colour.
        var hue = inGame && gameCtx.Map != 0 && !warMode
            ? ShaderHueTranslator.GetHueVector(0x0033)
            : Vector3.UnitZ;

        // Inset the source by 1px to clip the green/black hotspot+edge markers
        // baked into the cursor art (main does the same with BORDER_SIZE = 1).
        var uv = sprite.UV;
        uv.X += 1;
        uv.Y += 1;
        uv.Width -= 2;
        uv.Height -= 2;

        var dpi = game.DpiScale;
        if (dpi <= 0f) dpi = 1f;
        b.Begin(null, Matrix.CreateScale(dpi));
        // PointClamp always (not Linear at fractional DPI): the cursor art has a
        // marker ring at its outer edge that the 1px source inset removes, but
        // linear sampling would bleed that ring's colour back across the inset
        // boundary — a faint blue/green line on the top/left edges.
        b.SetSampler(SamplerState.PointClamp);

        b.Draw(
            sprite.Texture,
            new Vector2(mousePos.X - hotX, mousePos.Y - hotY),
            uv,
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

// Per-cursor hotspot cache. The green-marker scan touches raw art pixels, so
// it's done once per graphic and memoised. 0x2053..0x2079 is the full cursor
// range; the dictionary stays tiny (<= 32 entries).
internal sealed class GameCursorState
{
    private readonly System.Collections.Generic.Dictionary<ushort, (int X, int Y)> _hotspots = new();

    public (int X, int Y) Hotspot(ClassicUO.Renderer.Arts.Art arts, ushort graphic)
    {
        if (!_hotspots.TryGetValue(graphic, out var hs))
        {
            arts.GetCursorHotspot(graphic, out int hx, out int hy);
            hs = (hx, hy);
            _hotspots[graphic] = hs;
        }

        return hs;
    }
}
