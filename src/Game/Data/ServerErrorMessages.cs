using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.Data
{
    static class ServerErrorMessages
    {
        public static string[] LoginErrors { get; } =
        {
            "Incorrect password",
            "This character does not exist any more!",
            "This character already exists.",
            "Could not attach to game server.",
            "Could not attach to game server.",
            "A character is already logged in.",
            "Synchronization Error.",
            "You have been idle for to long.",
            "Could not attach to game server.",
            "Character transfer in progress."
        };

        public static string[] PickUpErrors { get; } =
        {
            "You can not pick that up.",
            "That is too far away.",
            "That is out of sight.",
            "That item does not belong to you.  You'll have to steal it.",
            "You are already holding an item."
        };
    }
}
