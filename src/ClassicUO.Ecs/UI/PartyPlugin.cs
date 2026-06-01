// Party system — ECS port of Game/Managers/PartyManager.cs + the party UI
// (PartyInviteGump, PartyGump manifest). The party roster lives in the
// PartyState resource; the server drives it through 0xBF subcommand 0x06.
//
// Membership feeds the party-mode health bar layout (HealthBarPlugin reads
// PartyState.Contains every frame and rebuilds a bar when its membership
// flips). Party chat (codes 3/4) is routed to the journal/overhead pipe as
// MessageType.Party; an invite (code 7) opens PartyInviteGump.

using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Network;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using TinyEcs.Bevy.UI.Widgets;
using ClayColor = Clay.Color;

namespace ClassicUO.Ecs;

// Party roster singleton (Res<PartyState>). Mirrors PartyManager: a fixed
// 10-slot member array (leader first), the inviter serial, and the loot flag.
internal sealed class PartyState
{
    public const int PartySize = 10;

    public uint Leader;
    public uint Inviter;
    public bool CanLoot;
    public readonly uint[] Members = new uint[PartySize];

    // Bumped on every roster change so open party UI (the manifest) can detect
    // it needs to rebuild without diffing the member array itself.
    public int Revision;

    public bool Contains(uint serial)
    {
        if (serial == 0)
            return false;
        for (int i = 0; i < PartySize; i++)
            if (Members[i] == serial)
                return true;
        return false;
    }

    public int IndexOf(uint serial)
    {
        for (int i = 0; i < PartySize; i++)
            if (Members[i] == serial)
                return i;
        return -1;
    }

    public int Count
    {
        get
        {
            int c = 0;
            for (int i = 0; i < PartySize; i++)
                if (Members[i] != 0)
                    c++;
            return c;
        }
    }

    public void Clear()
    {
        Leader = 0;
        Inviter = 0;
        for (int i = 0; i < PartySize; i++)
            Members[i] = 0;
    }
}

// Root marker for the party-invite popup.
internal struct PartyInviteWindow
{
    public uint Inviter;
}

