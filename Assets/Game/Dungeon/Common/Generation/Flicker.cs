using UnityEngine;

namespace Podz
{
    public class Flicker : MonoBehaviour
    {
        [SerializeField] Light _l; [SerializeField] float _base, _seed;
        public void Init(Light l) { _l = l; _base = l.intensity; _seed = UnityEngine.Random.value * 10f; }
        void Update() { if (_l != null) _l.intensity = _base * (0.8f + 0.4f * Mathf.PerlinNoise(_seed, Time.time * 3f)); }
    }
}
