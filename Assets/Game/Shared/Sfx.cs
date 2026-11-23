using UnityEngine;

namespace Podz
{
    public enum SfxId { Click, Capture, Ruin, Loot, Coin, Hurt, Warn, Strike, Bell, Card, Whoosh, Hit, Success, Fail, Pickup, Chop, Bubble, Sizzle, Splash, Stir, Pour, Ding, MandrakeScream }

    /// <summary>
    /// Звуки, сгенерированные кодом при запуске (синусы, шум, огибающие) — без файлов из интернета, лицензия не нужна.
    /// Пул из 8 AudioSource без Instantiate/Destroy. Вызов: Sfx.Play(SfxId.Coin, позиция). На MonsterDied не подписывается — звук добычи вызывает сам монстр.
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        public static Sfx I { get; private set; }
        public static int PlayedTotal { get; private set; }
        public static int ScreamsPlayed { get; private set; }
        public static bool LastScreamSpatial { get; private set; }
        public static Vector3 LastScreamPosition { get; private set; }
        internal static AudioClip FallbackMandrakeClip => I != null && I._clips != null ? I._clips[(int)SfxId.MandrakeScream] : null;
        public static bool TryMandrakeScream(Vector3 at, float volume = .95f) {
            if(I == null || !MandrakeVoice.TryPlay(at, volume)) return false;
            PlayedTotal++; ScreamsPlayed++; LastScreamSpatial=true; LastScreamPosition=at; return true;
        }
        public static int ClipCount => I != null ? I._clips.Length : 0;
        const int Rate = 22050;
        AudioClip[] _clips;
        AudioSource[] _pool;
        int _next;
        readonly float[] _last = new float[32];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { I = null; PlayedTotal = 0; ScreamsPlayed = 0; LastScreamSpatial = false; }

