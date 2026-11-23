using System;
using UnityEngine;

namespace Podz
{
    /// <summary>Typed authoring options for the next linked group, not another monster AI.</summary>
    [CreateAssetMenu(menuName = "Podz/Monster Model Recipe (all dungeon monsters except Slime)", fileName = "MonsterModel")]
    public class MonsterModelRecipe : ScriptableObject
    {
        public enum Species { Mushroom, Mandrake, Basilisk, LivingArmor, Mimic, Dragon }
        public enum Motion { Existing, None }
        [Serializable] public class MaterialOverride
        {
            [Tooltip("Exact MeshRenderer object name in the generated prefab. All matches are replaced. No code/string expressions.")]
            public string RendererName;
            [Tooltip("Persistent URP material. Its colour and emission are used, source asset is not changed.")]
            public Material Material;
        }
        [Header("Существующая геометрия (без новых вариантов внешности)")]
        public Species Monster;
        [Range(.75f, 1.25f), Tooltip("Uniform whole-rig scale, including colliders and grabs. 1 = exact original size. Changed scale needs hunting/VR retest; original thresholds remain unchanged.")]
        public float ModelScale = 1f;
        public Motion Animation = Motion.Existing;
        [Header("Необязательные материалы; пустой список = прежние цвета/текстуры")]
        public MaterialOverride[] Materials = new MaterialOverride[0];
        [Header("Мандрагора: только визуал реального захвата")]
        [Range(0f, 1f)] public float LeafGatherStrength = .9f;
        [Range(1f, 30f), Tooltip("1/seconds, not the gameplay holding time.")]
        public float LeafResponse = 12f;
        [HideInInspector] public GameObject LastPrefab;
    }
}
