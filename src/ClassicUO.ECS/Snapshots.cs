// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.ECS
{
    /// <summary>
    /// Read-only snapshot of a mobile entity for UI/plugin consumption.
    /// Populated by EcsRuntimeHost query methods — no direct ECS access needed.
    /// </summary>
    public readonly struct MobileSnapshot
    {
        public readonly uint Serial;
        public readonly ushort X;
        public readonly ushort Y;
        public readonly sbyte Z;
        public readonly ushort Graphic;
        public readonly ushort Hue;
        public readonly byte Direction;
        public readonly byte Notoriety;
        public readonly uint Flags;
        public readonly ushort Hits;
        public readonly ushort HitsMax;
        public readonly ushort Mana;
        public readonly ushort ManaMax;
        public readonly ushort Stamina;
        public readonly ushort StaminaMax;
        public readonly bool IsPlayer;
        public readonly bool InWarMode;
        public readonly bool IsDead;
        public readonly bool IsHidden;
        public readonly bool Exists;

        public MobileSnapshot(
            uint serial, ushort x, ushort y, sbyte z,
            ushort graphic, ushort hue, byte direction, byte notoriety,
            uint flags, ushort hits, ushort hitsMax,
            ushort mana, ushort manaMax, ushort stamina, ushort staminaMax,
            bool isPlayer, bool inWarMode, bool isDead, bool isHidden)
        {
            Serial = serial;
            X = x; Y = y; Z = z;
            Graphic = graphic; Hue = hue;
            Direction = direction; Notoriety = notoriety;
            Flags = flags;
            Hits = hits; HitsMax = hitsMax;
            Mana = mana; ManaMax = manaMax;
            Stamina = stamina; StaminaMax = staminaMax;
            IsPlayer = isPlayer; InWarMode = inWarMode;
            IsDead = isDead; IsHidden = isHidden;
            Exists = true;
        }

        public static MobileSnapshot Empty => default;
    }

    /// <summary>
    /// Read-only snapshot of an item entity for UI/plugin consumption.
    /// </summary>
    public readonly struct ItemSnapshot
    {
        public readonly uint Serial;
        public readonly ushort X;
        public readonly ushort Y;
        public readonly sbyte Z;
        public readonly ushort Graphic;
        public readonly ushort Hue;
        public readonly ushort Amount;
        public readonly uint Flags;
        public readonly uint ContainerSerial;
        public readonly byte Layer;
        public readonly bool OnGround;
        public readonly bool IsMulti;
        public readonly bool IsCorpse;
        public readonly bool IsLocked;
        public readonly bool Exists;

        public ItemSnapshot(
            uint serial, ushort x, ushort y, sbyte z,
            ushort graphic, ushort hue, ushort amount, uint flags,
            uint containerSerial, byte layer,
            bool onGround, bool isMulti, bool isCorpse, bool isLocked)
        {
            Serial = serial;
            X = x; Y = y; Z = z;
            Graphic = graphic; Hue = hue;
            Amount = amount; Flags = flags;
            ContainerSerial = containerSerial; Layer = layer;
            OnGround = onGround; IsMulti = isMulti;
            IsCorpse = isCorpse; IsLocked = isLocked;
            Exists = true;
        }

        public static ItemSnapshot Empty => default;
    }

    /// <summary>
    /// Render-ready data bundle for MobileView.Draw().
    /// Replaces legacy Mobile property reads when UseEcsRenderer is active.
    /// All fields are value types — zero-copy read from ECS components.
    /// </summary>
    public readonly struct EcsMobileRenderData
    {
        public readonly bool Exists;

        // Identity / Visual
        public readonly ushort Graphic;
        public readonly ushort Hue;
        public readonly byte AlphaHue;
        public readonly byte Direction;
        public readonly byte Notoriety;

        // Animation
        public readonly byte AnimGroup;
        public readonly byte FrameIndex;
        public readonly byte AnimDirection;
        public readonly ushort FrameCount;

        // Screen position (from RenderExtract)
        public readonly int ScreenX;
        public readonly int ScreenY;

        // World offset (sub-tile interpolation)
        public readonly float OffsetX;
        public readonly float OffsetY;
        public readonly float OffsetZ;

        // Depth sort
        public readonly short PriorityZ;
        public readonly float DepthZ;

        // Vitals
        public readonly ushort Hits;
        public readonly ushort HitsMax;

        // Mount graphic (0 = not mounted, 0xFFFF = boat mount)
        public readonly ushort MountGraphic;

        // State flags (decoded from tags)
        public readonly bool IsHidden;
        public readonly bool IsFemale;
        public readonly bool IsHuman;
        public readonly bool IsGargoyle;
        public readonly bool IsPoisoned;
        public readonly bool IsFlying;
        public readonly bool IsFrozen;
        public readonly bool IsYellowHits;
        public readonly bool IsWarMode;
        public readonly bool IsDead;
        public readonly bool IsPlayer;
        public readonly bool IsMounted;

        public EcsMobileRenderData(
            ushort graphic, ushort hue, byte alphaHue, byte direction, byte notoriety,
            byte animGroup, byte frameIndex, byte animDirection, ushort frameCount,
            int screenX, int screenY,
            float offsetX, float offsetY, float offsetZ,
            short priorityZ, float depthZ,
            ushort hits, ushort hitsMax,
            ushort mountGraphic,
            bool isHidden, bool isFemale, bool isHuman, bool isGargoyle,
            bool isPoisoned, bool isFlying, bool isFrozen, bool isYellowHits,
            bool isWarMode, bool isDead, bool isPlayer, bool isMounted)
        {
            Exists = true;
            Graphic = graphic; Hue = hue; AlphaHue = alphaHue;
            Direction = direction; Notoriety = notoriety;
            AnimGroup = animGroup; FrameIndex = frameIndex;
            AnimDirection = animDirection; FrameCount = frameCount;
            ScreenX = screenX; ScreenY = screenY;
            OffsetX = offsetX; OffsetY = offsetY; OffsetZ = offsetZ;
            PriorityZ = priorityZ; DepthZ = depthZ;
            Hits = hits; HitsMax = hitsMax;
            MountGraphic = mountGraphic;
            IsHidden = isHidden; IsFemale = isFemale;
            IsHuman = isHuman; IsGargoyle = isGargoyle;
            IsPoisoned = isPoisoned; IsFlying = isFlying;
            IsFrozen = isFrozen; IsYellowHits = isYellowHits;
            IsWarMode = isWarMode; IsDead = isDead;
            IsPlayer = isPlayer; IsMounted = isMounted;
        }

        public static EcsMobileRenderData Empty => default;
    }

    /// <summary>
    /// Render-ready data bundle for ItemView.Draw().
    /// Replaces legacy Item property reads when UseEcsRenderer is active.
    /// </summary>
    public readonly struct EcsItemRenderData
    {
        public readonly bool Exists;

        public readonly ushort Graphic;
        public readonly ushort Hue;
        public readonly byte AlphaHue;
        public readonly ushort Amount;

        // Screen position
        public readonly int ScreenX;
        public readonly int ScreenY;

        // World offset
        public readonly float OffsetX;
        public readonly float OffsetY;
        public readonly float OffsetZ;

        // Depth sort
        public readonly short PriorityZ;
        public readonly float DepthZ;

        // Item animation
        public readonly byte FrameIndex;
        public readonly bool IsAnimated;

        // Flags
        public readonly bool IsHidden;
        public readonly bool IsCorpse;
        public readonly bool IsMulti;

        public EcsItemRenderData(
            ushort graphic, ushort hue, byte alphaHue, ushort amount,
            int screenX, int screenY,
            float offsetX, float offsetY, float offsetZ,
            short priorityZ, float depthZ,
            byte frameIndex, bool isAnimated,
            bool isHidden, bool isCorpse, bool isMulti)
        {
            Exists = true;
            Graphic = graphic; Hue = hue; AlphaHue = alphaHue;
            Amount = amount;
            ScreenX = screenX; ScreenY = screenY;
            OffsetX = offsetX; OffsetY = offsetY; OffsetZ = offsetZ;
            PriorityZ = priorityZ; DepthZ = depthZ;
            FrameIndex = frameIndex; IsAnimated = isAnimated;
            IsHidden = isHidden; IsCorpse = isCorpse; IsMulti = isMulti;
        }

        public static EcsItemRenderData Empty => default;
    }

    /// <summary>
    /// Render-ready data bundle for GameEffectView.Draw().
    /// Replaces legacy GameEffect property reads when UseEcsRenderer is active.
    /// </summary>
    public readonly struct EcsEffectRenderData
    {
        public readonly bool Exists;

        public readonly ushort Graphic;
        public readonly ushort AnimationGraphic;
        public readonly ushort Hue;

        // World offset
        public readonly float OffsetX;
        public readonly float OffsetY;
        public readonly float OffsetZ;

        public EcsEffectRenderData(
            ushort graphic, ushort animationGraphic, ushort hue,
            float offsetX, float offsetY, float offsetZ)
        {
            Exists = true;
            Graphic = graphic; AnimationGraphic = animationGraphic; Hue = hue;
            OffsetX = offsetX; OffsetY = offsetY; OffsetZ = offsetZ;
        }

        public static EcsEffectRenderData Empty => default;
    }
}
