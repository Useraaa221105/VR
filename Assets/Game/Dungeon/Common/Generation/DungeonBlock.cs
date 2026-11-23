using System;
using UnityEngine;
namespace Podz
{
    /// <summary>Editable metadata, not automatic generation or a second floor implementation.</summary>
    public class DungeonBlock : MonoBehaviour
    {
        [Serializable] public struct Port {
            [Tooltip("Local metres, on the source opening centre at floor height.")] public Vector3 Position;
            [Tooltip("Outward local direction. Must match the actual opening.")] public Vector3 Forward;
            [Tooltip("Nominal clear opening in metres; verify the real collider/VR clearance.")] public Vector2 Clearance;
        }
        [HideInInspector] public string SourceKey;
        [Range(1,4)] public int Floor=1;
        public bool Corridor;
        [Tooltip("Local source footprint, metres. Not a substitute for collider inspection.")] public Bounds Footprint;
        public Port[] Connections=new Port[0];
        [Tooltip("Suggested INNER search zone, NOT guaranteed collision-free. Use actual raycast/clearance before placing anything.")]
        public Bounds PlacementSearchZone;
    }
}
