// SPDX-License-Identifier: BSD-2-Clause

using System;
using Flecs.NET.Core;

namespace ClassicUO.ECS.Systems
{
    /// <summary>
    /// RenderExtract phase systems that populate render-ready components
    /// from simulation state. These run every frame in the RenderExtract phase
    /// and produce data consumed by the existing renderer.
    ///
    /// Pipeline:
    ///   1. Clear previous frame's VisibleTag
    ///   2. Compute ScreenPosition (isometric projection)
    ///   3. Tag visible entities (viewport frustum culling)
    ///   4. Extract sprite/animation data for visible entities
    ///   5. Compute depth sort keys for visible entities
    ///   6. Extract light contributions
    /// </summary>
    public static class RenderExtractSystems
    {
        public static void Register(World world)
        {
            RegisterClearVisibility(world);
            RegisterComputeScreenPosition(world);
            RegisterTagVisible(world);
            RegisterExtractSprite(world);
            RegisterExtractMobileAnimation(world);
            RegisterComputeDepthSort(world);
            RegisterExtractLighting(world);
            RegisterExtractHealthBars(world);
            RegisterExtractNameplates(world);
            RegisterExtractOverheadText(world);
            RegisterExtractGhostMode(world);
            RegisterExtractSelection(world);
            RegisterExtractWeather(world);
        }

        // ── 1. Clear previous frame's visibility ─────────────────────

        private static void RegisterClearVisibility(World world)
        {
            world.System("RenderExtract_ClearVisibility")
                .Kind(Phases.RenderExtract)
                .With<VisibleTag>()
                .Run((Iter it) =>
                {
                    while (it.Next())
                    {
                        for (int i = 0; i < it.Count(); i++)
                            it.Entity(i).Remove<VisibleTag>();
                    }
                });
        }

        // ── 2. Compute isometric screen position ─────────────────────

        private static void RegisterComputeScreenPosition(World world)
        {
            world.System<WorldPosition, ViewportState>("RenderExtract_ScreenPosition")
                .Kind(Phases.RenderExtract)
                .Without<PendingRemovalTag>()
                .TermAt(1).Singleton()
                .Each((Entity entity, ref WorldPosition pos, ref ViewportState viewport) =>
                {
                    // Isometric projection matching legacy UpdateRealScreenPosition:
                    //   screenX = (X - Y) * 22 - cameraOffsetX - 22
                    //   screenY = (X + Y) * 22 - (Z * 4) - cameraOffsetY - 22
                    int screenX = ((pos.X - pos.Y) * 22) - viewport.CameraOffsetX - 22;
                    int screenY = ((pos.X + pos.Y) * 22) - (pos.Z * 4) - viewport.CameraOffsetY - 22;

                    // Apply WorldOffset if present
                    if (entity.Has<WorldOffset>())
                    {
                        ref readonly var offset = ref entity.Get<WorldOffset>();
                        screenX += (int)offset.OffsetX;
                        screenY += (int)(offset.OffsetY - offset.OffsetZ);
                    }

                    entity.Set(new ScreenPosition(screenX, screenY));
                });
        }

        // ── 3. Tag entities within viewport ──────────────────────────

        private static void RegisterTagVisible(World world)
        {
            world.System<ScreenPosition, WorldPosition, ViewportState>("RenderExtract_TagVisible")
                .Kind(Phases.RenderExtract)
                .Without<PendingRemovalTag>()
                .Without<VisibleTag>()
                .TermAt(2).Singleton()
                .Each((Entity entity, ref ScreenPosition screen, ref WorldPosition pos, ref ViewportState viewport) =>
                {
                    // Tile-based range check (coarse culling)
                    if (pos.X < viewport.MinTileX || pos.X > viewport.MaxTileX ||
                        pos.Y < viewport.MinTileY || pos.Y > viewport.MaxTileY)
                        return;

                    // Screen-space pixel culling (fine culling)
                    if (screen.X < viewport.MinPixelX || screen.X > viewport.MaxPixelX ||
                        screen.Y < viewport.MinPixelY || screen.Y > viewport.MaxPixelY)
                        return;

                    entity.Add<VisibleTag>();
                });
        }

        // ── 4. Extract sprite data ───────────────────────────────────

