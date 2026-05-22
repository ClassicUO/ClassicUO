// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Events;

namespace ClassicUO.Game.Targeting
{
    /// <summary>
    /// Tracks the last serial the player attacked. Listens to
    /// <see cref="EventSink.AttackTargetChanged"/> and exposes the running
    /// <c>LastAttack</c> serial to the facade.
    /// </summary>
    internal interface IAttackTargetTracker
    {
        uint LastAttack { get; set; }
        void OnAttackTargetChanged(AttackTargetChangedArgs e);
    }
}
