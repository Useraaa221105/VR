using System.Collections.Generic;
using UnityEngine;

namespace Podz
{
    /// <summary>
    /// Продукт на кухне. Подготовка: нож/тесак/молоток по доске, мойка (лежать в мойке 1 с), гриль (4 с).
    /// Неподготовленный в котле = испорченный суп. Упавший на пол НЕ исчезает: он «испорчен» — поднять E (в любом месте кухни) и выбросить в урну.
    /// Все продукты в статическом списке, который чистится в OnDestroy (кеш не копит удалённые объекты).
    /// </summary>
    public class Ingredient : GrabItem, IHittable
    {
        public static readonly List<Ingredient> All = new List<Ingredient>();
        public const int MaxFree = 40;

        [field: SerializeField] public ItemId Id { get; private set; }
        public bool Prepared { get; private set; }
        public bool Washed { get; private set; }
        public bool Burnt { get; private set; }
        /// <summary>Упал на пол — испорчен: в котёл и под нож нельзя, только в урну (TrashBin).</summary>
        public bool Dropped { get; private set; }
        public bool Spoiled => Dropped || Burnt;
        public bool ReturnsToFridge => _info.Monster && !Burnt && _hits == 0;
        public static int DropCount;
        public int Hits => _hits;
        int _hits;
        bool _despawning;
        public bool Cleaned { get; private set; }
        Vector3 _cleanTip;
        bool _hasCleanTip;
        float _cleanDistance;
        void CleanMushroom(Kitchen k)
        {
            if (Id != ItemId.MushroomLegs || Cleaned || Spoiled || IsHeld || !k.OnBoard(transform.position)) { _hasCleanTip = false; return; }
            var knife = Tool.FindWorldTool(ToolType.Knife);
            if (knife == null || knife.Holder == null || knife.Tip == null || Mathf.Abs(knife.BladeNormal.y) < .7f || Vector3.Distance(knife.Tip.position, transform.position) > .18f)
            { _hasCleanTip = false; return; }
            var tip = knife.Tip.position;
            if (_hasCleanTip)
            {
                var move = tip - _cleanTip;
                // Считаем фактический путь ножа около продукта, не «длину» из вызова метода/телепорта инструмента.
                if (move.magnitude < .18f && Mathf.Abs(move.y) < .04f) _cleanDistance += new Vector2(move.x, move.z).magnitude;
            }
            _cleanTip = tip; _hasCleanTip = true;
            if (_cleanDistance >= .15f) { Cleaned = true; Hints.Show("Ножки очищены — теперь два реза ножом сверху вниз", HintPri.Action); Refresh(); }
        }
        public bool Despawning => _despawning;
        float _washT, _grillT, _floorT;
        Renderer _r;
        [SerializeField] Transform _model;
        Transform _pieces;
        int _shownPieces = -1;
        public int Pieces => _shownPieces < 0 ? 1 : _shownPieces;
        [SerializeField] TextMesh _label;
        ItemInfo _info;

        public static Ingredient Spawn(ItemId id, Vector3 pos)
        {
            if (All.Count >= MaxFree) { Hints.Show($"На кухне уже {MaxFree} продуктов — используй их", HintPri.Error); return null; }
            var info = Items.Get(id);
            var prefab = SavedDungeonAssets.Active?.FindIngredient(id);
            if (prefab != null && prefab._model != null && prefab._label != null)
            {
                var saved = Instantiate(prefab, pos, Quaternion.identity);
                saved._info = info;
                saved.Rb = saved.GetComponent<Rigidbody>();
                saved._r = saved._model.GetComponentInChildren<Renderer>();
                saved.Prepared = info.Prep == PrepKind.None;
                SavedDungeonAssets.RestoreText(saved.gameObject);
                saved.Refresh();
                All.Add(saved);
                return saved;
            }
            // корень без масштаба: невидимый коллайдер-коробка + узнаваемая фигурка продукта (FoodModels)
            var go = new GameObject(info.Name);
            go.transform.position = pos;
            var bc = go.AddComponent<BoxCollider>(); bc.size = new Vector3(0.14f, 0.1f, 0.14f);
            var ing = go.AddComponent<Ingredient>();
            ing.Id = id; ing._info = info;
            ing.SetupPhysics(0.3f);
            ing._model = FoodModels.Build(id, go.transform, 0.14f);
            ing._model.localPosition = new Vector3(0, -0.05f, 0);
            ing._r = ing._model.GetComponentInChildren<Renderer>();
            ing._label = Prim.Text(go.transform, new Vector3(0, 0.16f, 0), "", 0.03f, Color.white);
            ing.Prepared = info.Prep == PrepKind.None;
            ing.Refresh();
            All.Add(ing);
            return ing;
        }

