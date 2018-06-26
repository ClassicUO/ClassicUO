using System;

namespace ClassicUO
{
    class Program
    {
        static void Main(string[] args)
        {
            using (GameLoop game = new GameLoop())
            {
                game.Run();
            }
        }
    }
}
