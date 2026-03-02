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
            RegisterApplyCreateOrUpdateMobile(world);
            RegisterApplyCreateOrUpdateItem(world);
            RegisterApplyDeleteEntity(world);
            RegisterApplyContainedItem(world);
            RegisterApplyClearContainer(world);
            RegisterApplyEquipItem(world);
            RegisterApplyUpdateVitals(world);
            RegisterApplySetWarmode(world);
            RegisterCommandCleanup(world);
        }

        // ── Mobile creation / update ────────────────────────────────

        private static void RegisterApplyCreateOrUpdateMobile(World world)
        {
            world.System<CmdCreateOrUpdateMobile>("NetApply_CreateOrUpdateMobile")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .Each((Entity cmdEntity, ref CmdCreateOrUpdateMobile cmd) =>
                {
                    Entity target = FindOrCreate(cmdEntity.CsWorld(), cmd.Serial);

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

                    IncrementApplied(cmdEntity.CsWorld());
                });
        }

        // ── Item creation / update ──────────────────────────────────

        private static void RegisterApplyCreateOrUpdateItem(World world)
        {
            world.System<CmdCreateOrUpdateItem>("NetApply_CreateOrUpdateItem")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .Each((Entity cmdEntity, ref CmdCreateOrUpdateItem cmd) =>
                {
                    Entity target = FindOrCreate(cmdEntity.CsWorld(), cmd.Serial);

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

                    IncrementApplied(cmdEntity.CsWorld());
                });
        }

        // ── Entity deletion ─────────────────────────────────────────

        private static void RegisterApplyDeleteEntity(World world)
        {
            world.System<CmdDeleteEntity>("NetApply_DeleteEntity")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .Each((Entity cmdEntity, ref CmdDeleteEntity cmd) =>
                {
                    Entity target = FindBySerial(cmdEntity.CsWorld(), cmd.Serial);
                    if (target != 0 && target.IsAlive())
                        target.Destruct();

                    IncrementApplied(cmdEntity.CsWorld());
                });
        }

        // ── Contained item (add to container) ───────────────────────

        private static void RegisterApplyContainedItem(World world)
        {
            world.System<CmdContainedItem>("NetApply_ContainedItem")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .Each((Entity cmdEntity, ref CmdContainedItem cmd) =>
                {
                    var w = cmdEntity.CsWorld();
                    Entity item = FindOrCreate(w, cmd.Serial);

                    if (!item.Has<ItemTag>())
                        item.Add<ItemTag>();

                    item.Set(new GraphicComponent(cmd.Graphic));
                    item.Set(new HueComponent(cmd.Hue));
                    item.Set(new AmountComponent(cmd.Amount));
                    item.Set(new ContainerLink(cmd.ContainerSerial));
                    item.Set(new WorldPosition(cmd.X, cmd.Y, 0));

                    if (item.Has<OnGroundTag>())
                        item.Remove<OnGroundTag>();

                    Entity container = FindBySerial(w, cmd.ContainerSerial);
                    if (container != 0 && container.IsAlive())
                        item.ChildOf(container);

                    IncrementApplied(w);
                });
        }

        // ── Clear container ─────────────────────────────────────────

        private static void RegisterApplyClearContainer(World world)
        {
            world.System<CmdClearContainer>("NetApply_ClearContainer")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .Each((Entity cmdEntity, ref CmdClearContainer cmd) =>
                {
                    var w = cmdEntity.CsWorld();
                    Entity container = FindBySerial(w, cmd.ContainerSerial);
                    if (container == 0 || !container.IsAlive())
                    {
                        IncrementApplied(w);
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

                    IncrementApplied(w);
                });
        }

        // ── Equip item ──────────────────────────────────────────────

        private static void RegisterApplyEquipItem(World world)
        {
            world.System<CmdEquipItem>("NetApply_EquipItem")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .Each((Entity cmdEntity, ref CmdEquipItem cmd) =>
                {
                    var w = cmdEntity.CsWorld();
                    Entity item = FindOrCreate(w, cmd.Serial);

                    if (!item.Has<ItemTag>())
                        item.Add<ItemTag>();

                    item.Set(new GraphicComponent(cmd.Graphic));
                    item.Set(new HueComponent(cmd.Hue));
                    item.Set(new LayerComponent(cmd.Layer));
                    item.Set(new ContainerLink(cmd.ContainerSerial));
                    item.Set(new AmountComponent(1));

                    if (item.Has<OnGroundTag>())
                        item.Remove<OnGroundTag>();

                    Entity mobile = FindBySerial(w, cmd.ContainerSerial);
                    if (mobile != 0 && mobile.IsAlive())
                    {
                        item.Add<EquippedOn>(mobile);
                        item.ChildOf(mobile);
                    }

                    IncrementApplied(w);
                });
        }

        // ── Update vitals ───────────────────────────────────────────

        private static void RegisterApplyUpdateVitals(World world)
        {
            world.System<CmdUpdateVitals>("NetApply_UpdateVitals")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .Each((Entity cmdEntity, ref CmdUpdateVitals cmd) =>
                {
                    var w = cmdEntity.CsWorld();
                    Entity target = FindBySerial(w, cmd.Serial);
                    if (target == 0 || !target.IsAlive())
                    {
                        IncrementApplied(w);
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

                    IncrementApplied(w);
                });
        }

        // ── Set warmode ─────────────────────────────────────────────

        private static void RegisterApplySetWarmode(World world)
        {
            world.System<CmdSetWarmode>("NetApply_SetWarmode")
                .Kind(Phases.NetApply)
                .With<NetworkCommand>()
                .Each((Entity cmdEntity, ref CmdSetWarmode cmd) =>
                {
                    var w = cmdEntity.CsWorld();
                    Entity target = FindBySerial(w, cmd.Serial);
                    if (target == 0 || !target.IsAlive())
                    {
                        IncrementApplied(w);
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

                    IncrementApplied(w);
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

        // ── Helpers ─────────────────────────────────────────────────

        private static Entity FindBySerial(World world, uint serial)
        {
            using var q = world.QueryBuilder<SerialComponent>().Build();

            Entity found = default;
            q.Each((Entity e, ref SerialComponent s) =>
            {
                if (s.Serial == serial)
                    found = e;
            });
            return found;
        }

        private static Entity FindOrCreate(World world, uint serial)
        {
            Entity found = FindBySerial(world, serial);
            if (found != 0 && found.IsAlive())
                return found;

            return world.Entity()
                .Set(new SerialComponent(serial));
        }

        private static void IncrementApplied(World world)
        {
            ref var c = ref world.GetMut<NetDebugCounters>();
            c = c with { CommandsApplied = c.CommandsApplied + 1 };
        }
    }
}