        void OnDestroy() { All.Remove(this); }

        public string Status => Id == ItemId.MushroomLegs && !Cleaned && !Spoiled ? "очистить плашмя 15см (ПК: ПКМ), потом 2 реза" : Burnt ? "сгорело — в урну" : Dropped ? "упал — в урну" : Prepared ? "готово" : _info.Prep == PrepKind.WashThenKnife && !Washed ? "помыть" : _info.Prep == PrepKind.WashOnly ? "промыть" :
            _info.Prep == PrepKind.Grill ? "на гриль" : $"{_hits}/{_info.Hits}";

        void Refresh()
        {
            if (_label != null) _label.text = $"{_info.Name}\n({Status})";
            // нарезка видна: после каждого удара продукт делится на кусочки (1 → 3 → … → 6)
            bool cut = _info.Prep == PrepKind.Knife || _info.Prep == PrepKind.Cleaver || _info.Prep == PrepKind.Hammer || _info.Prep == PrepKind.WashThenKnife;
            int want = !cut ? -1 : Prepared ? 6 : _hits > 0 ? Mathf.Min(5, 1 + _hits * 2) : -1;
            if (want != _shownPieces)
            {
                _shownPieces = want;
                if (_pieces != null) Destroy(_pieces.gameObject);
                _pieces = null;
                if (want > 0)
                {
                    _pieces = FoodModels.BuildPieces(Id, transform, 0.14f, want);
                    _pieces.localPosition = new Vector3(0, -0.05f, 0);
                }
                if (_model != null) _model.gameObject.SetActive(want <= 0);
            }
            var col = Burnt ? new Color(0.1f, 0.08f, 0.06f) : (Color?)null;
            if (col != null)
                foreach (var r in GetComponentsInChildren<Renderer>()) if (!(r is MeshRenderer) || r.GetComponent<TextMesh>() == null) Prim.SetColor(r, col.Value);
        }

        void Update()
        {
            if (_label != null && PlayerRig.I != null)
            {
                Prim.FaceTo(_label.transform, PlayerRig.I.HeadPos);
                _label.gameObject.SetActive((PlayerRig.I.HeadPos - transform.position).sqrMagnitude < 9f);
            }
            if (DTime.Delta <= 0) return;
            var k = Kitchen.I;
            if (k == null) return;
            CleanMushroom(k);
            // мойка
            if (!Washed && (_info.Prep == PrepKind.WashOnly || _info.Prep == PrepKind.WashThenKnife) && k.InSink(transform.position))
            {
                _washT += DTime.Delta;
                if (_washT >= 1f)
                {
                    Washed = true;
                    if (_info.Prep == PrepKind.WashOnly) Prepared = true;
                    Hints.Show($"{_info.Name}: помыто", HintPri.Action);
                    Sfx.Play(SfxId.Splash, transform.position, 0.5f);
                    Refresh();
                }
            }
            // гриль
            if (_info.Prep == PrepKind.Grill && !IsHeld && k.OnGrill(transform.position) && !Burnt)
            {
                _grillT += DTime.Delta;
                if (Mathf.Repeat(_grillT, 0.8f) < DTime.Delta) Sfx.Play(SfxId.Sizzle, transform.position, 0.5f);
                if (!Prepared && _grillT >= 4f) { Prepared = true; Hints.Show("Драконье мясо обжарено — снимай!", HintPri.Window); Refresh(); }
                else if (_grillT >= 12f) { Burnt = true; Prepared = false; Hints.Show("Мясо сгорело!", HintPri.Error); Refresh(); }
            }
            // упало на пол → испорчено (не исчезает: поднять E и выбросить в урну)
            if (!IsHeld && transform.position.y < k.FloorY + 0.3f && k.InKitchen(transform.position))
            {
                _floorT += DTime.Delta;
                if (!Dropped && _floorT > 0.15f) MarkDropped();
            }
            else _floorT = 0;
            // вылетел из кухни (сквозь стену, в зал) — вернуть на пол перед столом, чтобы его можно было поднять
            if (!IsHeld && (transform.position.y < -2f || (transform.position.y < 0.5f && !k.InKitchen(transform.position) && !k.InWindow(transform.position))))
                PlaceAt(k.transform.TransformPoint(new Vector3(-0.6f, 0.1f, 1.9f)));
        }

