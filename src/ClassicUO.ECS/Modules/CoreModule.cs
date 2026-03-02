// SPDX-License-Identifier: BSD-2-Clause

using Flecs.NET.Core;

namespace ClassicUO.ECS.Modules
{
    /// <summary>
    /// Core module: identity components, tags, relationships, singleton config,
    /// and world bootstrap. This module is always imported first.
    /// </summary>
    public struct CoreModule : IFlecsModule
    {
        public void InitModule(World world)
        {
            world.Module<CoreModule>();

            // ── Identity components ─────────────────────────────────
            world.Component<SerialComponent>();
            world.Component<NameIndex>();

            // ── Spatial / movement ──────────────────────────────────
            world.Component<WorldPosition>();
            world.Component<WorldOffset>();
            world.Component<DirectionComponent>();
            world.Component<MobileStep>();
            world.Component<StepBuffer>();
            world.Component<MovementTiming>();

            // ── Visual / animation ──────────────────────────────────
            world.Component<GraphicComponent>();
            world.Component<HueComponent>();
            world.Component<AnimationState>();
            world.Component<LightState>();

            // ── Status / combat ─────────────────────────────────────
            world.Component<Vitals>();
            world.Component<FlagsComponent>();
            world.Component<NotorietyComponent>();
            world.Component<RaceComponent>();
            world.Component<SpeedModeComponent>();

            // ── Inventory ───────────────────────────────────────────
            world.Component<ContainerLink>();
            world.Component<LayerComponent>();
            world.Component<AmountComponent>();
            world.Component<PriceComponent>();

            // ── Tags ────────────────────────────────────────────────
            world.Component<MobileTag>();
            world.Component<ItemTag>();
            world.Component<EffectTag>();
            world.Component<StaticTag>();
            world.Component<PlayerTag>();
            world.Component<MultiTag>();
            world.Component<CorpseTag>();
            world.Component<DeadTag>();
            world.Component<WarModeTag>();
            world.Component<RunningTag>();
            world.Component<HiddenTag>();
            world.Component<PoisonedTag>();
            world.Component<FemaleTag>();
            world.Component<RenamableTag>();
            world.Component<OnGroundTag>();
            world.Component<DamageableTag>();
            world.Component<OpenedTag>();
            world.Component<PendingRemovalTag>();
            world.Component<MountedTag>();
            world.Component<FlyingTag>();

            // ── Relationships ───────────────────────────────────────
            world.Component<OwnedBy>();
            world.Component<EquippedOn>();
            world.Component<Targeting>();
            world.Component<Affects>();
            world.Component<InPartyOf>();

            // ── Effects ────────────────────────────────────────────────
            world.Component<EffectLifetime>();
            world.Component<EffectSourceLink>();
            world.Component<EffectAnimPlayback>();

            // ── Pruning ────────────────────────────────────────────────
            world.Component<PruneTimer>();

            // ── Render extraction ──────────────────────────────────
            world.Component<ScreenPosition>();
            world.Component<RenderSprite>();
            world.Component<RenderAnimationFrame>();
            world.Component<RenderLayerKey>();
            world.Component<RenderLightContribution>();
            world.Component<RenderNameplate>();
            world.Component<RenderSelectionFlags>();
            world.Component<VisibleTag>();

            // ── Render singletons ──────────────────────────────────
            world.Component<LightingState>();
            world.Component<ViewportState>();

            // ── Session singletons ──────────────────────────────────
            world.Component<FrameTiming>();
            world.Component<MapIndex>();
            world.Component<ClientFeatureFlags>();
            world.Component<EcsLockedFeatureFlags>();
            world.Component<ViewRange>();
            world.Component<SeasonState>();
            world.Component<ParityCounters>();

            // Initialize singletons with defaults
            world.Set(new FrameTiming(0, 0));
            world.Set(new MapIndex(0));
            world.Set(new ClientFeatureFlags(0));
            world.Set(new EcsLockedFeatureFlags(0));
            world.Set(new ViewRange(18));
            world.Set(new SeasonState(0));
            world.Set(new ParityCounters(0, 0));
            world.Set(new PruneTimer(0));
            world.Set(new LightingState(0, 0, 0, 0, 0f));
            world.Set(new ViewportState(0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
        }
    }
}
