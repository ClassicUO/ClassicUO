using ClassicUO.Configuration;

namespace ClassicUO.LegionScripting.PyClasses
{
    public class PyProfile
    {
        public static string CharacterName => ProfileManager.CurrentProfile?.CharacterName ?? "";
        public static string ServerName => ProfileManager.CurrentProfile?.ServerName ?? "";
        public static uint LootBagSerial => ProfileManager.CurrentProfile?.GrabBagSerial ?? 0;
        public static uint FavoriteBagSerial => 0;
        public static int MoveItemDelay => ProfileManager.CurrentProfile?.MoveMultiObjectDelay ?? 1000;
        public static bool AutoLootEnabled => ProfileManager.CurrentProfile?.EnableAutoLoot ?? false;
    }
}
