using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using InputDevice = UnityEngine.XR.InputDevice;
using CommonUsages = UnityEngine.XR.CommonUsages;

namespace Podz
{
    /// <summary>Слайм (−1). Зелёный куб 0,4 м. Покой → Преследует → Готовится → Прыжок → На лице / Падает → Оглушён 2 с.</summary>
    public class Slime : Monster
    {
        [Header("Сохранённая модель (пусто = прежняя процедурная генерация)")]
        [SerializeField] BoxCollider _savedBody;
        [SerializeField] Transform _savedVisual;
        [SerializeField] AudioSource _stunAudio;
        [SerializeField] AudioClip _stunSound;
        public bool HasSavedModel => _savedBody != null && _savedVisual != null;

        // Жизненный цикл остаётся Monster.Init(kind, dungeon, room): не создаём второй спавнер.
        // Префаб в пустой сцене можно осмотреть; для AI нужны существующие игровые сервисы и Init.
        protected override bool BindSavedModel()
        {
            if (_savedBody == null && _savedVisual == null) return false;
            if (!HasSavedModel || !_savedBody.transform.IsChildOf(transform) || !_savedVisual.IsChildOf(transform))
                throw new System.InvalidOperationException("Slime: повреждены ссылки сохранённой модели. Проверьте prefab.");
            _col = _savedBody; _body = _savedBody.transform;
            Height = _savedBody.bounds.size.y;
            Radius = .22f * Mathf.Max(_savedBody.bounds.size.x, _savedBody.bounds.size.z) / .4f;
            // PropertyBlock не сериализуется Unity. Сохранённые цвета читаем из реальных материалов
            // до сборки прежнего MonsterCache, чтобы сигналы состояния/исчезновение видели верный цвет.
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                var material = renderer.sharedMaterial;
                if (material == null) continue;
                Color color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") :
                    material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
                Prim.SetColor(renderer, color);
            }
            return true;
        }

        Collider _col;
        Transform _body;
        Vector3 _from, _to;
        float _floorY;
        int _shake;
        float _stunnedAt = -10f;
        public const float StunTime = 3f; // пункт 12: окно «Оглушён» 3 с
        [SerializeField, Min(0)] float stunTime = StunTime;

        protected override void Build()
        {
            Height = 0.4f; Radius = 0.22f;
            var b = Prim.Cube("Body", transform, new Vector3(0, 0.2f, 0), new Vector3(0.4f, 0.4f, 0.4f), new Color(0.3f, 0.9f, 0.35f));
            _col = b.GetComponent<Collider>(); _body = b.transform;
            HitZone.Add(b, this, Zone.Body);
            Prim.Cube("Core", transform, new Vector3(0, 0.2f, 0.12f), Vector3.one * 0.12f, new Color(0.1f, 0.45f, 0.15f), false);
        }

        protected override void Tick(float dt)
        {
            switch (State)
            {
                case "Покой":
                    NoGlow();
                    Wander(0.4f, dt);
                    if (CanSeePlayer(6f)) SetState("Преследует");
                    break;
                case "Преследует":
                    NoGlow();
                    Face(ToPlayerFlat, dt);
                    if (DistToPlayer > 9f) SetState("Покой");
                    else if (DistToPlayer < 2.2f) SetState("Готовится");
                    else Move(ToPlayerFlat, 1.1f, dt);
                    break;
                case "Готовится":
                    Telegraph();
                    Face(ToPlayerFlat, dt);
                    if (StateTime > 1f)
                    {
                        _from = transform.position; _floorY = _from.y;
                        _to = P.HeadPos - Vector3.up * 0.2f;
                        SetState("Прыжок");
                    }
                    break;
                case "Прыжок":
                {
                    NoGlow();
                    float t = Mathf.Clamp01(StateTime / 0.5f);
                    var p = Vector3.Lerp(_from, _to, t);
                    p.y += Mathf.Sin(t * Mathf.PI) * 0.5f;
                    transform.position = p;
                    if (t >= 1f)
                    {
                        if ((P.HeadPos - Vector3.up * 0.2f - _to).magnitude < 0.6f) { SetState("На лице"); _shake = 0; _col.enabled = false; Hints.Show("Слайм на лице! Схвати его рукой у лица (ПК: E ×3)", HintPri.Danger); }
                        else { _from = transform.position; SetState("Падает"); }
                    }
                    break;
                }
                case "На лице":
                {
                    var cam = P.Cam.transform;
                    transform.position = cam.position + cam.forward * 0.15f - Vector3.up * 0.2f;
                    DamagePlayer(Dmg.SlimePerSec * dt, "слайм душит (E ×3 — сорвать)");
                    bool torn = HandTearing(XRNode.LeftHand, P.Left) || HandTearing(XRNode.RightHand, P.Right);
                    if (!P.IsXR && DIn.Down(Key.E)) { _shake++; torn = _shake >= 3; }
                    if (torn || StateTime > 6f)
                    {
                        var fwd = cam.forward; fwd.y = 0; fwd.Normalize();
                        _from = cam.position + fwd * 0.9f; _floorY = P.transform.position.y;
                        transform.position = _from;
                        _col.enabled = true;
                        SetState("Падает");
                    }
                    break;
                }
                case "Падает":
                {
                    var p = transform.position;
                    p.y = Mathf.MoveTowards(p.y, _floorY, 4f * dt);
                    transform.position = p;
                    if (Mathf.Abs(p.y - _floorY) < 0.01f) { SetState("Оглушён"); Hints.Show("Слайм оглушён — бей лопаткой (ЛКМ или ПКМ)!", HintPri.Window); }
                    break;
                }
                case "Оглушён":
                    Window();
                    WindowTimer(stunTime - StateTime, stunTime);
                    if (StateTime > stunTime) SetState("Преследует");
                    break;
            }
        }

