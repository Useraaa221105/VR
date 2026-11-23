using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Podz
{
    public enum DeathReason { Captured, Ruined, Despawned }

    /// <summary>
    /// Реестр («кеш») живых монстров. Монстр попадает сюда при спавне и ОБЯЗАТЕЛЬНО удаляется при смерти:
    /// список + словарь по id чистятся, счётчики показывают, что ничего не «висит» в памяти.
    /// </summary>
    public static class MonsterRegistry
    {
        static readonly List<Monster> _alive = new List<Monster>();
        static readonly Dictionary<int, Monster> _byId = new Dictionary<int, Monster>();
        static int _nextId;

        public static int TotalSpawned { get; private set; }
        public static int TotalRemoved { get; private set; }
        public static int AliveCount => _alive.Count;
        public static int CacheCount => _byId.Count;
        public static IReadOnlyList<Monster> Alive => _alive;

        public static int Register(Monster m)
        {
            int id = ++_nextId;
            _alive.Add(m);
            _byId[id] = m;
            TotalSpawned++;
            return id;
        }

        public static void Unregister(Monster m, int id)
        {
            bool a = _byId.Remove(id);
            bool b = _alive.Remove(m);
            if (a || b) TotalRemoved++;
        }

        public static bool TryGet(int id, out Monster m) => _byId.TryGetValue(id, out m);

        /// <summary>Убрать всех (выход с этажа). После вызова реестр обязан быть пуст.</summary>
        public static void DespawnAll()
        {
            for (int i = _alive.Count - 1; i >= 0; i--)
            {
                if (i >= _alive.Count) continue;
                var m = _alive[i];
                if (m != null) m.Die(DeathReason.Despawned);
            }
            int leaked = _alive.Count + _byId.Count;
            if (leaked > 0) Debug.LogError($"[Cleanup] в реестре остались записи: {leaked} — принудительная очистка");
            _alive.Clear();
            _byId.Clear();
            if (MonsterCache.EntryCount > 0) Debug.LogError($"[Cleanup] в кеше монстров остались записи: {MonsterCache.EntryCount}");
            MonsterCache.ClearAll();
        }

        /// <summary>Проверка: нет ли «мёртвых» ссылок и объектов-сирот. Клавиша F2 / кнопка в отладке.</summary>
        public static string Audit()
        {
            int nulls = 0, deadFlag = 0;
            foreach (var m in _alive) { if (m == null) nulls++; else if (m.Dead) deadFlag++; }
            int inScene = Object.FindObjectsByType<Monster>().Length;
            var sb = new StringBuilder();
            sb.Append($"Реестр: {_alive.Count}, словарь: {_byId.Count}, на сцене: {inScene}, null: {nulls}, мёртвых в списке: {deadFlag}. ");
            bool ok = nulls == 0 && deadFlag == 0 && _alive.Count == _byId.Count && inScene == _alive.Count;
            sb.Append(MonsterCache.Audit()).Append(". ");
            ok &= MonsterCache.EntryCount == _alive.Count;
            sb.Append(ok ? "OK — кеш чистый" : "ОШИБКА — есть утечка");
            return sb.ToString();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _alive.Clear(); _byId.Clear(); _nextId = 0; TotalSpawned = 0; TotalRemoved = 0;
        }
    }
}
