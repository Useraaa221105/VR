using System.Collections.Generic;
using UnityEngine;

namespace Podz
{
    /// <summary>
    /// Узнаваемые 3D-фигурки продуктов из примитивов (без коллайдеров). Одна функция для ледника, витрины,
    /// карточек заказа/рецепта и самих продуктов на кухне. size — примерный размер фигурки в метрах.
    /// </summary>
    public static class FoodModels
    {
        static float _s = 1f; // размер текущей фигурки: координаты ниже — в долях размера
        static GameObject P(PrimitiveType t, string n, Transform p, Vector3 pos, Vector3 size, Color c, Vector3? e = null, float glow = 0f)
        {
            var g = Prim.Part(t, n, p, pos * _s, size * _s, c, e);
            if (glow > 0) Prim.Glow(g, c * glow);
            return g;
        }

        public static Transform Build(ItemId id, Transform parent, float size, string name = null)
        {
            var root = new GameObject(name ?? ("Food_" + id)).transform;
            root.SetParent(parent, false);
            var t = new GameObject("Model").transform;
            t.SetParent(root, false);
            _s = size;
            var S = PrimitiveType.Sphere; var C = PrimitiveType.Cylinder; var B = PrimitiveType.Cube;
            switch (id)
            {
                case ItemId.SlimeJelly: // кубик желе с бликом и «ядром»
                    P(S, "Jelly", t, new Vector3(0, 0.38f, 0), new Vector3(0.9f, 0.7f, 0.9f), new Color(0.35f, 0.95f, 0.45f), null, 0.25f);
                    P(S, "Core", t, new Vector3(0, 0.36f, 0), new Vector3(0.4f, 0.32f, 0.4f), new Color(0.15f, 0.6f, 0.25f));
                    P(S, "Shine", t, new Vector3(-0.2f, 0.6f, -0.28f), new Vector3(0.2f, 0.12f, 0.1f), Color.white, null, 0.6f);
                    root.gameObject.AddComponent<FoodWobble>();
                    break;
                case ItemId.MushroomLegs: // две ножки гриба с кусочком шляпки
                    for (int s = -1; s <= 1; s += 2)
                    {
                        P(C, "Leg", t, new Vector3(s * 0.2f, 0.35f, 0), new Vector3(0.28f, 0.7f, 0.28f), new Color(0.96f, 0.9f, 0.75f), new Vector3(0, 0, s * 12f));
                        P(S, "Foot", t, new Vector3(s * 0.22f, 0.06f, 0), new Vector3(0.38f, 0.16f, 0.38f), new Color(0.85f, 0.78f, 0.6f));
                        P(S, "CapBit", t, new Vector3(s * 0.15f, 0.72f, 0), new Vector3(0.4f, 0.16f, 0.4f), new Color(0.85f, 0.25f, 0.2f));
                    }
                    break;
                case ItemId.MandrakeRoot: // корень с «лицом» и листьями
                    P(S, "Root", t, new Vector3(0, 0.3f, 0), new Vector3(0.5f, 0.6f, 0.5f), new Color(0.82f, 0.62f, 0.38f));
                    P(C, "Tail", t, new Vector3(0, 0.05f, 0), new Vector3(0.12f, 0.3f, 0.12f), new Color(0.7f, 0.5f, 0.3f), new Vector3(0, 0, 20));
                    for (int i = 0; i < 3; i++)
                        P(B, "Leaf", t, new Vector3(0, 0.75f, 0), new Vector3(0.12f, 0.45f, 0.03f), new Color(0.3f, 0.75f, 0.25f), new Vector3(0, i * 60f, -25f + i * 25f));
                    P(S, "Eye", t, new Vector3(-0.1f, 0.38f, -0.23f), Vector3.one * 0.07f, Color.black);
                    P(S, "Eye", t, new Vector3(0.1f, 0.38f, -0.23f), Vector3.one * 0.07f, Color.black);
                    P(S, "Mouth", t, new Vector3(0, 0.24f, -0.24f), new Vector3(0.12f, 0.06f, 0.04f), new Color(0.35f, 0.15f, 0.1f));
                    break;
                case ItemId.BasiliskMeat: // окорок на кости
                    P(S, "Meat", t, new Vector3(0.1f, 0.3f, 0), new Vector3(0.7f, 0.55f, 0.55f), new Color(0.9f, 0.55f, 0.4f));
                    P(S, "Skin", t, new Vector3(0.15f, 0.42f, 0), new Vector3(0.55f, 0.32f, 0.45f), new Color(0.85f, 0.65f, 0.3f));
                    P(C, "Bone", t, new Vector3(-0.38f, 0.3f, 0), new Vector3(0.12f, 0.4f, 0.12f), new Color(0.96f, 0.94f, 0.88f), new Vector3(0, 0, 90));
                    P(S, "Knob", t, new Vector3(-0.6f, 0.34f, 0), Vector3.one * 0.17f, new Color(0.96f, 0.94f, 0.88f));
                    P(S, "Knob", t, new Vector3(-0.6f, 0.24f, 0), Vector3.one * 0.15f, new Color(0.96f, 0.94f, 0.88f));
                    break;
                case ItemId.ShellfishMeat: // раковина с мясом
                    P(S, "ShellBottom", t, new Vector3(0, 0.12f, 0), new Vector3(0.85f, 0.2f, 0.7f), new Color(0.55f, 0.65f, 0.75f));
                    P(S, "ShellTop", t, new Vector3(0, 0.42f, 0.18f), new Vector3(0.85f, 0.18f, 0.7f), new Color(0.62f, 0.72f, 0.82f), new Vector3(-35, 0, 0));
                    P(S, "Meat", t, new Vector3(0, 0.22f, -0.05f), new Vector3(0.5f, 0.18f, 0.4f), new Color(1f, 0.75f, 0.7f));
                    for (int i = -1; i <= 1; i++) P(B, "Rib", t, new Vector3(i * 0.2f, 0.47f, 0.2f), new Vector3(0.04f, 0.04f, 0.55f), new Color(0.45f, 0.55f, 0.65f), new Vector3(-35, i * 15f, 0));
                    break;
                case ItemId.MimicMeat: // крабовая клешня/нога мимика
                    for (int i = 0; i < 3; i++)
                        P(C, "Seg", t, new Vector3(-0.3f + i * 0.28f, 0.2f + i * 0.08f, 0), new Vector3(0.28f - i * 0.03f, 0.3f, 0.28f - i * 0.03f), new Color(0.95f, 0.38f, 0.25f), new Vector3(0, 0, 70));
                    P(S, "Claw", t, new Vector3(0.45f, 0.45f, 0.05f), new Vector3(0.3f, 0.35f, 0.22f), new Color(0.95f, 0.35f, 0.22f));
                    P(S, "Claw", t, new Vector3(0.45f, 0.45f, -0.08f), new Vector3(0.22f, 0.3f, 0.12f), new Color(0.85f, 0.3f, 0.2f), new Vector3(0, 0, 30));
                    P(S, "Meat", t, new Vector3(-0.47f, 0.17f, 0), new Vector3(0.2f, 0.24f, 0.24f), new Color(1f, 0.95f, 0.9f));
                    break;
                case ItemId.DragonMeat: // мраморный стейк с тлеющими прожилками
                    P(B, "Steak", t, new Vector3(0, 0.15f, 0), new Vector3(0.85f, 0.28f, 0.6f), new Color(0.75f, 0.12f, 0.1f), new Vector3(0, 15, 0));
                    for (int i = 0; i < 3; i++) P(B, "Marble", t, new Vector3(-0.2f + i * 0.2f, 0.3f, 0), new Vector3(0.05f, 0.02f, 0.5f), new Color(1f, 0.55f, 0.2f), new Vector3(0, 15 + i * 20, 0), 1.6f);
                    P(B, "Fat", t, new Vector3(0, 0.15f, 0.31f), new Vector3(0.8f, 0.26f, 0.05f), new Color(0.98f, 0.9f, 0.8f), new Vector3(0, 15, 0));
                    break;
                case ItemId.Cabbage:
                    P(S, "Head", t, new Vector3(0, 0.4f, 0), Vector3.one * 0.75f, new Color(0.6f, 0.88f, 0.45f));
                    for (int i = 0; i < 4; i++) P(S, "Leaf", t, new Vector3(0, 0.38f, 0), new Vector3(0.85f, 0.5f, 0.12f), new Color(0.45f, 0.78f, 0.35f), new Vector3(0, i * 45f, 0));
                    break;
                case ItemId.Potato:
                    P(S, "Potato", t, new Vector3(0, 0.25f, 0), new Vector3(0.8f, 0.5f, 0.55f), new Color(0.78f, 0.62f, 0.4f), new Vector3(0, 20, 0));
                    for (int i = 0; i < 4; i++) P(S, "Eye", t, new Vector3(-0.25f + i * 0.17f, 0.36f + (i % 2) * 0.08f, -0.24f), Vector3.one * 0.05f, new Color(0.45f, 0.32f, 0.2f));
                    break;
                case ItemId.Carrot:
                    for (int i = 0; i < 4; i++) P(C, "Body", t, new Vector3(-0.3f + i * 0.17f, 0.18f, 0), new Vector3(0.32f - i * 0.06f, 0.18f, 0.32f - i * 0.06f), new Color(1f, 0.5f, 0.1f), new Vector3(0, 0, 90));
                    for (int i = -1; i <= 1; i++) P(B, "Top", t, new Vector3(-0.48f, 0.25f + i * 0.05f, 0), new Vector3(0.3f, 0.04f, 0.04f), new Color(0.3f, 0.75f, 0.25f), new Vector3(0, 0, 150 + i * 20));
                    break;
                default: // лук
                    P(S, "Bulb", t, new Vector3(0, 0.33f, 0), Vector3.one * 0.62f, new Color(0.95f, 0.85f, 0.65f));
                    P(C, "Tip", t, new Vector3(0, 0.72f, 0), new Vector3(0.08f, 0.2f, 0.08f), new Color(0.6f, 0.75f, 0.35f));
                    for (int i = 0; i < 3; i++) P(B, "Line", t, new Vector3(0, 0.33f, 0), new Vector3(0.63f, 0.6f, 0.01f), new Color(0.85f, 0.7f, 0.45f), new Vector3(0, i * 60f, 0));
                    break;
            }
            return root;
        }

