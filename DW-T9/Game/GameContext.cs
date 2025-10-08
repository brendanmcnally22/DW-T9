using System;

namespace DW_T9.Game
{
    public sealed class GameContext
    {
        public UI UI { get; }
        public Timer Timer { get; }
        public Player Player { get; }
        public AudioManager Audio { get; }   // ← NEW

        public GameContext(UI ui, Timer timer, Player player, AudioManager audio)
        {
            UI = ui;
            Timer = timer;
            Player = player;
            Audio = audio;
        }

        // Convenience helpers
        public void GiveItem(Item it) => Player.GiveItem(it);
        public bool HasItem(string idOrName) => Player.HasItem(idOrName);
        public void GiveCard(CardType card) => Player.AddCard(card);
    }
}
