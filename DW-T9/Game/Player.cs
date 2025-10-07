using System;
using System.Collections.Generic;

namespace DW_T9.Game
{
    public sealed class Player
    {
        public Inventory Inventory { get; } = new Inventory();
        public HashSet<CardType> CollectedCards { get; } = new HashSet<CardType>();

        public void GiveItem(Item item) => Inventory.Add(item);
        public bool HasItem(string nameOrId) => Inventory.Has(nameOrId);

        public void AddCard(CardType card) => CollectedCards.Add(card);
        public bool HasCard(CardType card) => CollectedCards.Contains(card);
    }

    public enum CardType
    {
        None,
        Beetle, // tutorial foyer
        Rat,    // living (grid)
        Raven,  // hallway (clock)
        Snake   // bedroom (bust)
    }
}
