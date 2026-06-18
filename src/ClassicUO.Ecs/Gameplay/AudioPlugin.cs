using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.IO.Audio;
using ClassicUO.Utility;
using Microsoft.Xna.Framework.Audio;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;

// Port of the legacy AudioManager (src/ClassicUO.Client/Game/Managers/
// AudioManager.cs) onto the ECS. Sound/music instances come from
// AssetsServer.Sounds (the Renderer.Sounds cache over SoundsLoader); this
// resource only orchestrates volume, lifetime and the two music slots
// (0 = ambient, 1 = war music overriding it).
//
// Legacy gates volume on window focus via ReproduceSoundsInBackground —
// FnaPlugin doesn't expose focus yet, so the client behaves as always-active.
internal sealed class AudioState
{
    public const float SOUND_DELTA = 250f;

    public bool CanReproduceAudio;
    public int LoginMusicIndex = -1;

    public readonly UOMusic[] CurrentMusic = new UOMusic[2];
    public readonly int[] CurrentMusicIndices = new int[2];
    public readonly LinkedList<UOSound> CurrentSounds = new();
    // serial -> (next allowed footstep time in engine ms, alternating L/R offset)
    public readonly Dictionary<uint, (float NextTime, int Offset)> Footsteps = new();

    public void PlaySoundWithDistance(
        AssetsServer assets, Profile profile, float totalMs,
        int index, int x, int y, int playerX, int playerY, int viewRange)
    {
        if (!CanReproduceAudio)
            return;

        int distX = System.Math.Abs(x - playerX);
        int distY = System.Math.Abs(y - playerY);
        int distance = System.Math.Max(distX, distY);

        float volume = profile.SoundVolume / SOUND_DELTA;
        float distanceFactor = 0.0f;

        if (distance >= 1)
        {
            float volumeByDist = volume / (viewRange + 1);
            distanceFactor = volumeByDist * distance;
        }

        if (distance > viewRange)
            volume = 0;

        if (volume < -1 || volume > 1f)
            return;

        if (!profile.EnableSound)
            volume = 0;

        var sound = (UOSound)assets.Sounds.GetSound(index);

        if (sound != null && sound.Play((uint)totalMs, volume, distanceFactor))
        {
            sound.X = x;
            sound.Y = y;
            sound.CalculateByDistance = true;

            CurrentSounds.AddLast(sound);
        }
    }

    // Port of AudioManager.PlaySound: a non-positional one-shot (UI sounds like
    // container open/close). No distance attenuation — full profile volume.
    public void PlaySound(AssetsServer assets, Profile profile, float totalMs, int index)
    {
        if (!CanReproduceAudio)
            return;

        float volume = profile.SoundVolume / SOUND_DELTA;

        if (volume < -1 || volume > 1f)
            return;

        if (!profile.EnableSound)
            volume = 0;

        var sound = (UOSound)assets.Sounds.GetSound(index);

        if (sound != null && sound.Play((uint)totalMs, volume))
        {
            sound.X = -1;
            sound.Y = -1;
            sound.CalculateByDistance = false;

            CurrentSounds.AddLast(sound);
        }
    }

    public void PlayMusic(
        AssetsServer assets, Profile profile, Settings settings, float totalMs,
        int music, bool isWarmode = false, bool isLogin = false)
    {
        if (!CanReproduceAudio)
            return;

        if (music >= Constants.MAX_MUSIC_DATA_INDEX_COUNT)
            return;

        float volume;

        if (isLogin)
        {
            volume = settings.LoginMusic ? settings.LoginMusicVolume / SOUND_DELTA : 0;
        }
        else
        {
            volume = profile.EnableMusic ? profile.MusicVolume / SOUND_DELTA : 0;

            if (!profile.EnableCombatMusic && isWarmode)
                return;
        }

        if (volume < -1 || volume > 1f)
            return;

        var m = assets.Sounds.GetMusic(music);

        if (m == null && CurrentMusic[0] != null)
        {
            StopMusic();
        }
        else if (m != null && (m != CurrentMusic[0] || isWarmode))
        {
            StopMusic();

            int idx = isWarmode ? 1 : 0;
            CurrentMusicIndices[idx] = music;
            CurrentMusic[idx] = (UOMusic)m;
            CurrentMusic[idx].Play((uint)totalMs, volume);
        }
    }

