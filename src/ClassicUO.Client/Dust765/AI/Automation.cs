using ClassicUO.Dust765.Managers;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Utility;
using System;

namespace ClassicUO.Game.Cheats.AIBot
{
    /// <summary>
    ///     Credits to linkuog for starting this project,
    ///     you can find me on Discord: #virtualfear
    /// </summary>
    internal sealed class AIBot : AIState
    {
        Mobile _target;
        public Mobile Player => World.Player;
        public Mobile Target
        {
            get => _target;
            set
            {
                Mobile oldTarget = _target;
                Mobile newTarget = value;

                if ( newTarget == oldTarget )
                    return;

                _target = newTarget;
                OnTargetChanged( oldTarget, newTarget );
            }
        }

       

        private void OnTargetChanged( Mobile oldTarget, Mobile newTarget )
        {
            if ( oldTarget != null )
            {
                oldTarget.Spell = null;
                oldTarget.Poison = null;
            }
            newTarget.Poison = new PoisonTimer();
            newTarget.Poison.OnTick = new PoisonTick( OnTargetPoison );
        }

        private void OnTargetPoison()
        {
        }

        public AIBot()
        {
            //base.Register( new MoveHandler( this ) );
            base.Register( new SpellCaster( this ) );

            EventSink.JournalEntryAdded += this.Journal_EntryAdded;
            ChatHandlers.OnSpellCast += this.ChatHandlers_OnSpellCast;
        }

        private void ChatHandlers_OnSpellCast( Mobile mob, SpellAction action )
        {
            if ( mob == World.Player )
                World.Player.Spell.Action = action;
            else if ( mob == _target )
                _target.Spell.Action = action;
        }

        private void Journal_EntryAdded( object sender, JournalEntry e )
        {
            if ( _target == null )
                return;
            
            if ( e.Name.Equals( _target.Name ) )
            {
                if ( e.Text.Contains( "stumbles around in confusion and pain." ) )
                    _target.Poison.Count++;

                return;
            }

            if ( e.Name.Equals( "You see" ) && e.Text.Contains( "the remains of" ) )
            {
                string name = e.Text.Remove( 0, "the remains of".Length );
                if ( name == World.Player.Name )
                {
                    GameActions.Print( "AI: You have won the duel." );
                } else if ( name == _target.Name )
                {
                    GameActions.Print( $"AI: {_target.Name} has won the duel." );
                    Automation.Toggle(); // Disables the bot
                } else
                {
                    GameActions.Print( "AI: Something went wrong.." );
                }
                return;
            }
           
            if ( e.Name.Equals( "System" ) )
            {
                switch ( e.Text )
                {
                    case "You are already casting a spell.":
                    break;

                    case "You have not yet recovered from casting a spell.":
                    break;
                }
            }

        }

        ~AIBot()
        {
            ChatHandlers.OnSpellCast -= this.ChatHandlers_OnSpellCast;
            EventSink.JournalEntryAdded -= this.Journal_EntryAdded;
        }

        protected override InternalState GetCondition()
        {
            if ( World.Player.IsDestroyed || World.Player.IsDead )
                return InternalState.Failed;

            if ( TargetManager.LastTargetInfo.Serial == 0 )
                return InternalState.Failed;

            if ( _target == null || _target != TargetManager.LastTargetInfo.Serial)
            {
                _target = World.Mobiles.Get( TargetManager.LastTargetInfo.Serial);
                return InternalState.Failed;
            }

            if ( !_target.IsPoisoned )
                _target.Poison.Count = 0;

            return InternalState.IsReady;
        }
        
        public void EquipCandle()
        {
            Item backpack = World.Player.FindItemByLayer(Layer.Backpack);
            Item item = null;

            if (backpack != null)
                item = backpack.FindItem(0xA28); // candle

            if (item == null)
                return;

            GameActions.DoubleClick( item.Serial );
            //GameActions.Equip( item.Serial, Layer.TwoHanded, World.Player.Serial );

            World.Player.Spell.Action = SpellAction.Unknown;
        }
    }

    internal static class Automation
    {
        public static DateTime _timer = DateTime.Now;

        public static bool Active { get; private set; } = false;
        public static AIBot Bot { get; private set; } = new AIBot();

        public static bool IsEnabled = false;

        public static void Initialize()
        {
            CommandManager.Register( "5x", s => Toggle() );
        }

        public static void Toggle()
        {
            GameActions.Print( World.Player, String.Format( "5x:{0}abled", ( Automation.IsEnabled = !Automation.IsEnabled ) == true ? "En" : "Dis" ), (ushort)RandomHelper.GetValue( 0, 256 ) );
        }

        public static void Update()
        {
            if ( !IsEnabled )
                return;

            if (_timer <= DateTime.Now)
            {
                Automation.Active = Automation.Bot.Invoke();
            }
        }
    }
}
