using UnityEngine;

namespace Podz
{
    /// <summary>
    /// Пункт 5: целебные эликсиры Сенши. Сумка до 10 штук (пункт 11), каждый +20 HP. При 10 бутылочка не подбирается — «Максимум 10 эликсиров».
    /// ПК: 6 в подземелье — бутылочка в руку, ЛКМ — выпить. VR: кнопка B на правом контроллере — выпить.
    /// Находятся в подземелье: 2–3 на этаж (светящиеся бутылочки), в сундуках на −3, иногда (25 %) остаются после пойманного монстра, 2 — в логове.
    /// </summary>
    public static class Elixirs
    {
        public const int Max = 10;
        public const float Heal = 20f;
        public const float DropChance = 0.25f;
        public const string Name = "Целебный эликсир Сенши";
        public static readonly Color Col = new Color(1f, 0.35f, 0.55f);
        public static int Count;
        public static int Drunk, Picked;
        public static string LastMessage = "";
        public static string FullText => $"Максимум {Max} эликсиров";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Count = 0; Drunk = 0; Picked = 0; LastMessage = ""; }

        static void Say(string msg, HintPri pri) { LastMessage = msg; Hints.Show(msg, pri, 2.5f); }

        /// <summary>Добавить эликсир в сумку. false — уже 10 (максимум).</summary>
        public static bool Add(int n = 1)
        {
            if (Count >= Max) { Say(FullText, HintPri.Error); return false; }
            Count = Mathf.Min(Max, Count + n);
            Picked++;
            Say($"+1 {Name} ({Count}/{Max}) — 6 и ЛКМ, чтобы выпить", HintPri.Action);
            Sfx.Play(SfxId.Pickup);
            return true;
        }

        /// <summary>Выпить один эликсир. Возвращает true, если выпит.</summary>
        public static bool Drink(PlayerHealth h)
        {
            if (h == null) return false;
            if (Count <= 0) { Say("Эликсиров нет — ищи светящиеся бутылочки в подземелье", HintPri.Error); return false; }
            if (h.HP >= PlayerHealth.Max) { Say("Здоровье уже полное — эликсир не нужен", HintPri.Error); return false; }
            Count--; Drunk++;
            float before = h.HP;
            h.Heal(Heal, PlayerHealth.HealSource.Elixir);
            h.HealFlash();
            Sfx.Play(SfxId.Bubble);
            Sfx.Play(SfxId.Success, null, 0.5f);
            Say($"+{h.HP - before:0} HP — {Name} (осталось {Count})", HintPri.Action);
            return true;
        }
    }

    /// <summary>Светящаяся бутылочка на полу: видна издалека (столб света + подпись), подбирается, если подойти ближе 1 м.</summary>

}
