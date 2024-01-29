using ClassicUO.Game.Scenes;
using System;

namespace ClassicUO
{
    static class Client
    {
        public static GameController Game { get; set; }
    }

    class GameController
    {
        // LoginScene is not supperted here
        private readonly Scene _scene = new GameScene();

        public T GetScene<T>() where T : Scene
        {
            Console.WriteLine("Invoked by reflection {0}", nameof(GetScene));
            return (T)_scene;
        }
    }
}

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

    public sealed class MacroManager : LinkedObject
    {
        public bool WaitingBandageTarget { get; set; }
        public long WaitForTargetTimer { get; set; }


        public void SetMacroToExecute(MacroObject macro)
        {
            Console.WriteLine("Invoked by reflection {0}", nameof(SetMacroToExecute));
        }

        public void Update()
        {
            Console.WriteLine("Invoked by reflection {0}", nameof(Update));
        }
    }

    class Macro : LinkedObject
    {
        public string Name { get; set; }
    }

    enum MacroType
    {
        RazorMacro = 70
    }

    enum MacroSubType
    {
        MSC_NONE = 0
    }

    public class MacroObject : LinkedObject
    {
        public object Code { get; set; }
        public object SubCode { get; set; }
    }

    public class MacroObjectString : MacroObject
    {
        public string Text { get; set; }
    }

    public abstract class LinkedObject
    {
        public bool IsEmpty => Items == null;
        public LinkedObject Previous, Next, Items;
    }
}

namespace ClassicUO.Game.Managers
{
    static class UIManager
    {
        public static void Add(object obj)
        {
            Console.WriteLine("Invoked by reflection {0}", nameof(Add));
        }

        public static void SavePosition(uint gumpId, object point)
        {
            Console.WriteLine("Invoked by reflection {0}", nameof(SavePosition));
        }
    }
}

namespace ClassicUO.Game.Scenes
{
    public sealed class GameScene : Scene
    {
        public MacroManager Macros { get; set; } = new MacroManager();
    }

    public sealed class LoginScene : Scene
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

    public abstract class Scene
    {

    }
}