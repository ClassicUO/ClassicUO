namespace ClassicUO.Game.UI.Gumps
{
    internal interface IActionBarDropTarget : ISpellDropTarget
    {
        void AcceptSkill(int skillIndex);
        void AcceptAbility(int abilityIndex);
    }
}
