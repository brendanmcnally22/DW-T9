using System;

namespace DW_T9.Game
{
    public sealed class CommandRouter
    {
        public string Verb { get; private set; } = "";
        public string Noun { get; private set; } = "";

        public void Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                Verb = "";
                Noun = "";
                return;
            }

            string[] parts = input.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            Verb = parts[0].ToLowerInvariant();
            Noun = parts.Length > 1 ? parts[1].ToLowerInvariant() : "";
        }
    }
}
