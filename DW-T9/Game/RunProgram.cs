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

            var ui = new UI { TotalSeconds = 480 };
            var timer = new Timer(480);
            var player = new Player();
            var audio = new AudioManager("Assets/Audio");

            // Start menu music
            audio.PlayMusic(SoundId.MenuTheme);

            var ctx = new GameContext(ui, timer, player, audio);
            var game = new Game(ctx);

            game.Run();

            audio.Dispose();
        }
    }
}