internal readonly struct PartyPlugin : IPlugin
{
    // Legacy ProfileManager default party-message hue (green). ECS Profile has
    // no PartyMessageHue field yet, so the constant stands in.
    private const ushort PartyMessageHue = 0x0044;

    public void Build(App app)
    {
        app.AddResource(new PartyState());

        // Debug-only: CUO_DEBUG_PARTY=1 forces the player + nearby mobiles into a
        // party and opens their bars, so the party-mode health bar can be
        // screenshotted with a single client (no second player needed). Stripped
        // from normal runs — the system isn't even registered without the flag.
        if (System.Environment.GetEnvironmentVariable("CUO_DEBUG_PARTY") == "1")
        {
            var debugFn = DebugForceParty;
            app.AddSystem(debugFn)
                .InStage(Stage.Update)
                .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
                .Build();
        }

        app.AddObserver<On<PacketReceived<OnExtendedCommandPacket_0xBF>>,
            ResMut<PartyState>,
            Commands,
            Res<GameContext>,
            Res<NetworkEntitiesMap>,
            Res<Profile>,
            Res<GumpBuilder>,
            Res<AssetsServer>,
            Res<UiZCounter>,
            PartyPacketParams>(HandlePartyPacket);
    }

    private static void HandlePartyPacket(
        On<PacketReceived<OnExtendedCommandPacket_0xBF>> trig,
        ResMut<PartyState> party,
        Commands commands,
        Res<GameContext> gameCtx,
        Res<NetworkEntitiesMap> entities,
        Res<Profile> profile,
        Res<GumpBuilder> builder,
        Res<AssetsServer> assets,
        Res<UiZCounter> zCounter,
        PartyPacketParams p)
    {
        var packet = trig.Event.Packet;
        if (packet.Command != 6 || packet.PartyCode is null)
            return;

        var st = party.Value;
        uint playerSerial = gameCtx.Value.PlayerSerial;

        switch (packet.PartyCode.Value)
        {
            case 1: // add
            case 2: // remove
            {
                bool isRemove = packet.PartyCode.Value == 2;
                st.Revision++; // roster changed — open manifest rebuilds

                // count <= 1: the party fell apart entirely.
                if (packet.PartyCount <= 1)
                {
                    st.Clear();
                    break;
                }

                uint toRemove = isRemove ? (packet.PartyRemovedSerial ?? 0xFFFF_FFFF) : 0xFFFF_FFFF;

                // The server resends the full roster (leader first). Rebuild it,
                // dropping the removed serial; the first surviving serial is the
                // leader.
                st.Clear();
                int idx = 0;
                foreach (var serial in packet.PartyMembers)
                {
                    if (isRemove && serial == toRemove)
                        continue;
                    if (idx >= PartyState.PartySize)
                        break;
                    if (idx == 0)
                        st.Leader = serial;
                    st.Members[idx++] = serial;
                }

                // If I'm the one removed, or only one member is left, disband.
                if (isRemove && (toRemove == playerSerial || st.Count <= 1))
                    st.Clear();
                break;
            }

            case 3: // party message (private tell)
            case 4: // party message (broadcast)
            {
                uint ser = packet.PartyMessageSerial ?? 0;
                string text = packet.PartyMessageText ?? string.Empty;
                string name = string.Empty;
                if (entities.Value.TryGet(ser, out var ent) && p.Names.Contains(ent))
                {
                    var (_, nameC) = p.Names.Get(ent);
                    name = nameC.Ref.Value ?? string.Empty;
                }

                p.Overhead.Send(new TextOverheadEvent
                {
                    Serial = ser,
                    Name = name,
                    Text = text,
                    Hue = PartyMessageHue,
                    Font = 3,
                    MessageType = MessageType.Party,
                });
                break;
            }

            case 7: // invite
            {
                st.Inviter = packet.PartyInviter ?? 0;
                if (st.Inviter == 0 || !profile.Value.PartyInviteGump)
                    break;

                // Don't stack duplicate invite popups.
                foreach (var _ in p.Invites)
                    return;

                string name = string.Empty;
                if (entities.Value.TryGet(st.Inviter, out var inviterEnt) && p.Names.Contains(inviterEnt))
                {
                    var (_, nameC) = p.Names.Get(inviterEnt);
                    name = nameC.Ref.Value ?? string.Empty;
                }

                OpenInviteGump(commands, builder.Value, assets.Value, zCounter.Value, st.Inviter, name);
                break;
            }
        }
    }

    // PartyInviteGump: a translucent box with the inviter's name and
    // Accept / Decline. Right-click-closable + draggable via UIMovable (legacy
    // is right-click-closable only; drag is a harmless extra). Buttons fire on
    // UiClick and route to Send_PartyAccept / Send_PartyDecline.
    private static void OpenInviteGump(
        Commands commands,
        GumpBuilder builder,
        AssetsServer assets,
        UiZCounter zCounter,
        uint inviter,
        string inviterName)
    {
        // Flex column: message on top, a centered Accept/Decline row below.
        // Root stays absolute (it floats on screen + drags), but its children
        // are laid out by flex — no hand-placed coordinates.
        var root = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(250),
                Top = Val.Px(150),
                Width = Val.Px(300),
                Height = Val.Px(96),
                FlexDirection = FlexDirection.Column,
                JustifyContent = JustifyContent.Center,
                AlignItems = AlignItems.Center,
                Gap = Val.Px(12),
                Padding = UiRect.All(12),
            })
            .Insert(new BackgroundColor(new ClayColor(0, 0, 0, 204)))
            .Insert(Interaction.None)
            .Insert(new PartyInviteWindow { Inviter = inviter })
            .Insert<UIMovable>()
            .Insert<UiContainsByBounds>()
            .Insert(new GlobalZIndex(zCounter.Bump()));
        var rootId = root.Id;

        var label = commands.Spawn()
            .Insert(new Node { Width = Val.Auto, Height = Val.Auto })
            .Insert(new Text($"{(string.IsNullOrEmpty(inviterName) ? "Someone" : inviterName)} has invited you to the party."))
            .Insert(new TextFont { FontId = 0, Size = 12 })
            .Insert(new TextColor(ClayColor.White));
        commands.AddChild(rootId, label.Id);

        var buttonRow = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                FlexDirection = FlexDirection.Row,
                JustifyContent = JustifyContent.Center,
                AlignItems = AlignItems.Center,
                Gap = Val.Px(16),
                Width = Val.Auto, Height = Val.Auto,
            });
        commands.AddChild(rootId, buttonRow.Id);
        var rowId = buttonRow.Id;

        var accept = AddTextButton(commands, "Accept");
        commands.AddChild(rowId, accept.Id);
        accept.Observe((On<UiClick> _, Res<NetClient> net, ResMut<PartyState> party, Commands cmd,
                        Query<Data<PartyInviteWindow>> wins) =>
        {
            if (party.Value.Inviter != 0 && party.Value.Leader == 0)
            {
                net.Value.Send_PartyAccept(party.Value.Inviter);
                party.Value.Leader = party.Value.Inviter;
                party.Value.Inviter = 0;
            }
            foreach (var (e, _) in wins) cmd.Entity(e.Ref).Despawn();
        });

        var decline = AddTextButton(commands, "Decline");
        commands.AddChild(rowId, decline.Id);
        decline.Observe((On<UiClick> _, Res<NetClient> net, ResMut<PartyState> party, Commands cmd,
                         Query<Data<PartyInviteWindow>> wins) =>
        {
            if (party.Value.Inviter != 0 && party.Value.Leader == 0)
            {
                net.Value.Send_PartyDecline(party.Value.Inviter);
                party.Value.Inviter = 0;
            }
            foreach (var (e, _) in wins) cmd.Entity(e.Ref).Despawn();
        });
    }

    // Small bordered text button (NiceButton stand-in): a flex box that centers
    // its label. Caller parents it (into the flex row) + attaches Observe.
    private static EntityCommands AddTextButton(Commands commands, string text)
    {
        var btn = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                Width = Val.Px(90), Height = Val.Px(26),
                JustifyContent = JustifyContent.Center,
                AlignItems = AlignItems.Center,
            })
            .Insert(new BackgroundColor(new ClayColor(60, 60, 60, 230)))
            .Insert(Interaction.None)
            .Insert(new Button());

        var lbl = commands.Spawn()
            .Insert(new Node { Width = Val.Auto, Height = Val.Auto })
            .Insert(new Text(text))
            .Insert(new TextFont { FontId = 0, Size = 12 })
            .Insert(new TextColor(ClayColor.White));
        commands.AddChild(btn.Id, lbl.Id);
        return btn;
    }

    // Debug harness helper (gated on CUO_DEBUG_PARTY). Forces the player +
    // up to three nearby mobiles into a party and opens their bars in a column
    // so the party-mode layout renders without a real party.
    private static void DebugForceParty(
        Local<bool> done,
        ResMut<PartyState> party,
        Commands commands,
        Res<GumpBuilder> builder,
        Res<AssetsServer> assets,
        Res<GameContext> gameCtx,
        Res<UiZCounter> zCounter,
        Res<NetClient> net,
        Query<Data<NetworkSerial>, Filter<With<Hits>>> mobilesQ,
        Query<Data<HealthBarWindow>> existingQ)
    {
        if (done.Value)
            return;

        uint player = gameCtx.Value.PlayerSerial;
        if (player == 0)
            return;

        var st = party.Value;
        st.Clear();
        st.Leader = player;
        st.Members[0] = player;

        int idx = 1;
        foreach (var (_, ns) in mobilesQ)
        {
            uint s = ns.Ref.Value;
            if (s == player || !SerialHelper.IsMobile(s) || st.Contains(s))
                continue;
            if (idx >= 4)
                break;
            st.Members[idx++] = s;
        }
        st.Revision++; // let an already-open manifest pick up the debug roster

        int col = 0;
        for (int i = 0; i < PartyState.PartySize; i++)
        {
            uint s = st.Members[i];
            if (s == 0)
                continue;
            HealthBarPlugin.OpenForSerial(commands, builder.Value, assets.Value, gameCtx.Value,
                zCounter.Value, net.Value, st, s, new Vector2(420, 90 + col * 60), center: false, existingQ);
            col++;
        }

        done.Value = true;
    }
}

// Bundled queries for the party packet observer (keeps the observer parameter
// count under the arity limit).
internal sealed class PartyPacketParams : CompositeSystemParam
{
    public readonly EventWriter<TextOverheadEvent> Overhead;
    public readonly Query<Data<EntityName>> Names;
    public readonly Query<Data<PartyInviteWindow>> Invites;

    public PartyPacketParams()
    {
        Overhead = Add(new EventWriter<TextOverheadEvent>());
        Names = Add(new Query<Data<EntityName>>());
        Invites = Add(new Query<Data<PartyInviteWindow>>());
    }
}
