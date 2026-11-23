namespace Podz
{
    /// <summary>
    /// Пункт 5: вся таблица урона в одном месте. HP игрока — 100.
    /// Дракон — самый сильный (максимум 25 за удар), споры и мелкие ловушки — 2–3, остальные монстры 3–25:
    /// слайм слабый, мандрагора/василиск/доспехи средние, мимик сильнее.
    /// </summary>
    public static class Dmg
    {
        // монстры
        public const float SlimePerSec = 2f;          // душит, пока на лице (≈ 2 в секунду, сорвать — E ×3)
        public const float MushroomSpores = 3f;        // облако спор вплотную
        public const float MandrakeScream = 8f;        // проснулась и закричала
        public const float MandrakePulled = 15f;       // выдернул, не срезав листья
        public const float BasiliskPeck = 12f;
        public const float BasiliskTail = 8f;          // хвост-змея сзади
        public const float ArmorSword = 12f;
        public const float MimicAmbush = 18f;          // открыл мимика рукой
        public const float MimicClaws = 15f;
        public const float DragonPaw = 25f;            // максимум в игре
        public const float DragonTail = 25f;
        public const float DragonRain = 20f;
        public const float DragonBreathPerSec = 10f;  // огонь 2,5 с без щита ≈ 25 — не больше максимума
        // ловушки
        public const float TrapSpores = 2f, TrapAcid = 3f, TrapVines = 2f;                 // мелкие
        public const float TrapDarts = 6f, TrapStalactite = 10f, TrapFireGrate = 10f, TrapRockfall = 12f, TrapPendulum = 15f;
        public const float Max = 25f;

        /// <summary>Таблица для карточек/отчёта.</summary>
        public static readonly (string who, string what, float dmg)[] Table =
        {
            ("Слайм", "душит на лице, в секунду", SlimePerSec),
            ("Ходячий гриб", "споры вплотную", MushroomSpores),
            ("Мандрагора", "крик", MandrakeScream),
            ("Мандрагора", "выдернул без среза", MandrakePulled),
            ("Василиск", "клевок", BasiliskPeck),
            ("Василиск", "хвост-змея сзади", BasiliskTail),
            ("Живые доспехи", "меч", ArmorSword),
            ("Мимик", "засада (открыл рукой)", MimicAmbush),
            ("Мимик", "клешни", MimicClaws),
            ("Красный дракон", "лапа / хвост", DragonPaw),
            ("Красный дракон", "огненный дождь", DragonRain),
            ("Красный дракон", "огонь без щита, в секунду", DragonBreathPerSec),
        };
    }
}
