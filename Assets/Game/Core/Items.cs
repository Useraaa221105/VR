using System.Collections.Generic;
using UnityEngine;

namespace Podz
{
    public enum ItemId
    {
        SlimeJelly, MushroomLegs, MandrakeRoot, BasiliskMeat, ShellfishMeat, MimicMeat, DragonMeat,
        Cabbage, Potato, Carrot, Onion
    }

    public enum PrepKind { None, Knife, Cleaver, Hammer, WashOnly, WashThenKnife, Grill }

    public struct ItemInfo
    {
        public string Name;
        public Color Color;
        public PrepKind Prep;
        public int Hits;        // сколько резов/ударов нужно
        public bool Monster;    // добывается в подземелье
        public string PrepText;
    }

    public static class Items
    {
        static readonly Dictionary<ItemId, ItemInfo> _info = new Dictionary<ItemId, ItemInfo>
        {
            { ItemId.SlimeJelly,   new ItemInfo { Name = "Желе слайма",       Color = new Color(0.3f, 0.95f, 0.4f), Prep = PrepKind.Knife,  Hits = 2, Monster = true, PrepText = "нарезать кубиками (2 реза ножом)" } },
            { ItemId.MushroomLegs, new ItemInfo { Name = "Ножки гриба",       Color = new Color(0.95f, 0.85f, 0.65f), Prep = PrepKind.Knife, Hits = 2, Monster = true, PrepText = "нарезать кольцами (2 реза ножом)" } },
            { ItemId.MandrakeRoot, new ItemInfo { Name = "Корень мандрагоры", Color = new Color(0.8f, 0.6f, 0.35f), Prep = PrepKind.WashThenKnife, Hits = 3, Monster = true, PrepText = "помыть в мойке, затем 3 реза" } },
            { ItemId.BasiliskMeat, new ItemInfo { Name = "Мясо василиска",    Color = new Color(0.95f, 0.75f, 0.55f), Prep = PrepKind.Cleaver, Hits = 3, Monster = true, PrepText = "порубить тесаком (3 удара)" } },
            { ItemId.ShellfishMeat,new ItemInfo { Name = "Мясо моллюсков",    Color = new Color(0.75f, 0.85f, 0.9f), Prep = PrepKind.WashOnly, Hits = 0, Monster = true, PrepText = "промыть в мойке" } },
            { ItemId.MimicMeat,    new ItemInfo { Name = "Мясо мимика",       Color = new Color(1f, 0.55f, 0.45f), Prep = PrepKind.Hammer, Hits = 2, Monster = true, PrepText = "расколоть панцирь (2 удара молотком)" } },
            { ItemId.DragonMeat,   new ItemInfo { Name = "Драконье мясо",     Color = new Color(0.85f, 0.15f, 0.1f), Prep = PrepKind.Grill, Hits = 0, Monster = true, PrepText = "обжарить на гриле (4 с)" } },
            { ItemId.Cabbage,      new ItemInfo { Name = "Капуста",           Color = new Color(0.55f, 0.85f, 0.4f), Prep = PrepKind.Knife, Hits = 3, PrepText = "нашинковать (3 реза)" } },
            { ItemId.Potato,       new ItemInfo { Name = "Картофель",         Color = new Color(0.8f, 0.68f, 0.45f), Prep = PrepKind.Knife, Hits = 2, PrepText = "нарезать (2 реза)" } },
            { ItemId.Carrot,       new ItemInfo { Name = "Морковь",           Color = new Color(1f, 0.5f, 0.1f), Prep = PrepKind.Knife, Hits = 2, PrepText = "нарезать (2 реза)" } },
            { ItemId.Onion,        new ItemInfo { Name = "Лук",               Color = new Color(0.9f, 0.85f, 0.75f), Prep = PrepKind.Knife, Hits = 2, PrepText = "нарезать (2 реза)" } },
        };

        public static ItemInfo Get(ItemId id) => _info[id];
        public static string Name(ItemId id) => _info[id].Name;
        public static readonly ItemId[] MonsterItems = { ItemId.SlimeJelly, ItemId.MushroomLegs, ItemId.MandrakeRoot, ItemId.BasiliskMeat, ItemId.ShellfishMeat, ItemId.MimicMeat, ItemId.DragonMeat };
        public static readonly ItemId[] Vegetables = { ItemId.Cabbage, ItemId.Potato, ItemId.Carrot, ItemId.Onion };
    }

    /// <summary>Хранилище «предмет → количество» с лимитом. Используется для рюкзака (100 слотов) и ледника.</summary>
    public class ItemStore
    {
        readonly Dictionary<ItemId, int> _items = new Dictionary<ItemId, int>();
        public readonly int Capacity;
        public readonly int PerItemLimit;

        public ItemStore(int capacity, int perItemLimit) { Capacity = capacity; PerItemLimit = perItemLimit; }

        public int Total { get { int s = 0; foreach (var kv in _items) s += kv.Value; return s; } }
        public int Count(ItemId id) => _items.TryGetValue(id, out var n) ? n : 0;
        public IEnumerable<KeyValuePair<ItemId, int>> All => _items;

        /// <summary>Добавляет сколько влезет, возвращает сколько добавлено.</summary>
        public int Add(ItemId id, int n)
        {
            int free = Capacity - Total;
            int perFree = PerItemLimit - Count(id);
            int add = Mathf.Max(0, Mathf.Min(n, Mathf.Min(free, perFree)));
            if (add > 0) _items[id] = Count(id) + add;
            return add;
        }

        public bool Take(ItemId id, int n = 1)
        {
            if (n <= 0) return false;
            int c = Count(id);
            if (c < n) return false;
            if (c == n) _items.Remove(id); else _items[id] = c - n;
            return true;
        }

        public void Clear() => _items.Clear();

        /// <summary>Потеря половины при смерти.</summary>
        public void LoseHalf()
        {
            var keys = new List<ItemId>(_items.Keys);
            foreach (var k in keys)
            {
                int keep = _items[k] / 2;
                if (keep <= 0) _items.Remove(k); else _items[k] = keep;
            }
        }
    }
}
