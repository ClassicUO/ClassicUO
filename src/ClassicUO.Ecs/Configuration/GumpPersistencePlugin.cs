// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;

namespace ClassicUO.Ecs;

// Legacy GumpType enum (src/ClassicUO.Client/Game/UI/Gumps/GumpType.cs) — the
// integer written as the gumps.xml `type` attribute. Kept verbatim so the file
// is cross-compatible with the legacy client (the source of truth). Only the
// types the ECS client actually has windows for are saved/restored; unknown
// types in a legacy-written file are skipped on restore.
internal enum LegacyGumpType
{
    None, Buff, Container, CounterBar, HealthBar, InfoBar, Journal, MacroButton,
    MiniMap, PaperDoll, SkillMenu, SpellBook, StatusGump, TipNotice, AbilityButton,
    SpellButton, SkillButton, RacialButton, WorldMap, Debug, NetStats, NameOverHeadHandler
}

internal sealed class SavedGump
{
    public int Type;
    public int X, Y;
    public uint Serial;
    public Dictionary<string, string> Attrs = new();
    public bool Handled;
}

// Parsed gumps.xml awaiting restore. RemainingMs is the WALL-CLOCK budget the
// per-type restore systems keep retrying (player-global windows take a frame to
// spawn then a frame to position; the player paperdoll waits for the server's
// 0x88). Wall-clock, not a frame count: a frame count gave high-refresh clients
// a shorter real-time window (240 frames = 1.6s at 144fps vs 4s at 60fps), so
// late-arriving gumps were dropped and "didn't restore" on fast monitors.
internal sealed class GumpRestoreQueue
{
    public readonly List<SavedGump> Gumps = new();
    public float RemainingMs;
}

// Anchorable-window save queries bundled into one system param (the SaveGumps
// param list is already near the 16 cap). Members is the entity->group lookup so
// each anchorable's group id rides into gumps.xml as the `anchor_group` attr.
internal sealed class AnchorSaveQueries : CompositeSystemParam
{
    public readonly Query<Data<Node, SpellCastButton>> Spell;
    public readonly Query<Data<Node, HealthBarWindow>> Health;
    public readonly Query<Data<Node, CombatAbilityIcon, UiCustom>, Filter<With<UiMovable>>> Ability;
    public readonly Query<Data<Node>, Filter<With<RacialFlyingIcon>, With<UiMovable>>> Racial;
    public readonly Query<Data<Node, SkillCastButton>> Skill;
    public readonly Query<Data<AnchorMember>> Members;

    public AnchorSaveQueries()
    {
        Spell = Add(new Query<Data<Node, SpellCastButton>>());
        Health = Add(new Query<Data<Node, HealthBarWindow>>());
        Ability = Add(new Query<Data<Node, CombatAbilityIcon, UiCustom>, Filter<With<UiMovable>>>());
        Racial = Add(new Query<Data<Node>, Filter<With<RacialFlyingIcon>, With<UiMovable>>>());
        Skill = Add(new Query<Data<Node, SkillCastButton>>());
        Members = Add(new Query<Data<AnchorMember>>());
    }
}

