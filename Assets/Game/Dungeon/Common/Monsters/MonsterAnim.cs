using System.Collections.Generic;
using UnityEngine;

namespace Podz
{
    /// <summary>Прежняя процедурная анимация; ссылки теперь сериализуются для сохранённого prefab.</summary>
    public class MonsterAnim : MonoBehaviour
    {
        public Monster Owner;
        [Tooltip("Idle/movement animation only. Hidden proxy suppression stays active even when unchecked.")]
        public bool Animate = true;
        public List<Renderer> Hidden = new List<Renderer>();       // скрытые игровые кубы
        public List<Renderer> HiddenAlways = new List<Renderer>(); // кубы, которые код монстра может включить — держим выключенными
        public List<Transform> Legs = new List<Transform>();
        public List<Transform> Wings = new List<Transform>();
        public Transform Squash, Wobble;
        Vector3 _last;
        float _phase, _speed;
        readonly Dictionary<Transform, Quaternion> _rest = new Dictionary<Transform, Quaternion>();
        readonly Dictionary<Transform, Vector3> _restScale = new Dictionary<Transform, Vector3>();

        void Start()
        {
            foreach (var r in Hidden) if (r != null) r.enabled = false;
            foreach (var r in HiddenAlways) if (r != null) r.enabled = false;
            foreach (var t in Legs) if (t != null) _rest[t] = t.localRotation;
            foreach (var t in Wings) if (t != null) { _rest[t] = t.localRotation; }
            if (Wobble != null) _rest[Wobble] = Wobble.localRotation;
            if (Squash != null) _restScale[Squash] = Squash.localScale;
            _last = transform.position;
        }

        public float Speed => _speed;

        void LateUpdate()
        {
            if (Owner == null || Owner.Dead) return;
            foreach (var r in HiddenAlways) if (r != null && r.enabled) r.enabled = false;
            if (!Animate) return;
            float dt = DTime.Delta;
            if (dt <= 0f) return;
            var d = transform.position - _last; d.y = 0;
            _speed = Mathf.Lerp(_speed, d.magnitude / dt, 0.2f);
            _last = transform.position;
            string st = Owner.State;
            bool windup = st == "Готовится" || st == "Замах" || st == "Вдох" || st == "Лапа: замах" || st == "Хвост: замах";
            bool stunned = st == "Оглушён" || st == "Устал" || st == "Перегрев" || st == "На колене" || st == "Треснул" || st == "Клюв застрял" || st == "Меч застрял";
            _phase += dt * (2f + _speed * 6f) * (windup ? 3f : 1f);

            foreach (var t in Legs)
                if (t != null) t.localRotation = _rest[t] * Quaternion.Euler(Mathf.Sin(_phase * 2f + t.GetSiblingIndex()) * Mathf.Clamp01(_speed) * 35f, 0, 0);
            foreach (var t in Wings)
                if (t != null)
                {
                    float flap = windup ? 25f : stunned ? 2f : 8f;
                    float side = t.localPosition.x >= 0 ? 1 : -1;
                    t.localRotation = _rest[t] * Quaternion.Euler(0, 0, side * Mathf.Sin(_phase * (windup ? 3f : 1f)) * flap);
                }
            if (Wobble != null) Wobble.localRotation = _rest[Wobble] * Quaternion.Euler(Mathf.Sin(_phase * 1.3f) * (windup ? 10f : 4f), 0, Mathf.Cos(_phase) * (windup ? 10f : 4f));
            if (Squash != null)
            {
                float k = stunned ? 0.25f : windup ? 0.18f : 0.08f + Mathf.Clamp01(_speed) * 0.1f;
                float s = Mathf.Sin(_phase * (windup ? 4f : 2f)) * k;
                var b = _restScale[Squash];
                Squash.localScale = new Vector3(b.x * (1 + s * 0.5f), b.y * (1 - s), b.z * (1 + s * 0.5f));
            }
        }
    }
}
