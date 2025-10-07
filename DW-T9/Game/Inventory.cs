using System;
using System.Collections.Generic;
using System.Linq;

namespace DW_T9.Game
{
    public sealed class Inventory
    {
        private readonly List<Item> _items = new();

        public bool Add(Item item)
        {
            if (_items.Any(i => i.Id.Equals(item.Id, StringComparison.OrdinalIgnoreCase)))
                return false;
            _items.Add(item);
            return true;
        }

        public bool Remove(string nameOrId)
        {
            var it = Find(nameOrId);
            if (it != null)
            {
                _items.Remove(it);
                return true;
            }
            return false;
        }

        public bool Has(string nameOrId) => Find(nameOrId) != null;

        public Item? Find(string nameOrId)
        {
            var key = nameOrId.ToLowerInvariant();
            return _items.FirstOrDefault(i =>
                i.Id.ToLowerInvariant() == key ||
                i.Name.ToLowerInvariant() == key);
        }

        public string ListAll()
        {
            if (_items.Count == 0) return "You have no Keys yet!.";
            return "Inventory:\n" + string.Join("\n",
                _items.Select(i => $"- \"{i.Name}\": {i.Description}"));
        }
    }
}