        void Awake()
        {
            I = this;
            _clips = new AudioClip[System.Enum.GetValues(typeof(SfxId)).Length];
            _clips[(int)SfxId.Click] = Make("click", 0.05f, (t, p) => Sine(1800f, t) * Env(p, 0.01f));
            _clips[(int)SfxId.Capture] = Make("capture", 0.45f, (t, p) => (Sine(Mathf.Lerp(520f, 1040f, p), t) * 0.6f + Noise() * 0.25f * (1 - p)) * Env(p, 0.05f));
            _clips[(int)SfxId.Ruin] = Make("ruin", 0.35f, (t, p) => (Noise() * 0.6f + Sine(Mathf.Lerp(220f, 80f, p), t) * 0.5f) * Env(p, 0.02f));
            _clips[(int)SfxId.Loot] = Make("loot", 0.18f, (t, p) => Sine(p < 0.5f ? 880f : 1320f, t) * Env(p, 0.02f));
            _clips[(int)SfxId.Coin] = Make("coin", 0.3f, (t, p) => (Sine(1975f, t) + Sine(2637f, t) * 0.6f) * 0.5f * Mathf.Exp(-p * 6f));
            _clips[(int)SfxId.Hurt] = Make("hurt", 0.25f, (t, p) => Square(Mathf.Lerp(180f, 90f, p), t) * 0.4f * Env(p, 0.02f));
            _clips[(int)SfxId.Warn] = Make("warn", 0.3f, (t, p) => Sine(660f, t) * (Mathf.Repeat(p * 6f, 1f) < 0.5f ? 1f : 0.2f) * 0.5f);
            _clips[(int)SfxId.Strike] = Make("strike", 0.4f, (t, p) => Noise() * Mathf.Exp(-p * 7f));
            _clips[(int)SfxId.Bell] = Make("bell", 1.0f, (t, p) => (Sine(1046f, t) + Sine(2093f, t) * 0.4f + Sine(3136f, t) * 0.2f) * 0.5f * Mathf.Exp(-p * 4f));
            _clips[(int)SfxId.Card] = Make("card", 0.5f, (t, p) => Sine(p < 0.33f ? 523f : p < 0.66f ? 659f : 784f, t) * 0.5f * Env(p, 0.05f));
            _clips[(int)SfxId.Whoosh] = Make("whoosh", 0.6f, (t, p) => Noise() * Mathf.Sin(p * Mathf.PI) * 0.4f);
            _clips[(int)SfxId.Hit] = Make("hit", 0.12f, (t, p) => (Noise() * 0.5f + Sine(140f, t)) * Mathf.Exp(-p * 9f));
            // «Добыто!»: радостное восходящее арпеджио до-ми-соль-до с колокольчиком
            _clips[(int)SfxId.Success] = Make("success", 0.9f, (t, p) =>
            {
                float[] f = { 523.25f, 659.25f, 783.99f, 1046.5f };
                int k = Mathf.Min(3, (int)(p * 5f));
                float local = p * 5f - k; if (k == 3) local = (p * 5f - 3f) / 2f;
                float e = Mathf.Clamp01(local / 0.08f) * Mathf.Exp(-local * (k == 3 ? 2.5f : 4f));
                return (Sine(f[k], t) + Sine(f[k] * 2f, t) * 0.3f + Sine(f[k] * 3f, t) * 0.1f) * 0.55f * e;
            });
            // «Испорчено»: грустное «уа-уа-уааа» — три нисходящих тона с вибрато
            _clips[(int)SfxId.Fail] = Make("fail", 1.1f, (t, p) =>
            {
                float[] f = { 392f, 370f, 349.2f };
                int k = Mathf.Min(2, (int)(p * 4f));
                float local = k < 2 ? p * 4f - k : (p * 4f - 2f) / 2f;
                float vib = k == 2 ? Mathf.Sin(2f * Mathf.PI * 6f * t) * 6f : 0f;
                float freq = (k == 2 ? Mathf.Lerp(f[2], f[2] * 0.9f, local) : f[k]) + vib;
                float e = Mathf.Clamp01(local / 0.06f) * (1f - local * 0.85f);
                float tri = Mathf.Asin(Mathf.Sin(2f * Mathf.PI * freq * t)) * 0.63f;
                return (tri + Sine(freq * 0.5f, t) * 0.3f) * 0.6f * e;
            });
            // ---- кухня ----
            _clips[(int)SfxId.Pickup] = Make("pickup", 0.12f, (t, p) => Sine(Mathf.Lerp(600f, 900f, p), t) * 0.5f * Env(p, 0.02f));
            _clips[(int)SfxId.Chop] = Make("chop", 0.14f, (t, p) => (Noise() * 0.7f + Sine(260f, t) * 0.6f) * Mathf.Exp(-p * 14f));
            _clips[(int)SfxId.Bubble] = Make("bubble", 0.5f, (t, p) =>
            {
                float k = Mathf.Repeat(p * 4f, 1f);
                return Sine(Mathf.Lerp(300f, 700f, k) + (int)(p * 4f) * 60f, t) * Mathf.Exp(-k * 7f) * 0.5f;
            });
            _clips[(int)SfxId.Sizzle] = Make("sizzle", 0.8f, (t, p) => Noise() * (0.25f + 0.15f * Mathf.Sin(t * 90f)) * Mathf.Sin(p * Mathf.PI));
            _clips[(int)SfxId.Splash] = Make("splash", 0.5f, (t, p) => (Noise() * 0.6f * Mathf.Exp(-p * 5f) + Sine(Mathf.Lerp(400f, 150f, p), t) * 0.3f * (1 - p)));
            _clips[(int)SfxId.Stir] = Make("stir", 0.35f, (t, p) => Noise() * 0.3f * Mathf.Sin(p * Mathf.PI) + Sine(180f + 40f * Mathf.Sin(p * 12f), t) * 0.15f * (1 - p));
            _clips[(int)SfxId.Pour] = Make("pour", 0.9f, (t, p) => Noise() * 0.35f * Mathf.Sin(p * Mathf.PI) * (0.7f + 0.3f * Mathf.Sin(t * 40f)));
            _clips[(int)SfxId.Ding] = Make("ding", 0.6f, (t, p) => (Sine(1568f, t) + Sine(2349f, t) * 0.5f) * 0.5f * Mathf.Exp(-p * 5f));
            // Original synthesized voiced scream, not a hint or a generic beep.
            _clips[(int)SfxId.MandrakeScream] = Make("mandrake_scream", 1.25f, (t,p) => {
                float pitch = 390f + 150f*Mathf.Sin(p*Mathf.PI) + 18f*Mathf.Sin(t*43f);
                return (Sine(pitch,t)*.5f + Sine(pitch*2f,t)*.23f + Sine(pitch*3f,t)*.12f + Noise()*.18f) * Mathf.Clamp01(p/.025f) * Mathf.Pow(1-p,.45f);
            });
            _pool = new AudioSource[8];
            for (int i = 0; i < _pool.Length; i++)
            {
                var go = new GameObject("Sfx" + i);
                go.transform.SetParent(transform, false);
                var a = go.AddComponent<AudioSource>();
                a.playOnAwake = false; a.spatialBlend = 0.7f; a.minDistance = 1.5f; a.maxDistance = 25f;
                _pool[i] = a;
            }
            GameEvents.LootGained += OnLoot;
            Wallet.Changed += OnCoins;
        }

