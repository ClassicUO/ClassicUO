namespace ClassicUO.Game.Managers
{
    internal interface IPartyManager
    {
        uint Leader { get; set; }
        uint Inviter { get; set; }
        bool CanLoot { get; set; }
        PartyMember[] Members { get; }
        long PartyHealTimer { get; set; }
        uint PartyHealTarget { get; set; }

        bool Contains(uint serial);
        void Clear();
    }
}
