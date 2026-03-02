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
            world.System<WorldPosition>("RenderExtract_ScreenPosition")
                .Kind(Phases.RenderExtract)
                .Without<PendingRemovalTag>()
                .Each((Entity entity, ref WorldPosition pos) =>
                {
                    ref readonly var viewport = ref entity.CsWorld().Get<ViewportState>();

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
            world.System<ScreenPosition, WorldPosition>("RenderExtract_TagVisible")
                .Kind(Phases.RenderExtract)
                .Without<PendingRemovalTag>()
                .Without<VisibleTag>()
                .Each((Entity entity, ref ScreenPosition screen, ref WorldPosition pos) =>
                {
                    ref readonly var viewport = ref entity.CsWorld().Get<ViewportState>();

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
    }
}
