// Runs the real Mobile.EnqueueStep / ProcessSteps pipeline headless (no window, sockets or
// game files). Fixture and the first two reproductions adapted from MithrilHammer's review
// harness on PR #1930.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using Xunit;

namespace ClassicUO.UnitTests.Game
{
    [CollectionDefinition("Step drain", DisableParallelization = true)]
    public sealed class StepDrainCollection { }

    [Collection("Step drain")]
    public sealed class StepDrainTests
    {
        private const int FRAME = 16;

        private static Mobile Create()
        {
            var game = (GameController)RuntimeHelpers.GetUninitializedObject(typeof(GameController));
            typeof(GameController).GetField("FrameDelay")!.SetValue(game, new uint[] { FRAME, FRAME / 2 });
            typeof(GameController).GetField("<UO>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(game, new UltimaOnline());
            typeof(Client).GetProperty("Game")!.SetValue(null, game);

            var world = (World)RuntimeHelpers.GetUninitializedObject(typeof(World));
            var player = (PlayerMobile)RuntimeHelpers.GetUninitializedObject(typeof(PlayerMobile));
            player.Serial = 2;
            typeof(World).GetProperty("Player")!.SetValue(world, player);

            Time.Ticks = 1000;

            return new Mobile(world, 1) { X = 100, Y = 100, Direction = Direction.East };
        }

        private sealed class Run
        {
            public int BackwardFrames, StandFrames;
            public float MaxJump, MaxLag;
            public readonly List<int> StepMs = new();
        }

        // Feeds an eastward stream (tile i arrives at arrivals[i-1]) at 60 fps and measures the
        // rendered motion in tiles. Lag is the server's tile minus the rendered position.
        private static Run Drive(Mobile m, IReadOnlyList<uint> arrivals, bool run, uint tail = 1500)
        {
            var r = new Run();
            int next = 0, serverTile = 0, prevTile = 0;
            float prevProgress = 0;
            uint stepStart = 0;
            uint end = arrivals[^1] + tail;

            for (uint now = 1000; now <= end; now += FRAME)
            {
                Time.Ticks = now;

                while (next < arrivals.Count && arrivals[next] <= now)
                {
                    serverTile = next + 1;
                    Assert.True(m.EnqueueStep(100 + serverTile, 100, 0, Direction.East, run));
                    next++;
                }

                m.ProcessSteps(out _, true);

                int tile = m.X - 100;
                float progress = tile + Math.Min(Math.Abs((float)m.Offset.X) / 22f, 0.999f);

                if (tile != prevTile)
                {
                    if (stepStart != 0) { r.StepMs.Add((int)(now - stepStart)); }
                    stepStart = now;
                    prevTile = tile;
                }

                if (now > 1000)
                {
                    float jump = progress - prevProgress;
                    if (jump < -0.001f) { r.BackwardFrames++; }
                    r.MaxJump = Math.Max(r.MaxJump, jump);
                    r.MaxLag = Math.Max(r.MaxLag, serverTile - progress);
                    if (jump == 0 && m.Steps.Count == 0 && now <= arrivals[^1]) { r.StandFrames++; }
                }

                prevProgress = progress;
            }

            return r;
        }

        private static List<uint> Steady(int cadence, int steps) =>
            Enumerable.Range(1, steps).Select(i => 1000u + (uint)(cadence * i)).ToList();

        [Fact]
        public void SteadyCadenceRendersAtTheServersPace()
        {
            var r = Drive(Create(), Steady(300, 24), run: true);

            Assert.Equal(0, r.BackwardFrames);
            Assert.InRange(r.MaxLag, 0, 1.05f);
            Assert.All(r.StepMs.Skip(2), ms => Assert.InRange(ms, 288, 320)); // frame-quantised 300
        }

        [Fact]
        public void ALaterPacketNeverMovesTheSpriteBackwards()
        {
            var m = Create();
            m.EnqueueStep(101, 100, 0, Direction.East, true);
            m.EnqueueStep(102, 100, 0, Direction.East, true);

            float previous = 0;

            for (uint now = 1000; now <= 1160; now += FRAME)
            {
                Time.Ticks = now;
                if (now == 1016) { m.EnqueueStep(103, 100, 0, Direction.East, true); }
                if (now == 1128) { m.EnqueueStep(104, 100, 0, Direction.East, true); }
                m.ProcessSteps(out _, true);

                float position = (m.X - 100) * 22 + m.Offset.X;
                Assert.True(position >= previous, $"sprite moved backwards at t={now}");
                previous = position;
            }
        }

        [Fact]
        public void ABacklogDrainsAtDoubleSpeedAtMost()
        {
            // 200 ms run; packets 10 and 11 are held and arrive together with 12.
            var arrivals = Steady(200, 24);
            arrivals[9] = arrivals[11];
            arrivals[10] = arrivals[11];

            var r = Drive(Create(), arrivals, run: true);

            Assert.Equal(0, r.BackwardFrames);
            Assert.True(r.StepMs.Min() >= 96, $"a step rendered in {r.StepMs.Min()} ms");
            Assert.InRange(r.MaxJump, 0, 0.2f);
            Assert.Equal(24, r.StepMs.Count + 1);
        }

        [Fact]
        public void SlowerThanWalkGlidesUpToTheCapThenStepsAndStands()
        {
            // 450 ms is under the 600 ms cap: the walker glides. 800 ms is over it: it stands
            // between steps as before.
            var glide = Drive(Create(), Steady(450, 16), run: false);
            var lurch = Drive(Create(), Steady(800, 16), run: false);

            Assert.True(glide.StandFrames <= 2 * 16, $"glide stood for {glide.StandFrames} frames");
            Assert.True(lurch.StandFrames >= 16 * (800 - 400) / FRAME / 2, $"lurch stood for only {lurch.StandFrames} frames");
            Assert.All(glide.StepMs.Skip(2), ms => Assert.InRange(ms, 432, 480));
        }

        [Fact]
        public void APauseForgetsTheCadence()
        {
            var m = Create();
            Drive(m, Steady(100, 6), run: true, tail: 0);
            Assert.Equal(96, m.StepInterval);

            Time.Ticks += 1500;
            m.EnqueueStep(107, 100, 0, Direction.East, true);

            Assert.Equal(0, m.StepInterval);
        }

        [Fact]
        public void ArrivalFacingTurnDoesNotAccelerateTheSoleMove()
        {
            var m = Create();
            m.EnqueueStep(101, 100, 0, Direction.East, true);
            Time.Ticks = 1200;
            m.ProcessSteps(out _, true);

            Time.Ticks = 1300;
            m.EnqueueStep(102, 100, 0, Direction.East, true);
            Time.Ticks = 1380;
            m.EnqueueStep(102, 100, 0, Direction.North, true);
            m.ProcessSteps(out _, true);

            Assert.Equal(101, m.X);
            Assert.InRange(m.Offset.X, 4, 7); // 80 ms into a 300 ms stride
        }

        [Fact]
        public void APairSpanningATurnKeepsTheCadence()
        {
            var m = Create();
            Drive(m, Steady(300, 4), run: true, tail: 0);
            Assert.InRange(m.StepInterval, 288, 304);

            Time.Ticks += 300;
            m.EnqueueStep(104, 100, 0, Direction.North, true); // facing-turn, same tile
            Time.Ticks += 80;
            m.EnqueueStep(104, 99, 0, Direction.North, true);

            Assert.InRange(m.StepInterval, 288, 304);
        }

        [Fact]
        public void TheLocalPlayerIsNotMeasured()
        {
            var m = Create();
            m.Serial = 2;

            for (int i = 1; i <= 3; i++)
            {
                Time.Ticks += 100;
                m.EnqueueStep(100 + i, 100, 0, Direction.East, true);
            }

            Assert.Equal(0, m.StepInterval);
        }
    }
}
