// SPDX-License-Identifier: BSD-2-Clause

using Flecs.NET.Core;

namespace ClassicUO.ECS
{
    /// <summary>
    /// Custom pipeline phases for the ClassicUO ECS execution order.
    /// Registered as Flecs phase entities with DependsOn ordering.
    ///
    /// Execution order:
    ///   PreInput → Input → PreNet → NetApply → PreSim →
    ///   Simulation → PostSim → RenderExtract → UiExtract → PostFrame
    /// </summary>
    public static class Phases
    {
        public static Entity PreInput { get; private set; }
        public static Entity Input { get; private set; }
        public static Entity PreNet { get; private set; }
        public static Entity NetApply { get; private set; }
        public static Entity PreSim { get; private set; }
        public static Entity Simulation { get; private set; }
        public static Entity PostSim { get; private set; }
        public static Entity RenderExtract { get; private set; }
        public static Entity UiExtract { get; private set; }
        public static Entity PostFrame { get; private set; }

        internal static void Register(World world)
        {
            // Chain of custom phases. Each DependsOn the previous,
            // forming a deterministic linear execution order.

            PreInput = world.Entity("PreInput")
                .Add(Ecs.Phase)
                .DependsOn(Ecs.OnLoad);

            Input = world.Entity("Input")
                .Add(Ecs.Phase)
                .DependsOn(PreInput);

            PreNet = world.Entity("PreNet")
                .Add(Ecs.Phase)
                .DependsOn(Input);

            NetApply = world.Entity("NetApply")
                .Add(Ecs.Phase)
                .DependsOn(PreNet);

            PreSim = world.Entity("PreSim")
                .Add(Ecs.Phase)
                .DependsOn(NetApply);

            Simulation = world.Entity("Simulation")
                .Add(Ecs.Phase)
                .DependsOn(PreSim);

            PostSim = world.Entity("PostSim")
                .Add(Ecs.Phase)
                .DependsOn(Simulation);

            RenderExtract = world.Entity("RenderExtract")
                .Add(Ecs.Phase)
                .DependsOn(PostSim);

            UiExtract = world.Entity("UiExtract")
                .Add(Ecs.Phase)
                .DependsOn(RenderExtract);

            PostFrame = world.Entity("PostFrame")
                .Add(Ecs.Phase)
                .DependsOn(UiExtract);
        }
    }
}
