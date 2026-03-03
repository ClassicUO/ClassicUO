// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using Flecs.NET.Core;

namespace ClassicUO.ECS
{
    /// <summary>
    /// Parity validation tool for comparing ECS state against expected values.
    ///
    /// Usage:
    ///   1. Capture a checkpoint: var cp = validator.CaptureCheckpoint();
    ///   2. Compare against expected: var diff = validator.Compare(cp, expected);
    ///   3. Assert no critical mismatches: diff.HasCriticalMismatch
    ///
    /// Used by replay harness and CI parity tests.
    /// </summary>
    public sealed class ParityValidator
    {
        private readonly EcsRuntimeHost _host;

        public ParityValidator(EcsRuntimeHost host)
        {
            _host = host;
        }

        /// <summary>
        /// Capture the current ECS state as a parity checkpoint.
        /// </summary>
        public ParityCheckpoint CaptureCheckpoint()
        {
            ref readonly var w = ref _host.World;

            ref readonly var parity = ref w.Get<ParityCounters>();
            ref readonly var timing = ref w.Get<FrameTiming>();

            int effectCount = 0;
            using var eq = w.QueryBuilder<SerialComponent>()
                .With<EffectTag>()
                .Without<PendingRemovalTag>()
                .Build();
            eq.Each((Entity _, ref SerialComponent __) => effectCount++);

            var playerSnap = _host.GetPlayerSnapshot();

            return new ParityCheckpoint(
                Tick: timing.Ticks,
                MobileCount: parity.MobileCount,
                ItemCount: parity.ItemCount,
                EffectCount: effectCount,
                PlayerX: playerSnap.X,
                PlayerY: playerSnap.Y,
                PlayerZ: playerSnap.Z,
                PlayerHits: playerSnap.Hits,
                PlayerMana: playerSnap.Mana,
                PlayerStamina: playerSnap.Stamina
            );
        }

        /// <summary>
        /// Compare two checkpoints and return a diff report.
        /// </summary>
        public ParityDiff Compare(ParityCheckpoint actual, ParityCheckpoint expected)
        {
            var mismatches = new List<string>();

            if (actual.MobileCount != expected.MobileCount)
                mismatches.Add($"MobileCount: {actual.MobileCount} vs {expected.MobileCount}");
            if (actual.ItemCount != expected.ItemCount)
                mismatches.Add($"ItemCount: {actual.ItemCount} vs {expected.ItemCount}");
            if (actual.EffectCount != expected.EffectCount)
                mismatches.Add($"EffectCount: {actual.EffectCount} vs {expected.EffectCount}");
            if (actual.PlayerX != expected.PlayerX || actual.PlayerY != expected.PlayerY || actual.PlayerZ != expected.PlayerZ)
                mismatches.Add($"PlayerPos: ({actual.PlayerX},{actual.PlayerY},{actual.PlayerZ}) vs ({expected.PlayerX},{expected.PlayerY},{expected.PlayerZ})");
            if (actual.PlayerHits != expected.PlayerHits)
                mismatches.Add($"PlayerHits: {actual.PlayerHits} vs {expected.PlayerHits}");
            if (actual.PlayerMana != expected.PlayerMana)
                mismatches.Add($"PlayerMana: {actual.PlayerMana} vs {expected.PlayerMana}");
            if (actual.PlayerStamina != expected.PlayerStamina)
                mismatches.Add($"PlayerStamina: {actual.PlayerStamina} vs {expected.PlayerStamina}");

            // Entity counts are critical mismatches; vitals are non-critical.
            bool critical = actual.MobileCount != expected.MobileCount
                || actual.ItemCount != expected.ItemCount
                || actual.PlayerX != expected.PlayerX
                || actual.PlayerY != expected.PlayerY;

            return new ParityDiff(mismatches, critical);
        }

        /// <summary>
        /// Validate that current ECS entity counts match legacy World counts.
        /// Returns null if matching, or a mismatch description.
        /// </summary>
        public string ValidateEntityCounts(int legacyMobileCount, int legacyItemCount)
        {
            var parity = _host.GetParityCounters();

            if (parity.MobileCount == legacyMobileCount && parity.ItemCount == legacyItemCount)
                return null;

            return $"Parity mismatch — ECS: {parity.MobileCount}M/{parity.ItemCount}I, " +
                   $"Legacy: {legacyMobileCount}M/{legacyItemCount}I";
        }
    }

    /// <summary>
    /// Result of comparing two parity checkpoints.
    /// </summary>
    public readonly struct ParityDiff
    {
        public readonly IReadOnlyList<string> Mismatches;
        public readonly bool HasCriticalMismatch;

        public ParityDiff(List<string> mismatches, bool hasCriticalMismatch)
        {
            Mismatches = mismatches;
            HasCriticalMismatch = hasCriticalMismatch;
        }

        public bool IsMatch => Mismatches.Count == 0;
    }
}