        /// <summary>Нарезанный продукт: несколько кусочков того же цвета (видно, что продукт разрезан).</summary>
        public static Transform BuildPieces(ItemId id, Transform parent, float size, int pieces)
        {
            var root = new GameObject("Pieces_" + id).transform;
            root.SetParent(parent, false);
            var c = Items.Get(id).Color;
            var rnd = new System.Random((int)id * 31 + pieces);
            for (int i = 0; i < pieces; i++)
            {
                float a = i * Mathf.PI * 2f / pieces;
                var p = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * size * 0.35f + Vector3.up * size * 0.12f;
                var g = Prim.Part(id == ItemId.SlimeJelly ? PrimitiveType.Cube : PrimitiveType.Sphere, "Piece", root, p,
                    new Vector3(size * 0.38f, size * 0.24f, size * 0.32f), Color.Lerp(c, Color.white, (float)rnd.NextDouble() * 0.2f), new Vector3(0, (float)rnd.NextDouble() * 90f, 0));
                if (id == ItemId.SlimeJelly || id == ItemId.DragonMeat) Prim.Glow(g, c * 0.25f);
            }
            return root;
        }

        /// <summary>Из какого монстра добывается продукт (null — овощ из кладовой).</summary>
        public static MonsterKind? Source(ItemId id)
        {
            foreach (var kv in Bestiary.Info) if (kv.Value.Loot == id) return kv.Key;
            return null;
        }

        public static string SourceText(ItemId id)
        {
            var k = Source(id);
            if (k == null) return "кладовая (сколько угодно)";
            var info = Bestiary.Get(k.Value);
            return $"{info.Name}, этаж {info.Floor}";
        }

        /// <summary>В каких блюдах используется.</summary>
        public static List<string> Dishes(ItemId id)
        {
            var l = new List<string>();
            foreach (var r in Recipes.All) if (System.Array.IndexOf(r.Items, id) >= 0) l.Add(r.Name);
            return l;
        }
    }

}
