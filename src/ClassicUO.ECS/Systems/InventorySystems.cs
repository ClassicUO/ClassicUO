// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using Flecs.NET.Core;

namespace ClassicUO.ECS.Systems
{
    /// <summary>
    /// Inventory systems: container cascade destruction.
    ///
    /// When a container entity is destroyed, all items with a ContainerLink
    /// pointing to that container are also destroyed (deferred via PendingRemovalTag).
    /// </summary>
    public static class InventorySystems
    {
        public static void Register(World world)
        {
            RegisterContainerCascade(world);
        }

        // ── Container cascade (PostSim) ──────────────────────────────

        private static void RegisterContainerCascade(World world)
        {
            // When an entity with SerialComponent gets PendingRemovalTag,
            // find all items linked to it and mark them for removal too.
            world.System<SerialComponent>("PostSim_ContainerCascade")
                .Kind(Phases.PostSim)
                .With<PendingRemovalTag>()
                .Run((Iter it) =>
                {
                    var w = it.World();
                    var toMark = new List<Entity>();

                    while (it.Next())
                    {
                        var serials = it.Field<SerialComponent>(0);
                        for (int i = 0; i < it.Count(); i++)
                        {
                            uint serial = serials[i].Serial;

                            // Find all items that reference this container
                            using var q = w.QueryBuilder<ContainerLink>()
                                .With<ItemTag>()
                                .Without<PendingRemovalTag>()
                                .Build();

                            q.Each((Entity child, ref ContainerLink link) =>
                            {
                                if (link.ContainerSerial == serial)
                                    toMark.Add(child);
                            });
                        }
                    }

                    // Apply deferred marks
                    foreach (var e in toMark)
                    {
                        if (e.IsAlive() && !e.Has<PendingRemovalTag>())
                            e.Add<PendingRemovalTag>();
                    }
                });
        }
    }
}
