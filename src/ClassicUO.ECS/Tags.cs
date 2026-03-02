// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.ECS
{
    // ── Entity Kind Tags ────────────────────────────────────────────────
    // Zero-size structs used as Flecs tags to classify entity archetypes.

    /// <summary>Entity is a mobile (player, NPC, creature).</summary>
    public struct MobileTag;

    /// <summary>Entity is an item (ground object, container content, equipment).</summary>
    public struct ItemTag;

    /// <summary>Entity is a visual effect (spell, projectile, particles).</summary>
    public struct EffectTag;

    /// <summary>Entity is a static map object.</summary>
    public struct StaticTag;

    /// <summary>Entity is the local player character.</summary>
    public struct PlayerTag;

    /// <summary>Entity is a multi-object (house, boat).</summary>
    public struct MultiTag;

    /// <summary>Entity is a corpse.</summary>
    public struct CorpseTag;

    // ── State Tags ──────────────────────────────────────────────────────

    /// <summary>Mobile is dead.</summary>
    public struct DeadTag;

    /// <summary>Mobile is in war mode.</summary>
    public struct WarModeTag;

    /// <summary>Mobile is running.</summary>
    public struct RunningTag;

    /// <summary>Mobile is hidden/invisible.</summary>
    public struct HiddenTag;

    /// <summary>Mobile is poisoned (SA+).</summary>
    public struct PoisonedTag;

    /// <summary>Mobile is female.</summary>
    public struct FemaleTag;

    /// <summary>Mobile is renamable.</summary>
    public struct RenamableTag;

    /// <summary>Item is on the ground (not in a container).</summary>
    public struct OnGroundTag;

    /// <summary>Item is damageable.</summary>
    public struct DamageableTag;

    /// <summary>Container is currently open.</summary>
    public struct OpenedTag;

    // ── Lifecycle Tags ───────────────────────────────────────────────────

    /// <summary>Entity is marked for deferred destruction at end of frame.</summary>
    public struct PendingRemovalTag;

    // ── Movement Tags ────────────────────────────────────────────────────

    /// <summary>Mobile is mounted (affects movement speed).</summary>
    public struct MountedTag;

    /// <summary>Mobile is flying (gargoyle flight, affects movement speed).</summary>
    public struct FlyingTag;

    // ── Render Tags ──────────────────────────────────────────────────────

    /// <summary>Entity is within the current viewport and should be drawn this frame.</summary>
    public struct VisibleTag;
}
