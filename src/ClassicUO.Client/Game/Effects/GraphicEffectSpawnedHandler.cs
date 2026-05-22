// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;
using ClassicUO.Game.Events;

namespace ClassicUO.Game.Effects
{
    /// <summary>
    /// Handles <see cref="EventSink.GraphicEffectSpawned"/> by forwarding the
    /// raised effect arguments to <see cref="World.SpawnEffect"/>.
    /// Replaces the legacy <c>OnGraphicEffectSpawned</c> handler that lived in
    /// <c>World.Subscribers</c>.
    /// </summary>
    internal sealed class GraphicEffectSpawnedHandler : IEventListener
    {
        private readonly World _world;

        public GraphicEffectSpawnedHandler(World world) => _world = world;

        public void Subscribe() => EventSink.GraphicEffectSpawned += OnGraphicEffectSpawned;

        public void Unsubscribe() => EventSink.GraphicEffectSpawned -= OnGraphicEffectSpawned;

        private void OnGraphicEffectSpawned(GraphicEffectSpawnedArgs e)
        {
            _world.SpawnEffect(
                (GraphicEffectType)e.EffectType,
                e.Source,
                e.Target,
                e.Graphic,
                (ushort)e.Hue,
                e.SourceX,
                e.SourceY,
                e.SourceZ,
                e.TargetX,
                e.TargetY,
                e.TargetZ,
                e.Speed,
                e.Duration,
                e.FixedDirection,
                e.DoesExplode,
                false,
                (GraphicEffectBlendMode)e.BlendMode
            );
        }
    }
}
