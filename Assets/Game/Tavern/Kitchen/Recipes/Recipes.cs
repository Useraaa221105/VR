using System.Collections.Generic;
using UnityEngine;

namespace Podz
{
    public enum Spice { Salt, Pepper, Herbs, Fire }

    public class Recipe
    {
        public int Index;
        public string Name;
        public ItemId[] Items;
        public int Heat;                 // 1 слабый, 2 средний, 3 сильный (учитывается с этапа 2)
        public Dictionary<Spice, int> Spices = new Dictionary<Spice, int>(); // учитываются с этапа 3
        public float CookTime;
        public int Price;
        public int Stage;
        public bool NeedsDragon;
        public Color SoupColor;
        public string SpiceText
        {
            get
            {
                var parts = new List<string>();
                foreach (var kv in Spices) parts.Add($"{Recipes.SpiceName(kv.Key)} ×{kv.Value}");
                return string.Join(", ", parts);
            }
        }
    }

    /// <summary>Источник правды по рецептам (ТЗ, раздел 12.5) + сопоставление мультимножеств.</summary>
    public static class Recipes
    {
        public static readonly List<Recipe> All = new List<Recipe>
        {
            R(0, "Желейная похлёбка", new[] { ItemId.SlimeJelly, ItemId.SlimeJelly, ItemId.Cabbage }, 2, 8, 10, 1, new Color(.38f, .49f, .17f), (Spice.Salt, 1)),
            R(1, "Суп из грибных ножек", new[] { ItemId.MushroomLegs, ItemId.Potato, ItemId.Onion }, 2, 10, 18, 1, new Color(.56f, .39f, .20f), (Spice.Salt, 1)),
            R(2, "Суп из мандрагоры", new[] { ItemId.MandrakeRoot, ItemId.MushroomLegs, ItemId.Carrot }, 1, 12, 30, 2, new Color(0.8f, 0.6f, 0.3f), (Spice.Herbs, 2)),
            R(3, "Бульон из василиска", new[] { ItemId.BasiliskMeat, ItemId.Carrot, ItemId.Onion }, 3, 14, 45, 2, new Color(0.95f, 0.8f, 0.4f), (Spice.Pepper, 1), (Spice.Salt, 1)),
            R(4, "Рагу «Полный доспех»", new[] { ItemId.ShellfishMeat, ItemId.Potato, ItemId.Carrot, ItemId.Cabbage }, 2, 14, 60, 3, new Color(0.7f, 0.75f, 0.8f), (Spice.Pepper, 2)),
            R(5, "Суп из мимика", new[] { ItemId.MimicMeat, ItemId.MandrakeRoot, ItemId.Onion }, 1, 12, 80, 3, new Color(1f, 0.6f, 0.5f), (Spice.Herbs, 1), (Spice.Salt, 1)),
            R(6, "Драконий суп", new[] { ItemId.DragonMeat, ItemId.BasiliskMeat, ItemId.Onion, ItemId.Carrot }, 3, 18, 150, 3, new Color(0.9f, 0.2f, 0.1f), (Spice.Fire, 2)),
        };

        static readonly SavedDungeonAssets.RecipeTuning[] Defaults = ExportTuning();

        public static SavedDungeonAssets.RecipeTuning[] ExportTuning() =>
            All.ConvertAll(recipe => new SavedDungeonAssets.RecipeTuning
            {
                Name = recipe.Name, CookSeconds = recipe.CookTime, Price = recipe.Price, Heat = recipe.Heat
            }).ToArray();

        public static void ApplyTuning(SavedDungeonAssets.RecipeTuning[] values)
        {
            if (values == null || values.Length != All.Count) values = Defaults;
            for (int i = 0; i < All.Count; i++)
            {
                var value = values[i];
                All[i].CookTime = value.CookSeconds > 0 ? value.CookSeconds : Defaults[i].CookSeconds;
                All[i].Price = Mathf.Max(0, value.Price);
                All[i].Heat = value.Heat >= 1 && value.Heat <= 3 ? value.Heat : Defaults[i].Heat;
            }
        }