// Saves/restores open gump windows to <profile>/gumps.xml, matching the legacy
// format. Save snapshots live window entities (deferred despawns keep them alive
// during OnExit). Restore re-opens player-global windows via their OpenOrFocus
// and re-opens containers/spellbooks by double-clicking the saved serial (the
// server resends contents) with the saved position seeded into the position
// memory — exactly what legacy ReadGumps does (SavePosition + DoubleClickDelayed).
internal readonly struct GumpPersistencePlugin : IPlugin
{
    public void Build(App app)
    {
        app.AddResource(new GumpRestoreQueue());

        // Single save system (profile.json + globalprofile.json + settings.json +
        // gumps.xml) so there's one Loaded check + reset, no race between the
        // profile and gump writes. Runs on logout (OnExit — deferred despawns keep
        // windows alive) and just before Environment.Exit(0) on an in-game close.
        // Saving runs on logout (OnExit — deferred despawns keep windows alive) and
        // just before Environment.Exit(0) on an in-game close. SPLIT into two
        // systems only because one system can't exceed 16 params and the gump
        // snapshot needs many window queries. SaveProfile MUST run before SaveGumps:
        // both gate on ProfileContext.Loaded, and SaveGumps clears it (so a later
        // login-screen flush won't clobber gumps.xml with an empty snapshot) — if
        // SaveGumps ran first it would clear Loaded and SaveProfile would skip.
        // OnExit runs in registration order; the app-exit flush is ordered by label.
        var saveProfileFn = SaveProfile;
        var saveGumpsFn = SaveGumps;
        app.AddSystem(saveProfileFn).OnExit(GameState.GameScreen).Build();
        app.AddSystem(saveGumpsFn).OnExit(GameState.GameScreen).Build();

        var flushProfileFn = SaveProfile;
        var flushGumpsFn = SaveGumps;
        app.AddSystem(flushProfileFn)
           .InStage(Stage.PostUpdate)
           .Label("cuo:save_profile_flush")
           .Before("cuo:app_exit")
           .RunIf(static (Res<UoGame> game) => FnaPlugin.IsAppExiting(game.Value))
           .SingleThreaded()
           .Build();
        app.AddSystem(flushGumpsFn)
           .InStage(Stage.PostUpdate)
           .After("cuo:save_profile_flush")
           .Before("cuo:app_exit")
           .RunIf(static (Res<UoGame> game) => FnaPlugin.IsAppExiting(game.Value))
           .SingleThreaded()
           .Build();

        // Parse gumps.xml once on entering the world (after PersistencePlugin's
        // LoadProfile has set ProfilePath — registered before this plugin).
        var parseFn = ParseGumps;
        app.AddSystem(parseFn).OnEnter(GameState.GameScreen).Build();

        // Per-type restore. Each runs while the queue is live; player-global
        // windows spawn one frame, position the next.
        var paperdollFn = RestorePaperdoll;
        var statusFn = RestoreStatus;
        var buffFn = RestoreBuff;
        var miniFn = RestoreMiniMap;
        var skillsFn = RestoreSkills;
        var journalFn = RestoreJournal;
        var worldmapFn = RestoreWorldMap;
        var containersFn = RestoreContainers;
        var spellButtonsFn = RestoreSpellButtons;
        var skillButtonsFn = RestoreSkillButtons;
        var abilityButtonsFn = RestoreAbilityButtons;
        var racialButtonsFn = RestoreRacialButtons;
        var healthBarsFn = RestoreHealthBars;

        app.AddSystem(paperdollFn).InStage(Stage.Update).RunIf((Res<State<GameState>> s, Res<GumpRestoreQueue> q)
            => s.Value.Current == GameState.GameScreen && q.Value.RemainingMs > 0f).Build();
        app.AddSystem(statusFn).InStage(Stage.Update).RunIf((Res<State<GameState>> s, Res<GumpRestoreQueue> q)
            => s.Value.Current == GameState.GameScreen && q.Value.RemainingMs > 0f).Build();
        app.AddSystem(buffFn).InStage(Stage.Update).RunIf((Res<State<GameState>> s, Res<GumpRestoreQueue> q)
            => s.Value.Current == GameState.GameScreen && q.Value.RemainingMs > 0f).Build();
        app.AddSystem(miniFn).InStage(Stage.Update).RunIf((Res<State<GameState>> s, Res<GumpRestoreQueue> q)
            => s.Value.Current == GameState.GameScreen && q.Value.RemainingMs > 0f).Build();
        app.AddSystem(skillsFn).InStage(Stage.Update).RunIf((Res<State<GameState>> s, Res<GumpRestoreQueue> q)
            => s.Value.Current == GameState.GameScreen && q.Value.RemainingMs > 0f).Build();
        app.AddSystem(journalFn).InStage(Stage.Update).RunIf((Res<State<GameState>> s, Res<GumpRestoreQueue> q)
            => s.Value.Current == GameState.GameScreen && q.Value.RemainingMs > 0f).Build();
        app.AddSystem(worldmapFn).InStage(Stage.Update).RunIf((Res<State<GameState>> s, Res<GumpRestoreQueue> q)
            => s.Value.Current == GameState.GameScreen && q.Value.RemainingMs > 0f).Build();
        app.AddSystem(containersFn).InStage(Stage.Update).RunIf((Res<State<GameState>> s, Res<GumpRestoreQueue> q)
            => s.Value.Current == GameState.GameScreen && q.Value.RemainingMs > 0f).Build();
        app.AddSystem(spellButtonsFn).InStage(Stage.Update).RunIf((Res<State<GameState>> s, Res<GumpRestoreQueue> q)
            => s.Value.Current == GameState.GameScreen && q.Value.RemainingMs > 0f).Build();
        app.AddSystem(skillButtonsFn).InStage(Stage.Update).RunIf((Res<State<GameState>> s, Res<GumpRestoreQueue> q)
            => s.Value.Current == GameState.GameScreen && q.Value.RemainingMs > 0f).Build();
        app.AddSystem(abilityButtonsFn).InStage(Stage.Update).RunIf((Res<State<GameState>> s, Res<GumpRestoreQueue> q)
            => s.Value.Current == GameState.GameScreen && q.Value.RemainingMs > 0f).Build();
        app.AddSystem(racialButtonsFn).InStage(Stage.Update).RunIf((Res<State<GameState>> s, Res<GumpRestoreQueue> q)
            => s.Value.Current == GameState.GameScreen && q.Value.RemainingMs > 0f).Build();
        app.AddSystem(healthBarsFn).InStage(Stage.Update).RunIf((Res<State<GameState>> s, Res<GumpRestoreQueue> q)
            => s.Value.Current == GameState.GameScreen && q.Value.RemainingMs > 0f).Build();

        // Countdown / cleanup (after the restore systems each frame).
        var tickFn = RestoreTick;
        app.AddSystem(tickFn)
           .InStage(Stage.Last)
           .RunIf(static (Res<GumpRestoreQueue> q) => q.Value.RemainingMs > 0f)
           .Build();
    }


    // ---- save ---------------------------------------------------------------

    private static void SaveProfile(
        ResMut<ProfileContext> ctx,
        ResMut<Profile> profile,
        Res<GlobalProfile> global,
        Res<Settings> settings,
        Res<Camera> camera)
    {
        // Settings persist on every save (last server, account, etc.).
        settings.Value.Save();

        if (!ctx.Value.Loaded || string.IsNullOrEmpty(ctx.Value.ProfilePath))
            return;

        // The resizable game window lives in camera.Bounds (GameScreenPlugin's
        // drag/resize own it; profile is only read at InitCamera). Snapshot it
        // back into the profile so a resized/moved viewport persists. Skip in
        // full-size mode — its bounds are the whole backbuffer, not a saved size.
        if (!profile.Value.GameWindowFullSize)
        {
            var b = camera.Value.Bounds;
            if (b.Width > 0 && b.Height > 0)
            {
                profile.Value.GameWindowPosition = new Point(b.X, b.Y);
                profile.Value.GameWindowSize = new Point(b.Width, b.Height);
            }
        }

        ProfileManager.Save(profile.Value, global.Value, ctx.Value.ProfilePath);
    }

    // Runs AFTER SaveProfile (see Build): both gate on Loaded; this one clears it
    // last so the gump snapshot is written before the gate closes.
    private static void SaveGumps(
        ResMut<ProfileContext> ctx,
        Query<Data<Node, PaperdollWindow>> paperdollQ,
        Query<Data<Node>, Filter<With<StatusBarWindow>>> statusQ,
        Query<Data<Node, BuffBarLayout>, Filter<With<BuffGumpUI>>> buffQ,
        Query<Data<Node, MiniMapWindow>> miniQ,
        Query<Data<Node, SkillsWindow>> skillsQ,
        Query<Data<Node, JournalWindow>> journalQ,
        Query<Data<Node>, Filter<With<WorldMapWindow>>> worldmapQ,
        Query<Data<Node, ContainerWindow, ContainerGumpTag>> containerQ,
        Query<Data<Node, GridContainerWindow, ContainerGumpTag>> gridQ,
        Query<Data<Node, SpellbookWindow>> spellbookQ,
        AnchorSaveQueries anchorSave)
    {
        // Append the entity's anchor group id (if any) so the restored windows
        // re-form their group. GroupId is a runtime counter — its absolute value
        // is irrelevant, only that co-grouped windows share it.
        void AddAnchorGroup(List<(string, string)> attrs, ulong ent)
        {
            if (anchorSave.Members.TryGet(ent, out var r))
            {
                var (_, m) = r;
                if (m.Ref.GroupId > 0) attrs.Add(("anchor_group", m.Ref.GroupId.ToString()));
            }
        }
        if (!ctx.Value.Loaded || string.IsNullOrEmpty(ctx.Value.ProfilePath))
            return;

        // Reset so a later flush (e.g. closing at the login screen after logout)
        // doesn't re-save or clobber gumps.xml with an empty snapshot.
        ctx.Value.Loaded = false;

        var path = Path.Combine(ctx.Value.ProfilePath, "gumps.xml");

        try
        {
            using var xml = new XmlTextWriter(path, Encoding.UTF8)
            {
                Formatting = Formatting.Indented,
                IndentChar = '\t',
                Indentation = 1
            };

            xml.WriteStartDocument(true);
            xml.WriteStartElement("gumps");

            // Legacy only restores the player's own paperdoll (others Dispose()).
            foreach (var (_, node, pd) in paperdollQ)
                if (pd.Ref.IsPlayer)
                    WriteGump(xml, LegacyGumpType.PaperDoll, node, pd.Ref.Serial, ("isminimized", "False"));

            foreach (var (_, node) in statusQ)
                WriteGump(xml, LegacyGumpType.StatusGump, node, 0);

            // direction is graphic - 0x757F (LeftVertical..RightHorizontal = 0..3,
            // identical to legacy GumpDirection) — legacy reads BOTH attrs and a
            // mismatched pair renders broken there, so write the real value.
            foreach (var (_, node, buff) in buffQ)
                WriteGump(xml, LegacyGumpType.Buff, node, 0,
                    ("graphic", buff.Ref.Graphic.ToString()),
                    ("direction", (buff.Ref.Graphic - 0x757F).ToString()));

            foreach (var (_, node, mini) in miniQ)
                WriteGump(xml, LegacyGumpType.MiniMap, node, 0,
                    ("isminimized", mini.Ref.UseLargeMap.ToString()));

            foreach (var (_, node, sk) in skillsQ)
                WriteGump(xml, LegacyGumpType.SkillMenu, node, 0,
                    ("isminimized", sk.Ref.Minimized.ToString()), ("height", sk.Ref.SpecialHeight.ToString()));

            foreach (var (_, node, jr) in journalQ)
                WriteGump(xml, LegacyGumpType.Journal, node, 0,
                    ("height", jr.Ref.SpecialHeight.ToString()), ("isminimized", jr.Ref.Minimized.ToString()));

            foreach (var (_, node) in worldmapQ)
                WriteGump(xml, LegacyGumpType.WorldMap, node, 0);

            foreach (var (_, node, c, tag) in containerQ)
                WriteGump(xml, LegacyGumpType.Container, node, c.Ref.Serial,
                    ("graphic", tag.Ref.OriginalGraphic.ToString()), ("isminimized", tag.Ref.IsMinimized.ToString()));

            foreach (var (_, node, g, tag) in gridQ)
                WriteGump(xml, LegacyGumpType.Container, node, g.Ref.Serial,
                    ("graphic", tag.Ref.OriginalGraphic.ToString()), ("isminimized", "False"));

            foreach (var (_, node, sb) in spellbookQ)
                WriteGump(xml, LegacyGumpType.SpellBook, node, sb.Ref.Serial, ("isminimized", "False"));

            // Spell icons dragged off the book onto the screen (legacy SpellButton).
            // serial=0; "id" = cast id (== legacy spell.ID), "graphic" = the icon so
            // restore needs no castId->icon reverse lookup.
            foreach (var (ent, node, btn) in anchorSave.Spell)
            {
                var a = new List<(string, string)> { ("id", btn.Ref.CastId.ToString()), ("graphic", btn.Ref.Icon.ToString()) };
                AddAnchorGroup(a, ent.Ref);
                WriteGump(xml, LegacyGumpType.SpellButton, node, 0, a.ToArray());
            }

            // Per-mobile health bars (legacy HealthBar) — serial + position; the
            // bar resolves the mobile each frame and grays out when out of range,
            // so a serial whose mobile isn't loaded yet at login still restores.
            foreach (var (ent, node, hb) in anchorSave.Health)
            {
                var a = new List<(string, string)>();
                AddAnchorGroup(a, ent.Ref);
                WriteGump(xml, LegacyGumpType.HealthBar, node, hb.Ref.Serial, a.ToArray());
            }

            // Floating combat-ability buttons (legacy AbilityButton). UiMovable
            // filter keeps the in-book (docked) icons out. "primary" picks which
            // ability the double-click uses; "graphic" is the save-time icon.
            foreach (var (ent, node, ab, uic) in anchorSave.Ability)
            {
                if (!ab.Ref.Floating) continue;
                var a = new List<(string, string)> { ("primary", ab.Ref.Primary.ToString()), ("graphic", ((UOCustomRender)uic.Ref.Data).AssetId.ToString()) };
                AddAnchorGroup(a, ent.Ref);
                WriteGump(xml, LegacyGumpType.AbilityButton, node, 0, a.ToArray());
            }

            // Floating gargoyle-flight toggle (legacy RacialButton) — fixed graphic,
            // position only. UiMovable filter excludes the docked book icon.
            foreach (var (ent, node) in anchorSave.Racial)
            {
                var a = new List<(string, string)>();
                AddAnchorGroup(a, ent.Ref);
                WriteGump(xml, LegacyGumpType.RacialButton, node, 0, a.ToArray());
            }

            // Floating skill buttons torn off the skills list (legacy SkillButton).
            // "id" = skill index; restore re-resolves the name from assets.
            foreach (var (ent, node, sk) in anchorSave.Skill)
            {
                var a = new List<(string, string)> { ("id", sk.Ref.SkillId.ToString()) };
                AddAnchorGroup(a, ent.Ref);
                WriteGump(xml, LegacyGumpType.SkillButton, node, 0, a.ToArray());
            }

            xml.WriteEndElement();
            xml.WriteEndDocument();
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
        }
    }

    private static void WriteGump(XmlTextWriter xml, LegacyGumpType type, Ptr<Node> node, uint serial, params (string name, string value)[] extra)
    {
        xml.WriteStartElement("gump");
        xml.WriteAttributeString("type", ((int)type).ToString());
        xml.WriteAttributeString("x", ((int)node.Ref.Left.Value).ToString());
        xml.WriteAttributeString("y", ((int)node.Ref.Top.Value).ToString());
        xml.WriteAttributeString("serial", serial.ToString());
        foreach (var (name, value) in extra)
            xml.WriteAttributeString(name, value);
        xml.WriteEndElement();
    }

    // ---- restore ------------------------------------------------------------

    private static void ParseGumps(Res<ProfileContext> ctx, ResMut<GumpRestoreQueue> queue)
    {
        queue.Value.Gumps.Clear();
        queue.Value.RemainingMs = 0f;

        if (!ctx.Value.Loaded || string.IsNullOrEmpty(ctx.Value.ProfilePath))
            return;

        var path = Path.Combine(ctx.Value.ProfilePath, "gumps.xml");
        if (!File.Exists(path))
            return;

        var doc = new XmlDocument();
        try { doc.Load(path); }
        catch (Exception e) { Log.Error(e.ToString()); return; }

        var root = doc["gumps"];
        if (root == null)
            return;

        foreach (XmlElement el in root.ChildNodes)
        {
            if (el.Name != "gump")
                continue;

            try
            {
                var g = new SavedGump
                {
                    Type = int.Parse(el.GetAttribute("type")),
                    X = int.Parse(el.GetAttribute("x")),
                    Y = int.Parse(el.GetAttribute("y")),
                    Serial = uint.Parse(el.GetAttribute("serial")),
                };
                foreach (XmlAttribute a in el.Attributes)
                    g.Attrs[a.Name] = a.Value;
                queue.Value.Gumps.Add(g);
            }
            catch (Exception e) { Log.Error(e.ToString()); }
        }

        // 5s wall-clock: enough for player-global windows to spawn + the server
        // to resend container contents after the staggered double-clicks.
        queue.Value.RemainingMs = 5000f;
    }

    private static void RestoreTick(ResMut<GumpRestoreQueue> queue, Res<Time> time)
    {
        queue.Value.RemainingMs -= time.Value.Frame * 1000f;
        if (queue.Value.RemainingMs <= 0f)
            queue.Value.Gumps.Clear();
    }

    private static SavedGump Next(GumpRestoreQueue queue, LegacyGumpType type)
    {
        foreach (var g in queue.Gumps)
            if (!g.Handled && g.Type == (int)type)
                return g;
        return null;
    }

    private static void Place(Ptr<Node> node, SavedGump g)
    {
        node.Ref.Left = Val.Px(g.X);
        node.Ref.Top = Val.Px(g.Y);
    }

    // The player's own paperdoll auto-opens on login (server 0x88); only reposition.
    private static void RestorePaperdoll(ResMut<GumpRestoreQueue> queue, Query<Data<Node, PaperdollWindow>> q)
    {
        var g = Next(queue.Value, LegacyGumpType.PaperDoll);
        if (g == null) return;
        foreach (var (_, node, pd) in q)
        {
            if (!pd.Ref.IsPlayer) continue;
            Place(node, g);
            g.Handled = true;
            return;
        }
    }

    private static void RestoreStatus(
        ResMut<GumpRestoreQueue> queue,
        Commands commands,
        Res<GumpBuilder> builder,
        Res<AssetsServer> assets,
        Res<GameContext> gameCtx,
        Res<Profile> profile,
        ResMut<UiZCounter> z,
        Query<Data<StatusBarWindow>> tagQ,
        Query<Data<Node, StatusBarWindow>> nodeQ)
    {
        var g = Next(queue.Value, LegacyGumpType.StatusGump);
        if (g == null) return;
        foreach (var (_, node, _) in nodeQ) { Place(node, g); g.Handled = true; return; }
        StatusBarPlugin.OpenOrFocus(commands, builder.Value, assets.Value, gameCtx.Value, profile.Value, z.Value, tagQ);
    }

    private static void RestoreBuff(
        ResMut<GumpRestoreQueue> queue,
        Commands commands,
        Res<GumpBuilder> builder,
        ResMut<UiZCounter> z,
        ResMut<PlayerBuffs> buffs,
        Query<Data<Node>, Filter<With<BuffGumpUI>>> rootQ,
        Query<Data<Node, BuffBarLayout>, Filter<With<BuffGumpUI>>> layoutQ)
    {
        var g = Next(queue.Value, LegacyGumpType.Buff);
        if (g == null) return;
        foreach (var (_, node, lay) in layoutQ)
        {
            Place(node, g);
            var graphic = AttrUShort(g, "graphic");
            // Legacy-written files (and older ECS ones) may carry only direction;
            // derive the graphic from it (0x757F + dir).
            if (graphic == 0)
            {
                int dir = AttrInt(g, "direction");
                if (dir >= 0 && dir <= 3) graphic = (ushort)(0x757F + dir);
            }
            if (graphic != 0) lay.Ref.Graphic = graphic;
            // Force Sync to rebuild the bg / icon row / toggle for the restored
            // direction. Without this the open-time Dirty was already consumed
            // (with the default graphic) before this system ran, so the saved
            // direction never took visual effect.
            buffs.Value.Dirty = true;
            g.Handled = true;
            return;
        }
        BuffGumpPlugin.OpenOrFocus(commands, builder.Value, z.Value, buffs.Value, rootQ);
    }

    private static void RestoreMiniMap(
        ResMut<GumpRestoreQueue> queue,
        Commands commands,
        Res<GumpBuilder> builder,
        ResMut<UiZCounter> z,
        Query<Data<MiniMapWindow>> tagQ,
        Query<Data<Node, MiniMapWindow>> nodeQ)
    {
        var g = Next(queue.Value, LegacyGumpType.MiniMap);
        if (g == null) return;
        foreach (var (_, node, mini) in nodeQ)
        {
            Place(node, g);
            mini.Ref.UseLargeMap = AttrBool(g, "isminimized");
            g.Handled = true;
            return;
        }
        MiniMapPlugin.OpenOrFocus(commands, builder.Value, z.Value, tagQ);
    }

    private static void RestoreSkills(
        ResMut<GumpRestoreQueue> queue,
        Commands commands,
        Res<GumpBuilder> builder,
        Res<AssetsServer> assets,
        ResMut<UiZCounter> z,
        Res<PlayerSkills> skills,
        Query<Data<SkillsWindow>> tagQ,
        Query<Data<Node, SkillsWindow>> nodeQ)
    {
        var g = Next(queue.Value, LegacyGumpType.SkillMenu);
        if (g == null) return;
        foreach (var (_, node, sk) in nodeQ)
        {
            Place(node, g);
            var h = AttrInt(g, "height");
            if (h > 0) sk.Ref.SpecialHeight = h;
            sk.Ref.Minimized = AttrBool(g, "isminimized");
            sk.Ref.Dirty = true;
            g.Handled = true;
            return;
        }
        SkillsGumpPlugin.OpenOrFocus(commands, builder.Value, assets.Value, z.Value, skills.Value, tagQ);
    }

    private static void RestoreJournal(
        ResMut<GumpRestoreQueue> queue,
        Commands commands,
        Res<GumpBuilder> builder,
        Res<AssetsServer> assets,
        ResMut<UiZCounter> z,
        Query<Data<JournalWindow>> tagQ,
        Query<Data<Node, JournalWindow>> nodeQ)
    {
        var g = Next(queue.Value, LegacyGumpType.Journal);
        if (g == null) return;
        foreach (var (_, node, jr) in nodeQ)
        {
            Place(node, g);
            var h = AttrInt(g, "height");
            if (h > 0) jr.Ref.SpecialHeight = h;
            jr.Ref.Minimized = AttrBool(g, "isminimized");
            g.Handled = true;
            return;
        }
        JournalPlugin.OpenOrFocus(commands, builder.Value, assets.Value, z.Value, tagQ);
    }

    private static void RestoreWorldMap(
        ResMut<GumpRestoreQueue> queue,
        Commands commands,
        Res<WorldMapState> state,
        Res<Profile> profile,
        ResMut<UiZCounter> z,
        Query<Data<WorldMapWindow>> tagQ,
        Query<Data<Node>, Filter<With<WorldMapWindow>>> nodeQ)
    {
        var g = Next(queue.Value, LegacyGumpType.WorldMap);
        if (g == null) return;
        foreach (var (_, node) in nodeQ) { Place(node, g); g.Handled = true; return; }
        WorldMapGumpPlugin.OpenOrFocus(commands, state.Value, profile.Value, z.Value, tagQ);
    }

    // Containers + spellbooks: legacy re-double-clicks the serial so the server
    // resends contents; seed the saved position so it lands where it was. The
    // double-clicks are STAGGERED through DelayedAction (legacy's UseItemQueue
    // sends one per second) — firing them all in one frame floods the server on
    // a multi-container login. Position memory is seeded now (consumed when the
    // container reopens), only the network send is delayed. Captured net/memory
    // are stable singletons; the callbacks touch only those + value-type serials.
    private const float ContainerReopenStaggerMs = 600f;

    private static void RestoreContainers(
        ResMut<GumpRestoreQueue> queue,
        Res<NetClient> net,
        Res<DelayedAction> delayed,
        ResMut<ContainerPositionMemory> memory)
    {
        var client = net.Value;
        var actions = delayed.Value;
        var mem = memory.Value;
        var index = 0;

        foreach (var g in queue.Value.Gumps)
        {
            if (g.Handled) continue;
            if (g.Type != (int)LegacyGumpType.Container && g.Type != (int)LegacyGumpType.SpellBook) continue;
            g.Handled = true;
            if (g.Serial == 0) continue;
            mem.Saved[g.Serial] = new Point(g.X, g.Y);
            var serial = g.Serial;
            actions.Add(() => client.Send_DoubleClick(serial), index * ContainerReopenStaggerMs);
            index++;
        }
    }

    // Spell icons are pure client-side UI (no server resend) — respawn each saved
    // SpellButton in one pass via the spellbook's shared spawner (rebuilds the
    // tooltip + double-click-to-cast). Position seeded straight from the saved x/y.
    private static void RestoreSpellButtons(
        ResMut<GumpRestoreQueue> queue,
        Commands commands,
        Res<GumpBuilder> builder,
        ResMut<UiZCounter> z,
        ResMut<AnchorCounter> counter,
        Res<UOFileManager> fileManager,
        ResMut<ObjectPropertyLists> opl)
    {
        foreach (var g in queue.Value.Gumps)
        {
            if (g.Handled) continue;
            if (g.Type != (int)LegacyGumpType.SpellButton) continue;
            g.Handled = true;
            var castId = AttrInt(g, "id");
            var iconGfx = AttrUShort(g, "graphic");
            if (castId == 0 || iconGfx == 0) continue;
            var ec = SpellbookGumpPlugin.SpawnFloatingSpellButton(
                commands, builder.Value, z.Value, fileManager.Value, opl.Value,
                castId, iconGfx, null, new Vector2(g.X, g.Y));
            ApplyAnchorGroup(ec, g, counter.Value);
        }
    }

    // Floating skill buttons — client-side, one pass. "id" is the skill index;
    // re-resolve the name from assets to rebuild the button (legacy SkillButton).
    private static void RestoreSkillButtons(
        ResMut<GumpRestoreQueue> queue,
        Commands commands,
        Res<GumpBuilder> builder,
        Res<AssetsServer> assets,
        ResMut<UiZCounter> z,
        ResMut<AnchorCounter> counter)
    {
        foreach (var g in queue.Value.Gumps)
        {
            if (g.Handled) continue;
            if (g.Type != (int)LegacyGumpType.SkillButton) continue;
            g.Handled = true;
            if (!g.Attrs.ContainsKey("id")) continue;
            int skillId = AttrInt(g, "id");
            if (skillId < 0 || skillId >= assets.Value.Skills.SkillsCount) continue;
            string nm = assets.Value.Skills.Skills[skillId].Name;
            var ec = SkillsGumpPlugin.SpawnFloatingSkillButton(
                commands, builder.Value, z.Value, skillId, nm, new Vector2(g.X, g.Y));
            ApplyAnchorGroup(ec, g, counter.Value);
        }
    }

    // Floating combat-ability buttons — client-side, spawn in one pass. Icon comes
    // from the saved graphic; the "primary" flag picks which ability double-click
    // uses (the action reads the CURRENT primary/secondary, like legacy).
    private static void RestoreAbilityButtons(
        ResMut<GumpRestoreQueue> queue,
        Commands commands,
        Res<GumpBuilder> builder,
        ResMut<UiZCounter> z,
        ResMut<AnchorCounter> counter)
    {
        foreach (var g in queue.Value.Gumps)
        {
            if (g.Handled) continue;
            if (g.Type != (int)LegacyGumpType.AbilityButton) continue;
            g.Handled = true;
            var iconGfx = AttrUShort(g, "graphic");
            if (iconGfx == 0) continue;
            var ec = CombatBookGumpPlugin.SpawnFloatingAbilityButton(
                commands, builder.Value, z.Value, AttrBool(g, "primary"), iconGfx, new Vector2(g.X, g.Y));
            ApplyAnchorGroup(ec, g, counter.Value);
        }
    }

    // Floating gargoyle-flight toggle — fixed graphic, client-side, one pass.
    private static void RestoreRacialButtons(
        ResMut<GumpRestoreQueue> queue,
        Commands commands,
        Res<GumpBuilder> builder,
        ResMut<UiZCounter> z,
        ResMut<AnchorCounter> counter,
        Res<UOFileManager> fileManager,
        ResMut<ObjectPropertyLists> opl)
    {
        foreach (var g in queue.Value.Gumps)
        {
            if (g.Handled) continue;
            if (g.Type != (int)LegacyGumpType.RacialButton) continue;
            g.Handled = true;
            var ec = RacialBookGumpPlugin.SpawnFloatingRacialButton(
                commands, builder.Value, z.Value, fileManager.Value, opl.Value, new Vector2(g.X, g.Y));
            ApplyAnchorGroup(ec, g, counter.Value);
        }
    }

    // Per-mobile health bars — re-open each saved serial at its saved spot. The bar
    // sends its own status request + resolves the mobile per frame, so a serial
    // whose mobile isn't in range yet still restores (grays until data arrives).
    private static void RestoreHealthBars(
        ResMut<GumpRestoreQueue> queue,
        Commands commands,
        Res<GumpBuilder> builder,
        Res<AssetsServer> assets,
        Res<GameContext> gameCtx,
        ResMut<UiZCounter> z,
        ResMut<AnchorCounter> counter,
        Res<NetClient> net,
        Res<PartyState> party,
        Query<Data<HealthBarWindow>> existingQ)
    {
        foreach (var g in queue.Value.Gumps)
        {
            if (g.Handled) continue;
            if (g.Type != (int)LegacyGumpType.HealthBar) continue;
            g.Handled = true;
            if (g.Serial == 0) continue;
            var id = HealthBarPlugin.OpenForSerial(commands, builder.Value, assets.Value, gameCtx.Value, z.Value,
                net.Value, party.Value, g.Serial, new Vector2(g.X, g.Y), center: false, existingQ);
            if (id != 0) ApplyAnchorGroup(commands.Entity(id), g, counter.Value);
        }
    }

    // Reattach a restored anchorable to its saved group and keep AnchorCounter
    // ahead of every restored id so a new group minted this session can't collide.
    private static void ApplyAnchorGroup(EntityCommands ec, SavedGump g, AnchorCounter counter)
    {
        int grp = AttrInt(g, "anchor_group");
        if (grp <= 0) return;
        ec.Insert(new AnchorMember { GroupId = grp });
        counter.Next = Math.Max(counter.Next, grp + 1);
    }

    private static bool AttrBool(SavedGump g, string key)
        => g.Attrs.TryGetValue(key, out var v) && bool.TryParse(v, out var b) && b;

    private static int AttrInt(SavedGump g, string key)
        => g.Attrs.TryGetValue(key, out var v) && int.TryParse(v, out var n) ? n : 0;

    private static ushort AttrUShort(SavedGump g, string key)
        => g.Attrs.TryGetValue(key, out var v) && ushort.TryParse(v, out var n) ? n : (ushort)0;
}
