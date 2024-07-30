using ClassicUO.Dust765.Managers;
using ClassicUO.Game.Managers;
using System;

namespace ClassicUO.Game.Cheats.AIBot
{
    internal sealed class SpellCaster : AIState
    {
        SpellAction _nextSpell;
        DateTime _nextCastTime;

        AIBot _bot;

        public SpellCaster(AIBot bot)
        {
            _bot = bot;
        }

        protected override InternalState OnUpdate()
        {
            switch( World.Player.Spell.Action )
            {
                case SpellAction.Poison:
                case SpellAction.Weaken:
                case SpellAction.Clumsy:
                case SpellAction.Feeblemind:
                case SpellAction.MagicArrow:
                TargetManager.Target(_bot.Target);
                _bot.Player.Spell.Action = SpellAction.Unknown;
                return InternalState.Success;
            }
            return InternalState.IsReady;
        }

        protected override InternalState GetCondition()
        {
            _nextSpell = GetNextSpell();

            if ( World.Player.Spell.IsCasting )
                return InternalState.Failed;

            if ( TargetManager.IsTargeting )
                return InternalState.IsReady;

            if ( _bot.Target.Spell.IsCasting )
            {
                _bot.EquipCandle();
                return InternalState.Failed;
            }

            if ( TargetManager.IsTargeting )

            {
                if ( _bot.Target.IsPoisoned )
                {
                    if ( _bot.Target.Poison.HasElapsed )
                         return InternalState.Failed;

                    if ( World.Player.Spell.Action == SpellAction.MagicArrow )
                    {
                        if ( _bot.Target.Spell.IsActive )
                            return InternalState.Failed;

                        if ( _bot.Target.Spell.IsCasting )
                            return InternalState.IsReady;
                    }

                } else
                {
                    switch ( World.Player.Spell.Action )
                    {
                        case SpellAction.MagicArrow:
                        if ( _bot.Target.Spell.IsCasting )
                            return InternalState.IsReady;
                        break;

                    }
                }
                return InternalState.Failed;
            }

            // Wait until we can cast again
            if ( _nextSpell == SpellAction.Unknown || DateTime.Now < _nextCastTime )
                return InternalState.Failed;

            GameActions.CastSpell( (int)_nextSpell );
            _nextCastTime = DateTime.Now + GetCursorTime( _nextSpell );
            return InternalState.Failed;
        }

        private SpellAction GetNextSpell()
        {
            if ( !_bot.Target.IsPoisoned )
                return SpellAction.Poison;
            return SpellAction.MagicArrow;
        }
        
        /// <summary>
        ///     Gets the <see cref="TimeSpan"/> duration of the current spell action.
        /// </summary>
        /// <returns><see cref="TimeSpan"/></returns>
        public static TimeSpan GetCursorTime( SpellAction spell )
        {
            switch( spell )
            {
                case SpellAction.MiniHeal:
                case SpellAction.MagicArrow:
                return TimeSpan.FromSeconds( 0.52 );

                case SpellAction.Clumsy:
                case SpellAction.Feeblemind:
                case SpellAction.NightSight:
                case SpellAction.Weaken:
                case SpellAction.Agility:
                case SpellAction.Cunning:
                case SpellAction.Cure:
                case SpellAction.Harm:
                case SpellAction.MagicTrap:
                case SpellAction.MagicUntrap:
                case SpellAction.Strength:
                case SpellAction.Bless:
                case SpellAction.Fireball:
                case SpellAction.MagicLock:
                case SpellAction.Unlock:
                case SpellAction.Poison:
                case SpellAction.Telekinesis:
                case SpellAction.Teleport:
                case SpellAction.WallOfStone:
                return TimeSpan.FromSeconds( 1.0 );

                case SpellAction.ArchCure:
                case SpellAction.ArchProtection:
                case SpellAction.Curse:
                case SpellAction.FireField:
                case SpellAction.GreaterHeal:
                case SpellAction.Lightning:
                case SpellAction.ManaDrain:
                case SpellAction.Recall:
                return TimeSpan.FromSeconds( 1.28 );

                case SpellAction.DispelField:
                case SpellAction.MindBlast:
                case SpellAction.Paralyze:
                case SpellAction.PoisonField:
                return TimeSpan.FromSeconds( 1.5 );

                case SpellAction.Dispel:
                case SpellAction.EnergyBolt:
                case SpellAction.Explosion:
                case SpellAction.Invisibility:
                case SpellAction.Mark:
                case SpellAction.MassCurse:
                case SpellAction.ParalyzeField:
                case SpellAction.Reveal:
                return TimeSpan.FromSeconds( 1.8 );

                case SpellAction.ChainLightning:
                case SpellAction.EnergyField:
                case SpellAction.Flamestrike:
                case SpellAction.GateTravel:
                case SpellAction.ManaVampire:
                case SpellAction.MassDispel:
                case SpellAction.MeteorSwarm:
                return TimeSpan.FromSeconds( 2.0 );

                case SpellAction.EnergyVortex:
                case SpellAction.Ressurection:
                return TimeSpan.FromSeconds( 2.2 );

                case SpellAction.BladeSpirits:
                return TimeSpan.FromSeconds( 7.5 );
            }
            return TimeSpan.Zero;
        }
    }
}