        void OnDestroy()
        {
            GameEvents.LootGained -= OnLoot;
            Wallet.Changed -= OnCoins;
            if (I == this) I = null;
        }

        int _coins;
        void OnCoins(int c) { if (c > _coins) Play(SfxId.Coin); _coins = c; }
        void OnLoot(ItemId id, int n) { if (n > 0) Play(SfxId.Loot); }
        /// <summary>Сыграть звук (в точке мира или «у уха», если позиции нет). Один и тот же звук — не чаще 20 раз в секунду.</summary>
        public static void Play(SfxId id, Vector3? at = null, float volume = 0.8f)
        {
            if (I == null) return;
            // Крик не занимает общий пул: соседние удары/лут не обрывают его.
            // Все прочие звуки кухни/таверны и их настройки остаются без изменений.
            if(id==SfxId.MandrakeScream) {
                TryMandrakeScream(at ?? (PlayerRig.I!=null?PlayerRig.I.HeadPos:Vector3.zero), volume);
                return;
            }
            int k = (int)id;
            if (Time.unscaledTime - I._last[k] < 0.05f) return;
            I._last[k] = Time.unscaledTime;
            var a = I._pool[I._next];
            I._next = (I._next + 1) % I._pool.Length;
            var head = PlayerRig.I != null ? PlayerRig.I.HeadPos : Vector3.zero;
            a.transform.position = at ?? head;
            a.spatialBlend = at.HasValue ? 0.7f : 0f;
            a.clip = I._clips[k];
            a.volume = volume;
            a.pitch = Random.Range(0.95f, 1.05f);
            a.Play();
            PlayedTotal++;
            if(id == SfxId.MandrakeScream) { ScreamsPlayed++; LastScreamSpatial=at.HasValue; LastScreamPosition=a.transform.position; }
        }

        // ---- синтез ----
        static float Sine(float f, float t) => Mathf.Sin(2f * Mathf.PI * f * t);
        static float Square(float f, float t) => Mathf.Sign(Mathf.Sin(2f * Mathf.PI * f * t));
        static float Noise() => Random.value * 2f - 1f;
        static float Env(float p, float attack) => Mathf.Clamp01(p / attack) * (1f - p);

        static AudioClip Make(string name, float dur, System.Func<float, float, float> f)
        {
            int n = Mathf.CeilToInt(dur * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f(i / (float)Rate, i / (float)n), -1f, 1f) * 0.7f;
            var c = AudioClip.Create(name, n, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }
    }
}
