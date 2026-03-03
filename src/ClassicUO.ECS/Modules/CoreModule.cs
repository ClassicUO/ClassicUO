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

            // ── Animation ───────────────────────────────────────────
            world.Component<IdleAnimationState>();
            world.Component<ItemAnimationState>();

            // ── Player Stats / Skills ──────────────────────────────
            world.Component<PlayerStats>();
            world.Component<StatLocks>();
            world.Component<EcsSkillEntry>();
            world.Component<SkillsComponent>();
            world.Component<ObjectProperties>();

            // ── Inventory ───────────────────────────────────────────
            world.Component<ContainerLink>();
            world.Component<LayerComponent>();
            world.Component<AmountComponent>();
            world.Component<PriceComponent>();

            // ── Boat / Multi ──────────────────────────────────────
            world.Component<BoatState>();
            world.Component<BoatLink>();
            world.Component<OnBoatTag>();

            // ── Weather ───────────────────────────────────────────
            world.Component<WeatherState>();

            // ── Social / Party ─────────────────────────────────────
            world.Component<PartyState>();
            world.Component<OverheadText>();
            world.Component<PartyTag>();
            world.Component<OverheadTextTag>();

            // ── Health Bar / Buff ──────────────────────────────────
            world.Component<HealthBarFlags>();
            world.Component<BuffEntry>();
            world.Component<BuffTag>();

            // ── Death / Corpse ─────────────────────────────────────
            world.Component<CorpseOwnerLink>();
            world.Component<AutoOpenCorpseTag>();

            // ── Targeting ──────────────────────────────────────────
            world.Component<TargetingState>();
            world.Component<LastTargetInfo>();

            // ── Manager Migration ──────────────────────────────────
            world.Component<MultiPlacement>();
            world.Component<DelayedClickState>();
            world.Component<UseItemQueueState>();
            world.Component<MacroState>();
            world.Component<AuraState>();

            // ── Deferred Removal ──────────────────────────────────
            world.Component<PendingRemovalDelay>();
            world.Component<EntityRemovedEvent>();
            world.Component<RecalcAbilitiesTag>();
            world.Component<AbilitiesState>();

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
            world.Component<ExplodeOnExpiryTag>();
            world.Component<MountedTag>();
            world.Component<FlyingTag>();
            world.Component<FrozenTag>();
            world.Component<YellowHitsTag>();
            world.Component<IsHumanTag>();
            world.Component<IsGargoyleTag>();

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
            world.Component<EffectTarget>();
            world.Component<EffectMovement>();

            // ── Item Hold ─────────────────────────────────────────────
            world.Component<ItemHoldState>();

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
            world.Component<RenderHealthBar>();
            world.Component<RenderTextOverlay>();
            world.Component<TooltipRequest>();
            world.Component<VisibleTag>();
            world.Component<MouseOverTag>();
            world.Component<SelectedTag>();

            // ── Render singletons ──────────────────────────────────
            world.Component<LightingState>();
            world.Component<ViewportState>();
            world.Component<WorldMapUpdateList>();
            world.Component<WeatherRenderData>();

            // ── UI Extract singletons ───────────────────────────────
            world.Component<BuffBarSnapshot>();
            world.Component<StatusSnapshot>();

            // ── Session singletons ──────────────────────────────────
            world.Component<FrameTiming>();
            world.Component<MapIndex>();
            world.Component<ClientFeatureFlags>();
            world.Component<EcsLockedFeatureFlags>();
            world.Component<ViewRange>();
            world.Component<SeasonState>();
            world.Component<ParityCounters>();

            // ── Diagnostics / rollout ────────────────────────────────
            world.Component<SubsystemFlags>();
            world.Component<CutoverFlags>();
            world.Component<FrameDiagnostics>();
            world.Component<ParityCheckpoint>();

            // Initialize singletons with defaults
            world.Set(new FrameTiming(0, 0));
            world.Set(new MapIndex(0));
            world.Set(new ClientFeatureFlags(0));
            world.Set(new EcsLockedFeatureFlags(0));
            world.Set(new ViewRange(18));
            world.Set(new SeasonState(0));
            world.Set(new ParityCounters(0, 0));
            world.Set(new PruneTimer(0));
            world.Set(new WeatherState(0xFE, 0, 0));
            world.Set(new PartyState());
            world.Set(new ItemHoldState());
            world.Set(new AbilitiesState(0, 0));
            world.Set(new LightingState(0, 0, 0, 0, 0f));
            world.Set(new ViewportState(0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
            world.Set(new WorldMapUpdateList());
            world.Set(SubsystemFlags.AllEnabled);
            world.Set(new CutoverFlags());
            world.Set(new FrameDiagnostics(0, 0, 0, 0, 0, 0));
            world.Set(new TargetingState());
            world.Set(new LastTargetInfo());
            world.Set(new MultiPlacement());
            world.Set(new DelayedClickState());
            world.Set(new UseItemQueueState());
            world.Set(new MacroState { MacroNameIndex = -1 });
            world.Set(new AuraState(false, 0));
            world.Set(new WeatherRenderData());
            world.Set(new BuffBarSnapshot());
            world.Set(new StatusSnapshot());
        }
    }
}