        private static void RegisterExtractSprite(World world)
        {
            world.System<GraphicComponent, HueComponent>("RenderExtract_Sprite")
                .Kind(Phases.RenderExtract)
                .With<VisibleTag>()
                .Each((Entity entity, ref GraphicComponent graphic, ref HueComponent hue) =>
                {
                    // Default alpha: fully opaque (0xFF)
                    byte alpha = 0xFF;

                    // Hidden entities get zero alpha
                    if (entity.Has<HiddenTag>())
                        alpha = 0;

                    entity.Set(new RenderSprite(graphic.Graphic, hue.Hue, alpha));
                });
        }

        // ── 5. Extract mobile animation frame ────────────────────────

        private static void RegisterExtractMobileAnimation(World world)
        {
            world.System<AnimationState, DirectionComponent>("RenderExtract_MobileAnimation")
                .Kind(Phases.RenderExtract)
                .With<MobileTag>()
                .With<VisibleTag>()
                .Each((Entity entity, ref AnimationState anim, ref DirectionComponent dir) =>
                {
                    entity.Set(new RenderAnimationFrame(
                        anim.Group,
                        anim.FrameIndex,
                        dir.Direction,
                        anim.FrameCount
                    ));
                });
        }

        // ── 6. Compute depth sort key ────────────────────────────────

        private static void RegisterComputeDepthSort(World world)
        {
            world.System<WorldPosition>("RenderExtract_DepthSort")
                .Kind(Phases.RenderExtract)
                .With<VisibleTag>()
                .Each((Entity entity, ref WorldPosition pos) =>
                {
                    // PriorityZ defaults to Z coordinate
                    short priorityZ = pos.Z;

                    // Adjust for items (items on ground get slight Z offset)
                    if (entity.Has<ItemTag>())
                        priorityZ += 1;

                    // Depth calculation matching legacy CalculateDepthZ:
                    //   depthZ = (X + Y) + (127 + PriorityZ) * 0.01f
                    int x = pos.X;
                    int y = pos.Y;

                    // Offset-based direction adjustment (matching legacy View.cs)
                    if (entity.Has<WorldOffset>())
                    {
                        ref readonly var offset = ref entity.Get<WorldOffset>();
                        if (offset.OffsetX > 0 && offset.OffsetY > 0)
                        {
                            priorityZ += (short)Math.Max(0, (int)offset.OffsetZ);
                            x++;
                        }
                        else if (offset.OffsetX < 0 && offset.OffsetY > 0)
                        {
                            priorityZ += (short)Math.Max(0, (int)offset.OffsetZ);
                            y++;
                        }
                    }

                    float depthZ = (x + y) + (127 + priorityZ) * 0.01f;

                    entity.Set(new RenderLayerKey(priorityZ, depthZ));
                });
        }

        // ── 7. Extract light contributions ───────────────────────────

        private static void RegisterExtractLighting(World world)
        {
            world.System<LightState, ScreenPosition>("RenderExtract_Lighting")
                .Kind(Phases.RenderExtract)
                .With<VisibleTag>()
                .Each((Entity entity, ref LightState light, ref ScreenPosition screen) =>
                {
                    if (light.LightID == 0 && light.LightLevel == 0)
                        return;

                    entity.Set(new RenderLightContribution(
                        light.LightID,
                        0, // Default color (no override)
                        screen.X,
                        screen.Y
                    ));
                });
        }

        // ── 8. Extract health bars ──────────────────────────────────

        private static void RegisterExtractHealthBars(World world)
        {
            world.System<Vitals, NotorietyComponent>("RenderExtract_HealthBars")
                .Kind(Phases.RenderExtract)
                .With<MobileTag>()
                .With<VisibleTag>()
                .Each((Entity entity, ref Vitals vitals, ref NotorietyComponent notoriety) =>
                {
                    float hitsPercent = vitals.HitsMax > 0
                        ? (float)vitals.Hits / vitals.HitsMax
                        : 0f;

                    bool isPoisoned = false;
                    bool isYellowBar = false;
                    if (entity.Has<HealthBarFlags>())
                    {
                        var hbf = entity.Get<HealthBarFlags>();
                        isPoisoned = hbf.Poisoned;
                        isYellowBar = hbf.YellowBar;
                    }

                    entity.Set(new RenderHealthBar
                    {
                        HitsPercent = hitsPercent,
                        Notoriety = notoriety.Notoriety,
                        IsPoisoned = isPoisoned,
                        IsYellowBar = isYellowBar,
                        IsPlayer = entity.Has<PlayerTag>(),
                        ShowBar = true
                    });
                });
        }

