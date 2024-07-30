namespace ClassicUO.Game.GameObjects
{
    using ClassicUO.Game.Cheats.AIBot;

    internal partial class Mobile
    {
        public SpellTimer Spell { get; set; } = new SpellTimer();
        public PoisonTimer Poison { get; set; } = new PoisonTimer();
    }
}