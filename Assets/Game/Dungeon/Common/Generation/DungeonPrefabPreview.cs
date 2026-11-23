using UnityEngine;
namespace Podz
{
    /// <summary>Ownership record for explicit cleanup, not an automatic spawner.</summary>
    public class DungeonPrefabPreview : MonoBehaviour
    {
        [HideInInspector] public GameObject[] OwnedInstances=new GameObject[0];
    }
}
