// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Ecs.Logging;
using ClassicUO.Utility.Logging;
using Microsoft.Extensions.Logging;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;

// Per-session persistence state. Holds the account/server/character that name
// the profile folder (captured at the server- and character-select screens) and
// the resolved profile path once loaded. Username comes from Settings.Username
// (set at login). Loaded gates the save passes so we never write a profile we
// never loaded (agent / auto paths that skip the select screens).
internal sealed class ProfileContext
{
    public string ServerName = string.Empty;
    public string CharacterName = string.Empty;
    // Server display names keyed by server index, captured from the 0xA8 list so
    // SelectServer can resolve the chosen server's name (folder + LastServerName).
    public System.Collections.Generic.Dictionary<int, string> ServerNames = new();
    public string ProfilePath = string.Empty;
    public bool Loaded;
}

// Wires settings.json + profile.json persistence. Legacy ClassicUO.Client is the
// source of truth: same Data/Profiles/<user>/<server>/<char>/ layout, same
// snake_case profile.json (see ProfileJsonContext), Settings.Save() on the same
// triggers (server select, login-with-saveaccount, logout, app exit).
internal readonly struct PersistencePlugin : IPlugin
{
    public void Build(App app)
    {
        app.AddResource(new ProfileContext());
        app.AddResource(new GlobalProfile());

        var loadFn = LoadProfile;
        app.AddSystem(loadFn).OnEnter(GameState.GameScreen).Build();

        // Saving (profile.json + globalprofile.json + settings.json + gumps.xml)
        // is done by GumpPersistencePlugin in a single system — one Loaded check,
        // no race between profile and gump writes.
    }

    private static void LoadProfile(
        Commands commands,
        Res<Settings> settings,
        ResMut<ProfileContext> ctx,
        Res<ILogger> logger)
    {
        var user = settings.Value.Username;
        var server = ctx.Value.ServerName;
        var ch = ctx.Value.CharacterName;

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(server) || string.IsNullOrEmpty(ch))
        {
            logger.Value.LogWarning($"profile load skipped — missing identity (user='{user}' server='{server}' char='{ch}')");
            return;
        }

        var (profile, global, path) = ProfileManager.Load(server, user, ch, logger.Value);
        ctx.Value.ProfilePath = path;
        ctx.Value.Loaded = true;

        // Swap the registered Res<Profile>/Res<GlobalProfile> boxes — every system
        // re-fetches its resource box each run, so the loaded values take effect
        // next frame (Res<T>.Fetch in TinyEcs).
        commands.InsertResource(profile);
        commands.InsertResource(global);

        logger.Value.LogTrace($"profile loaded: {path}");
    }
}