        public int Shakes => _shake;

        public override HuntStep Hunt()
        {
            switch (State)
            {
                case "На лице": return HuntStep.Of(2, 3, IsXR ? "Слайм на лице! Сорви его рукой (Grip у лица)!" : $"Слайм на лице! ЖМИ E 3 раза — сорви! ({_shake}/3)", null, false, true);
                case "Оглушён": return HuntStep.Of(3, 3, $"Оглушён! Бей лопаткой ещё раз — {Lmb}!", _body, true);
                case "Готовится": case "Прыжок": case "Падает": return HuntStep.Of(2, 3, $"Сейчас прыгнет — шагни вбок или бей лопаткой ({Lmb})", _body, DistToPlayer < 2.5f);
                default: return HuntStep.Of(2, 3, $"Ударь слайма лопаткой — {Lmb} (оглушишь)", _body, DistToPlayer < 2.5f);
            }
        }

        bool HandTearing(XRNode node, HandController h)
        {
            return P.IsXR && XRIn.Grip(node) > 0.6f && h.Held == null && Vector3.Distance(h.transform.position, P.HeadPos) < 0.35f;
        }

        public override void OnHit(HitInfo h, Zone zone)
        {
            if (State == "Прыжок" || State == "На лице" || State == "Падает") return;
            if (h.Type == ToolType.Spatula)
            {
                // ЛЮБОЙ удар лопаткой (ЛКМ сверху или ПКМ сбоку, плашмя или ребром) по оглушённому слайму = добыт.
                // Не оглушён — удар лопаткой сбивает его с ног (оглушает), второй удар добывает. Лопаткой слайма не испортить.
                if (h.Speed < 0.6f) return;
                if (State == "Оглушён" && Time.time - _stunnedAt > 0.35f) { Capture(ItemId.SlimeJelly); return; }
                if (State != "Оглушён")
                {
                    SetState("Оглушён"); _stunnedAt = Time.time; // тот же взмах не должен сразу и добыть
                    Hints.Show("Оглушил! Слайм светится золотым — бей лопаткой ещё раз (ЛКМ или ПКМ)", HintPri.Window, 3f);
                }
                return;
            }
            else if (h.IsHuntingTool && h.Speed >= 1f)
            {
                Hints.Show($"Для слайма нужна лопатка (левое бедро, ПК: {Tools.Key(ToolType.Spatula)})", HintPri.Error);
                Ruin($"{Tools.Name(h.Type)} не подходит — от слайма ничего не осталось. Слайма бьют только лопаткой (клавиша {Tools.Key(ToolType.Spatula)})");
            }
        }

        // как бы слайм ни покинул лицо (сорван, упал, оглушён) — его коллайдер снова включается, иначе по нему не попасть
        protected override void OnStateChanged(string from, string to)
        {
            if (from == "На лице" && _col != null) _col.enabled = true;
            // Необязательный назначенный звук только нового префаба; старый путь/баланс не меняется.
            if (to == "Оглушён" && _stunAudio != null && _stunSound != null) _stunAudio.PlayOneShot(_stunSound);
        }

        protected override void CancelAttack()
        {
            if(_col != null)_col.enabled=true;
            if(State=="На лице" || State=="Прыжок")transform.position=new Vector3(transform.position.x,_floorY,transform.position.z);
            base.CancelAttack();
        }
        protected override void OnPlayerDied() { if (_col != null) _col.enabled = true; SetState("Покой"); }
    }
}
