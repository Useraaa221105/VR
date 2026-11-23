using UnityEngine;
namespace Podz
{
    [CreateAssetMenu(menuName="Podz/Dungeon prefab catalog",fileName="FloorConfig")]
    public class DungeonPrefabCatalog : ScriptableObject
    {
        [Range(1,4),Tooltip("1=caves, 2=ruins, 3=fortress, 4=lair. Not tavern stages.")] public int Floor=1;
        [Tooltip("Same fortress planning algorithm: 6–9 connected rooms. Floors1/2 have existing fixed eight-room graphs, not random layouts.")] public int Seed=1;
        [Min(0),Tooltip("One source room or corridor index per explicit generation button.")] public int SourceIndex;
        public bool Corridor;
        [Range(0,9),Tooltip("Fortress only: 0=original Random.Range(6,10); or explicitly6–9 rooms. Not fewer/new generation rules.")]
        public int FortressRooms=0;
        [Tooltip("Persistent DungeonBlock prefabs, manually replaceable if SourceKey, footprint and ports agree.")]
        public GameObject[] Blocks=new GameObject[0];
        [System.Serializable] public class Placement {
            [Tooltip("Persistent existing object/monster prefab. No Main/rig/profile inside.")] public GameObject Prefab;
            [Min(0)] public int RoomIndex;
            [Tooltip("Local metres relative to source room floor centre. Must clear colliders and openings.")] public Vector3 Position;
            [Range(-180,180)] public float Yaw;
        }
        [Tooltip("Explicit authoring placements for REVIEW only. Does NOT reproduce the old runtime random population/distribution or initialize AI. Leave empty to review architecture only.")]
        public Placement[] ReviewPlacements=new Placement[0];
        [HideInInspector] public GameObject LastPrefab;
    }
}
