using System;
using UnityEngine;

namespace Podz
{
    /// <summary>Шина событий. Все подписчики обязаны отписываться (OnDisable/OnDestroy) — иначе мёртвые объекты остаются в памяти.</summary>
    public static class GameEvents
    {
        public static event Action<Monster> MonsterSpawned;
        public static event Action<Monster, DeathReason> MonsterDied;
        public static event Action PlayerDied;
        public static event Action<ItemId, int> LootGained;

        public static void RaiseMonsterSpawned(Monster m) => MonsterSpawned?.Invoke(m);
        public static void RaiseMonsterDied(Monster m, DeathReason r) => MonsterDied?.Invoke(m, r);
        public static void RaisePlayerDied() => PlayerDied?.Invoke();
        public static void RaiseLoot(ItemId id, int n) => LootGained?.Invoke(id, n);

        /// <summary>Сколько подписчиков на PlayerDied — должно совпадать с числом живых монстров (+ системные). Видно в отладке.</summary>
        public static int PlayerDiedSubscribers => PlayerDied == null ? 0 : PlayerDied.GetInvocationList().Length;
        public static int MonsterDiedSubscribers => MonsterDied == null ? 0 : MonsterDied.GetInvocationList().Length;

        // Сброс статиков, если в Unity выключена перезагрузка домена (Enter Play Mode Options)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            MonsterSpawned = null;
            MonsterDied = null;
            PlayerDied = null;
            LootGained = null;
        }
    }

    /// <summary>Время подземелья: останавливается при показе карточки бестиария. Время таверны — обычное (таверна выключается на время вылазки).</summary>
    public static class DTime
    {
        public static bool Paused;
        public static float Delta => Paused ? 0f : Time.deltaTime;
        public static float Now { get; private set; }
        public static void Tick() { if (!Paused) Now += Time.deltaTime; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Paused = false; Now = 0; }
    }
}
