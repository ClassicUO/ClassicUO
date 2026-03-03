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

    /// <summary>Mobile is frozen/paralyzed (Flags bit 0x01).</summary>
    public struct FrozenTag;

    /// <summary>Mobile has yellow health bar (invulnerable/NPC flag, Flags bit 0x08).</summary>
    public struct YellowHitsTag;

    /// <summary>Mobile graphic is a human or elf body type. Derived from Graphic at creation.</summary>
    public struct IsHumanTag;

    /// <summary>Mobile graphic is a gargoyle body type. Derived from Graphic at creation.</summary>
    public struct IsGargoyleTag;

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

    // ── Boat Tags ──────────────────────────────────────────────────────

    /// <summary>Entity is currently on a boat.</summary>
    public struct OnBoatTag;

    // ── Social Tags ────────────────────────────────────────────────────

    /// <summary>Mobile is a member of the player's party.</summary>
    public struct PartyTag;

    /// <summary>Entity is an overhead text entry.</summary>
    public struct OverheadTextTag;

    // ── Buff Tags ──────────────────────────────────────────────────────

    /// <summary>Entity is a buff/debuff child of a mobile.</summary>
    public struct BuffTag;

    // ── Corpse Tags ──────────────────────────────────────────────────────

    /// <summary>Corpse should auto-open when player is within range.</summary>
    public struct AutoOpenCorpseTag;

    // ── Effect Tags ─────────────────────────────────────────────────────

    /// <summary>Effect should spawn an explosion when it expires (moving effects with Explode flag).</summary>
    public struct ExplodeOnExpiryTag;

    // ── Selection Tags ────────────────────────────────────────────────────

    /// <summary>Entity is under the mouse cursor this frame.</summary>
    public struct MouseOverTag;

    /// <summary>Entity is explicitly selected (e.g., clicked).</summary>
    public struct SelectedTag;

    // ── Render Tags ──────────────────────────────────────────────────────

    /// <summary>Entity is within the current viewport and should be drawn this frame.</summary>
    public struct VisibleTag;
}
