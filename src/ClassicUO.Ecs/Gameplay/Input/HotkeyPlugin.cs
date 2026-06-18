// Configurable hotkeys — the Options "Hotkeys" page. Each Profile.Hotkey binds a
// built-in action to a key/mouse combo + a pressed/released trigger. This plugin
// owns the shared state, dispatches the edge actions, and runs the key-capture
// recorder the Options gump drives. Walk actions are continuous (held key) and
// handled in PlayerMovementPlugin, not here. There is no built-in keyboard
// handling otherwise in ECS — this supersedes the dead Disable*Btn flags.
//
// Gating is "always fire": the ECS chat bar permanently holds keyboard focus, so
// gating on chat content is moot. A bound non-text key (Tab / arrows / F-keys /
// Ctrl-combos) doesn't collide with typing.

using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Input;
using ClassicUO.Network;
using Microsoft.Xna.Framework.Input;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;

namespace ClassicUO.Ecs;

// Which hotkey row the Options gump is currently recording a binding for
// (-1 = idle). Recording stays live (each captured press previews on the row)
// until the user presses OK (commit) or CANCEL/ESC (restore the snapshot). The
// Orig* fields hold the binding as it was when recording began.
internal sealed class HotkeyCapture
{
    public int Index = -1;
    public bool HasPending;
    public int OrigKey, OrigMouse, OrigWheel;
    public bool OrigCtrl, OrigShift, OrigAlt;
}

// Sent-chat-line history for the Ctrl+Q/W recall hotkeys. Index points one past
// the last entry after a send (so the first "prev" recalls the most recent line).
internal sealed class ChatHistory { public readonly List<string> Items = new(); public int Index; }

// Last resolved target (TargetSelf / a normal target click record into this) so
// the LastTarget hotkey can resend it under a fresh cursor.
internal sealed class LastTargetState
{
    public bool Has;
    public uint Serial;
    public ushort Graphic;
    public ushort X, Y;
    public sbyte Z;

    public void Set(uint serial, ushort graphic, ushort x, ushort y, sbyte z)
    {
        Has = true;
        Serial = serial; Graphic = graphic; X = x; Y = y; Z = z;
    }
}

// Last double-clicked world object, for the UseLastObject hotkey.
internal sealed class LastUsedObjectState { public bool Has; public uint Serial; }

// Query bundle for DispatchHotkeys (keeps it under the 16-param budget, like
// WorldAuraInputs).
internal sealed class HotkeyDispatchQueries : CompositeSystemParam
{
    public readonly Query<Data<ServerFlags>, With<Player>> PlayerFlags;
    public readonly Query<Data<Graphic, WorldPosition>, With<Player>> PlayerObj;
    public readonly Query<Data<NetworkSerial, Graphic>, Filter<Without<ContainedInto>, Without<IsMulti>>> World;
    public readonly Query<Data<Text>> Texts;

    public HotkeyDispatchQueries()
    {
        PlayerFlags = Add(new Query<Data<ServerFlags>, With<Player>>());
        PlayerObj = Add(new Query<Data<Graphic, WorldPosition>, With<Player>>());
        World = Add(new Query<Data<NetworkSerial, Graphic>, Filter<Without<ContainedInto>, Without<IsMulti>>>());
        Texts = Add(new Query<Data<Text>>());
    }
}

// Query bundle for DispatchOpenHotkeys (gump-open actions): the player's
// equipment + a serial lookup for the backpack, plus each openable window's
// existence query (so OpenOrFocus can focus instead of re-spawning).
internal sealed class HotkeyOpenQueries : CompositeSystemParam
{
    public readonly Query<Data<EquipmentSlots>, With<Player>> PlayerSlots;
    public readonly Query<Data<NetworkSerial>> Serials;
    public readonly Query<Data<PaperdollWindow>> Paperdolls;
    public readonly Query<Data<JournalWindow>> Journals;
    public readonly Query<Data<MiniMapWindow>> Minimaps;
    public readonly Query<Data<SkillsWindow>> Skills;
    public readonly Query<Data<WorldMapWindow>> WorldMaps;
    public readonly Query<Data<StatusBarWindow>> StatusBars;
    public readonly Query<Data<Node>, Filter<With<BuffGumpUI>>> Buffs;
    public readonly Query<Data<CombatBookWindow>> CombatBooks;
    public readonly Query<Data<OptionsWindow>> OptionsWindows;
    public readonly Query<Data<LogoutGumpWindow>> Logouts;

