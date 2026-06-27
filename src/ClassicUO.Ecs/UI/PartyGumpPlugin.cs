// Party manifest (PartyGump) — ECS port of Game/UI/Gumps/PartyGump.cs. Opened
// from the paperdoll party button. Lists the 10 party slots with Tell / Kick
// controls and the bottom action row (send message, loot toggle, leave/disband,
// add member, OK, Cancel). Single instance; the loot toggle rebuilds it.

using ClassicUO.Ecs.Logging;
using ClassicUO.Network;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using TinyEcs.Bevy.UI.Widgets;

namespace ClassicUO.Ecs;

// cuo:modding contract type — do not merge/rename (queried by WIT path).
internal struct PartyManifestWindow
{
    public bool CanLoot;
}

// Set on the open manifest by party mutation sites (roster add/remove, leader
// flip) to request a row rebuild. Consumed + removed by RebuildDirtyManifests.
// Observer-driven instead of a per-frame revision compare.
internal struct PartyManifestDirty;

// A manifest member-name label. Its text is resolved live every frame (the
// member's name often arrives after they join), mirroring legacy
// PartyMember.Name being a property re-read each frame.
internal struct PartyMemberLabel
{
    public uint Serial;
}

internal readonly struct PartyGumpPlugin : IPlugin
{
    private const ushort Background = 0x0A28;
    private const ushort MemberLine = 0x0475;
    private const ushort LabelHue = 0x0386;

    public void Build(App app)
    {
        var refreshFn = RebuildDirtyManifests;
        app.AddSystem(refreshFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .Build();

        // Member names resolve live (a member's name can arrive after they join).
        var namesFn = RefreshMemberNames;
        app.AddSystem(namesFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .Build();

        var despawnFn = Despawn;
        app.AddSystem(despawnFn).OnExit(GameState.GameScreen).Build();
    }

    // Rebuild the rows of any manifest tagged PartyManifestDirty (roster / leader
    // changed). Only touches tagged windows — no per-frame state compare. Mirrors
    // legacy UIManager.GetGump<PartyGump>()?.RequestUpdateContents().
    private static void RebuildDirtyManifests(
        Commands commands,
        Res<PartyState> party,
        Res<GumpBuilder> builder,
        Res<AssetsServer> assets,
        Res<GameContext> gameCtx,
        Res<NetworkEntitiesMap> entities,
        Query<Data<EntityName>> nameQ,
        Query<Data<PartyManifestWindow>, Filter<With<PartyManifestDirty>>> windowsQ,
        Query<Data<TinyEcs.Children>> childrenQ)
    {
        foreach (var (ent, win) in windowsQ)
        {
            ulong rootId = ent.Ref;
            if (childrenQ.TryGet(rootId, out var childrenRow))
            {
                var (_, kids) = childrenRow;
                foreach (var cid in kids.Ref)
                    commands.Entity(cid).Despawn();
            }

            Populate(commands, builder.Value, assets.Value, gameCtx.Value.PlayerSerial,
                party.Value, entities.Value, nameQ, rootId, win.Ref.CanLoot);
            commands.Entity(rootId).Remove<PartyManifestDirty>();
        }
    }

    // Open the manifest, or focus it (bump z) if already open. Names resolve from
    // the live entity map at build time.
    public static void OpenOrFocus(
        Commands commands,
        GumpBuilder builder,
        AssetsServer assets,
        UiZCounter zCounter,
        PartyState party,
        uint playerSerial,
        NetworkEntitiesMap entities,
        Query<Data<EntityName>> nameQ,
        Query<Data<PartyManifestWindow>> existingQ)
    {
        if (builder.TryFocusExisting<PartyManifestWindow>(commands, existingQ, zCounter)) return;

        Build(commands, builder, assets, zCounter, party, playerSerial, entities, nameQ, party.CanLoot);
    }

    private static void Build(
        Commands commands,
        GumpBuilder builder,
        AssetsServer assets,
        UiZCounter zCounter,
        PartyState party,
        uint playerSerial,
        NetworkEntitiesMap entities,
        Query<Data<EntityName>> nameQ,
        bool canLoot)
    {
        var root = builder.AddGumpNinePatch(commands, Background, Vector3.UnitZ, new Vector2(200, 80), new Vector2(450, 480))
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(200), Top = Val.Px(80),
                Width = Val.Px(450), Height = Val.Px(480),
            })
            .Insert(Interaction.None)
            .Insert(new PartyManifestWindow { CanLoot = canLoot })
            .Insert<UiMovable>()
            .Insert<UiContainsByBounds>()
            .Insert(new GlobalZIndex(zCounter.Bump()));

        Populate(commands, builder, assets, playerSerial, party, entities, nameQ, root.Id, canLoot);
    }

    // Build the rows + action buttons under an existing manifest root. Called on
    // open, on roster change (refresh), and on the loot toggle — always after the
    // old children are despawned so the window stays in place.
    private static void Populate(
        Commands commands,
        GumpBuilder builder,
        AssetsServer assets,
        uint playerSerial,
        PartyState party,
        NetworkEntitiesMap entities,
        Query<Data<EntityName>> nameQ,
        ulong rootId,
        bool canLoot)
    {
        bool isLeader = party.Leader == 0 || party.Leader == playerSerial;
        bool isMember = party.Leader != 0 && party.Leader != playerSerial;

        AddLabel(commands, rootId, "Tell", 1, new Vector2(40, 30));
        AddLabel(commands, rootId, "Kick", 1, new Vector2(80, 30));
        AddLabel(commands, rootId, "Party Manifest", 2, new Vector2(153, 20));

        int yPtr = 48;
        for (int i = 0; i < PartyState.PartySize; i++)
        {
            uint serial = party.Members[i];

            var tell = builder.AddButton(commands, (0x0FAB, 0x0FAD, 0x0FAC), Vector3.UnitZ, new Vector2(40, yPtr + 2));
            int slot = i;
            tell.Observe((On<UiClick> _, Res<ILogger> logger) =>
                logger.Value.LogTrace("[Party] tell slot {Slot} — chat input not wired", slot + 1));
            commands.AddChild(rootId, tell.Id);

            if (isLeader)
            {
                var kick = builder.AddButton(commands, (0x0FB1, 0x0FB3, 0x0FB2), Vector3.UnitZ, new Vector2(80, yPtr + 2));
                uint kickSerial = serial;
                kick.Observe((On<UiClick> _, Res<NetClient> net) =>
                {
                    if (kickSerial != 0)
                        net.Value.Send_PartyRemoveRequest(kickSerial);
                });
                commands.AddChild(rootId, kick.Id);
            }

            var linePic = builder.AddGump(commands, MemberLine, Vector3.UnitZ, new Vector2(130, yPtr));
            commands.AddChild(rootId, linePic.Id);

            // Tagged so RefreshMemberNames keeps it current — at build time the
            // member's name may not be known yet.
            var color = UoFontRuntime.AsciiHue(LabelHue);
            ushort fontId = (ushort)(2 | UoFontRuntime.AsciiFlag);
            var memberLbl = commands.Spawn()
                .Insert(new Node
                {
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(140), Top = Val.Px(yPtr + 1),
                    Width = Val.Auto, Height = Val.Auto,
                })
                .Insert(new Text(ResolveName(serial, entities, nameQ)))
                .Insert(new TextFont { FontId = fontId, Size = 12 })
                .Insert(new TextColor(color))
                .Insert(new PartyMemberLabel { Serial = serial });
            commands.AddChild(rootId, memberLbl.Id);

            yPtr += 25;
        }

        // Send-message row.
        var sendBtn = builder.AddButton(commands, (0x0FAB, 0x0FAD, 0x0FAC), Vector3.UnitZ, new Vector2(70, 307));
        sendBtn.Observe((On<UiClick> _, Res<ILogger> logger) =>
            logger.Value.LogTrace("[Party] send message — chat input not wired"));
        commands.AddChild(rootId, sendBtn.Id);
        AddLabel(commands, rootId, "Send the party a message", 2, new Vector2(110, 307));

        // Loot toggle (graphic + text reflect the current state). Click flips the
        // flag and rebuilds the rows in place (keeps the window position).
        ushort lootGraphic = canLoot ? (ushort)0x0FA2 : (ushort)0x0FA9;
        var lootBtn = builder.AddButton(commands, (lootGraphic, lootGraphic, lootGraphic), Vector3.UnitZ, new Vector2(70, 334));
        lootBtn.Observe((On<UiClick> _,
                         Commands cmd,
                         Res<GumpBuilder> gb,
                         Res<AssetsServer> a,
                         Res<GameContext> ctx,
                         Res<PartyState> st,
                         Res<NetworkEntitiesMap> map,
                         Query<Data<EntityName>> names,
                         Query<Data<PartyManifestWindow>> wins,
                         Query<Data<TinyEcs.Children>> kidsQ) =>
        {
            foreach (var (e, w) in wins)
            {
                bool newLoot = !w.Ref.CanLoot;
                w.Ref.CanLoot = newLoot;
                ulong rid = e.Ref;
                if (kidsQ.TryGet(rid, out var kidsRow))
                {
                    var (_, kids) = kidsRow;
                    foreach (var c in kids.Ref)
                        cmd.Entity(c).Despawn();
                }
                Populate(cmd, gb.Value, a.Value, ctx.Value.PlayerSerial, st.Value, map.Value, names, rid, newLoot);
            }
        });
        commands.AddChild(rootId, lootBtn.Id);
        AddLabel(commands, rootId, canLoot ? "Party can loot me" : "Party cannot loot me", 2, new Vector2(110, 334));

        // Leave / disband.
        var leaveBtn = builder.AddButton(commands, (0x0FAE, 0x0FB0, 0x0FAF), Vector3.UnitZ, new Vector2(70, 360));
        uint pSerial = playerSerial;
        leaveBtn.Observe((On<UiClick> _, Res<NetClient> net, Res<PartyState> st) =>
        {
            if (st.Value.Leader != 0)
                net.Value.Send_PartyRemoveRequest(pSerial);
        });
        commands.AddChild(rootId, leaveBtn.Id);
        AddLabel(commands, rootId, isMember ? "Leave the party" : "Disband the party", 2, new Vector2(110, 360));

        // Add member (leader only).
        if (isLeader)
        {
            var addBtn = builder.AddButton(commands, (0x0FA8, 0x0FAA, 0x0FA9), Vector3.UnitZ, new Vector2(70, 385));
            addBtn.Observe((On<UiClick> _, Res<NetClient> net, Res<PartyState> st, Res<GameContext> ctx) =>
            {
                if (st.Value.Leader == 0 || st.Value.Leader == ctx.Value.PlayerSerial)
                    net.Value.Send_PartyInviteRequest();
            });
            commands.AddChild(rootId, addBtn.Id);
            AddLabel(commands, rootId, "Add new member", 2, new Vector2(110, 385));
        }

        // OK: persist a loot-flag change, then close.
        var okBtn = builder.AddButton(commands, (0x00F9, 0x00F8, 0x00F7), Vector3.UnitZ, new Vector2(130, 430));
        okBtn.Observe((On<UiClick> _,
                       Commands cmd,
                       Res<NetClient> net,
                       ResMut<PartyState> st,
                       Query<Data<PartyManifestWindow>> wins) =>
        {
            foreach (var (e, w) in wins)
            {
                if (st.Value.Leader != 0 && st.Value.CanLoot != w.Ref.CanLoot)
                {
                    st.Value.CanLoot = w.Ref.CanLoot;
                    net.Value.Send_PartyChangeLootTypeRequest(w.Ref.CanLoot);
                }
                cmd.Entity(e.Ref).Despawn();
            }
        });
        commands.AddChild(rootId, okBtn.Id);

        // Cancel: just close.
        var cancelBtn = builder.AddButton(commands, (0x00F3, 0x00F1, 0x00F2), Vector3.UnitZ, new Vector2(236, 430));
        cancelBtn.Observe((On<UiClick> _, Commands cmd, Query<Data<PartyManifestWindow>> wins) =>
        {
            foreach (var (e, _) in wins)
                cmd.Entity(e.Ref).Despawn();
        });
        commands.AddChild(rootId, cancelBtn.Id);
    }

    // Resolve a member slot's display name. Empty slot -> blank; an in-party
    // serial whose name hasn't arrived yet -> "(Not seeing)" (legacy
    // ResGeneral.NotSeeing fallback).
    private static string ResolveName(uint serial, NetworkEntitiesMap entities, Query<Data<EntityName>> nameQ)
    {
        if (serial == 0)
            return string.Empty;
        if (entities.TryGet(serial, out var e) && nameQ.TryGet(e, out var nameRow))
        {
            var (_, nameC) = nameRow;
            if (!string.IsNullOrEmpty(nameC.Ref.Value))
                return nameC.Ref.Value;
        }
        return "(Not seeing)";
    }

    // Re-resolve every member label's text each frame so a name that lands after
    // the member joined shows up without a roster-change rebuild.
    private static void RefreshMemberNames(
        Res<NetworkEntitiesMap> entities,
        Query<Data<EntityName>> nameQ,
        Query<Data<PartyMemberLabel, Text>> labelsQ)
    {
        foreach (var (_, tag, text) in labelsQ)
            text.Ref.Value = ResolveName(tag.Ref.Serial, entities.Value, nameQ);
    }

    private static void AddLabel(Commands commands, ulong rootId, string text, int font, Vector2 pos)
    {
        var color = UoFontRuntime.AsciiHue(LabelHue);
        ushort fontId = (ushort)(font | UoFontRuntime.AsciiFlag);
        var lbl = commands.Spawn()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(pos.X), Top = Val.Px(pos.Y),
                Width = Val.Auto, Height = Val.Auto,
            })
            .Insert(new Text(text))
            .Insert(new TextFont { FontId = fontId, Size = 12 })
            .Insert(new TextColor(color));
        commands.AddChild(rootId, lbl.Id);
    }

    private static void Despawn(
        Commands commands,
        Query<Data<PartyManifestWindow>> windowsQ)
    {
        // Despawning the window root cascades to its children (same as the
        // canonical WindowDragPlugin right-click close), so no manual child loop.
        foreach (var (ent, _) in windowsQ)
            commands.Entity(ent.Ref).Despawn();
    }
}
