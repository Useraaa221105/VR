using System;
using UnityEngine;
namespace Podz
{
    public class Puddle : MonoBehaviour
    {
        [SerializeField] float _r;
        public static Puddle Create(Transform parent, Vector3 worldPos, float radius, Color c)
        {
            var template = SavedDungeonAssets.FindTemplate<Puddle>(SavedDungeonAssets.Active?.CavePuddle);
            if (template != null && template._r > 0)
            {
                var saved = Instantiate(template, worldPos, Quaternion.identity, parent);
                saved.name = "Puddle";
                var disc = saved.transform.Find("Disc");
                if (disc != null)
                {
                    var scale = disc.localScale;
                    scale.x *= radius / template._r;
                    scale.z *= radius / template._r;
                    disc.localScale = scale;
                    var renderer = disc.GetComponent<Renderer>();
                    renderer.enabled = true;
                    Prim.SetColor(renderer, c);
                }
                saved._r = radius;
                return saved;
            }
            var go = new GameObject("Puddle");
            go.transform.SetParent(parent, true);
            go.transform.position = worldPos;
            FX.Disc(go.transform, Vector3.up * 0.012f, radius, c);
            var p = go.AddComponent<Puddle>();
            p._r = radius;
            return p;
        }

        void Update()
        {
            var P = PlayerRig.I;
            if (P == null) return;
            var d = P.Feet - transform.position; d.y = 0;
            if (d.magnitude < _r) P.Health.Apply(Status.Slowed, 0.3f);
        }
    }
}
