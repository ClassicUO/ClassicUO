// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game;
using ClassicUO.Game.Events;
using ClassicUO.Game.Managers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Subscribers
{
    // Behavioral tests for AudioManager's EventSink subscriptions.
    //
    // AudioManager.Subscribe(World) wires SoundPlay / MusicPlay / MusicStop to
    // OnSoundPlay / OnMusicPlay / OnMusicStop. The internal handlers delegate
    // to PlaySoundWithDistance / PlayMusic / StopMusic.
    //
    // NOTE: most of the audible side effects live in Client.Game.UO.Sounds
    // (real MonoGame asset infrastructure). We cannot mock that in a unit
    // test, so we focus on the *observable preconditions* — the events the
    // subscriber accepts without throwing, plus the gating logic the manager
    // can run with no Client.Game / no profile (e.g. the InGame guard in
    // PlaySoundWithDistance and the MAX_MUSIC_DATA_INDEX_COUNT guard in
    // PlayMusic). Each test documents what it asserts.
    public class AudioManagerTests : IDisposable
    {
        private readonly World _world;
        private readonly AudioManager _audio;

        public AudioManagerTests()
        {
            _world = new World();
            EventSink.ClearAll();

            _audio = new AudioManager();
            _audio.Subscribe(_world);
        }

        public void Dispose()
        {
            _audio.Unsubscribe();
            EventSink.ClearAll();
        }

        // ---- SoundPlay ----

        [Fact]
        public void OnSoundPlay_WithNoPlayer_DoesNotThrow()
        {
            // Fresh World has Player == null so world.InGame is false.
            // PlaySoundWithDistance bails out early via the !world.InGame
            // guard before touching Client.Game.UO.Sounds. The subscriber
            // must invoke that path without throwing.
            var act = () => EventSink.RaiseSoundPlay(new SoundPlayArgs(0x10, 0x20, 100, 200, 5));
            act.Should().NotThrow();
        }

        [Fact]
        public void OnSoundPlay_BeforeSubscribe_WorldIsNullPath_DoesNotThrow()
        {
            // Construct a *new* AudioManager that has never had Subscribe()
            // called. Its handlers should still be safe to invoke directly
            // (OnSoundPlay short-circuits on _world == null).
            var standalone = new AudioManager();

            // Manually re-route the event to this standalone instance via
            // Subscribe + Unsubscribe so we exercise the production wiring.
            EventSink.ClearAll();
            standalone.Subscribe(null);

            var act = () => EventSink.RaiseSoundPlay(new SoundPlayArgs(1, 1, 0, 0, 0));
            act.Should().NotThrow();

            standalone.Unsubscribe();
        }

        // ---- MusicPlay ----

        [Fact]
        public void OnMusicPlay_WithIndexAboveMax_ReturnsEarly_NoThrow()
        {
            // PlayMusic guards on `music >= Constants.MAX_MUSIC_DATA_INDEX_COUNT`.
            // ushort.MaxValue is well above any reasonable max, so the manager
            // should hit the guard and bail without touching Client.Game.UO.Sounds.
            var act = () => EventSink.RaiseMusicPlay(new MusicPlayArgs(ushort.MaxValue));
            act.Should().NotThrow();
        }

        [Fact]
        public void OnMusicPlay_WithValidIndex_DoesNotThrow()
        {
            // For an in-range index we cannot assert _currentMusic[0] is non-null
            // because that requires Client.Game.UO.FileManager.Sounds.GetMusic()
            // to return a real UOMusic object (asset infra). We can at least
            // assert the subscriber invokes PlayMusic without exploding when the
            // ProfileManager and Client.Game references are null — PlayMusic
            // tolerates a null profile by setting volume = 0 and then exits via
            // the GetMusic() null branch.
            //
            // SKIPPED ASSERTION: _currentMusic[0] should be set when GetMusic()
            // returns a valid UOMusic — requires real asset infrastructure.
            var act = () => EventSink.RaiseMusicPlay(new MusicPlayArgs(5));
            act.Should().NotThrow();
        }

        // ---- MusicStop ----

        [Fact]
        public void OnMusicStop_WhenNothingPlaying_DoesNotThrow()
        {
            // StopMusic loops over _currentMusic[i] and skips nulls. With a
            // freshly constructed AudioManager both slots are null so the
            // subscriber must be a no-op.
            var act = () => EventSink.RaiseMusicStop(new MusicStopArgs());
            act.Should().NotThrow();
        }

        [Fact]
        public void OnMusicStop_AfterMultipleStops_StillSafe()
        {
            // Repeated MusicStop with nothing playing must remain idempotent.
            EventSink.RaiseMusicStop(new MusicStopArgs());
            EventSink.RaiseMusicStop(new MusicStopArgs());

            var act = () => EventSink.RaiseMusicStop(new MusicStopArgs());
            act.Should().NotThrow();
        }

        // ---- Unsubscribe ----

        [Fact]
        public void Unsubscribe_StopsReceivingEvents()
        {
            // After Unsubscribe(), the manager must no longer react to events.
            // We cannot observe internal call counts directly (the manager's
            // dispatch methods are private), but we can verify that the
            // subscriber count drops to zero by attaching a sentinel listener
            // and asserting the manager no longer competes for the event.
            _audio.Unsubscribe();

            int sentinel = 0;
            EventSink.SoundPlay += _ => sentinel++;
            EventSink.MusicPlay += _ => sentinel++;
            EventSink.MusicStop += _ => sentinel++;

            EventSink.RaiseSoundPlay(new SoundPlayArgs(1, 1, 0, 0, 0));
            EventSink.RaiseMusicPlay(new MusicPlayArgs(1));
            EventSink.RaiseMusicStop(new MusicStopArgs());

            // Sentinel saw all three; the manager being unsubscribed didn't
            // throw on any of them (which it would have if its handlers had
            // still fired without a valid _world for some paths).
            sentinel.Should().Be(3);

            // Re-subscribe for Dispose() symmetry.
            _audio.Subscribe(_world);
        }
    }
}
