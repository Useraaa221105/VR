using System;
using UnityEngine;
namespace Podz
{
    public class Portal : MonoBehaviour
    {
        public Func<bool> CanUse;
        public Action OnUse;
        public string Label;
        [SerializeField] float _radius = 0.9f; float _stay;
        bool _armed;
        [SerializeField] TextMesh _text;

        public static Portal Create(Transform parent, Vector3 worldPos, string label, Color color, Action onUse, Func<bool> canUse = null)
        {
            var assets = SavedDungeonAssets.Active;
            var recipe = label == "В таверну" || label == "Домой" ? assets?.ReturnPortal : assets?.DescentPortal;
            if (recipe != null && recipe.LastPrefab != null && recipe.LastPrefab.GetComponentInChildren<Portal>() != null)
            {
                var instance = Instantiate(recipe.LastPrefab, worldPos, Quaternion.identity, parent);
                var saved = instance.GetComponentInChildren<Portal>();
                var discRenderer = saved.transform.Find("Disc")?.GetComponent<Renderer>();
                if (discRenderer != null) Prim.SetColor(discRenderer, color, color * 1.5f);
                var light = saved.GetComponentInChildren<Light>();
                if (light != null) light.color = color;
                saved.Label = label;
                saved.OnUse = onUse;
                saved.CanUse = canUse;
                saved.name = "Portal_" + label;
                if (saved._text != null)
                {
                    saved._text.text = label;
                    saved._text.GetComponent<Renderer>().sharedMaterial = Prim.DepthTextMaterial;
                }
                return saved;
            }
            var go = new GameObject("Portal_" + label);
            go.transform.SetParent(parent, true);
            go.transform.position = worldPos;
            var p = go.AddComponent<Portal>();
            p.Label = label; p.OnUse = onUse; p.CanUse = canUse;
            var disc = FX.Disc(go.transform, Vector3.up * 0.02f, p._radius, color);
            Prim.SetEmission(disc.GetComponent<Renderer>(), color * 1.5f);
            Prim.PointLight(go.transform, Vector3.up * 1.2f, color, 4f, 2f);
            p._text = Prim.Text(go.transform, Vector3.up * 2.1f, label, 0.12f, Color.white);
            return p;
        }

        void Update()
        {
            var P = PlayerRig.I;
            if (P == null) return;
            var d = P.Feet - transform.position; d.y = 0;
            bool inside = d.magnitude < _radius && Mathf.Abs(P.Feet.y - transform.position.y) < .25f;
            if (!inside) { _armed = true; _stay = 0; return; }
            if (!_armed || Fader.I == null || Fader.I.Busy) return;
            _stay += Time.deltaTime;
            if (_stay < 0.6f) return;
            _stay = 0; _armed = false;
            if (CanUse != null && !CanUse()) return;
            OnUse?.Invoke();
        }

        // надпись поворачиваем в LateUpdate — после всех перемещений игрока за кадр
        // (и после телепорта), поэтому она читается с любой стороны и никогда не бывает зеркальной
        void LateUpdate() => FaceNow();

        public void FaceNow()
        {
            if (_text != null && PlayerRig.I != null) Prim.FaceTo(_text.transform, PlayerRig.I.HeadPos);
        }

        /// <summary>Насколько надпись смотрит на игрока: 1 — ровно на него (читается), −1 — спиной (зеркально).</summary>
        public float TextFacing
        {
            get
            {
                if (_text == null || PlayerRig.I == null) return 0f;
                var d = _text.transform.position - PlayerRig.I.HeadPos; d.y = 0;
                var f = _text.transform.forward; f.y = 0;
                return Vector3.Dot(d.normalized, f.normalized);
            }
        }

        public static readonly System.Collections.Generic.List<Portal> All = new System.Collections.Generic.List<Portal>();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void OnDestroy() { OnUse = null; CanUse = null; }
    }
}
