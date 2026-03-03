// SPDX-License-Identifier: BSD-2-Clause

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ClassicUO.ECS
{
    // ── Identity ────────────────────────────────────────────────────────

    /// <summary>UO serial number. Primary lookup key for legacy parity.</summary>
    public record struct SerialComponent(uint Serial);

    /// <summary>Optional human-readable name stored as an index into an external string table.</summary>
    public record struct NameIndex(int Index);

    // ── Spatial / Movement ──────────────────────────────────────────────

    /// <summary>Tile-aligned world position.</summary>
    public record struct WorldPosition(ushort X, ushort Y, sbyte Z);

    /// <summary>Sub-tile render offset for smooth animation interpolation.</summary>
    public record struct WorldOffset(float OffsetX, float OffsetY, float OffsetZ);

    /// <summary>Facing direction (0-7). Matches the UO Direction enum values.</summary>
    public record struct DirectionComponent(byte Direction);

    /// <summary>A single movement step in a mobile's step queue.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MobileStep
    {
        public int X;
        public int Y;
        public sbyte Z;
        public byte Direction;
        public byte Run; // 0 = walk, 1 = run
    }

    /// <summary>
    /// Fixed-capacity ring buffer holding up to 5 queued movement steps.
    /// Stored inline as a blittable component (no managed allocations).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct StepBuffer
    {
        public const int MAX_STEPS = 5;

        private MobileStep _s0, _s1, _s2, _s3, _s4;
        public int Head;
        public int Count;

        public readonly bool IsFull => Count >= MAX_STEPS;
        public readonly bool IsEmpty => Count == 0;

        public readonly MobileStep GetStep(int ringIndex) => ringIndex switch
        {
            0 => _s0, 1 => _s1, 2 => _s2, 3 => _s3, 4 => _s4,
            _ => default
        };

        private void SetStep(int ringIndex, MobileStep step)
        {
            switch (ringIndex)
            {
                case 0: _s0 = step; break;
                case 1: _s1 = step; break;
                case 2: _s2 = step; break;
                case 3: _s3 = step; break;
                case 4: _s4 = step; break;
            }
        }

        public readonly MobileStep Front() => GetStep(Head);

        public readonly MobileStep Back() => GetStep((Head + Count - 1 + MAX_STEPS) % MAX_STEPS);

        public void Enqueue(MobileStep step)
        {
            if (Count >= MAX_STEPS) return;
            int idx = (Head + Count) % MAX_STEPS;
            SetStep(idx, step);
            Count++;
        }

        public void Dequeue()
        {
            if (Count == 0) return;
            Head = (Head + 1) % MAX_STEPS;
            Count--;
        }

        public void Clear()
        {
            Head = 0;
            Count = 0;
        }
    }

    /// <summary>Movement timing state for step interpolation.</summary>
    public record struct MovementTiming(long LastStepTime);

    // ── Visual / Animation ──────────────────────────────────────────────

    /// <summary>Primary sprite graphic ID.</summary>
    public record struct GraphicComponent(ushort Graphic);

    /// <summary>Color tint applied to the graphic.</summary>
    public record struct HueComponent(ushort Hue);

    /// <summary>Animation playback state for a mobile or effect.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct AnimationState
    {
        public byte Group;
        public byte FrameIndex;
        public byte FrameCount;
        public byte Interval;
        public ushort RepeatMode;
        public bool Repeat;
        public bool Forward;
        public bool FromServer;
        public long LastChangeTime;
    }

    /// <summary>Light emission properties.</summary>
    public record struct LightState(byte LightID, byte LightLevel);

    /// <summary>Idle animation timing/metadata for mobiles.</summary>
    public record struct IdleAnimationState(long NextIdleCheck, byte IdleAction);

    /// <summary>Item-specific animation (animated statics, multi-frame items).</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ItemAnimationState
    {
        public byte FrameIndex;
        public byte FrameCount;
        public long NextFrameTime;
        public int IntervalMs;
        public bool IsAnimated;
    }

    // ── Status / Combat ─────────────────────────────────────────────────

    /// <summary>Vital statistics for mobiles.</summary>
    public record struct Vitals(
        ushort Hits,
        ushort HitsMax,
        ushort Mana,
        ushort ManaMax,
        ushort Stamina,
        ushort StaminaMax
    );

    /// <summary>Entity state flags (Hidden, Frozen, WarMode, etc.).</summary>
    public record struct FlagsComponent(uint Flags);

    /// <summary>PK/faction notoriety level.</summary>
    public record struct NotorietyComponent(byte Notoriety);

    /// <summary>Mobile race.</summary>
    public record struct RaceComponent(byte Race);

    /// <summary>Current attack target serial. 0 = no target.</summary>
    public record struct AttackTarget(uint TargetSerial);

    // ── Player Stats / Skills ────────────────────────────────────────────

    /// <summary>Extended player stats from 0x11 CharacterStatus.</summary>
    public record struct PlayerStats(
        ushort Strength, ushort Dexterity, ushort Intelligence,
        uint Gold, ushort Weight, ushort WeightMax,
        ushort PhysResist, ushort FireResist, ushort ColdResist,
        ushort PoisonResist, ushort EnergyResist,
        short Luck, ushort DamageMin, ushort DamageMax,
        uint TithingPoints, ushort StatsCap,
        byte Followers, byte FollowersMax,
        // Type >= 6 extended combat stats
        short MaxPhysResist, short MaxFireResist, short MaxColdResist,
        short MaxPoisonResist, short MaxEnergyResist,
        short DefenseChanceInc, short MaxDefenseChanceInc,
        short HitChanceInc, short SwingSpeedInc, short DamageInc,
        short LowerReagentCost, short SpellDamageInc,
        short FasterCastRecovery, short FasterCasting, short LowerManaCost
    );

    /// <summary>Stat locks from 0xBF sub-0x19.</summary>
    public record struct StatLocks(byte StrLock, byte DexLock, byte IntLock);

    /// <summary>A single skill entry.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct EcsSkillEntry
    {
        public ushort Value;  // current * 10
        public ushort Base;   // base * 10
        public ushort Cap;    // cap * 10
        public byte Lock;     // 0=Up, 1=Down, 2=Locked
    }

    /// <summary>
    /// Fixed-capacity inline buffer for up to 60 skill entries.
    /// UO has 58 skills; 60 provides headroom.
    /// Uses InlineArray for blittable Flecs compatibility.
    /// </summary>
    [InlineArray(60)]
    public struct SkillsBuffer
    {
        private EcsSkillEntry _element0;
    }

    /// <summary>Player-only skills component wrapping the inline buffer.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SkillsComponent
    {
        public SkillsBuffer Skills;
        public byte SkillCount; // actual number of skills received
    }

    /// <summary>OPL (Object Property List) revision tracking.</summary>
    public record struct ObjectProperties(uint Revision, bool NeedsRefresh);

    /// <summary>Overhead damage display state. Shows floating damage number.</summary>
    public record struct OverheadDamage(ushort Amount, long StartTick);

    /// <summary>
    /// Targeting cursor state. Singleton — tracks whether targeting is active
    /// and what kind of target is expected.
    /// CursorTarget: 0=None, 1=Object, 2=Position, 3=MultiPlacement
    /// TargetType: 0=Neutral, 1=Harmful, 2=Beneficial, 3=Cancel
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TargetingState
    {
        public byte CursorTarget;
        public byte TargetType;
        public uint CursorID;
        public ushort MultiGraphic;
        public bool IsTargeting;
    }

    /// <summary>
    /// Last known target info for re-targeting. Singleton.
    /// </summary>
    public record struct LastTargetInfo(uint Serial, ushort Graphic, ushort X, ushort Y, sbyte Z, bool IsEntity);

    // ── Multi Placement ───────────────────────────────────────────────────

    /// <summary>
    /// Multi-placement preview state. Singleton — used during house/boat multi placement targeting.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MultiPlacement
    {
        public ushort Graphic;
        public ushort Hue;
        public ushort X, Y;
        public sbyte Z;
        public bool IsValid;
    }

    // ── Delayed Click ────────────────────────────────────────────────────

    /// <summary>
    /// Singleton tracking delayed single-click state. After 500ms with no double-click,
    /// a CmdSingleClick is emitted. If double-click occurs within 500ms, it cancels
    /// the pending click and emits CmdUseObject instead.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DelayedClickState
    {
        public uint Serial;
        public long ClickTime;
        public bool Pending;
    }

    // ── Use Item Queue ───────────────────────────────────────────────────

    /// <summary>
    /// Singleton for queued item use with anti-cheat cooldown.
    /// System emits CmdUseObject when cooldown expires.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct UseItemQueueState
    {
        public uint Serial;
        public long NextUseTime;
        public bool HasPending;
    }

    // ── Macro Bridge ─────────────────────────────────────────────────────

    /// <summary>
    /// Bridge singleton exposing macro playback state to ECS queries.
    /// Actual macro logic remains in legacy MacroManager.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct MacroState
    {
        public bool IsPlaying;
        public bool IsRecording;
        public int MacroNameIndex;  // string table index, -1 if none
    }

    // ── Aura ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Render-only singleton tracking aura ring display around player/target.
    /// </summary>
    public record struct AuraState(bool Enabled, ushort Hue);

    // ── Party ────────────────────────────────────────────────────────────

    /// <summary>Party membership singleton tracking the local player's party state.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct PartyState
    {
        public uint LeaderSerial;
        public byte MemberCount;
        public bool IsInParty;
    }

    /// <summary>Overhead text entity component. Child of the speaking entity or standalone.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct OverheadText
    {
        public uint SourceSerial;
        public ushort Hue;
        public byte Type;           // 0=speech, 1=system, 2=emote, 6=label
        public long StartTick;
        public ushort DurationMs;
        public int TextIndex;       // index into EcsRuntimeHost string table
    }

    // ── Health Bar / Buff ──────────────────────────────────────────────

    /// <summary>SA-era healthbar flags (poison green bar, invulnerable yellow bar).</summary>
    public record struct HealthBarFlags(bool Poisoned, bool YellowBar);

    /// <summary>
    /// Single buff/debuff entry. Stored on child entities of the buffed mobile.
    /// Duration=0 means permanent. Expiry = StartTick + Duration*1000.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct BuffEntry
    {
        public ushort IconId;
        public ushort Duration;
        public long StartTick;
        public uint TitleCliloc;
        public uint DescriptionCliloc;
    }

    // ── Death / Corpse ──────────────────────────────────────────────────

    /// <summary>Links a corpse entity to its original mobile serial.</summary>
    public record struct CorpseOwnerLink(uint OwnerSerial);

    // ── Rollout / Diagnostics Singletons ──────────────────────────────

    /// <summary>
    /// Granular subsystem toggle flags for staged rollout.
    /// Each flag enables/disables a specific ECS subsystem.
    /// When disabled, the corresponding systems skip execution.
    /// </summary>
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct SubsystemFlags
    {
        public bool NetworkEnabled;
        public bool MovementEnabled;
        public bool CombatEnabled;
        public bool InventoryEnabled;
        public bool EffectsEnabled;
        public bool RenderExtractEnabled;
        public bool UiInputEnabled;
        public bool LifecycleEnabled;

        public static SubsystemFlags AllEnabled => new()
        {
            NetworkEnabled = true,
            MovementEnabled = true,
            CombatEnabled = true,
            InventoryEnabled = true,
            EffectsEnabled = true,
            RenderExtractEnabled = true,
            UiInputEnabled = true,
            LifecycleEnabled = true
        };
    }

    /// <summary>
    /// Cutover phase flags for phased migration from legacy to ECS-only.
    /// Each flag controls whether ECS or legacy code is authoritative for that subsystem.
    /// All default to false (legacy authoritative). Set to true to switch to ECS.
    /// Runtime-toggleable for safe rollback.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct CutoverFlags
    {
        /// <summary>Phase 1: Renderer reads entity data from ECS instead of legacy World.Mobiles/Items.</summary>
        public bool UseEcsRenderer;
        /// <summary>Phase 2: UI gumps read from ECS bridge APIs instead of legacy entity properties.</summary>
        public bool UseEcsUiData;
        /// <summary>Phase 3: Packet handlers only write ECS commands, no legacy state mutations.</summary>
        public bool EcsOnlyNetwork;
        /// <summary>Phase 4: Legacy game state removed. ECS is sole authority.</summary>
        public bool LegacyRetired;
    }

    /// <summary>
    /// Per-frame diagnostics data collected during PostFrame.
    /// Used for performance gating and parity dashboards.
    /// </summary>
    public record struct FrameDiagnostics(
        int EntityCount,
        int MobileCount,
        int ItemCount,
        int EffectCount,
        int CommandsProcessed,
        float FrameTimeMs
    );

    /// <summary>
    /// Parity checkpoint snapshot for replay comparison.
    /// Captures key state metrics at a given tick.
    /// </summary>
    public record struct ParityCheckpoint(
        uint Tick,
        int MobileCount,
        int ItemCount,
        int EffectCount,
        ushort PlayerX,
        ushort PlayerY,
        sbyte PlayerZ,
        ushort PlayerHits,
        ushort PlayerMana,
        ushort PlayerStamina
    );

    /// <summary>Movement speed mode.</summary>
    public record struct SpeedModeComponent(byte SpeedMode);

    // ── Item / Inventory ────────────────────────────────────────────────

    /// <summary>
    /// Container link stored as a serial fallback for items not yet modeled
    /// with Flecs ChildOf relationships. Will be replaced by pure relationships
    /// once the inventory module is fully migrated.
    /// </summary>
    public record struct ContainerLink(uint ContainerSerial);

    /// <summary>Position within a container gump (for ContainerGump/GridLootGump).</summary>
    public record struct ContainerPosition(ushort X, ushort Y, ushort GridIndex);

    /// <summary>Equipment layer slot.</summary>
    public record struct LayerComponent(byte Layer);

    /// <summary>Stack amount for stackable items.</summary>
    public record struct AmountComponent(ushort Amount);

    /// <summary>Vendor price.</summary>
    public record struct PriceComponent(uint Price);

    // ── Session / Global Singletons ─────────────────────────────────────

    /// <summary>Per-frame timing data. Set once per frame before pipeline runs.</summary>
    public record struct FrameTiming(uint Ticks, float DeltaSeconds);

    /// <summary>Current map index.</summary>
    public record struct MapIndex(byte Index);

    /// <summary>Server-reported client feature bit mask.</summary>
    public record struct ClientFeatureFlags(uint Features);

    /// <summary>Server-reported locked feature bit mask.</summary>
    public record struct EcsLockedFeatureFlags(ulong Features);

    /// <summary>Client view range in tiles.</summary>
    public record struct ViewRange(byte Range);

    /// <summary>Current season.</summary>
    public record struct SeasonState(byte Season);

    /// <summary>ECS parity tracking: counts of mirrored entities for validation.</summary>
    public record struct ParityCounters(int MobileCount, int ItemCount);

    // ── Effect Movement ──────────────────────────────────────────────────

    /// <summary>Target position for a moving effect. TargetSerial=0 means ground target.</summary>
    public record struct EffectTarget(ushort X, ushort Y, sbyte Z, uint TargetSerial);

    /// <summary>Interpolation state for a moving effect. Progress goes 0.0 → 1.0.</summary>
    public record struct EffectMovement(byte Speed, float Progress);

    // ── Item Hold / Cursor State ────────────────────────────────────────

    /// <summary>
    /// Singleton tracking cursor-held item state.
    /// Serial=0 means nothing held.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ItemHoldState
    {
        public uint Serial;
        public ushort Graphic;
        public ushort Hue;
        public ushort Amount;
        public bool Enabled;
        public bool Dropped;
        public ushort DropX, DropY;
        public sbyte DropZ;
        public uint DropContainer;
    }

    // ── Effect Lifecycle ──────────────────────────────────────────────────

    /// <summary>Timed lifetime for effect entities. Duration == 0 means infinite.</summary>
    public record struct EffectLifetime(long Duration, long NextFrameTime, int IntervalMs);

    /// <summary>Links an effect to its source entity serial (for dependency cleanup).</summary>
    public record struct EffectSourceLink(uint SourceSerial);

    /// <summary>Animation playback state for effects (frame cycling).</summary>
    public record struct EffectAnimPlayback(byte AnimIndex, byte FrameCount, ushort AnimationGraphic);

    // ── Deferred Removal ──────────────────────────────────────────────────

    /// <summary>Delay before actual destruction. Decremented each frame.</summary>
    public record struct PendingRemovalDelay(byte FramesRemaining);

    /// <summary>Tag indicating entity removal should trigger UI bridge invalidation.</summary>
    public record struct EntityRemovedEvent(uint Serial);

    /// <summary>Tag requesting abilities recalculation on a mobile after equipment change.</summary>
    public struct RecalcAbilitiesTag;

    /// <summary>Current combat abilities state. Singleton, set from legacy UpdateAbilities().</summary>
    public record struct AbilitiesState(ushort Primary, ushort Secondary);

    // ── Pruning ───────────────────────────────────────────────────────────

    /// <summary>Tick of last distance prune check. Singleton throttle.</summary>
    public record struct PruneTimer(long NextPruneTick);

    // ── Boat / Multi ──────────────────────────────────────────────────────

    /// <summary>Boat movement state for multi entities.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct BoatState
    {
        public byte Speed;
        public byte Direction;
        public byte MovingDirection;
        public bool IsMoving;
    }

    /// <summary>Links an entity to the boat it is riding on.</summary>
    public record struct BoatLink(uint BoatSerial);

    // ── Weather ──────────────────────────────────────────────────────────

    /// <summary>Weather state singleton. Type: 0=rain, 1=fierce storm, 2=snow, 3=storm brewing, 0xFE=none.</summary>
    public record struct WeatherState(byte Type, byte Count, byte Temperature);

    // ── Render Extraction ─────────────────────────────────────────────────

    /// <summary>
    /// Isometric screen position computed from world position.
    /// Formula: screenX = (X - Y) * 22 - cameraOffsetX - 22
    ///          screenY = (X + Y) * 22 - (Z * 4) - cameraOffsetY - 22
    /// </summary>
    public record struct ScreenPosition(int X, int Y);

    /// <summary>Render-ready sprite data. Populated during RenderExtract phase.</summary>
    public record struct RenderSprite(ushort Graphic, ushort Hue, byte AlphaHue);

    /// <summary>Animation frame data for mobile rendering.</summary>
    public record struct RenderAnimationFrame(
        byte AnimGroup,
        byte FrameIndex,
        byte Direction,
        ushort FrameCount
    );

    /// <summary>
    /// Depth sorting key for isometric draw ordering.
    /// DepthZ = (X + Y) + (127 + PriorityZ) * 0.01f
    /// </summary>
    public record struct RenderLayerKey(short PriorityZ, float DepthZ);

    /// <summary>Light source contribution for the lighting pass.</summary>
    public record struct RenderLightContribution(byte LightID, ushort Color, int DrawX, int DrawY);

    /// <summary>Nameplate display data referencing external name string table.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RenderNameplate
    {
        public int NameIndex;
        public uint Serial;
        public byte Notoriety;
        public bool IsPlayer;
    }

    /// <summary>Selection/interaction state flags (hover, selected, targeted).</summary>
    public record struct RenderSelectionFlags(byte Flags);

    // ── Render Health Bar ──────────────────────────────────────────────────

    /// <summary>Render-ready health bar data for visible mobiles.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RenderHealthBar
    {
        public float HitsPercent;
        public byte Notoriety;
        public bool IsPoisoned;
        public bool IsYellowBar;
        public bool IsPlayer;
        public bool ShowBar;
    }

    // ── Render Text Overlay ──────────────────────────────────────────────

    /// <summary>Render-ready overhead text data for visible text entities.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RenderTextOverlay
    {
        public int TextIndex;       // string table index
        public ushort Hue;
        public byte Type;           // 0=speech, 1=system, 2=emote, 6=label
        public int ScreenX, ScreenY;
        public long RemainingMs;
    }

    // ── Tooltip ──────────────────────────────────────────────────────────

    /// <summary>Tooltip revision tracking on an entity.</summary>
    public record struct TooltipRequest(uint Revision, bool NeedsRefresh);

    // ── Weather Render Data ──────────────────────────────────────────────

    /// <summary>
    /// Weather rendering data singleton. Populated from WeatherState.
    /// Particle simulation remains in legacy renderer.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WeatherRenderData
    {
        public byte Type;
        public byte Count;
        public byte Temperature;
        public bool Active;
    }

    // ── UI Extract Singletons ────────────────────────────────────────────

    /// <summary>
    /// Buff bar snapshot singleton. Populated during UiExtract from player buff children.
    /// Fixed-capacity inline array for up to 32 buff icons.
    /// </summary>
    [InlineArray(32)]
    public struct BuffIconBuffer
    {
        private BuffEntry _element0;
    }

    /// <summary>Buff bar snapshot for UI consumption.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct BuffBarSnapshot
    {
        public BuffIconBuffer Icons;
        public byte Count;
    }

    /// <summary>
    /// Full status gump snapshot singleton. Populated during UiExtract from player entity.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct StatusSnapshot
    {
        public uint Serial;
        public Vitals Vitals;
        public PlayerStats Stats;
        public StatLocks Locks;
        public byte Race;
        public bool IsFemale;
        public bool IsValid;
    }

    // ── Render Singletons ─────────────────────────────────────────────────

    /// <summary>
    /// Global lighting state. Mirrors legacy IsometricLight.
    /// Light levels: 0 = brightest, 32 = darkest.
    /// IsometricLevel = max(Personal, 32-Overall) * 0.03125f
    /// </summary>
    public record struct LightingState(
        int Overall,
        int Personal,
        int RealOverall,
        int RealPersonal,
        float IsometricLevel
    );

    /// <summary>
    /// Camera/viewport state for the current frame.
    /// Set by the scene before RenderExtract systems run.
    /// </summary>
    public record struct ViewportState(
        int MinTileX, int MinTileY,
        int MaxTileX, int MaxTileY,
        int MinPixelX, int MinPixelY,
        int MaxPixelX, int MaxPixelY,
        int CameraOffsetX, int CameraOffsetY
    );

    // ── World Map Tracking ──────────────────────────────────────────────

    /// <summary>Single entry for world-map entity tracking (party/ally dots).</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WorldMapUpdateEntry
    {
        public uint Serial;
        public int X;
        public int Y;
        public int HpPercent;
        public bool IsGuild;
        public int NameIndex;   // -1 = no name
    }

    /// <summary>
    /// Singleton buffer of world-map entity updates per frame.
    /// Flushed by GameScene to WMapManager. Uses a simple count + inline array.
    /// </summary>
    public struct WorldMapUpdateList
    {
        public int Count;
        private WorldMapUpdateEntry _e0, _e1, _e2, _e3, _e4, _e5, _e6, _e7,
            _e8, _e9, _e10, _e11, _e12, _e13, _e14, _e15,
            _e16, _e17, _e18, _e19, _e20, _e21, _e22, _e23,
            _e24, _e25, _e26, _e27, _e28, _e29, _e30, _e31;

        public void Add(WorldMapUpdateEntry entry)
        {
            if (Count >= 32) return;
            switch (Count)
            {
                case 0: _e0 = entry; break;   case 1: _e1 = entry; break;
                case 2: _e2 = entry; break;   case 3: _e3 = entry; break;
                case 4: _e4 = entry; break;   case 5: _e5 = entry; break;
                case 6: _e6 = entry; break;   case 7: _e7 = entry; break;
                case 8: _e8 = entry; break;   case 9: _e9 = entry; break;
                case 10: _e10 = entry; break;  case 11: _e11 = entry; break;
                case 12: _e12 = entry; break;  case 13: _e13 = entry; break;
                case 14: _e14 = entry; break;  case 15: _e15 = entry; break;
                case 16: _e16 = entry; break;  case 17: _e17 = entry; break;
                case 18: _e18 = entry; break;  case 19: _e19 = entry; break;
                case 20: _e20 = entry; break;  case 21: _e21 = entry; break;
                case 22: _e22 = entry; break;  case 23: _e23 = entry; break;
                case 24: _e24 = entry; break;  case 25: _e25 = entry; break;
                case 26: _e26 = entry; break;  case 27: _e27 = entry; break;
                case 28: _e28 = entry; break;  case 29: _e29 = entry; break;
                case 30: _e30 = entry; break;  case 31: _e31 = entry; break;
            }
            Count++;
        }

        public WorldMapUpdateEntry Get(int index)
        {
            return index switch
            {
                0 => _e0, 1 => _e1, 2 => _e2, 3 => _e3,
                4 => _e4, 5 => _e5, 6 => _e6, 7 => _e7,
                8 => _e8, 9 => _e9, 10 => _e10, 11 => _e11,
                12 => _e12, 13 => _e13, 14 => _e14, 15 => _e15,
                16 => _e16, 17 => _e17, 18 => _e18, 19 => _e19,
                20 => _e20, 21 => _e21, 22 => _e22, 23 => _e23,
                24 => _e24, 25 => _e25, 26 => _e26, 27 => _e27,
                28 => _e28, 29 => _e29, 30 => _e30, 31 => _e31,
                _ => default
            };
        }
    }
}
