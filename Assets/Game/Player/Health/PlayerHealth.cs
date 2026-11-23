using System.Collections.Generic;
using UnityEngine;

namespace Podz
{
    public enum Status { Slowed, SlimeFace, Spores, Petrified, Stunned }

    /// <summary>HP 100, статусы, эффекты экрана без тряски камеры.</summary>
    public class PlayerHealth : MonoBehaviour
    {
        public float HP { get; private set; } = 100;
        public const float Max = 100;
        public float TotalDamage { get; private set; }
        public string LastSource { get; private set; }
        readonly Dictionary<Status, float> _until = new Dictionary<Status, float>();
        Renderer _overlay;
        float _flash;
        float _lastDamageHint;

        public void Init(PlayerRig rig)
        {
            var holder = new GameObject("StatusOverlay").transform;
            holder.SetParent(rig.Cam.transform, false);
            holder.localPosition = new Vector3(0, 0, 0.1f);
            _overlay = Prim.Quad("Overlay", holder, Vector3.zero, new Vector2(1f, 1f), new Color(0, 0, 0, 0));
        }

        public bool Has(Status s) => _until.TryGetValue(s, out var t) && Time.time < t;
        public const float PetrifyProtection = 6f, StunProtection = 4f;
        float _stunProtectedUntil;
        public bool CanStun => !Has(Status.Stunned) && Time.time >= _stunProtectedUntil;
        float _petrifyProtectedUntil;
        public float PetrifyProtectedUntil => _petrifyProtectedUntil;
        public bool CanPetrify => !Has(Status.Petrified) && Time.time >= _petrifyProtectedUntil;
        public void Apply(Status s, float seconds)
        {
            if(s == Status.Stunned) {
                if(!CanStun) return;
                _until[s] = Time.time + Mathf.Min(2f, seconds);
                _stunProtectedUntil = _until[s] + StunProtection;
                return;
            }
            if (s == Status.Petrified)
            {
                if (!CanPetrify) return;
                _until[s] = Time.time + seconds;
                _petrifyProtectedUntil = _until[s] + PetrifyProtection;
                return;
            }
            _until[s] = Mathf.Max(Has(s) ? _until[s] : 0, Time.time + seconds);
        }
        public void Clear(Status s) { if (s == Status.Petrified && Has(s)) _petrifyProtectedUntil = Time.time + PetrifyProtection; _until.Remove(s); }

        public float MoveMultiplier
        {
            get
            {
                if (Has(Status.Petrified) || Has(Status.Stunned)) return 0f;
                if (Has(Status.Slowed) || Has(Status.Spores)) return 0.5f;
                return 1f;
            }
        }

        float _entryUntil;
        public bool EntryProtected => Time.time < _entryUntil;
        public void ProtectEntry(float seconds) { _entryUntil = Time.time + seconds; AttackGate.Clear(); }
        float _pendingDamage;

        public void Damage(float amount, string source)
        {
            if (amount <= 0 || HP <= 0 || EntryProtected) return;
            HP -= amount;
            Sfx.Play(SfxId.Hurt);
            TotalDamage += amount; LastSource = source;
            _flash = 0.35f;
            _pendingDamage += amount; // непрерывный урон (дыхание, яд) копится, чтобы не писать «−0 HP»
            if (Time.time - _lastDamageHint > 0.8f)
            {
                _lastDamageHint = Time.time;
                string tip = HP > 0 && HP < 50 && Elixirs.Count > 0 ? $" · {(PlayerRig.I != null && PlayerRig.I.IsXR ? "B" : "6 и ЛКМ")} — выпить эликсир ({Elixirs.Count})" : "";
                Hints.Show($"−{Mathf.CeilToInt(_pendingDamage)} HP · {source}{tip}", HintPri.Danger, tip.Length > 0 ? 2.5f : 1.5f);
                _pendingDamage = 0;
            }
            if (HP <= 0)
            {
                HP = 0;
                Debug.Log("[Player] смерть");
                GameEvents.RaisePlayerDied();
            }
        }

        /// <summary>Пункт 11: откуда может прийти лечение — только колодец и эликсир.</summary>
        public enum HealSource { Well, Elixir }
        public static int HealsWell, HealsElixir;
        public static float HealedTotal;
        public void Heal(float amount, HealSource src)
        {
            float before = HP;
            HP = Mathf.Min(Max, HP + amount);
            HealedTotal += HP - before;
            if (src == HealSource.Well) HealsWell++; else HealsElixir++;
        }
        /// <summary>Обморок: Тоби вытащил в таверну — снова полные HP (новая попытка, не лечение).</summary>
        public static int Revives;
        public void Revive() { Revives++; HP = Max; _until.Clear(); _petrifyProtectedUntil = Time.time + PetrifyProtection; }

        float _heal;
        /// <summary>Пункт 5: зелёная вспышка экрана, когда выпит эликсир.</summary>
        public void HealFlash() { _heal = 0.6f; }
        public bool HealFlashing => _heal > 0;

        public void ResetAll()
        {
            _petrifyProtectedUntil = 0; _stunProtectedUntil = 0; _entryUntil = 0;
            HP = Max;
            _until.Clear();
        }

        void Update()
        {
            _flash = Mathf.Max(0, _flash - Time.deltaTime);
            Color c = new Color(0, 0, 0, 0);
            if (Has(Status.Slowed)) c = new Color(0.2f, 0.4f, 0.9f, 0.18f);
            if (Has(Status.Spores)) c = new Color(0.9f, 0.85f, 0.2f, 0.3f);
            if (Has(Status.Petrified)) c = new Color(0.5f, 0.5f, 0.5f, 0.55f);
            if (Has(Status.Stunned)) c = new Color(1f, 1f, 1f, 0.3f);
            if (_flash > 0) c = Color.Lerp(c, new Color(1f, 0f, 0f, 0.45f), _flash / 0.35f);
            _heal = Mathf.Max(0, _heal - Time.deltaTime);
            if (_heal > 0) c = Color.Lerp(c, new Color(0.2f, 1f, 0.35f, 0.4f), _heal / 0.6f);
            if (HP < 30 && c.a < 0.1f && _heal <= 0) c = new Color(0.6f, 0f, 0f, 0.12f + 0.06f * Mathf.Sin(Time.time * 5f));
            Prim.SetOverlayColor(_overlay, c);
            _overlay.enabled = c.a > 0.01f;
        }
    }
}
