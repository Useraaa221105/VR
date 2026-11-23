using System;
using UnityEngine;

namespace Podz
{
    /// <summary>Монеты игрока. Сохраняются GameFlow в SaveData.coins.</summary>
    public static class Wallet
    {
        public static int Coins { get; private set; }
        public static event Action<int> Changed;

        public static void Set(int n) { Coins = Mathf.Max(0, n); Changed?.Invoke(Coins); }
        public static void Add(int n) { if (n == 0) return; Coins = Mathf.Max(0, Coins + n); Changed?.Invoke(Coins); }
        public static bool Spend(int n)
        {
            if (Coins < n) return false;
            Coins -= n; Changed?.Invoke(Coins);
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Coins = 0; Changed = null; }
    }
}
