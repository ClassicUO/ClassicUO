// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.ECS
{
    // ── Relationship Types ──────────────────────────────────────────────
    // Zero-size structs used as the first element of Flecs pairs.
    // Usage: entity.Add<OwnedBy>(ownerEntity)
    //
    // Built-in Flecs relationships also used:
    //   - Ecs.ChildOf : containment (item in container, entity in namespace)
    //   - Ecs.IsA     : prefab inheritance for archetypes/templates

    /// <summary>
    /// Ownership relationship. Pair target is the owning entity.
    /// Example: item.Add&lt;OwnedBy&gt;(playerEntity)
    /// </summary>
    public struct OwnedBy;

    /// <summary>
    /// Equipment relationship. Pair target is the mobile wearing the item.
    /// Layer is stored in LayerComponent on the item entity itself.
    /// Example: item.Add&lt;EquippedOn&gt;(mobileEntity)
    /// </summary>
    public struct EquippedOn;

    /// <summary>
    /// Targeting relationship. Pair target is the entity being targeted.
    /// Example: mobile.Add&lt;Targeting&gt;(targetEntity)
    /// </summary>
    public struct Targeting;

    /// <summary>
    /// Effect/projectile relationship. Pair target is the affected entity.
    /// Example: effect.Add&lt;Affects&gt;(targetEntity)
    /// </summary>
    public struct Affects;

    /// <summary>
    /// Party membership relationship. Pair target is the party leader entity.
    /// Example: member.Add&lt;InPartyOf&gt;(leaderEntity)
    /// </summary>
    public struct InPartyOf;
}