        public void MarkDropped()
        {
            if (Dropped) return;
            Dropped = true; DropCount++;
            Hints.Show($"{_info.Name} упал на пол! Подними (E) — {TrashBin.Tip.ToLower()}", HintPri.Error, 4f);
            Refresh();
        }

        /// <summary>Убрать продукт. Если это мясо монстра и оно не испорчено — вернуть в ледник.</summary>
        public void Despawn(bool returnToFridge)
        {
            if (_despawning) return;
            _despawning = true;
            All.Remove(this);
            foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = false;
            if (returnToFridge && _info.Monster && !Burnt && _hits == 0) Inventory.Fridge.Add(Id, 1);
            if (IsHeld) Holder.ForceRelease();
            Destroy(gameObject);
        }

        public void OnToolHit(HitInfo h)
        {
            if (_despawning) return;
            if (Spoiled) { if (h.Speed > 0.5f) Hints.Show($"{_info.Name}: {(Dropped ? "упал на пол" : "сгорел")} — выброси в урну и возьми новый", HintPri.Error); return; }
            if (Prepared) return;
            var k = Kitchen.I;
            bool onBoard = k != null && k.OnBoard(transform.position);
            PrepKind need = _info.Prep == PrepKind.WashThenKnife ? PrepKind.Knife : _info.Prep;
            ToolType? tool = need == PrepKind.Knife ? ToolType.Knife : need == PrepKind.Cleaver ? ToolType.Cleaver : need == PrepKind.Hammer ? ToolType.Hammer : (ToolType?)null;
            if (tool == null) { if (h.Speed > 0.5f) Hints.Show($"{_info.Name}: {_info.PrepText}", HintPri.Action); return; }
            if (h.Type != tool.Value)
            {
                if (h.Speed > 0.5f)
                {
                    Hints.Show($"{_info.Name}: нужен {Tools.Name(tool.Value)}", HintPri.Error);
                    string verb = tool.Value == ToolType.Knife ? "не режут" : tool.Value == ToolType.Cleaver ? "не рубят" : "не отбивают";
                    KitchenCoach.Forbid($"Этим {verb} — возьми {Tools.Name(tool.Value).ToLower()} ({Tools.KitchenKey(tool.Value)})");
                }
                return;
            }
            if (!onBoard) { Hints.Show("Положи на разделочную доску", HintPri.Error); KitchenCoach.Forbid("Не на весу — сначала положи продукт на доску (E)"); return; }
            if (Id == ItemId.MushroomLegs && !Cleaned) { Hints.Show("Сначала проведи ножом плашмя 15см: на ПК — ПКМ по ножкам на доске", HintPri.Action); return; }
            if (tool == ToolType.Knife && (!h.Down || !h.Edge || Mathf.Abs(h.BladeNormal.y) > .5f))
            { Hints.Show("Режь ножом сверху вниз, лезвие вертикально (ПК: ЛКМ)", HintPri.Action); return; }
            if (_info.Prep == PrepKind.WashThenKnife && !Washed) { Hints.Show("Сначала помыть в мойке", HintPri.Error); KitchenCoach.Forbid("Сначала помой в мойке, потом режь"); return; }
            float min = tool == ToolType.Knife ? 0.8f : 1.5f;
            if (h.Speed < min) { Hints.Show($"Слабо ({h.Speed:0.0} м/с, нужно {min})", HintPri.Error); return; }
            _hits++;
            Sfx.Play(SfxId.Chop, transform.position);
            if (_hits >= _info.Hits) { Prepared = true; Hints.Show($"{_info.Name}: нарезано — неси в котёл", HintPri.Action); }
            Refresh();
        }
    }
}
