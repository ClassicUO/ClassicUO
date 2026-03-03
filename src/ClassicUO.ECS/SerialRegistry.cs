// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using Flecs.NET.Core;

namespace ClassicUO.ECS
{
    /// <summary>
    /// O(1) serial → entity lookup shared across all ECS systems.
    /// Backed by the same Dictionary that EcsRuntimeHost observers maintain.
    /// Initialized during bootstrap; systems call the static helpers instead of
    /// building per-call queries.
    /// </summary>
    internal static class SerialRegistry
    {
        private static Dictionary<uint, Entity> _index;

        internal static void Initialize(Dictionary<uint, Entity> index)
        {
            _index = index;
        }

        internal static void Clear()
        {
            _index = null;
        }

        /// <summary>
        /// Look up the ECS entity for a given UO serial.
        /// Returns default (0) entity if not found or dead.
        /// </summary>
        internal static Entity FindBySerial(uint serial)
        {
            if (_index != null && _index.TryGetValue(serial, out var entity) && entity.IsAlive())
                return entity;
            return default;
        }

        /// <summary>
        /// Get or create the ECS entity for a given UO serial.
        /// If no entity exists for this serial, creates one with SerialComponent.
        /// </summary>
        internal static Entity FindOrCreate(World world, uint serial)
        {
            var found = FindBySerial(serial);
            if (found != 0)
                return found;

            return world.Entity()
                .Set(new SerialComponent(serial));
        }

        /// <summary>
        /// Find the player entity. Returns default (0) if no player exists.
        /// Uses the serial index to avoid query creation when a cached player entity is available.
        /// </summary>
        internal static Entity FindPlayer(World world)
        {
            Entity found = default;
            using var q = world.QueryBuilder<WorldPosition>()
                .With<PlayerTag>()
                .Build();

            q.Each((Entity e, ref WorldPosition _) =>
            {
                found = e;
            });
            return found;
        }

        /// <summary>
        /// Get the player's world position. Returns default if no player.
        /// </summary>
        internal static WorldPosition GetPlayerPosition(World world)
        {
            WorldPosition result = default;
            using var q = world.QueryBuilder<WorldPosition>()
                .With<PlayerTag>()
                .Build();

            q.Each((Entity _, ref WorldPosition pos) =>
            {
                result = pos;
            });
            return result;
        }
    }
}
