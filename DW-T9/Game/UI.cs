using System;
using System.Threading;

namespace DW_T9.Game
{
    public enum JumpscareId { Shadow, Slam, Whisper, Custom }

    public sealed class UI
    {
        public int TypeDelayMs { get; set; } = 10;
        public int TotalSeconds { get; set; } = 480; // used for the time progress bar

        public void Clear()
        {
            try { Console.Clear(); }
            catch { Console.WriteLine("\n--------------------------------\n"); }
        }

        public void Frame(string roomName, int secondsLeft, int cardsInserted, int cardsTotal)
        {
            Console.WriteLine("========================================");
            Console.WriteLine($" THE MAD MANOR  | Room: {roomName}");
            RenderTimer(secondsLeft, TotalSeconds); // ← shows mm:ss + progress bar
            Console.WriteLine($" Cards: {cardsInserted}/{cardsTotal}");
            Console.WriteLine("========================================\n");
            Console.WriteLine("Commands below");
            Console.WriteLine(" look | inspect <x> | pickup <x> | inventory | use <x> | enter <ans> | insert <card> | go <room> | back | help | quit\n");
        }

        // --- Timer rendering helpers ---
        public void RenderTimer(int secondsLeft, int totalSeconds)
        {
            int mm = secondsLeft / 60;
            int ss = secondsLeft % 60;

            int barWidth = 24;
            double pct = Math.Clamp((double)secondsLeft / Math.Max(1, totalSeconds), 0, 1);
            int fill = (int)(pct * barWidth);
            string bar = "[" + new string('#', fill) + new string('-', Math.Max(0, barWidth - fill)) + "]";

            Console.WriteLine($" Time: {mm:00}:{ss:00} {bar}");
        }

        public void RenderTimerSimple(int secondsLeft)
        {
            int mm = secondsLeft / 60;
            int ss = secondsLeft % 60;
            Console.WriteLine($" Time left: {mm:00}:{ss:00}");
        }

        public void Type(string text)
        {
            if (TypeDelayMs <= 0) { Console.WriteLine(text); return; }
            foreach (char c in text) { Console.Write(c); Thread.Sleep(TypeDelayMs); }
            Console.WriteLine();
        }

        public void Toast(string text) => Console.WriteLine(text);
        public void Hint(string text) => Console.WriteLine($"[{text}]");
        public void Rule() => Console.WriteLine("----------------------------------------");

        // Intro now just prints (no auto-clear after)
        public void ShowIntro()
        {
            Clear();
            var prev = TypeDelayMs;
            TypeDelayMs = Math.Max(12, prev);   // make intro readable
            Type("THE MAD MANOR");
            Type("");
            Type("You and your buddy delivered pizza to the WRONG house... ");
            Type("");
            Type("Find all 4 cards to put together the riddle!, then use the Final Key to escape.");
            Type("");
            TypeDelayMs = prev;
        }

        public void ShowJumpscare(JumpscareId id, int durationMs = 600, string? customAscii = null)
        {
            string art = customAscii ?? GetDefaultArt(id);
            Flash(2, 70);
            TryBeep(500, 80); TryBeep(120, 80);

            Console.WriteLine();
            Console.WriteLine("###############  JUMPSCARE  ###############");
            if (!string.IsNullOrWhiteSpace(art)) Console.WriteLine(art);
            Console.WriteLine("###########################################");
            Thread.Sleep(Math.Max(200, durationMs));

            // Do NOT auto-clear; we’ll keep the scare visible until the next command clears.
        }

        private void Flash(int times, int pauseMs)
        {
            for (int i = 0; i < times; i++)
            {
                InvertColors(true);
                Thread.Sleep(pauseMs);
                InvertColors(false);
                Thread.Sleep(pauseMs);
            }
        }

        private void InvertColors(bool on)
        {
            try
            {
                if (on) { Console.BackgroundColor = ConsoleColor.Black; Console.ForegroundColor = ConsoleColor.Green; }
                else { Console.ResetColor(); Console.ForegroundColor = ConsoleColor.Green; Console.BackgroundColor = ConsoleColor.Black; }
                Console.WriteLine();
            }
            catch { }
        }

        private void TryBeep(int f, int ms) { try { Console.Beep(f, ms); } catch { } }

        private string GetDefaultArt(JumpscareId id) => id switch
        {
            JumpscareId.Shadow => @"
   .-.
  (o o)   A shadow flickers—
   |O|    too close.
   | |",
            JumpscareId.Slam => @"
+---------+
|  BANG!  |
+---------+",
            JumpscareId.Whisper => @"
  ~ ~ ~
 ( sss )  A whisper brushes your ear.
  ~ ~ ~",
            _ => ""
        };
    }
}