        // ── 9. Extract nameplates ───────────────────────────────────

        private static void RegisterExtractNameplates(World world)
        {
            world.System<NameIndex, SerialComponent>("RenderExtract_Nameplates")
                .Kind(Phases.RenderExtract)
                .With<VisibleTag>()
                .Each((Entity entity, ref NameIndex nameIdx, ref SerialComponent serial) =>
                {
                    byte notoriety = entity.Has<NotorietyComponent>()
                        ? entity.Get<NotorietyComponent>().Notoriety : (byte)0;

                    entity.Set(new RenderNameplate
                    {
                        NameIndex = nameIdx.Index,
                        Serial = serial.Serial,
                        Notoriety = notoriety,
                        IsPlayer = entity.Has<PlayerTag>()
                    });
                });
        }

        // ── 10. Extract overhead text ────────────────────────────────

        private static void RegisterExtractOverheadText(World world)
        {
            world.System<OverheadText, FrameTiming>("RenderExtract_OverheadText")
                .Kind(Phases.RenderExtract)
                .With<OverheadTextTag>()
                .TermAt(1).Singleton()
                .Each((Entity entity, ref OverheadText text, ref FrameTiming ft) =>
                {
                    long elapsed = (long)ft.Ticks - text.StartTick;
                    long remaining = text.DurationMs - elapsed;
                    if (remaining <= 0)
                        return; // expired, will be cleaned up by SocialSystems

                    // Try to get parent entity's screen position for text placement.
                    int screenX = 0, screenY = 0;
                    Entity source = SerialRegistry.FindBySerial(text.SourceSerial);
                    if (source != 0 && source.IsAlive() && source.Has<ScreenPosition>())
                    {
                        var sp = source.Get<ScreenPosition>();
                        screenX = sp.X;
                        screenY = sp.Y - 20; // above entity
                    }

                    entity.Set(new RenderTextOverlay
                    {
                        TextIndex = text.TextIndex,
                        Hue = text.Hue,
                        Type = text.Type,
                        ScreenX = screenX,
                        ScreenY = screenY,
                        RemainingMs = remaining
                    });
                });
        }

        // ── 11. Ghost mode rendering ──────────────────────────────────

        private static void RegisterExtractGhostMode(World world)
        {
            world.System<RenderSprite>("RenderExtract_GhostMode")
                .Kind(Phases.RenderExtract)
                .With<DeadTag>()
                .With<MobileTag>()
                .With<VisibleTag>()
                .Each((Entity entity, ref RenderSprite sprite) =>
                {
                    // Semi-transparent ghost (50% alpha = 0x80)
                    sprite = new RenderSprite(sprite.Graphic, sprite.Hue, 0x80);
                });
        }

        // ── 12. Selection highlight ──────────────────────────────────

        private static void RegisterExtractSelection(World world)
        {
            // MouseOver = flag 0x01, Selected = flag 0x02, Targeted = flag 0x04
            world.System("RenderExtract_Selection")
                .Kind(Phases.RenderExtract)
                .With<VisibleTag>()
                .Run((Iter it) =>
                {
                    while (it.Next())
                    {
                        for (int i = 0; i < it.Count(); i++)
                        {
                            var e = it.Entity(i);
                            byte flags = 0;

                            if (e.Has<MouseOverTag>())
                                flags |= 0x01;
                            if (e.Has<SelectedTag>())
                                flags |= 0x02;

                            if (flags != 0)
                                e.Set(new RenderSelectionFlags(flags));
                        }
                    }
                });
        }

        // ── 13. Weather render data ─────────────────────────────────

        private static void RegisterExtractWeather(World world)
        {
            world.System<WeatherState, WeatherRenderData>("RenderExtract_Weather")
                .Kind(Phases.RenderExtract)
                .TermAt(0).Singleton()
                .TermAt(1).Singleton()
                .Run((Iter it) =>
                {
                    while (it.Next())
                    {
                        var w = it.World();
                        ref readonly var weather = ref w.Get<WeatherState>();
                        ref var renderData = ref w.GetMut<WeatherRenderData>();
                        renderData = new WeatherRenderData
                        {
                            Type = weather.Type,
                            Count = weather.Count,
                            Temperature = weather.Temperature,
                            Active = weather.Type != 0xFE && weather.Count > 0
                        };
                    }
                });
        }
    }
}