        static Recipe R(int i, string name, ItemId[] items, int heat, float cook, int price, int stage, Color c, params (Spice, int)[] spices)
        {
            var r = new Recipe { Index = i, Name = name, Items = items, Heat = heat, CookTime = cook, Price = price, Stage = stage, SoupColor = c, NeedsDragon = i == 6 };
            foreach (var s in spices) r.Spices[s.Item1] = s.Item2;
            return r;
        }

        public static string SpiceName(Spice s)
        {
            switch (s) { case Spice.Salt: return "соль"; case Spice.Pepper: return "перец"; case Spice.Herbs: return "травы"; default: return "огн. порошок"; }
        }

        public static string HeatName(int h) => h <= 1 ? "слабый" : h == 2 ? "средний" : "сильный";

        public static bool Available(Recipe r, int stage, bool dragon) => r.Stage <= stage && (!r.NeedsDragon || dragon);

        public static List<Recipe> ForStage(int stage, bool dragon)
        {
            var l = new List<Recipe>();
            foreach (var r in All) if (Available(r, stage, dragon)) l.Add(r);
            return l;
        }

        /// <summary>Совпадает ли набор полностью с рецептом.</summary>
        public static Recipe Match(List<ItemId> contents, int stage, bool dragon)
        {
            foreach (var r in All)
                if (Available(r, stage, dragon) && SameMultiset(r.Items, contents)) return r;
            return null;
        }

        /// <summary>Может ли набор ещё стать каким-то рецептом (является подмножеством).</summary>
        public static bool CanBecome(List<ItemId> contents, int stage, bool dragon)
        {
            foreach (var r in All)
                if (Available(r, stage, dragon) && IsSubMultiset(contents, r.Items)) return true;
            return false;
        }

        static bool SameMultiset(ItemId[] a, List<ItemId> b) => a.Length == b.Count && IsSubMultiset(b, a);

        static bool IsSubMultiset(List<ItemId> sub, ItemId[] of)
        {
            var left = new List<ItemId>(of);
            foreach (var x in sub) if (!left.Remove(x)) return false;
            return true;
        }

        public static bool HasIngredients(Recipe r)
        {
            var need = new Dictionary<ItemId, int>();
            foreach (var i in r.Items) { if (!Items.Get(i).Monster) continue; need.TryGetValue(i, out var n); need[i] = n + 1; }
            foreach (var kv in need) if (Inventory.Fridge.Count(kv.Key) < kv.Value) return false;
            return true;
        }

        public static string MissingIngredients(Recipe r)
        {
            if (r == null) return "";
            foreach (var bowl in Bowl.All) if (bowl != null && bowl.Recipe == r && bowl.Courier == null && bowl.ServedTo == null) return "";
            foreach (var pot in Cauldron.All) if (pot != null && pot.Recipe == r && (pot.State == SoupState.Cooking || pot.State == SoupState.Ready)) return "";
            var need = new Dictionary<ItemId, int>();
            foreach (var id in r.Items) if (Items.Get(id).Monster) { need.TryGetValue(id, out var n); need[id] = n + 1; }
            var missing = new List<string>();
            foreach (var kv in need)
            {
                int loose = 0; foreach (var ing in Ingredient.All) if (ing != null && !ing.Despawning && !ing.Spoiled && ing.Id == kv.Key) loose++;
                int n = kv.Value - Inventory.Fridge.Count(kv.Key) - loose;
                if (n > 0) missing.Add($"{Items.Name(kv.Key)} ×{n}");
            }
            return string.Join(", ", missing);
        }

        public static string ItemsText(Recipe r)
        {
            var parts = new List<string>();
            foreach (var i in r.Items) parts.Add(Items.Name(i));
            return string.Join(" + ", parts);
        }
    }
}
