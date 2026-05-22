// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Combat
{
    internal sealed class CombatSwingHandler : IEventListener
    {
        private const int TIME_TURN_TO_LASTTARGET = 2000;

        private readonly World _world;

        public CombatSwingHandler(World world) => _world = world;

        public void Subscribe()
        {
            EventSink.CombatSwing += OnCombatSwing;
        }

        public void Unsubscribe()
        {
            EventSink.CombatSwing -= OnCombatSwing;
        }

        private void OnCombatSwing(CombatSwingArgs e)
        {
            if (_world.Player == null) return;
            if (e.Attacker != _world.Player.Serial) return;

            if (
                _world.TargetManager.LastAttack == e.Defender
                && _world.Player.InWarMode
                && _world.Player.Walker.LastStepRequestTime + TIME_TURN_TO_LASTTARGET < Time.Ticks
                && _world.Player.Steps.Count == 0
            )
            {
                Mobile enemy = _world.Mobiles.Get(e.Defender);
                if (enemy != null)
                {
                    Direction pdir = DirectionHelper.GetDirectionAB(_world.Player.X, _world.Player.Y, enemy.X, enemy.Y);

                    int x = _world.Player.X;
                    int y = _world.Player.Y;
                    sbyte z = _world.Player.Z;

                    if (_world.Player.Pathfinder.CanWalk(ref pdir, ref x, ref y, ref z) && _world.Player.Direction != pdir)
                    {
                        _world.Player.Walk(pdir, false);
                    }
                }
            }
        }
    }
}
