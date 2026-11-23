using UnityEngine;

namespace Podz
{
    public sealed class FortressGeometryBlock : MonoBehaviour
    {
        public Vector2 RoomSize;
        [Range(0, 15)] public int Openings;
        [Min(0)] public float PassageLength;
    }
}
