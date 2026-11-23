using UnityEngine;
namespace Podz
{
    [CreateAssetMenu(menuName="Podz/Dungeon existing object recipe",fileName="DungeonObject")]
    public class DungeonObjectRecipe : ScriptableObject
    {
        public enum ObjectKind { Stalactite, Spores, Rockfall, Vines, DartPlate, Pendulum, FireGrate, AcidDrop,
            Puddle, Well, Elixir, DoorWithHandles, StepsAndPlatform, PortalVisual, PhysicalLoot, FireCover, FakeArmor, Chest, ReturnPortalVisual }
        [Tooltip("Only existing builders/variants, not new models or gameplay.")] public ObjectKind Kind;
        [Range(1,4),Tooltip("Source floor controls existing visual variant. No tavern stage or progression changes.")] public int Floor=1;
        [Tooltip("Geometry/phase seed only. Does not grant items or open floors.")] public int Seed=1;
        [Range(2.5f,8f),Tooltip("Metres above the floor, ONLY stone/rockfall visual; match actual ceiling before gameplay.")] public float CeilingHeight=4;
        [Tooltip("Only the seven existing monster food drops are supported by PhysicalLoot.")] public ItemId Loot;
        [HideInInspector] public GameObject LastPrefab;
    }
}
