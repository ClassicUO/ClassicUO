using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;


namespace ClassicUO.Dust765.Managers
{
    public enum InternalAction : int
    {
        Unknown = 0,
        Bola = 1001,
    }

    internal delegate void ChatCallback<T>(Mobile mob, T value);

    internal static class ChatHandlers
    {
        public static event ChatCallback<SpellAction> OnSpellCast;
        public static event ChatCallback<InternalAction> OnTargetAction;

        public static void InvokeTargetAction(Mobile mob, InternalAction action)
        {
            if (OnTargetAction != null)
                OnTargetAction(mob, action);
        }

        public static void InvokeSpellCast(Mobile mob, SpellAction spell)
        {
            if (OnSpellCast != null)
                OnSpellCast(mob, spell);
        }
    }

    internal static class ChatManager
    {
        public static void Initialize()
        {
            Chat.MessageReceived += Chat_MessageReceived;
        }

        private static void Chat_MessageReceived(object sender, MessageEventArgs e)
        {
            if (e.Parent is Mobile mob)
            {
                if (e.Type == MessageType.Spell)
                {
                    SpellAction spell = SpellAction.Unknown;

                    foreach (var spellDefinition in SpellsMagery.GetAllSpells.Values)
                    {
                        if (spellDefinition.Name == e.Text || spellDefinition.PowerWords == e.Text)
                        {
                            spell = (SpellAction)spellDefinition.ID;
                            break;
                        }
                    }
                    ChatHandlers.InvokeSpellCast(mob, spell);
                }
                else if (e.Type == MessageType.Emote)
                {
                    InternalAction action = InternalAction.Unknown;

                    switch (e.Cliloc)
                    {
                        default:
                            switch (e.Hue)
                            {
                                default:
                                    action = InternalAction.Unknown;
                                    break;

                                case 0x3B2:
                                    action = InternalAction.Bola;
                                    break;
                            }
                            break;

                        case 1049632:
                            // RunUO\Scripts\Items\Misc\Bola.cs - L56
                            // from.LocalOverheadMessage( MessageType.Emote, 0x3B2, 1049632 ); // * You begin to swing the bola...*
                            action = InternalAction.Bola;
                            break;
                    }
                    ChatHandlers.InvokeTargetAction(mob, action);
                }
            }
        }
    }
}
