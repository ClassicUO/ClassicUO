// SPDX-License-Identifier: BSD-2-Clause

using Flecs.NET.Core;

namespace ClassicUO.ECS.Systems
{
    /// <summary>
    /// NetApply phase systems that consume transient command entities
    /// and apply state changes to game entities.
    ///
    /// Commands are created in packet-order by EnqueueCommand (monotonic
    /// SequenceIndex). Flecs iterates entities within an archetype in
    /// insertion order, so packet ordering is naturally preserved without
    /// explicit sorting.
    /// </summary>
    public static class NetApplySystems
    {
        public static void Register(World world)
        {
            RegisterApplyEnterWorld(world);
            RegisterApplyCreateOrUpdateMobile(world);
            RegisterApplyCreateOrUpdateItem(world);
            RegisterApplyDeleteEntity(world);
            RegisterApplyContainedItem(world);
            RegisterApplyClearContainer(world);
            RegisterApplyEquipItem(world);
            RegisterApplyUpdateVitals(world);
            RegisterApplySetWarmode(world);
            RegisterApplyDisplayDeath(world);
            RegisterApplyDeathScreen(world);
            RegisterApplyCorpseEquipment(world);
            RegisterApplyDenyMoveItem(world);
            RegisterApplyEndDragging(world);
            RegisterApplyDropItemAccepted(world);
            RegisterCommandCleanup(world);
        }

        // ── Enter world (player bootstrap) ────────────────────────