    public HotkeyOpenQueries()
    {
        PlayerSlots = Add(new Query<Data<EquipmentSlots>, With<Player>>());
        Serials = Add(new Query<Data<NetworkSerial>>());
        Paperdolls = Add(new Query<Data<PaperdollWindow>>());
        Journals = Add(new Query<Data<JournalWindow>>());
        Minimaps = Add(new Query<Data<MiniMapWindow>>());
        Skills = Add(new Query<Data<SkillsWindow>>());
        WorldMaps = Add(new Query<Data<WorldMapWindow>>());
        StatusBars = Add(new Query<Data<StatusBarWindow>>());
        Buffs = Add(new Query<Data<Node>, Filter<With<BuffGumpUI>>>());
        CombatBooks = Add(new Query<Data<CombatBookWindow>>());
        OptionsWindows = Add(new Query<Data<OptionsWindow>>());
        Logouts = Add(new Query<Data<LogoutGumpWindow>>());
    }
}

internal readonly struct HotkeyPlugin : IPlugin
{
    private const ushort CorpseGraphic = 0x2006;

    // Only the X buttons are recordable as buttons — Left/Right/Middle stay free
    // (Left/Right drive the OK/CANCEL clicks; the wheel is captured separately).
    private static readonly MouseButtonType[] s_captureButtons =
    {
        MouseButtonType.XButton1, MouseButtonType.XButton2,
    };

    public void Build(App app)
    {
        app.AddResource(new HotkeyCapture());
        app.AddResource(new ChatHistory());
        app.AddResource(new LastTargetState());
        app.AddResource(new LastUsedObjectState());

        var dispatchFn = DispatchHotkeys;
        app.AddSystem(dispatchFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s, Res<NetClient> net, Res<HotkeyCapture> cap)
                => s.Value.Current == GameState.GameScreen && net.Value.IsConnected && cap.Value.Index < 0)
            .Build();

        var dispatchOpenFn = DispatchOpenHotkeys;
        app.AddSystem(dispatchOpenFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s, Res<NetClient> net, Res<HotkeyCapture> cap)
                => s.Value.Current == GameState.GameScreen && net.Value.IsConnected && cap.Value.Index < 0)
            .Build();

        var captureFn = CaptureHotkey;
        app.AddSystem(captureFn)
            .InStage(Stage.First)
            .RunIf((Res<HotkeyCapture> cap) => cap.Value.Index >= 0)
            .Build();
    }

    private static void DispatchHotkeys(
        ResMut<Profile> profile,
        Res<KeyboardContext> kb,
        Res<MouseContext> mouse,
        Res<NetClient> net,
        Res<GameContext> gameCtx,
        ResMut<TargetingState> targeting,
        ResMut<TargetQueueState> queue,
        ResMut<LastTargetState> lastTarget,
        Res<LastUsedObjectState> lastObj,
        ResMut<ChatHistory> history,
        Res<ChatField> chat,
        Res<Time> time,
        HotkeyDispatchQueries q)
    {
        var hotkeys = profile.Value.Hotkeys;
        if (hotkeys == null) return;

        bool ctrl = Held(kb.Value, Keys.LeftControl, Keys.RightControl);
        bool shift = Held(kb.Value, Keys.LeftShift, Keys.RightShift);
        bool alt = Held(kb.Value, Keys.LeftAlt, Keys.RightAlt);

        foreach (var hk in hotkeys)
        {
            if (hk == null) continue;

            // Walk actions are continuous (held) — PlayerMovementPlugin owns them.
            // Open-gump actions are handled by DispatchOpenHotkeys (its own param
            // budget). This system handles the simple edge actions.
            switch (hk.Action)
            {
                case HotkeyAction.None:
                case HotkeyAction.WalkNorth:
                case HotkeyAction.WalkSouth:
                case HotkeyAction.WalkEast:
                case HotkeyAction.WalkWest:
                case HotkeyAction.OpenBackpack:
                case HotkeyAction.OpenPaperdoll:
                case HotkeyAction.OpenJournal:
                case HotkeyAction.OpenWorldMap:
                case HotkeyAction.OpenMinimap:
                case HotkeyAction.OpenSkills:
                case HotkeyAction.OpenStatus:
                case HotkeyAction.OpenBuffs:
                case HotkeyAction.OpenCombatBook:
                case HotkeyAction.OpenOptions:
                case HotkeyAction.Logout:
                    continue;
            }

            if (!MatchEdge(hk, kb.Value, mouse.Value, ctrl, shift, alt))
                continue;

            // A wheel-bound hotkey owns the tick (else the camera also zooms).
            if (hk.Wheel != 0)
                mouse.Value.ConsumeWheel();

            switch (hk.Action)
            {
                case HotkeyAction.ToggleWar:
                {
                    bool war = false;
                    foreach (var (_, sf) in q.PlayerFlags) { war = (sf.Ref.Value & Flags.WarMode) != 0; break; }
                    net.Value.Send_ChangeWarMode(!war);
                    break;
                }

                case HotkeyAction.ChatHistoryPrev:
                    RecallHistory(history.Value, chat.Value, q.Texts, -1);
                    break;
                case HotkeyAction.ChatHistoryNext:
                    RecallHistory(history.Value, chat.Value, q.Texts, +1);
                    break;

                case HotkeyAction.AllNames:
                    foreach (var (_, serial, graphic) in q.World)
                    {
                        var s = serial.Ref.Value;
                        if (SerialHelper.IsMobile(s) || graphic.Ref.Value == CorpseGraphic)
                            net.Value.Send_ClickRequest(s);
                    }
                    break;

                case HotkeyAction.TargetSelf:
                {
                    var ps = gameCtx.Value.PlayerSerial;
                    if (ps == 0) break;
                    ushort g = 0, px = 0, py = 0; sbyte pz = 0; bool found = false;
                    foreach (var (_, gfx, pos) in q.PlayerObj)
                    {
                        g = gfx.Ref.Value; px = pos.Ref.X; py = pos.Ref.Y; pz = pos.Ref.Z;
                        found = true;
                        break;
                    }
                    if (!found) break;
                    ApplyOrQueue(net.Value, targeting.Value, queue.Value, lastTarget.Value, time.Value.Total, ps, g, px, py, pz);
                    break;
                }

                case HotkeyAction.LastTarget:
                {
                    if (!lastTarget.Value.Has) break;
                    var lt = lastTarget.Value;
                    ApplyOrQueue(net.Value, targeting.Value, queue.Value, lastTarget.Value, time.Value.Total, lt.Serial, lt.Graphic, lt.X, lt.Y, lt.Z);
                    break;
                }

                case HotkeyAction.UseLastObject:
                    if (lastObj.Value.Has)
                        net.Value.Send_DoubleClick(lastObj.Value.Serial);
                    break;

                case HotkeyAction.ToggleAlwaysRun:
                    profile.Value.AlwaysRun = !profile.Value.AlwaysRun;
                    break;

                case HotkeyAction.OpenDoor:
                    net.Value.Send_OpenDoor();
                    break;

                case HotkeyAction.CancelTarget:
                    if (targeting.Value.IsTargeting)
                    {
                        net.Value.Send_TargetCancel(
                            targeting.Value.Mode, targeting.Value.CursorId, targeting.Value.CursorType);
                        targeting.Value.Clear();
                    }
                    break;

                case HotkeyAction.ClearTargetQueue:
                    queue.Value.Clear();
                    break;

                case HotkeyAction.CastSpell:
                    if (hk.Param > 0)
                        net.Value.Send_CastSpell(hk.Param, gameCtx.Value.ClientVersion);
                    break;

                case HotkeyAction.UseSkill:
                    net.Value.Send_UseSkill(hk.Param);
                    break;
            }
        }
    }

    // Gump-open hotkeys (own param budget, separate from DispatchHotkeys). Each
    // reuses the plugin's OpenOrFocus entry point so a second press focuses the
    // existing window instead of stacking a duplicate.
    private static void DispatchOpenHotkeys(
        Res<Profile> profile,
        Res<KeyboardContext> kb,
        Res<MouseContext> mouse,
        Res<NetClient> net,
        Res<GameContext> gameCtx,
        Commands commands,
        Res<GumpBuilder> builder,
        Res<AssetsServer> assets,
        Res<UiZCounter> zCounter,
        ResMut<WorldMapState> worldMap,
        Res<PlayerSkills> skills,
        Res<PlayerBuffs> buffs,
        Res<UiSurface> surface,
        Res<OptionsUiState> optionsState,
        HotkeyOpenQueries q)
    {
        var hotkeys = profile.Value.Hotkeys;
        if (hotkeys == null) return;

        bool ctrl = Held(kb.Value, Keys.LeftControl, Keys.RightControl);
        bool shift = Held(kb.Value, Keys.LeftShift, Keys.RightShift);
        bool alt = Held(kb.Value, Keys.LeftAlt, Keys.RightAlt);

        foreach (var hk in hotkeys)
        {
            if (hk == null) continue;
            switch (hk.Action)
            {
                case HotkeyAction.OpenBackpack:
                case HotkeyAction.OpenPaperdoll:
                case HotkeyAction.OpenJournal:
                case HotkeyAction.OpenWorldMap:
                case HotkeyAction.OpenMinimap:
                case HotkeyAction.OpenSkills:
                case HotkeyAction.OpenStatus:
                case HotkeyAction.OpenBuffs:
                case HotkeyAction.OpenCombatBook:
                case HotkeyAction.OpenOptions:
                case HotkeyAction.Logout:
                    break;
                default:
                    continue;
            }

            if (!MatchEdge(hk, kb.Value, mouse.Value, ctrl, shift, alt))
                continue;
            if (hk.Wheel != 0)
                mouse.Value.ConsumeWheel();

            switch (hk.Action)
            {
                case HotkeyAction.OpenBackpack:
                    foreach (var (_, slots) in q.PlayerSlots)
                    {
                        var bp = slots.Ref[Layer.Backpack];
                        if (bp != 0 && q.Serials.TryGet(bp, out var serialRow))
                        {
                            var (_, ns) = serialRow;
                            net.Value.Send_DoubleClick(ns.Ref.Value);
                        }
                        break;
                    }
                    break;

                case HotkeyAction.OpenPaperdoll:
                {
                    var ps = gameCtx.Value.PlayerSerial;
                    if (ps == 0) break;
                    bool focused = false;
                    foreach (var (ent, w) in q.Paperdolls)
                    {
                        if (w.Ref.Serial != ps) continue;
                        commands.Entity(ent.Ref).Insert(new GlobalZIndex(zCounter.Value.Bump()));
                        focused = true;
                        break;
                    }
                    if (!focused)
                        net.Value.Send_DoubleClick(ps | 0x80000000);
                    break;
                }

                case HotkeyAction.OpenJournal:
                    JournalPlugin.OpenOrFocus(commands, builder.Value, assets.Value, zCounter.Value, q.Journals);
                    break;

                case HotkeyAction.OpenWorldMap:
                    WorldMapGumpPlugin.OpenOrFocus(commands, worldMap.Value, profile.Value, zCounter.Value, q.WorldMaps);
                    break;

                case HotkeyAction.OpenMinimap:
                    MiniMapPlugin.OpenOrFocus(commands, builder.Value, zCounter.Value, q.Minimaps);
                    break;

                case HotkeyAction.OpenSkills:
                    if (gameCtx.Value.PlayerSerial != 0)
                        net.Value.Send_SkillsRequest(gameCtx.Value.PlayerSerial);
                    SkillsGumpPlugin.OpenOrFocus(commands, builder.Value, assets.Value, zCounter.Value, skills.Value, q.Skills);
                    break;

                case HotkeyAction.OpenStatus:
                    if (gameCtx.Value.PlayerSerial != 0)
                        net.Value.Send_StatusRequest(gameCtx.Value.PlayerSerial);
                    StatusBarPlugin.OpenOrFocus(commands, builder.Value, assets.Value, gameCtx.Value, profile.Value, zCounter.Value, q.StatusBars);
                    break;

                case HotkeyAction.OpenBuffs:
                    BuffGumpPlugin.OpenOrFocus(commands, builder.Value, zCounter.Value, buffs.Value, q.Buffs);
                    break;

                case HotkeyAction.OpenCombatBook:
                    CombatBookGumpPlugin.OpenOrFocus(commands, builder.Value, assets.Value, zCounter.Value, gameCtx.Value, q.CombatBooks);
                    break;

                case HotkeyAction.OpenOptions:
                    OptionsGumpPlugin.OpenOrFocus(commands, zCounter.Value, surface.Value, optionsState.Value, q.OptionsWindows);
                    break;

                case HotkeyAction.Logout:
                    LogoutGumpPlugin.OpenOrFocus(commands, builder.Value, assets.Value, zCounter.Value, surface.Value, q.Logouts);
                    break;
            }
        }
    }

    // UOSteam `target`: apply the resolved target to the live object cursor if one
    // is up; otherwise queue it for the next cursor (5s timeout). Records it as the
    // last target on a direct apply.
    private static void ApplyOrQueue(
        NetClient net, TargetingState targeting, TargetQueueState queue, LastTargetState lastTarget,
        float now, uint serial, ushort graphic, ushort x, ushort y, sbyte z)
    {
        if (targeting.IsTargeting && targeting.Mode == 0)
        {
            net.Send_TargetObject(serial, graphic, x, y, z, targeting.CursorId, targeting.CursorType);
            lastTarget.Set(serial, graphic, x, y, z);
            targeting.Clear();
        }
        else
        {
            queue.Set(serial, graphic, x, y, z, now);
        }
    }

    // Recall a sent line into the chat input. dir < 0 = previous (older),
    // dir > 0 = next (newer; clears at the end). Mirrors legacy Ctrl+Q/W.
    private static void RecallHistory(ChatHistory h, ChatField chat, Query<Data<Text>> texts, int dir)
    {
        if (h.Items.Count == 0) return;
        var glyph = chat.Glyph;
        if (glyph == 0 || !texts.TryGet(glyph, out var row)) return;
        var (_, t) = row;

        if (dir < 0)
        {
            if (h.Index > 0) h.Index--;
            t.Ref = new Text(h.Items[h.Index]);
        }
        else if (h.Index < h.Items.Count - 1)
        {
            h.Index++;
            t.Ref = new Text(h.Items[h.Index]);
        }
        else
        {
            // Legacy clears at the end and leaves the index put (so a following
            // Ctrl+Q steps back from there), not reset past the end.
            t.Ref = new Text(string.Empty);
        }
    }

    // True when the binding's key OR mouse edge fired this frame AND the modifier
    // state matches exactly. A binding whose main key IS a modifier (e.g. All
    // Names on Shift) excludes that modifier from the exact match, else it could
    // never fire.
    private static bool MatchEdge(Hotkey hk, KeyboardContext kb, MouseContext m, bool ctrl, bool shift, bool alt)
    {
        bool edge = false;
        if (hk.Key != 0)
        {
            var k = (Keys)hk.Key;
            edge = hk.OnRelease ? kb.IsReleased(k) : kb.IsPressedOnce(k);
        }
        if (!edge && hk.Mouse != 0)
        {
            var b = (MouseButtonType)hk.Mouse;
            edge = hk.OnRelease ? m.IsReleased(b) : m.IsPressedOnce(b);
        }
        // Wheel is a momentary tick — sign match, OnRelease ignored.
        if (!edge && hk.Wheel != 0)
            edge = (hk.Wheel > 0 && m.Wheel > 0) || (hk.Wheel < 0 && m.Wheel < 0);
        if (!edge) return false;

        bool reqCtrl = hk.Ctrl, reqShift = hk.Shift, reqAlt = hk.Alt;
        var bk = (Keys)hk.Key;
        if (bk == Keys.LeftControl || bk == Keys.RightControl) reqCtrl = ctrl;
        if (bk == Keys.LeftShift || bk == Keys.RightShift) reqShift = shift;
        if (bk == Keys.LeftAlt || bk == Keys.RightAlt) reqAlt = alt;
        return reqCtrl == ctrl && reqShift == shift && reqAlt == alt;
    }

    // Live key/mouse/wheel recorder. Each captured press previews on the row
    // (the binding is written live; dispatch is gated off while recording).
    // Recording stays open until the Options gump's OK (commit, with the
    // duplicate check) or CANCEL button — ESC is a CANCEL shortcut here.
    private static void CaptureHotkey(
        ResMut<HotkeyCapture> capture,
        Res<KeyboardContext> kb,
        Res<MouseContext> mouse,
        ResMut<Profile> profile,
        Res<OptionsUiState> options)
    {
        int idx = capture.Value.Index;
        var hotkeys = profile.Value.Hotkeys;
        if (idx < 0 || hotkeys == null || idx >= hotkeys.Count) { capture.Value.Index = -1; return; }

        // ESC = unbind: clear the binding and stop recording (Cancel restores).
        if (kb.Value.IsPressedOnce(Keys.Escape))
        {
            var cleared = hotkeys[idx];
            cleared.Key = 0;
            cleared.Mouse = 0;
            cleared.Wheel = 0;
            cleared.Ctrl = cleared.Shift = cleared.Alt = false;
            capture.Value.Index = -1;
            options.Value.Dirty = true;
            return;
        }

        Keys capturedKey = Keys.None;
        foreach (var k in kb.Value.GetPressedKeys())
        {
            if (!kb.Value.IsPressedOnce(k) || IsModifier(k)) continue;
            capturedKey = k;
            break;
        }
        var capturedMouse = MouseButtonType.None;
        int capturedWheel = 0;
        if (capturedKey == Keys.None)
        {
            if (mouse.Value.Wheel > 0) capturedWheel = 1;
            else if (mouse.Value.Wheel < 0) capturedWheel = -1;
            else
                foreach (var b in s_captureButtons)
                    if (mouse.Value.IsPressedOnce(b)) { capturedMouse = b; break; }
        }
        if (capturedKey == Keys.None && capturedMouse == MouseButtonType.None && capturedWheel == 0)
            return; // nothing this frame — keep the current preview

        var hk = hotkeys[idx];
        hk.Key = capturedKey == Keys.None ? 0 : (int)capturedKey;
        hk.Mouse = (int)capturedMouse;
        hk.Wheel = capturedWheel;
        hk.Ctrl = Held(kb.Value, Keys.LeftControl, Keys.RightControl);
        hk.Shift = Held(kb.Value, Keys.LeftShift, Keys.RightShift);
        hk.Alt = Held(kb.Value, Keys.LeftAlt, Keys.RightAlt);
        capture.Value.HasPending = true;
        options.Value.Dirty = true;
    }

    // Snapshot the binding so CANCEL/ESC/window-close can restore it.
    internal static void BeginCapture(HotkeyCapture cap, Profile profile, int idx)
    {
        if (profile.Hotkeys == null || idx < 0 || idx >= profile.Hotkeys.Count) return;
        var hk = profile.Hotkeys[idx];
        cap.Index = idx;
        cap.HasPending = false;
        cap.OrigKey = hk.Key; cap.OrigMouse = hk.Mouse; cap.OrigWheel = hk.Wheel;
        cap.OrigCtrl = hk.Ctrl; cap.OrigShift = hk.Shift; cap.OrigAlt = hk.Alt;
    }

    internal static void RestoreCapture(HotkeyCapture cap, Profile profile)
    {
        int idx = cap.Index;
        if (profile.Hotkeys == null || idx < 0 || idx >= profile.Hotkeys.Count) return;
        var hk = profile.Hotkeys[idx];
        hk.Key = cap.OrigKey; hk.Mouse = cap.OrigMouse; hk.Wheel = cap.OrigWheel;
        hk.Ctrl = cap.OrigCtrl; hk.Shift = cap.OrigShift; hk.Alt = cap.OrigAlt;
    }

    // A bound combo that matches another binding exactly. Unbound (all-zero)
    // never conflicts, so multiple unbound rows are fine.
    internal static bool IsDuplicate(List<Hotkey> list, int idx)
    {
        if (list == null || idx < 0 || idx >= list.Count) return false;
        var h = list[idx];
        if (h.Key == 0 && h.Mouse == 0 && h.Wheel == 0) return false;
        for (int i = 0; i < list.Count; i++)
        {
            if (i == idx) continue;
            var o = list[i];
            if (o.Key == h.Key && o.Mouse == h.Mouse && o.Wheel == h.Wheel
                && o.Ctrl == h.Ctrl && o.Shift == h.Shift && o.Alt == h.Alt)
                return true;
        }
        return false;
    }

    private static bool Held(KeyboardContext kb, Keys a, Keys b)
        => kb.IsPressed(a) || kb.IsPressed(b) || kb.IsPressedOnce(a) || kb.IsPressedOnce(b);

    private static bool IsModifier(Keys k) =>
        k == Keys.LeftControl || k == Keys.RightControl ||
        k == Keys.LeftShift || k == Keys.RightShift ||
        k == Keys.LeftAlt || k == Keys.RightAlt;
}
