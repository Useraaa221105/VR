using System.Collections.Generic;
using UnityEngine;

namespace Podz
{
    /// <summary>
    /// Кеш данных монстра: рендеры, исходные цвета, коллайдеры (быстрый поиск «коллайдер → монстр»), id зон попадания.
    /// Заполняется при спавне, ПОЛНОСТЬЮ очищается в момент смерти (Monster.Die → MonsterCache.Release).
    /// Заодно сразу стирает id зон монстра из кулдаунов всех инструментов.
    /// </summary>
    public static class MonsterCache
    {
        public class Entry
        {
            public readonly List<Renderer> Renderers = new List<Renderer>();
            public readonly List<Color> Colors = new List<Color>();
            public readonly List<Collider> Colliders = new List<Collider>();
            public readonly List<int> HitIds = new List<int>();
            public void Clear() { Renderers.Clear(); Colors.Clear(); Colliders.Clear(); HitIds.Clear(); }
        }

        static readonly Dictionary<int, Entry> _entries = new Dictionary<int, Entry>();
        static readonly Dictionary<Collider, Monster> _byCollider = new Dictionary<Collider, Monster>();
        static readonly MaterialPropertyBlock _mpb = new MaterialPropertyBlock();

        public static int EntryCount => _entries.Count;
        public static int ColliderCount => _byCollider.Count;
        public static int Released { get; private set; }

        public static Entry Build(Monster m, int id)
        {
            var e = new Entry();
            foreach (var r in m.GetComponentsInChildren<Renderer>(true))
            {
                if (r.GetComponent<TextMesh>() != null) continue;
                r.GetPropertyBlock(_mpb);
                e.Renderers.Add(r);
                e.Colors.Add(_mpb.GetColor("_BaseColor"));
            }
            foreach (var c in m.GetComponentsInChildren<Collider>(true))
            {
                e.Colliders.Add(c);
                _byCollider[c] = m;
            }
            foreach (var z in m.GetComponentsInChildren<HitZone>(true)) e.HitIds.Add(z.GetHashCode());
            _entries[id] = e;
            return e;
        }

        /// <summary>Монстр сам удалил свою часть (кнопка мимика) — убрать её из кеша сразу.</summary>
        public static void Forget(Collider c) { if (c != null) _byCollider.Remove(c); }

        public static bool TryGetMonster(Collider c, out Monster m) => _byCollider.TryGetValue(c, out m);

        /// <summary>Очистить всё, что закешировано по монстру. Вызывается один раз при смерти.</summary>
        public static void Release(int id, string name)
        {
            if (!_entries.TryGetValue(id, out var e)) return;
            foreach (var c in e.Colliders) if (c != null) _byCollider.Remove(c);
            // коллайдеры могли уже уничтожиться (null-ключи) — подчистить их тоже
            List<Collider> stale = null;
            foreach (var kv in _byCollider) if (kv.Key == null || kv.Value == null || kv.Value.Id == id) (stale ??= new List<Collider>()).Add(kv.Key);
            if (stale != null) foreach (var c in stale) _byCollider.Remove(c);
            Tool.ForgetTargets(e.HitIds);
            int r = e.Renderers.Count, cl = e.Colliders.Count;
            e.Clear();
            _entries.Remove(id);
            Released++;
            Debug.Log($"[Cache] {name}: кеш очищен (рендеров {r}, коллайдеров {cl}). Осталось записей: {_entries.Count}, коллайдеров: {_byCollider.Count}");
        }

        public static void ClearAll()
        {
            foreach (var e in _entries.Values) e.Clear();
            _entries.Clear();
            _byCollider.Clear();
        }

        public static string Audit()
        {
            int nullKeys = 0;
            foreach (var kv in _byCollider) if (kv.Key == null || kv.Value == null || kv.Value.Dead) nullKeys++;
            bool ok = nullKeys == 0 && _entries.Count == MonsterRegistry.AliveCount;
            return $"Кеш: записей {_entries.Count}, коллайдеров {_byCollider.Count}, битых {nullKeys} — {(ok ? "OK" : "УТЕЧКА")}";
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { ClearAll(); Released = 0; }
    }
}
