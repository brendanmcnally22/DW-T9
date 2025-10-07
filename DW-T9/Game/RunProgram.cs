using System;

namespace DW_T9.Game
{
    internal class RunProgram
    {
        static void Main(string[] args)
        {
            Console.Title = "DW_T9 Escape Game";
            Console.ForegroundColor = ConsoleColor.Green;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Clear();

            UI ui = new UI();
            Timer timer = new Timer(480); // 8 minutes
            Player player = new Player();
            GameContext ctx = new GameContext(ui, timer, player);

            Game game = new Game(ctx);
            game.Run();
        }
    }
}
