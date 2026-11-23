using UnityEngine;

namespace Podz
{
    /// <summary>
    /// Где какие инструменты можно брать (исправление 4).
    /// Подземелье: охотничьи 1–5 (+6 эликсир); кухонные нож и половник — нельзя, они остаются на кухне.
    /// Кухня/таверна (пункт 7 — свои номера): 1 нож, 2 половник, 3 тесак, 4 молоток — тесак и молоток можно (по рецептам ими разделывают мясо василиска и мимика);
    /// лопатка 1, сачок 2, котёл-щит 5 — нельзя (охотничьи). При попытке — подсказка «почему» и короткий сигнал.
    /// </summary>
    public static class ToolRules
    {
        public static bool InDungeon => DungeonManager.I != null && DungeonManager.I.InDungeon;
        public static int Blocked;
        public static string LastWhy = "";

        public static bool IsKitchenTool(ToolType t) => t == ToolType.Knife || t == ToolType.Ladle;
        public static bool IsHuntOnly(ToolType t) => t == ToolType.Spatula || t == ToolType.Net || t == ToolType.Shield;

        public static bool Allowed(ToolType t) => Allowed(t, out _, out _);

        public static bool Allowed(ToolType t, out string why, out string note)
        {
            why = ""; note = "";
            if (InDungeon && IsKitchenTool(t))
            {
                note = "кухня";
                why = $"{Tools.Name(t)} — кухонный, он остался на кухне: монстра им не добыть. Здесь охотятся инструментами 1–5 (6 — эликсир)";
                return false;
            }
            if (!InDungeon && IsHuntOnly(t))
            {
                note = "охота";
                why = $"{Tools.Name(t)} — для охоты в подземелье, на кухне им только испачкаешь еду. Здесь: 1 — нож, 2 — половник, 3 — тесак, 4 — молоток (тесак и молоток — для разделки мяса)";
                return false;
            }
            return true;
        }

        /// <summary>Проверка при выборе инструмента: нельзя — подсказка «почему» и звук.</summary>
        public static bool Check(ToolType t)
        {
            if (Allowed(t, out var why, out _)) return true;
            Blocked++; LastWhy = why;
            Hints.Show(why, HintPri.Error, 4.5f);
            Sfx.Play(SfxId.Warn, null, 0.5f);
            return false;
        }

        /// <summary>Каждый кадр: если игрок перешёл в другое место с «чужим» инструментом в руке — инструмент убирается на место.</summary>
        public static void Tick(PlayerRig p)
        {
            if (p == null) return;
            foreach (var h in new[] { p.Right, p.Left })
            {
                var t = h != null ? h.HeldTool : null;
                if (t == null || Allowed(t.Type)) continue;
                h.ForceRelease();
                t.ReturnHome();
                Check(t.Type);
            }
        }
    }
}
