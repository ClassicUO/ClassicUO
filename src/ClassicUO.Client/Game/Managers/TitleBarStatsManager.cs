using ClassicUO.Configuration;
using Microsoft.Xna.Framework;
using System;

namespace ClassicUO.Game.Managers
{
    public enum TitleBarStatsMode
    {
        Text = 0,
        Percent = 1,
        ProgressBar = 2
    }

    public static class TitleBarStatsManager
    {
        public static void UpdateTitleBar()
        {
            if (ProfileManager.CurrentProfile == null)
                return;

            // only update when we actually have a logged-in character
            if (!ProfileManager.CurrentProfile.EnableTitleBarStats || World.Player == null || !World.InGame)
                return;

            string statsText = GenerateStatsText();
            string title = string.IsNullOrEmpty(World.Player.Name)
                ? statsText
                : $"{World.Player.Name} - {statsText}";

            Client.Game.SetWindowTitle(title, skipStats: true);
        }

        private static string GenerateStatsText()
        {
            // guard against cases where we're not actually in game yet
            if (ProfileManager.CurrentProfile == null || World.Player == null || !World.InGame)
                return string.Empty;

            switch (ProfileManager.CurrentProfile.TitleBarStatsMode)
            {
                case TitleBarStatsMode.Text:
                    return $"HP {World.Player.Hits}/{World.Player.HitsMax}, MP {World.Player.Mana}/{World.Player.ManaMax}, SP {World.Player.Stamina}/{World.Player.StaminaMax}";

                case TitleBarStatsMode.Percent:
                    int hpPercent = World.Player.HitsMax > 0 ? (World.Player.Hits * 100) / World.Player.HitsMax : 100;
                    int mpPercent = World.Player.ManaMax > 0 ? (World.Player.Mana * 100) / World.Player.ManaMax : 100;
                    int spPercent = World.Player.StaminaMax > 0 ? (World.Player.Stamina * 100) / World.Player.StaminaMax : 100;
                    return $"HP {hpPercent}%, MP {mpPercent}%, SP {spPercent}%";

                case TitleBarStatsMode.ProgressBar:
                    string hpBar = GenerateProgressBar(World.Player.Hits, World.Player.HitsMax);
                    string mpBar = GenerateProgressBar(World.Player.Mana, World.Player.ManaMax);
                    string spBar = GenerateProgressBar(World.Player.Stamina, World.Player.StaminaMax);
                    return $"HP[{hpBar}] {World.Player.Hits}/{World.Player.HitsMax}, MP[{mpBar}] {World.Player.Mana}/{World.Player.ManaMax}, SP[{spBar}] {World.Player.Stamina}/{World.Player.StaminaMax}";
                default:
                    return $"HP {World.Player.Hits}/{World.Player.HitsMax}, MP {World.Player.Mana}/{World.Player.ManaMax}, SP {World.Player.Stamina}/{World.Player.StaminaMax}";
            }
        }

        private static string GenerateProgressBar(ushort current, ushort max)
        {
            const int barLength = 8;
            const char fullBlock = '|';
            const char partialBlock = '\\';
            const char emptyBlock = ' ';

            if (max == 0)
                return new string(emptyBlock, barLength);

            float percentage = (float)current / max;
            int filledBlocks = (int)Math.Floor(percentage * barLength);
            bool hasPartial = (percentage * barLength) - filledBlocks > 0.5f;

            string result = "";

            for (int i = 0; i < filledBlocks; i++)
                result += fullBlock;

            if (hasPartial && filledBlocks < barLength)
            {
                result += partialBlock;
                filledBlocks++;
            }

            while (result.Length < barLength)
                result += emptyBlock;

            return result;
        }

        public static void ForceUpdate() => UpdateTitleBar();

        /// <summary>
        /// Health bar colour similar to nameplates: green when high, orange at mid and red when low.
        /// </summary>
        public static Color GetHealthColor(ushort current, ushort max)
        {
            if (max == 0)
                return new Color(50, 180, 50); // default green
            float pct = (float)current / max;
            if (pct <= 0.25f)
                return Color.Red;
            if (pct <= 0.5f)
                return Color.Orange;
            return new Color(50, 180, 50);
        }

        public static string GetPreviewText()
        {
            if (ProfileManager.CurrentProfile == null)
                return string.Empty;

            if (World.Player == null)
            {
                switch (ProfileManager.CurrentProfile.TitleBarStatsMode)
                {
                    case TitleBarStatsMode.Text:
                        return "PlayerName - HP 85/100, MP 42/50, SP 95/100";
                    case TitleBarStatsMode.Percent:
                        return "PlayerName - HP 85%, MP 84%, SP 95%";
                    case TitleBarStatsMode.ProgressBar:
                        return "PlayerName - HP[||||\\   ] 85/100, MP[||||\\   ] 42/50, SP[||||\\   ] 95/100";
                    default:
                        return "PlayerName - HP 85/100, MP 42/50, SP 95/100";
                }
            }

            string statsText = GenerateStatsText();
            return string.IsNullOrEmpty(World.Player.Name) ? statsText : $"{World.Player.Name} - {statsText}";
        }
    }
}
