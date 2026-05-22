// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Events;

namespace ClassicUO.Game.Targeting
{
    /// <summary>
    /// Concrete <see cref="IAttackTargetTracker"/>. Holds the running
    /// <c>LastAttack</c> serial and, on <see cref="EventSink.AttackTargetChanged"/>,
    /// flips it to the new attacker after closing the previous status window
    /// and requesting the new attacker's mobile status.
    /// </summary>
    internal sealed class AttackTargetTracker : IAttackTargetTracker
    {
        private readonly World _world;

        public AttackTargetTracker(World world)
        {
            _world = world;
        }

        public uint LastAttack { get; set; }

        public void OnAttackTargetChanged(AttackTargetChangedArgs e)
        {
            GameActions.SendCloseStatus(_world, LastAttack);
            LastAttack = e.Serial;
            GameActions.RequestMobileStatus(_world, e.Serial);
        }
    }
}
