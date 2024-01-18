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
            Global.Host.ReflectionUsePrimaryAbility();
            Console.WriteLine("Invoked by reflection {0}", nameof(UsePrimaryAbility));
        }

        public static void UseSecondaryAbility()
        {
            Global.Host.ReflectionUseSecondaryAbility();
            Console.WriteLine("Invoked by reflection {0}", nameof(UseSecondaryAbility));
        }
    }

    static class Pathfinder
    {
        public static bool AutoWalking
        {
            get => Global.Host.ReflectionAutowalking(-1);
            set => Global.Host.ReflectionAutowalking((sbyte)(value ? 1 : 0));
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