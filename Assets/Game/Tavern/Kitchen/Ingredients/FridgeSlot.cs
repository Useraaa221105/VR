using System.Collections.Generic;
using UnityEngine;

namespace Podz
{
    public class FridgeSlot : MonoBehaviour, IGrabbable
    {
        [field: SerializeField] public ItemId Id { get; private set; }
        [SerializeField] Fridge _f;
        [SerializeField] List<Transform> _models = new List<Transform>();
        [SerializeField] TextMesh _name, _count;
        [SerializeField] Renderer _glow;
        int _shown = -1;
        public int VisibleModels { get; private set; }
        public string LabelText => _name != null ? $"{_name.text} {_count.text}" : "";

        public static FridgeSlot Create(Fridge f, ItemId id, Vector3 local, float width)
        {
            var go = new GameObject("Slot_" + id);
            go.transform.SetParent(f.transform, false);
            go.transform.localPosition = local;
            var s = go.AddComponent<FridgeSlot>();
            s.Id = id; s._f = f;
            var bc = go.AddComponent<BoxCollider>();
            bc.isTrigger = true; bc.center = new Vector3(0, 0.15f, 0.05f); bc.size = new Vector3(width - 0.02f, 0.32f, 0.42f);
            float sz = Fridge.Infinite(id) ? 0.13f : 0.12f;
            // до 4 фигурок: 2 спереди, 2 сзади
            for (int i = 0; i < 4; i++)
            {
                var m = FoodModels.Build(id, go.transform, sz);
                m.localPosition = new Vector3((i % 2 == 0 ? -1 : 1) * width * 0.2f, 0, i < 2 ? 0.06f : -0.1f);
                m.localRotation = Quaternion.Euler(0, 180 + (i * 37) % 50 - 25, 0);
                s._models.Add(m);
            }
            // подпись: имя и крупное количество
            Prim.Cube("Tag", go.transform, new Vector3(0, -0.005f, 0.24f), new Vector3(width - 0.05f, 0.07f, 0.01f), new Color(0.12f, 0.1f, 0.08f), false);
            s._glow = Prim.Cube("NewGlow", go.transform, new Vector3(0, -0.012f, 0.05f), new Vector3(width - 0.04f, 0.006f, 0.36f), new Color(1f, 0.85f, 0.3f), false).GetComponent<Renderer>();
            s._name = Prim.Text(go.transform, new Vector3(width / 2 - 0.035f, -0.005f, 0.252f), Items.Name(id), Items.Name(id).Length > 13 ? 0.019f : Items.Name(id).Length > 10 ? 0.022f : 0.026f, Color.white, TextAnchor.MiddleLeft);
            s._count = Prim.Text(go.transform, new Vector3(-width / 2 + 0.04f, -0.005f, 0.252f), "", 0.04f, new Color(0.6f, 1f, 0.7f), TextAnchor.MiddleRight);
            s._name.transform.localRotation = Quaternion.Euler(0, 180, 0);
            s._count.transform.localRotation = Quaternion.Euler(0, 180, 0);
            s._count.fontStyle = FontStyle.Bold;
            // текст повёрнут на 180° (читается спереди) — поэтому имя у +x, количество у −x
            if (Application.isPlaying) s.Refresh(true);
            return s;
        }

        bool _savedModels;
        internal void Bind(Fridge owner) { _f = owner; _savedModels = true; }

        void Refresh(bool force = false)
        {
            int n = Fridge.Count(Id);
            if (n == _shown && !force) return;
            _shown = n;
            bool inf = Fridge.Infinite(Id);
            int vis = inf ? 3 : Mathf.Min(4, n);
            VisibleModels = vis;
            for (int i = 0; i < _models.Count; i++) _models[i].gameObject.SetActive(i < Mathf.Max(1, vis));
            // пусто — одна «призрачная» тёмная фигурка
            if (!inf && n == 0) foreach (var r in _models[0].GetComponentsInChildren<Renderer>()) Prim.SetColor(r, new Color(0.25f, 0.3f, 0.35f));
            else if (!inf && n > 0 && _wasEmpty && _savedModels)
                foreach (var r in _models[0].GetComponentsInChildren<Renderer>()) r.SetPropertyBlock(null);
            else if (!inf && n > 0 && _wasEmpty) { var p = _models[0].localPosition; var rot = _models[0].localRotation; Destroy(_models[0].gameObject); _models[0] = FoodModels.Build(Id, transform, 0.12f); _models[0].localPosition = p; _models[0].localRotation = rot; }
            _wasEmpty = !inf && n == 0;
            _count.text = inf ? "∞" : "×" + n;
            _count.color = inf ? new Color(1f, 0.9f, 0.6f) : n > 0 ? new Color(0.6f, 1f, 0.7f) : new Color(1f, 0.45f, 0.4f);
        }
        bool _wasEmpty;

        void Update()
        {
            Refresh();
            bool isNew = _f != null && _f.IsNew(Id);
            Prim.SetColor(_glow, new Color(1f, 0.85f, 0.3f), isNew ? new Color(1f, 0.75f, 0.2f) * (0.8f + 0.6f * Mathf.Sin(Time.time * 5f)) : Color.black);
            if (_glow.enabled != isNew) _glow.enabled = isNew;
        }

        // ---------- IGrabbable: «взять с полки» = достать один продукт и сразу держать его ----------
        public bool CanGrab(HandController hand) => Fridge.Count(Id) > 0;
        public void Grab(HandController hand)
        {
            hand.ClearIfHolding(this);
            if (_f != null) _f.Take(Id, hand);
        }
        public void Release(HandController hand, Vector3 v) { }
    }
}