    public void StopMusic()
    {
        for (int i = 0; i < 2; i++)
        {
            if (CurrentMusic[i] != null)
            {
                CurrentMusic[i].Stop();
                CurrentMusic[i].Dispose();
                CurrentMusic[i] = null;
            }
        }
    }

    public void StopWarMusic(AssetsServer assets, Profile profile, Settings settings, float totalMs)
    {
        PlayMusic(assets, profile, settings, totalMs, CurrentMusicIndices[0]);
    }

    public UOMusic GetCurrentMusic(float totalMs)
    {
        for (int i = 0; i < 2; i++)
        {
            if (CurrentMusic[i] != null && CurrentMusic[i].IsPlaying((uint)totalMs))
                return CurrentMusic[i];
        }

        return null;
    }
}

internal readonly struct AudioPlugin : IPlugin
{
    public void Build(App app)
    {
        app.AddResource(new AudioState());

        app.AddSystem(Stage.Startup, (ResMut<AudioState> audio) =>
        {
            // Same hardware probe the legacy AudioManager.Initialize does:
            // constructing a dynamic instance throws when no audio device
            // exists, and every play path bails out from then on.
            try
            {
                new DynamicSoundEffectInstance(0, AudioChannels.Stereo).Dispose();
                audio.Value.CanReproduceAudio = true;
            }
            catch (NoAudioHardwareException)
            {
                audio.Value.CanReproduceAudio = false;
            }
        });

        var playLoginMusicFn = PlayLoginMusic;
        app.AddSystem(playLoginMusicFn)
            .OnEnter(GameState.LoginScreen)
            .Build();

        // Legacy GameScene.Load stops the login loop; in-game music then
        // arrives from the server (0x6D / season change / warmode).
        app.AddSystem((ResMut<AudioState> audio) => audio.Value.StopMusic())
            .OnEnter(GameState.GameScreen)
            .Build();

        var updateAudioFn = UpdateAudio;
        app.AddSystem(updateAudioFn)
            .InStage(Stage.Update)
            .Build();

        var processFootstepsFn = ProcessFootsteps;
        app.AddSystem(processFootstepsFn)
            .InStage(Stage.Update)
            .Build();

        app.AddObserver<On<PacketReceived<OnSoundEffectPacket_0x54>>,
            ResMut<AudioState>,
            Res<AssetsServer>,
            Res<Profile>,
            Res<Time>,
            Res<GameContext>>(OnSoundEffect);

        app.AddObserver<On<PacketReceived<OnPlayMusicPacket_0x6D>>,
            ResMut<AudioState>,
            Res<AssetsServer>,
            Res<Profile>,
            Res<Settings>,
            Res<Time>>(OnPlayMusic);

        app.AddObserver<On<PacketReceived<OnSeasonChangePacket_0xBC>>,
            ResMut<AudioState>,
            Res<AssetsServer>,
            Res<Profile>,
            Res<Settings>,
            Res<Time>>(OnSeasonMusic);

        app.AddObserver<On<PacketReceived<OnWarmodePacket_0x72>>,
            ResMut<AudioState>,
            Res<AssetsServer>,
            Res<Profile>,
            Res<Settings>,
            Res<Time>>(OnWarmodeMusic);
    }

    static void PlayLoginMusic(
        ResMut<AudioState> audio,
        Res<AssetsServer> assets,
        Res<Profile> profile,
        Res<Settings> settings,
        Res<Time> time,
        Res<GameContext> gameCtx)
    {
        if (audio.Value.LoginMusicIndex < 0)
        {
            audio.Value.LoginMusicIndex = gameCtx.Value.ClientVersion switch
            {
                >= ClientVersion.CV_7000 => 78, // LoginLoop
                > ClientVersion.CV_308Z => 0,
                _ => 8 // stones2
            };
        }

        audio.Value.PlayMusic(
            assets.Value, profile.Value, settings.Value, time.Value.Total,
            audio.Value.LoginMusicIndex, isLogin: true);
    }

    // Per-frame upkeep, port of AudioManager.Update + UpdateCurrentMusicVolume:
    // pump the mp3 stream buffers, re-derive music volume from the profile
    // (war slot mutes the ambient slot), drop finished one-shot sounds.
    static void UpdateAudio(
        ResMut<AudioState> audio,
        Res<Time> time,
        Res<Profile> profile,
        Res<Settings> settings,
        Res<UoGame> game)
    {
        var state = audio.Value;

        if (!state.CanReproduceAudio)
            return;

        // Legacy AudioManager: a deactivated (unfocused) window mutes all audio
        // unless ReproduceSoundsInBackground is set. MasterVolume gates the
        // one-shot UOSound instances (SoundEffectInstance); music is a
        // DynamicSoundEffectInstance unaffected by it, so it's muted via the
        // explicit per-slot volume below.
        bool muted = !game.Value.IsActive && !profile.Value.ReproduceSoundsInBackground;
        Microsoft.Xna.Framework.Audio.SoundEffect.MasterVolume = muted ? 0f : 1f;

        bool runningWarMusic = state.CurrentMusic[1] != null;

        for (int i = 0; i < 2; i++)
        {
            var music = state.CurrentMusic[i];

            if (music == null)
                continue;

            float volume = music.Index == state.LoginMusicIndex
                ? (settings.Value.LoginMusic ? settings.Value.LoginMusicVolume / AudioState.SOUND_DELTA : 0)
                : (profile.Value.EnableMusic ? profile.Value.MusicVolume / AudioState.SOUND_DELTA : 0);

            if (i == 0 && runningWarMusic)
                volume = 0;

            if (muted)
                volume = 0;

            if (volume >= -1 && volume <= 1f)
                music.Volume = volume;

            music.Update();
        }

        var node = state.CurrentSounds.First;

        while (node != null)
        {
            var next = node.Next;

            if (!node.Value.IsPlaying((uint)time.Value.Total))
            {
                node.Value.Stop();
                state.CurrentSounds.Remove(node);
            }

            node = next;
        }
    }

    // Port of Mobile.ProcessFootstepsSound: walking humans emit 0x012B/0x012C
    // alternating, mounted 0x0129 (run) or 0x012A-style single (walk), spaced
    // by the legacy delay table. Distance attenuation from the player center.
    static void ProcessFootsteps(
        Res<Time> time,
        Res<Profile> profile,
        ResMut<AudioState> audio,
        Res<AssetsServer> assets,
        Res<GameContext> gameCtx,
        Query<Data<NetworkSerial, Graphic, MobileSteps, ServerFlags, EquipmentSlots>,
              Filter<Without<ContainedInto>, Optional<ServerFlags>, Optional<EquipmentSlots>>> mobiles)
    {
        if (!audio.Value.CanReproduceAudio ||
            !profile.Value.EnableFootstepsSound ||
            gameCtx.Value.PlayerSerial == 0)
            return;

        var total = time.Value.Total;

        foreach (var (_, serial, graphic, steps, flags, equip) in mobiles)
        {
            if (steps.Ref.Index < 0)
                continue;

            if (!Races.IsHuman(graphic.Ref.Value) || Walkability.IsDeadBody(graphic.Ref.Value))
                continue;

            if (flags.IsValid() && flags.Ref.Value.HasFlag(Flags.Hidden))
                continue;

            var footstep = audio.Value.Footsteps.TryGetValue(serial.Ref.Value, out var s) ? s : default;

            if (footstep.NextTime >= total)
                continue;

            ref var step = ref steps.Ref[steps.Ref.Index];
            bool mounted = equip.IsValid() && equip.Ref[Layer.Mount] != 0;

            int incID = footstep.Offset;
            int soundID = 0x012B;
            int delaySound = 400;

            if (mounted)
            {
                if (step.Run)
                {
                    soundID = 0x0129;
                    delaySound = 150;
                }
                else
                {
                    incID = 0;
                    delaySound = 350;
                }
            }

            delaySound = delaySound * 13 / 10;
            soundID += incID;

            audio.Value.PlaySoundWithDistance(
                assets.Value, profile.Value, total,
                soundID, step.X, step.Y,
                gameCtx.Value.CenterX, gameCtx.Value.CenterY,
                gameCtx.Value.MaxObjectsDistance);

            audio.Value.Footsteps[serial.Ref.Value] = (total + delaySound, (incID + 1) % 2);
        }
    }

    static void OnSoundEffect(
        On<PacketReceived<OnSoundEffectPacket_0x54>> trig,
        ResMut<AudioState> audio,
        Res<AssetsServer> assets,
        Res<Profile> profile,
        Res<Time> time,
        Res<GameContext> gameCtx)
    {
        var packet = trig.Event.Packet;

        audio.Value.PlaySoundWithDistance(
            assets.Value, profile.Value, time.Value.Total,
            packet.Index, packet.X, packet.Y,
            gameCtx.Value.CenterX, gameCtx.Value.CenterY,
            gameCtx.Value.MaxObjectsDistance);
    }

    static void OnPlayMusic(
        On<PacketReceived<OnPlayMusicPacket_0x6D>> trig,
        ResMut<AudioState> audio,
        Res<AssetsServer> assets,
        Res<Profile> profile,
        Res<Settings> settings,
        Res<Time> time)
    {
        var index = trig.Event.Packet.Index;

        // Midi stop variant 6D 1F FF (legacy PacketHandlers.PlayMusic).
        if (index == 0x1FFF)
        {
            audio.Value.StopMusic();
            return;
        }

        audio.Value.PlayMusic(assets.Value, profile.Value, settings.Value, time.Value.Total, index);
    }

    // Mirror of legacy World.ChangeSeason's music swap: the season track only
    // replaces silence or the login loop, never music the server picked.
    static void OnSeasonMusic(
        On<PacketReceived<OnSeasonChangePacket_0xBC>> trig,
        ResMut<AudioState> audio,
        Res<AssetsServer> assets,
        Res<Profile> profile,
        Res<Settings> settings,
        Res<Time> time)
    {
        var current = audio.Value.GetCurrentMusic(time.Value.Total);

        if (current == null || current.Index == audio.Value.LoginMusicIndex)
        {
            audio.Value.PlayMusic(
                assets.Value, profile.Value, settings.Value, time.Value.Total,
                trig.Event.Packet.Music);
        }
    }

    // Legacy GameActions.RequestWarMode picks combat1..3 on entering war and
    // restores the previous ambient track on leaving; keyed off the server
    // 0x72 confirmation here.
    static void OnWarmodeMusic(
        On<PacketReceived<OnWarmodePacket_0x72>> trig,
        ResMut<AudioState> audio,
        Res<AssetsServer> assets,
        Res<Profile> profile,
        Res<Settings> settings,
        Res<Time> time)
    {
        if (trig.Event.Packet.WarmodeEnabled)
        {
            audio.Value.PlayMusic(
                assets.Value, profile.Value, settings.Value, time.Value.Total,
                RandomHelper.GetValue(0, 3) % 3 + 38, isWarmode: true);
        }
        else
        {
            audio.Value.StopWarMusic(assets.Value, profile.Value, settings.Value, time.Value.Total);
        }
    }
}
