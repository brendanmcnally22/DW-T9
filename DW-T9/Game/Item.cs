using System;

namespace DW_T9.Game
{
    public sealed class Item
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public int Points { get; }

        public Item(string id, string name, string description, int points = 0)
        {
            Id = id;
            Name = name;
            Description = description;
            Points = points;
        }
    }
}

