using UnityEngine;

namespace Podz
{
    /// <summary>Желе слегка «дышит» — живое.</summary>
    public class FoodWobble : MonoBehaviour
    {
        float _seed;
        void Start() { _seed = Random.value * 5f; }
        void Update()
        {
            float k = Mathf.Sin((Time.time + _seed) * 4f) * 0.05f;
            transform.localScale = new Vector3(1f + k, 1f - k, 1f + k);
        }
    }
}
