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

    /// <summary>Movement speed mode.</summary>
    public record struct SpeedModeComponent(byte SpeedMode);

    // ── Item / Inventory ────────────────────────────────────────────────

    /// <summary>
    /// Container link stored as a serial fallback for items not yet modeled
    /// with Flecs ChildOf relationships. Will be replaced by pure relationships
    /// once the inventory module is fully migrated.
    /// </summary>
    public record struct ContainerLink(uint ContainerSerial);

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
    public record struct EcsLockedFeatureFlags(uint Features);

    /// <summary>Client view range in tiles.</summary>
    public record struct ViewRange(byte Range);

    /// <summary>Current season.</summary>
    public record struct SeasonState(byte Season);

    /// <summary>ECS parity tracking: counts of mirrored entities for validation.</summary>
    public record struct ParityCounters(int MobileCount, int ItemCount);

    // ── Effect Lifecycle ──────────────────────────────────────────────────

    /// <summary>Timed lifetime for effect entities. Duration == 0 means infinite.</summary>
    public record struct EffectLifetime(long Duration, long NextFrameTime, int IntervalMs);

    /// <summary>Links an effect to its source entity serial (for dependency cleanup).</summary>
    public record struct EffectSourceLink(uint SourceSerial);

    /// <summary>Animation playback state for effects (frame cycling).</summary>
    public record struct EffectAnimPlayback(byte AnimIndex, byte FrameCount, ushort AnimationGraphic);

    // ── Pruning ───────────────────────────────────────────────────────────

    /// <summary>Tick of last distance prune check. Singleton throttle.</summary>
    public record struct PruneTimer(long NextPruneTick);

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
    public record struct RenderNameplate(int NameIndex);

    /// <summary>Selection/interaction state flags (hover, selected, targeted).</summary>
    public record struct RenderSelectionFlags(byte Flags);

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
}
