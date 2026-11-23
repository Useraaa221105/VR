using UnityEngine;

namespace Podz
{
    /// <summary>Параметры редакторского построения, не второй AI и не замена данных профиля.</summary>
    [CreateAssetMenu(menuName = "Podz/Slime Model Recipe", fileName = "SlimeModel")]
    public class SlimeModelRecipe : ScriptableObject
    {
        public enum Motion { ExistingSquash, None }

        [Header("Геометрия существующего желейного слайма, метры")]
        [Tooltip("Ширина / высота / глубина визуала. Исходный профиль: 0.58 / 0.42 / 0.58 м.")]
        public Vector3 VisualSize = new Vector3(.58f, .42f, .58f);
        [Tooltip("Размер прежней зоны Body. Отдельно от визуала: по умолчанию прежние 0.4 м, без изменения баланса.")]
        public Vector3 HitboxSize = new Vector3(.4f, .4f, .4f);

        [Header("Материалы (пусто = существующее оформление)")]
        [Tooltip("Материал оболочки. Генератор сохранит локальную копию, исходный материал не изменяется.")]
        public Material JellyMaterial;
        [Tooltip("Необязательно: общий материал глаз/пузырьков/рта. Пусто сохраняет их разные исходные цвета.")]
        public Material DetailMaterial;
        public Motion Animation = Motion.ExistingSquash;

        [Header("Необязательный звук нового префаба")]
        [Tooltip("Звук входа в Оглушён. Пусто = не добавлять новый звук. Остальные звуки охоты остаются прежними.")]
        public AudioClip StunSound;

        [HideInInspector] public GameObject LastPrefab;
    }
}
