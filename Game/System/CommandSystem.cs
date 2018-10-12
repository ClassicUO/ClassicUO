using ClassicUO.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.System
{
    public static class CommandSystem
    {
        private static readonly Dictionary<string, EventHandler> _commandDictionary = new Dictionary<string, EventHandler>();

        

        public static void Register(string commandName, EventHandler commandHandler)
        {
            
            if (_commandDictionary.ContainsKey(commandName))
            {
                Log.Message(LogTypes.Error, string.Format($"Attempted to register command: '{0}' twice."));
            }
            else
            {
                _commandDictionary.Add(commandName, commandHandler);
            }

        }

        public static void TriggerCommandHandler(string commandName)
        {
            if (_commandDictionary.TryGetValue(commandName, out EventHandler commandHandler))
            {
                commandHandler.Raise();
            }
            
        }

    }
}
