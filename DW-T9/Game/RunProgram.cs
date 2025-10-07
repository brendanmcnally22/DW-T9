using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DW_T9.Game
{
    public static class RunProgram
    {
        public static void Main(string[] args)
        {
            try
            {
                var game = new Game();
                game.Run();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Fatal error: " + ex.Message);
                Console.ResetColor();
            }
        }
    }
}