        private static void RegisterApplyEnterWorld(World world)
        {
            world.System<CmdEnterWorld, MapIndex, NetDebugCounters>("NetApply_EnterWorld")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .TermAt(2).Singleton()
                .Each((Entity cmdEntity, ref CmdEnterWorld cmd, ref MapIndex mapIdx, ref NetDebugCounters counters) =>
                {
                    Entity player = SerialRegistry.FindOrCreate(cmdEntity.CsWorld(), cmd.Serial);

                    if (!player.Has<MobileTag>())
                        player.Add<MobileTag>();
                    if (!player.Has<PlayerTag>())
                        player.Add<PlayerTag>();

                    player.Set(new WorldPosition(cmd.X, cmd.Y, cmd.Z));
                    player.Set(new DirectionComponent(cmd.Direction));
                    player.Set(new GraphicComponent(cmd.Graphic));

                    mapIdx = new MapIndex(cmd.MapIndex);

                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        // ── Mobile creation / update ────────────────────────────────

        private static void RegisterApplyCreateOrUpdateMobile(World world)
        {
            world.System<CmdCreateOrUpdateMobile, NetDebugCounters>("NetApply_CreateOrUpdateMobile")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .Each((Entity cmdEntity, ref CmdCreateOrUpdateMobile cmd, ref NetDebugCounters counters) =>
                {
                    Entity target = SerialRegistry.FindOrCreate(cmdEntity.CsWorld(), cmd.Serial);

                    if (!target.Has<MobileTag>())
                        target.Add<MobileTag>();

                    if (cmd.IsPlayer && !target.Has<PlayerTag>())
                        target.Add<PlayerTag>();

                    target.Set(new WorldPosition(cmd.X, cmd.Y, cmd.Z));
                    target.Set(new DirectionComponent(cmd.Direction));
                    target.Set(new GraphicComponent(cmd.Graphic));
                    target.Set(new HueComponent(cmd.Hue));
                    target.Set(new FlagsComponent(cmd.Flags));
                    target.Set(new NotorietyComponent(cmd.Notoriety));

                    // Decode flags into individual tags for ECS queries and rendering.
                    // EntityFlags: Frozen=0x01, Female=0x02, Poisoned/Flying=0x04,
                    //              YellowBar=0x08, WarMode=0x40, Hidden=0x80
                    uint flags = cmd.Flags;
                    bool isGargoyle = cmd.Graphic == 0x029A || cmd.Graphic == 0x029B;

                    SetOrRemoveTag<FrozenTag>(target, (flags & 0x01) != 0);
                    SetOrRemoveTag<FemaleTag>(target, (flags & 0x02) != 0);

                    // Bit 0x04 is Poisoned for non-gargoyles, Flying for gargoyles
                    if (isGargoyle)
                    {
                        SetOrRemoveTag<FlyingTag>(target, (flags & 0x04) != 0);
                        if (target.Has<PoisonedTag>()) target.Remove<PoisonedTag>();
                    }
                    else
                    {
                        SetOrRemoveTag<PoisonedTag>(target, (flags & 0x04) != 0);
                        if (target.Has<FlyingTag>()) target.Remove<FlyingTag>();
                    }

                    SetOrRemoveTag<YellowHitsTag>(target, (flags & 0x08) != 0);
                    SetOrRemoveTag<WarModeTag>(target, (flags & 0x40) != 0);
                    SetOrRemoveTag<HiddenTag>(target, (flags & 0x80) != 0);

                    // Graphic-derived tags
                    SetOrRemoveTag<IsGargoyleTag>(target, isGargoyle);
                    SetOrRemoveTag<IsHumanTag>(target, IsHumanGraphic(cmd.Graphic));

                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        /// <summary>Add tag if condition is true, remove if false. Safe for missing tags.</summary>
        private static void SetOrRemoveTag<T>(Entity entity, bool condition) where T : struct
        {
            if (condition)
            {
                if (!entity.Has<T>()) entity.Add<T>();
            }
            else
            {
                if (entity.Has<T>()) entity.Remove<T>();
            }
        }

        /// <summary>
        /// Check if a graphic ID corresponds to a human/elf body type.
        /// Matches legacy Mobile.IsHuman property ranges.
        /// </summary>
        private static bool IsHumanGraphic(ushort graphic)
        {
            return (graphic >= 0x0190 && graphic <= 0x0193)
                || (graphic >= 0x00B7 && graphic <= 0x00BA)
                || (graphic >= 0x025D && graphic <= 0x0260)
                || graphic == 0x029A || graphic == 0x029B
                || graphic == 0x02B6 || graphic == 0x02B7
                || graphic == 0x03DB || graphic == 0x03DF
                || graphic == 0x03E2
                || graphic == 0x02E8 || graphic == 0x02E9
                || graphic == 0x04E5;
        }

        // ── Item creation / update ──────────────────────────────────

        private static void RegisterApplyCreateOrUpdateItem(World world)
        {
            world.System<CmdCreateOrUpdateItem, NetDebugCounters>("NetApply_CreateOrUpdateItem")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .Each((Entity cmdEntity, ref CmdCreateOrUpdateItem cmd, ref NetDebugCounters counters) =>
                {
                    Entity target = SerialRegistry.FindOrCreate(cmdEntity.CsWorld(), cmd.Serial);

                    if (!target.Has<ItemTag>())
                        target.Add<ItemTag>();

                    target.Set(new WorldPosition(cmd.X, cmd.Y, cmd.Z));
                    target.Set(new GraphicComponent(cmd.Graphic));
                    target.Set(new HueComponent(cmd.Hue));
                    target.Set(new FlagsComponent(cmd.Flags));

                    if (cmd.Amount > 0)
                        target.Set(new AmountComponent(cmd.Amount));

                    if (cmd.Direction != 0)
                        target.Set(new DirectionComponent(cmd.Direction));

                    if (!target.Has<OnGroundTag>())
                        target.Add<OnGroundTag>();

                    if (cmd.ItemType == 2 && !target.Has<MultiTag>())
                        target.Add<MultiTag>();

                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        // ── Entity deletion ─────────────────────────────────────────

        private static void RegisterApplyDeleteEntity(World world)
        {
            world.System<CmdDeleteEntity, NetDebugCounters>("NetApply_DeleteEntity")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .Each((Entity cmdEntity, ref CmdDeleteEntity cmd, ref NetDebugCounters counters) =>
                {
                    Entity target = SerialRegistry.FindBySerial(cmd.Serial);
                    if (target != 0 && target.IsAlive())
                        target.Destruct();

                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        // ── Contained item (add to container) ───────────────────────

        private static void RegisterApplyContainedItem(World world)
        {
            world.System<CmdContainedItem, NetDebugCounters>("NetApply_ContainedItem")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .Each((Entity cmdEntity, ref CmdContainedItem cmd, ref NetDebugCounters counters) =>
                {
                    var w = cmdEntity.CsWorld();
                    Entity item = SerialRegistry.FindOrCreate(w, cmd.Serial);

                    if (!item.Has<ItemTag>())
                        item.Add<ItemTag>();

                    item.Set(new GraphicComponent(cmd.Graphic));
                    item.Set(new HueComponent(cmd.Hue));
                    item.Set(new AmountComponent(cmd.Amount));
                    item.Set(new ContainerLink(cmd.ContainerSerial));
                    item.Set(new WorldPosition(cmd.X, cmd.Y, 0));
                    item.Set(new ContainerPosition(cmd.X, cmd.Y, 0));

                    if (item.Has<OnGroundTag>())
                        item.Remove<OnGroundTag>();

                    Entity container = SerialRegistry.FindBySerial(cmd.ContainerSerial);
                    if (container != 0 && container.IsAlive())
                        item.ChildOf(container);

                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        // ── Clear container ─────────────────────────────────────────

        private static void RegisterApplyClearContainer(World world)
        {
            world.System<CmdClearContainer, NetDebugCounters>("NetApply_ClearContainer")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .Each((Entity cmdEntity, ref CmdClearContainer cmd, ref NetDebugCounters counters) =>
                {
                    var w = cmdEntity.CsWorld();
                    Entity container = SerialRegistry.FindBySerial(cmd.ContainerSerial);
                    if (container == 0 || !container.IsAlive())
                    {
                        counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                        return;
                    }

                    // Copy ref params to locals for capture in nested lambda.
                    uint containerSerial = cmd.ContainerSerial;
                    bool keepEquipped = cmd.KeepEquipped;

                    // Collect children to destroy.
                    using var q = w.QueryBuilder<ContainerLink>()
                        .With<ItemTag>()
                        .Build();

                    // Two-pass: collect first, then destroy.
                    var toDestroy = new System.Collections.Generic.List<Entity>();
                    q.Each((Entity child, ref ContainerLink link) =>
                    {
                        if (link.ContainerSerial != containerSerial)
                            return;
                        if (keepEquipped && child.Has<LayerComponent>())
                        {
                            ref readonly var layer = ref child.Get<LayerComponent>();
                            if (layer.Layer != 0)
                                return;
                        }
                        toDestroy.Add(child);
                    });

                    foreach (var e in toDestroy)
                    {
                        if (e.IsAlive())
                            e.Destruct();
                    }

                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        // ── Equip item ──────────────────────────────────────────────

        private static void RegisterApplyEquipItem(World world)
        {
            world.System<CmdEquipItem, NetDebugCounters>("NetApply_EquipItem")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .Each((Entity cmdEntity, ref CmdEquipItem cmd, ref NetDebugCounters counters) =>
                {
                    var w = cmdEntity.CsWorld();
                    Entity item = SerialRegistry.FindOrCreate(w, cmd.Serial);

                    if (!item.Has<ItemTag>())
                        item.Add<ItemTag>();

                    item.Set(new GraphicComponent(cmd.Graphic));
                    item.Set(new HueComponent(cmd.Hue));
                    item.Set(new LayerComponent(cmd.Layer));
                    item.Set(new ContainerLink(cmd.ContainerSerial));
                    item.Set(new AmountComponent(1));

                    if (item.Has<OnGroundTag>())
                        item.Remove<OnGroundTag>();

                    Entity mobile = SerialRegistry.FindBySerial(cmd.ContainerSerial);
                    if (mobile != 0 && mobile.IsAlive())
                    {
                        item.Add<EquippedOn>(mobile);
                        item.ChildOf(mobile);
                    }

                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        // ── Update vitals ───────────────────────────────────────────

        private static void RegisterApplyUpdateVitals(World world)
        {
            world.System<CmdUpdateVitals, NetDebugCounters>("NetApply_UpdateVitals")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .Each((Entity cmdEntity, ref CmdUpdateVitals cmd, ref NetDebugCounters counters) =>
                {
                    Entity target = SerialRegistry.FindBySerial(cmd.Serial);
                    if (target == 0 || !target.IsAlive())
                    {
                        counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                        return;
                    }

                    Vitals current = target.Has<Vitals>()
                        ? target.Get<Vitals>()
                        : default;

                    target.Set(new Vitals(
                        Hits: (cmd.ValidFields & 1) != 0 ? cmd.Hits : current.Hits,
                        HitsMax: (cmd.ValidFields & 1) != 0 ? cmd.HitsMax : current.HitsMax,
                        Mana: (cmd.ValidFields & 2) != 0 ? cmd.Mana : current.Mana,
                        ManaMax: (cmd.ValidFields & 2) != 0 ? cmd.ManaMax : current.ManaMax,
                        Stamina: (cmd.ValidFields & 4) != 0 ? cmd.Stamina : current.Stamina,
                        StaminaMax: (cmd.ValidFields & 4) != 0 ? cmd.StaminaMax : current.StaminaMax
                    ));

                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        // ── Set warmode ─────────────────────────────────────────────

        private static void RegisterApplySetWarmode(World world)
        {
            world.System<CmdSetWarmode, NetDebugCounters>("NetApply_SetWarmode")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .Each((Entity cmdEntity, ref CmdSetWarmode cmd, ref NetDebugCounters counters) =>
                {
                    Entity target = SerialRegistry.FindBySerial(cmd.Serial);
                    if (target == 0 || !target.IsAlive())
                    {
                        counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                        return;
                    }

                    if (cmd.WarMode)
                    {
                        if (!target.Has<WarModeTag>())
                            target.Add<WarModeTag>();
                    }
                    else
                    {
                        if (target.Has<WarModeTag>())
                            target.Remove<WarModeTag>();
                    }

                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        // ── Display death ─────────────────────────────────────────

        private static void RegisterApplyDisplayDeath(World world)
        {
            world.System<CmdDisplayDeath, NetDebugCounters>("NetApply_DisplayDeath")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .Each((Entity cmdEntity, ref CmdDisplayDeath cmd, ref NetDebugCounters counters) =>
                {
                    var w = cmdEntity.CsWorld();

                    // Find the dying mobile and add DeadTag.
                    Entity mobile = SerialRegistry.FindBySerial(cmd.Serial);
                    if (mobile != 0 && mobile.IsAlive())
                    {
                        if (!mobile.Has<DeadTag>())
                            mobile.Add<DeadTag>();
                    }

                    // Create corpse entity if a valid corpse serial was provided.
                    if (cmd.CorpseSerial != 0)
                    {
                        Entity corpse = SerialRegistry.FindOrCreate(w, cmd.CorpseSerial);

                        if (!corpse.Has<ItemTag>())
                            corpse.Add<ItemTag>();
                        if (!corpse.Has<CorpseTag>())
                            corpse.Add<CorpseTag>();
                        if (!corpse.Has<OnGroundTag>())
                            corpse.Add<OnGroundTag>();

                        corpse.Set(new GraphicComponent(0x2006)); // corpse graphic
                        corpse.Set(new CorpseOwnerLink(cmd.Serial));

                        // Copy position from dying mobile to corpse.
                        if (mobile != 0 && mobile.IsAlive() && mobile.Has<WorldPosition>())
                        {
                            ref readonly var pos = ref mobile.Get<WorldPosition>();
                            corpse.Set(new WorldPosition(pos.X, pos.Y, pos.Z));

                            if (mobile.Has<DirectionComponent>())
                            {
                                ref readonly var dir = ref mobile.Get<DirectionComponent>();
                                corpse.Set(new DirectionComponent(dir.Direction));
                            }
                        }
                    }

                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        // ── Death screen (player death) ──────────────────────────

        private static void RegisterApplyDeathScreen(World world)
        {
            world.System<CmdDeathScreen, NetDebugCounters>("NetApply_DeathScreen")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .Each((Entity cmdEntity, ref CmdDeathScreen cmd, ref NetDebugCounters counters) =>
                {
                    if (cmd.Action != 1)
                    {
                        // Player died — add DeadTag to player entity.
                        Entity player = SerialRegistry.FindPlayer(cmdEntity.CsWorld());
                        if (player != 0 && player.IsAlive())
                        {
                            if (!player.Has<DeadTag>())
                                player.Add<DeadTag>();
                        }
                    }

                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        // ── Corpse equipment ──────────────────────────────────────

        private static void RegisterApplyCorpseEquipment(World world)
        {
            world.System<CmdCorpseEquipment, NetDebugCounters>("NetApply_CorpseEquipment")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .Each((Entity cmdEntity, ref CmdCorpseEquipment cmd, ref NetDebugCounters counters) =>
                {
                    var w = cmdEntity.CsWorld();
                    Entity corpse = SerialRegistry.FindBySerial(cmd.CorpseSerial);
                    if (corpse == 0 || !corpse.IsAlive())
                    {
                        counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                        return;
                    }

                    Entity item = SerialRegistry.FindOrCreate(w, cmd.ItemSerial);

                    if (!item.Has<ItemTag>())
                        item.Add<ItemTag>();

                    item.Set(new LayerComponent(cmd.Layer));
                    item.Set(new ContainerLink(cmd.CorpseSerial));

                    if (item.Has<OnGroundTag>())
                        item.Remove<OnGroundTag>();

                    item.Add<EquippedOn>(corpse);
                    item.ChildOf(corpse);

                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        // ── Item Hold: Deny Move (NetApply) ─────────────────────────

        private static void RegisterApplyDenyMoveItem(World world)
        {
            world.System<CmdDenyMoveItem, ItemHoldState, NetDebugCounters>("NetApply_DenyMoveItem")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .TermAt(1).Singleton()
                .TermAt(2).Singleton()
                .Each((Entity cmdEntity, ref CmdDenyMoveItem cmd, ref ItemHoldState hold, ref NetDebugCounters counters) =>
                {
                    // Server denied the item move — reset cursor hold state.
                    hold = default;
                    counters = counters with { CommandsApplied = counters.CommandsApplied + 1 };
                });
        }

        // ── Item Hold: End Dragging (NetApply) ──────────────────────

        private static void RegisterApplyEndDragging(World world)
        {
            world.System("NetApply_EndDragging")
                .Kind(Phases.NetApply)
                .With<CmdEndDragging>()
                .With<NetworkCommand>()
                .Run((Iter it) =>
                {
                    while (it.Next())
                    {
                        ref var hold = ref it.World().GetMut<ItemHoldState>();
                        ref var counters = ref it.World().GetMut<NetDebugCounters>();

                        hold.Enabled = false;
                        hold.Dropped = false;
                        counters = counters with { CommandsApplied = counters.CommandsApplied + it.Count() };
                    }
                });
        }

        // ── Item Hold: Drop Accepted (NetApply) ─────────────────────

        private static void RegisterApplyDropItemAccepted(World world)
        {
            world.System("NetApply_DropItemAccepted")
                .Kind(Phases.NetApply)
                .With<CmdDropItemAccepted>()
                .With<NetworkCommand>()
                .Run((Iter it) =>
                {
                    while (it.Next())
                    {
                        ref var hold = ref it.World().GetMut<ItemHoldState>();
                        ref var counters = ref it.World().GetMut<NetDebugCounters>();

                        hold = default;
                        counters = counters with { CommandsApplied = counters.CommandsApplied + it.Count() };
                    }
                });
        }

        // ── Command cleanup (PostFrame) ─────────────────────────────

        private static void RegisterCommandCleanup(World world)
        {
            // Use Run with Iter to destroy all command entities each frame.
            world.System("PostFrame_CommandCleanup")
                .Kind(Phases.PostFrame)
                .With<NetworkCommand>()
                .Run((Iter it) =>
                {
                    while (it.Next())
                    {
                        for (int i = 0; i < it.Count(); i++)
                            it.Entity(i).Destruct();
                    }
                });
        }

    }
}
