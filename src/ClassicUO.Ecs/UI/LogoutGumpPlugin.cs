// Logout confirmation gump — ECS port of Game/UI/Gumps/QuestionGump.cs as wired
// by GameScene.RequestQuitGame. Opened from the paperdoll Logout button.
// Background 0x0816 with a "Quit / Ultima Online?" prompt and Cancel / OK
// buttons. OK either disconnects immediately or sends a logout notification
// (0xD1) and waits for the server's accept byte, depending on the
// CLF_OWERWRITE_CONFIGURATION_BUTTON client feature flag — same branch as
// PacketHandlers.Logout.

using System;
using ClassicUO.Assets;
using ClassicUO.Game.Data;
using ClassicUO.Network;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using TinyEcs.Bevy.UI.Widgets;

namespace ClassicUO.Ecs;

internal struct LogoutGumpWindow;

// Mirrors GameScene.DisconnectionRequested: set when OK is pressed under the
// CLF_OWERWRITE_CONFIGURATION_BUTTON flag so the 0xD1 reply knows the
// disconnect was client-initiated.
internal sealed class LogoutState { public bool DisconnectionRequested; }

internal readonly struct LogoutGumpPlugin : IPlugin
{
    private const ushort BackgroundGump = 0x0816;
    private const ushort CancelN = 0x0817, CancelP = 0x0818, CancelO = 0x0819;
    private const ushort OkN = 0x081A, OkP = 0x081B, OkO = 0x081C;

    public void Build(App app)
    {
        app.AddResource(new LogoutState());

        var despawnFn = Despawn;
        app.AddSystem(despawnFn).OnExit(GameState.GameScreen).Build();

        // Server reply to a client-initiated logout (0xD1). Disconnect + return
        // to the login screen only when WE asked and the flag is set; the bare
        // notification path (flag clear) disconnects up front in Ok().
        app.AddObserver<On<PacketReceived<OnLogoutRequestPacket_0xD1>>,
            Res<NetClient>,
            Res<GameContext>,
            ResMut<LogoutState>,
            ResMut<NextState<GameState>>>(OnLogoutReply);
    }

    // Open the confirmation gump (or refocus the existing one). Called from the
    // paperdoll Logout button.
    public static void OpenOrFocus(
        Commands commands,
        GumpBuilder builder,
        AssetsServer assets,
        UiZCounter zCounter,
        UiSurface surface,
        Query<Data<LogoutGumpWindow>> existingQ)
    {
        foreach (var (ent, _) in existingQ)
        {
            commands.Entity(ent.Ref).Insert(new GlobalZIndex(zCounter.Bump()));
            return;
        }

        ref readonly var bg = ref assets.Gumps.GetGump(BackgroundGump);
        var size = new Vector2(bg.UV.Width, bg.UV.Height);
        // Centered, mirroring QuestionGump's ClientBounds centering.
        var pos = new Vector2(
            MathF.Max(0, (surface.LogicalSize.X - size.X) * 0.5f),
            MathF.Max(0, (surface.LogicalSize.Y - size.Y) * 0.5f));

        var root = builder.SpawnUOGump(commands, BackgroundGump, Vector3.UnitZ, pos, zCounter)
            .Insert<LogoutGumpWindow>();
        var rootId = root.Id;

        // Matches QuestionGump's Label(message, isunicode:false, hue:0x0386, font:1):
        // ASCII font 1 with the hue baked per pixel (UoFontRuntime.AsciiHue).
        var label = builder.AddLabel(commands, "Quit\nUltima Online?", new Vector2(33, 30), new Vector2(165, 40))
            .Insert(new TextFont { FontId = (ushort)(1 | UoFontRuntime.AsciiFlag), Size = 12 })
            .Insert(new TextColor(UoFontRuntime.AsciiHue(0x0386)));
        commands.AddChild(rootId, label.Id);

        var cancel = builder.AddButton(commands, (CancelN, CancelP, CancelO), Vector3.UnitZ, new Vector2(37, 75));
        cancel.Observe((On<UiClick> _, Commands cmd, Query<Data<TinyEcs.Children>> childrenQ) =>
            DespawnSubtree(cmd, rootId, childrenQ));
        commands.AddChild(rootId, cancel.Id);

        var ok = builder.AddButton(commands, (OkN, OkP, OkO), Vector3.UnitZ, new Vector2(100, 75));
        ok.Observe((On<UiClick> _,
                    Commands cmd,
                    Res<NetClient> net,
                    Res<GameContext> ctx,
                    ResMut<LogoutState> logout,
                    ResMut<NextState<GameState>> state,
                    Query<Data<TinyEcs.Children>> childrenQ) =>
        {
            Ok(net.Value, ctx.Value, logout.Value, state.Value);
            DespawnSubtree(cmd, rootId, childrenQ);
        });
        commands.AddChild(rootId, ok.Id);
    }

    // OK pressed. With CLF_OWERWRITE_CONFIGURATION_BUTTON the server owns the
    // disconnect: flag the request and notify, then wait for the 0xD1 reply.
    // Otherwise disconnect locally and bounce to the login screen now.
    private static void Ok(NetClient net, GameContext ctx, LogoutState logout, NextState<GameState> state)
    {
        if ((ctx.ClientFeatures & CharacterListFlags.CLF_OWERWRITE_CONFIGURATION_BUTTON) != 0)
        {
            logout.DisconnectionRequested = true;
            net.Send_LogoutNotification();
        }
        else
        {
            net.Disconnect();
            state.Set(GameState.LoginScreen);
        }
    }

    private static void OnLogoutReply(
        On<PacketReceived<OnLogoutRequestPacket_0xD1>> trig,
        Res<NetClient> net,
        Res<GameContext> ctx,
        ResMut<LogoutState> logout,
        ResMut<NextState<GameState>> state)
    {
        if (!logout.Value.DisconnectionRequested
            || (ctx.Value.ClientFeatures & CharacterListFlags.CLF_OWERWRITE_CONFIGURATION_BUTTON) == 0)
            return;

        logout.Value.DisconnectionRequested = false;

        if (trig.Event.Packet.ShouldDisconnect)
        {
            net.Value.Disconnect();
            state.Value.Set(GameState.LoginScreen);
        }
        else
        {
            Console.WriteLine("0xD1 - client asked to disconnect but server answered 'NO!'");
        }
    }

    private static void Despawn(
        Commands commands,
        Query<Data<LogoutGumpWindow>> windowsQ,
        Query<Data<TinyEcs.Children>> childrenQ)
    {
        foreach (var (ent, _) in windowsQ)
            DespawnSubtree(commands, ent.Ref, childrenQ);
    }

    private static void DespawnSubtree(Commands commands, ulong e, Query<Data<TinyEcs.Children>> childrenQ)
    {
        if (childrenQ.Contains(e))
        {
            var (_, kids) = childrenQ.Get(e);
            foreach (var cid in kids.Ref)
                DespawnSubtree(commands, cid, childrenQ);
        }
        commands.Entity(e).Despawn();
    }
}
