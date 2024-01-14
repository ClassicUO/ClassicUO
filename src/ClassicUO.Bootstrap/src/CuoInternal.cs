using System;

namespace ClassicUO.Game
{
    static class World
    {
        public static Map Map { get; } = new Map();
    }

    sealed class Map
    {
        public int GetMapZ(int x, int y, out sbyte groundZ, out sbyte staticZ)
        {
            Console.WriteLine("Invoked by reflection {0}", nameof(GetMapZ));

            groundZ = staticZ = 0;
            return 0;
        }
    }

    static class GameActions
    {
        public static void UsePrimaryAbility()
        {
            Console.WriteLine("Invoked by reflection {0}", nameof(UsePrimaryAbility));
        }

        public static void UseSecondaryAbility()
        {
            Console.WriteLine("Invoked by reflection {0}", nameof(UseSecondaryAbility));
        }
    }
}

namespace ClassicUO.Game.Scenes
{
    public sealed class LoginScene
    {
        public void Connect()
        {
            Console.WriteLine("Invoked by reflection {0}", nameof(Connect));
        }

        public void SelectServer()
        {
            Console.WriteLine("Invoked by reflection {0}", nameof(SelectServer));
        }

        public void SelectCharacter()
        {
            Console.WriteLine("Invoked by reflection {0}", nameof(SelectCharacter));
        }
    }
}