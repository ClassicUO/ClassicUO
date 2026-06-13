using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Network;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;

// Corpses already auto-double-clicked this session. A marker on the corpse
// entity (auto-cleaned when it despawns) so re-entering range doesn't re-open
// it — mirrors legacy PlayerMobile.AutoOpenedCorpses.
struct AutoOpenedCorpse;

// Auto-open nearby corpses (Profile.AutoOpenCorpses + AutoOpenCorpseRange +
// CorpseOpenOptions). Ports legacy PlayerMobile.TryOpenCorpses: on each frame
// the player is in-world, double-click corpses within range once, gated by the
// targeting / hidden options. Default off, so the scan costs nothing unless
// enabled.
internal readonly struct AutoLootPlugin : IPlugin
{
    const ushort CorpseGraphic = 0x2006;

    public void Build(App app)
    {
        var fn = AutoOpenCorpses;
        app
            .AddSystem(fn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> state) => state.Value.Current == GameState.GameScreen)
            .RunIf((Res<Profile> profile) => profile.Value.AutoOpenCorpses)
            .Build();
    }

    private static void AutoOpenCorpses(
        Res<Profile> profile,
        Res<TargetingState> targeting,
        Res<NetClient> network,
        Commands commands,
        Query<Data<WorldPosition, ServerFlags>, Filter<With<Player>, Optional<ServerFlags>>> playerQ,
        Query<Data<NetworkSerial, WorldPosition, Graphic>,
            Filter<Without<MobAnimation>, Without<ContainedInto>, Without<AutoOpenedCorpse>>> corpseQ)
    {
        int playerX = 0, playerY = 0;
        bool hidden = false, hasPlayer = false;
        foreach (var (pos, flags) in playerQ)
        {
            playerX = pos.Ref.X;
            playerY = pos.Ref.Y;
            hidden = flags.IsValid() && (flags.Ref.Value & Flags.Hidden) != 0;
            hasPlayer = true;
            break;
        }
        if (!hasPlayer)
            return;

        var opt = profile.Value.CorpseOpenOptions;
        if ((opt == 1 || opt == 3) && targeting.Value.IsTargeting)
            return;
        if ((opt == 2 || opt == 3) && hidden)
            return;

        var range = profile.Value.AutoOpenCorpseRange;

        foreach (var (ent, serial, pos, graphic) in corpseQ)
        {
            if (graphic.Ref.Value != CorpseGraphic)
                continue;

            var dist = System.Math.Max(
                System.Math.Abs(pos.Ref.X - playerX),
                System.Math.Abs(pos.Ref.Y - playerY));
            if (dist > range)
                continue;

            commands.Entity(ent.Ref).Insert<AutoOpenedCorpse>();
            network.Value.Send_DoubleClick(serial.Ref.Value);
        }
    }
}
