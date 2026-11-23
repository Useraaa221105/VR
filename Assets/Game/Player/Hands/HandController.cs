using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using InputDevice = UnityEngine.XR.InputDevice;
using CommonUsages = UnityEngine.XR.CommonUsages;

namespace Podz
{
    /// <summary>
    /// Рука игрока. XR: grip — взять (инструмент из кобуры / предмет / часть монстра), курок — нажать кнопку.
    /// ПК: 1–7 инструменты, ЛКМ — удар сверху, ПКМ — горизонтальный срез, F/СКМ — тычок, E — взять/нажать, Q — бросить/тянуть.
    /// </summary>
    public class HandController : MonoBehaviour
    {
        public bool IsLeft { get; private set; }
        public XRNode Node { get; private set; }
        public IGrabbable Held { get; private set; }
        public Tool HeldTool => Held as Tool;
        public Vector3 Velocity { get; private set; }
        public bool Swinging { get; private set; }

        PlayerRig _rig;
        Renderer _vis;
        Edge _grip, _trig, _sec;
        // ---- пункт 5: эликсир в руке (ПК: 6 в подземелье, ЛКМ — выпить; VR: B на правом контроллере) ----
        public bool ElixirOut { get; private set; }
        GameObject _elixirVis;
        static bool InDungeonNow => DungeonManager.I != null && DungeonManager.I.InDungeon;
        bool _xr;
        Vector3 _deskRest;
        readonly Vector3[] _pos = new Vector3[8];
        readonly float[] _t = new float[8];
        int _i;
        static readonly Collider[] _cols = new Collider[24];

        public static HandController Create(PlayerRig rig, bool left)
        {
            var go = new GameObject(left ? "LeftHand" : "RightHand");
            var h = go.AddComponent<HandController>();
            h._rig = rig;
            h.IsLeft = left;
            h.Node = left ? XRNode.LeftHand : XRNode.RightHand;
            h._vis = Prim.Cube("HandVis", go.transform, Vector3.zero, new Vector3(0.07f, 0.04f, 0.1f), new Color(0.95f, 0.78f, 0.62f), false).GetComponent<Renderer>();
            return h;
        }

        public void SetMode(bool xr)
        {
            _xr = xr;
            if (xr)
            {
                transform.SetParent(_rig.Offset, false);
            }
            else
            {
                transform.SetParent(_rig.Cam.transform, false);
                _deskRest = IsLeft ? new Vector3(-0.24f, -0.26f, 0.42f) : new Vector3(0.22f, -0.25f, 0.4f);
                transform.localPosition = _deskRest;
                transform.localRotation = Quaternion.identity;
            }
            _vis.enabled = xr;
        }

        public void UpdatePose()
        {
            if (!_xr) return;
            if (XRIn.Pose(Node, out var p, out var r))
            {
                transform.localPosition = p;
                transform.localRotation = r;
            }
        }

        void Update()
        {
            if (_xr) { UpdatePose(); XRUpdate(); }
            else if (!IsLeft) DesktopUpdate();
            TrackVelocity();
        }

        void TrackVelocity()
        {
            _i = (_i + 1) % _pos.Length;
            _pos[_i] = transform.position;
            _t[_i] = Time.time;
            int oldest = (_i + 1) % _pos.Length;
            float dt = _t[_i] - _t[oldest];
            Velocity = dt > 0.0001f ? (_pos[_i] - _pos[oldest]) / dt : Vector3.zero;
        }

        // ================= XR =================
        void XRUpdate()
        {
            _grip.Update(XRIn.Grip(Node) > 0.6f);
            _trig.Update(XRIn.Trigger(Node) > 0.6f);
            if (_grip.Down) TryGrabNear();
            if (_grip.Up && Held != null) Release(Velocity);
            if (_trig.Down) TryPressNear();
            if (!IsLeft)
            {
                _sec.Update(XRIn.Secondary(Node));
                if (_sec.Down) VrDrinkElixir();
            }
        }

        /// <summary>VR: кнопка B — выпить эликсир (только в подземелье).</summary>
        public bool VrDrinkElixir()
        {
            if (!InDungeonNow) { Hints.Show("Эликсиры пьют в подземелье", HintPri.Error); return false; }
            return Elixirs.Drink(_rig.Health);
        }

        void ShowElixir(bool on)
        {
            if (on && _elixirVis == null)
            {
                _elixirVis = new GameObject("ElixirInHand");
                _elixirVis.transform.SetParent(transform, false);
                _elixirVis.transform.localPosition = new Vector3(-0.02f, 0.03f, 0.05f);
                ElixirPickup.BuildBottle(_elixirVis.transform, 0.7f);
            }
            if (_elixirVis != null) _elixirVis.SetActive(on);
            ElixirOut = on;
        }

        void ToggleElixir()
        {
            if (ElixirOut) { ShowElixir(false); return; }
            if (!InDungeonNow) { Hints.Show("Эликсиры — только в подземелье", HintPri.Error); return; }
            if (Elixirs.Count <= 0) { Elixirs.LastMessage = "Эликсиров нет — ищи светящиеся бутылочки в подземелье"; Hints.Show(Elixirs.LastMessage, HintPri.Error); return; }
            if (Held != null) ForceRelease();
            ShowElixir(true);
            Hints.Show($"Эликсир в руке ({Elixirs.Count}/{Elixirs.Max}) — ЛКМ, чтобы выпить (+20 HP)", HintPri.Action, 3f);
        }

        /// <summary>ПК: 6 в подземелье — достать/убрать эликсир (для автотеста — то же, что клавиша).</summary>
        public void PcSelectElixir() => ToggleElixir();
        /// <summary>ПК: ЛКМ с эликсиром в руке — выпить.</summary>
        public bool PcDrinkElixir()
        {
            if (!ElixirOut) return false;
            bool ok = Elixirs.Drink(_rig.Health);
            if (Elixirs.Count <= 0) ShowElixir(false);
            return ok;
        }

        void TryGrabNear()
        {
            // 1) инструмент из кобуры на теле
            foreach (var tool in _rig.Tools.Values)
            {
                if (tool.IsHome && Vector3.Distance(tool.transform.position, transform.position) < 0.25f)
                {
                    if (!ToolRules.Check(tool.Type)) return; // VR: охотничий инструмент на кухне не достаётся
                    Grab(tool);
                    return;
                }
            }
            // 2) ближайший предмет / часть монстра
            int n = Physics.OverlapSphereNonAlloc(transform.position, 0.13f, _cols, ~0, QueryTriggerInteraction.Collide);
            IGrabbable best = null;
            float bd = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var g = _cols[i].GetComponentInParent<IGrabbable>();
                if (g == null || !g.CanGrab(this)) continue;
                float d = Vector3.Distance(_cols[i].ClosestPoint(transform.position), transform.position);
                if (d < bd) { bd = d; best = g; }
            }
            if (best != null) Grab(best);
        }

        void TryPressNear()
        {
            int n = Physics.OverlapSphereNonAlloc(transform.position, 0.1f, _cols, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < n; i++)
            {
                var p = _cols[i].GetComponentInParent<Pressable>();
                if (p != null) { p.Press(); return; }
            }
        }

        // ================= общие =================
        public void Grab(IGrabbable g)
        {
            if (g == null || !g.CanGrab(this)) return;
            if (Held != null) Release(Vector3.zero);
            if (ElixirOut) ShowElixir(false);
            Held = g;
            g.Grab(this);
        }

        public void Release(Vector3 v)
        {
            var g = Held;
            Held = null;
            if (g != null && (g as Object) != null) g.Release(this, v);
        }

        /// <summary>Отпустить без броска (телепорт, смерть монстра и т.п.). Инструменты возвращаются в кобуру.</summary>
        public void ForceRelease()
        {
            var g = Held;
            Held = null;
            if (g == null || (g as Object) == null) return;
            g.Release(this, Vector3.zero);
            if (g is Tool t) t.ReturnHome();
        }

        /// <summary>Вызывается объектом, если он уничтожен/забран, чтобы рука не держала «мёртвую» ссылку.</summary>
        public void ClearIfHolding(IGrabbable g) { if (Held == g) Held = null; }

        // ================= ПК-режим =================
        void DesktopUpdate()
        {
            var left = _rig.Left;
            // выбор инструментов
            // пункт 7: номера по месту, всегда с 1 (подземелье: 1–5 инструменты, 6 эликсир; кухня: 1 нож, 2 половник, 3 тесак, 4 молоток)
            if (ElixirOut && !InDungeonNow) ShowElixir(false);
            for (int n = 1; n <= 7; n++)
                if (DIn.Down(Key.Digit1 + (n - 1))) PcKey(n);
            if (DIn.Down(Key.Digit0) || DIn.Down(Key.Backquote)) { if (HeldTool != null) ForceRelease(); if (ElixirOut) ShowElixir(false); }
            if (ElixirOut && DIn.LMB) { PcDrinkElixir(); return; }

            if (DIn.Down(Key.E)) DesktopInteract(left);
            if (DIn.Down(Key.Q)) DesktopThrow(left);

            if (!Swinging && HeldTool != null && HeldTool.Type != ToolType.Shield)
            {
                if (DIn.LMB) StartCoroutine(Chop());
                else if (DIn.RMB) StartCoroutine(Slash());
                else if (DIn.MMB || DIn.Down(Key.F)) StartCoroutine(Poke());
            }
        }

        // ---- публичные действия ПК (те же, что клавиши) — для автотеста и обучения ----
        public void PcSelect(ToolType t) { if (t == ToolType.Knife || t == ToolType.Ladle) SelectWorldTool(t); else if (t == ToolType.Shield) ToggleShield(_rig.Left); else SelectTool(t); }
        /// <summary>0 — ЛКМ (сверху вниз), 1 — ПКМ (горизонтально), 2 — F (медленный тычок).</summary>
        public bool PcSwing(int kind)
        {
            if (Swinging || HeldTool == null || HeldTool.Type == ToolType.Shield) return false;
            StartCoroutine(kind == 0 ? Chop() : kind == 1 ? Slash() : Poke());
            return true;
        }
        public void PcInteract() => DesktopInteract(_rig.Left);
        /// <summary>Пункт 7: нажатие цифры n в текущем месте (то же, что клавиша; для автотеста).</summary>
        public static int LastKeyPressed; public static string LastKeyResult = "";
        public void PcKey(int n)
        {
            LastKeyPressed = n;
            bool dungeon = InDungeonNow;
            if (dungeon && n == 6) { ToggleElixir(); LastKeyResult = "эликсир"; return; }
            var t = Tools.AtKey(n, dungeon);
            if (t == null)
            {
                LastKeyResult = "";
                Hints.Show(dungeon ? "В подземелье: 1–5 — инструменты, 6 — эликсир" : "На кухне: 1 — нож, 2 — половник, 3 — тесак, 4 — молоток", HintPri.Error, 2.5f);
                return;
            }
            LastKeyResult = Tools.Name(t.Value);
            PcSelect(t.Value);
        }
        public void PcThrow() => DesktopThrow(_rig.Left);

        void SelectTool(ToolType t)
        {
            if (!ToolRules.Check(t)) return;
            if (!_rig.Tools.TryGetValue(t, out var tool)) { Hints.Show($"{Tools.Name(t)} ещё не куплен", HintPri.Error); return; }
            if (HeldTool == tool) { ForceRelease(); return; }
            if (Held != null) ForceRelease();
            Grab(tool);
        }

        void SelectWorldTool(ToolType t)
        {
            if (!ToolRules.Check(t)) return;
            var tool = Tool.FindWorldTool(t);
            if (tool == null) { Hints.Show($"{Tools.Name(t)} есть только на кухне", HintPri.Error); return; }
            if (HeldTool == tool) { ForceRelease(); return; }
            if (Held != null) ForceRelease();
            if (tool.Holder != null) tool.Holder.ForceRelease();
            Grab(tool);
        }

        void ToggleShield(HandController left)
        {
            if (!_rig.Tools.TryGetValue(ToolType.Shield, out var s)) { Hints.Show("Котёл-щит ещё не куплен (400 монет)", HintPri.Error); return; }
            if (left.Held == (IGrabbable)s) { left.ForceRelease(); return; }
            if (!ToolRules.Check(ToolType.Shield)) return;
            if (left.Held != null) left.ForceRelease();
            left.Grab(s);
        }

        public const float FloorPickRadius = 2.2f;
        public static int FloorPicks;
        /// <summary>Баг-лист, пункт 1: E поднимает ближайший продукт/миску с пола в радиусе 2.2 м (без прямой видимости — под столом тоже).</summary>
        public bool PickFromFloor()
        {
            if (Held != null || Kitchen.I == null) return false;
            var feet = _rig.Feet;
            GrabItem best = null; float bd = FloorPickRadius * FloorPickRadius;
            void Try(GrabItem g)
            {
                if (g == null || g.IsHeld || g.transform.position.y > Kitchen.I.FloorY + 0.35f) return;
                var d = g.transform.position - feet; d.y = 0;
                if (d.sqrMagnitude < bd) { bd = d.sqrMagnitude; best = g; }
            }
            foreach (var i in Ingredient.All) Try(i);
            foreach (var b in Bowl.All) Try(b);
            if (best == null) return false;
            Grab(best);
            FloorPicks++;
            if (best is Ingredient ing && ing.Dropped) Hints.Show($"{Items.Name(ing.Id)} поднят. {TrashBin.Tip}", HintPri.Tutorial, 4f);
            return true;
        }

        void DesktopInteract(HandController left)
        {
            var cam = _rig.Cam.transform;
            // урна рядом и смотрим в её сторону — выбрасываем, даже если луч задел край стола
            if (TrashBin.I != null && Held is GrabItem tr0 && (tr0 is Ingredient || tr0 is Bowl))
            {
                var to = TrashBin.I.transform.position + Vector3.up * 0.5f - cam.position;
                var flat = new Vector3(to.x, 0, to.z);
                if (flat.magnitude < 1.8f && Vector3.Angle(cam.forward, to) < 30f) { TrashBin.I.Throw(tr0); return; }
            }
            if (Rays.First(cam.position, cam.forward, 2.6f, _rig.transform, out var hit))
            {
                var loot = hit.collider.GetComponentInParent<DungeonLoot>();
                if (loot != null && InDungeonNow) { loot.Collect(this); return; }
                var press = hit.collider.GetComponentInParent<Pressable>();
                if (press != null) { press.Press(); return; }

                // урна: держим продукт/миску → выбросить
                var bin = hit.collider.GetComponentInParent<TrashBin>();
                if (bin != null)
                {
                    if (Held is GrabItem trash && (trash is Ingredient || trash is Bowl)) { bin.Throw(trash); return; }
                    if (PickFromFloor()) return;
                    Hints.Show("Урна: подними упавший продукт (E) и выброси сюда", HintPri.Action);
                    return;
                }

                if (HeldTool != null && HeldTool.Type == ToolType.Ladle)
                {
                    var c = hit.collider.GetComponentInParent<Cauldron>();
                    if (c != null) { c.StirOnce(); return; }
                }

                var g = hit.collider.GetComponentInParent<IGrabbable>();
                if (g != null && g.CanGrab(this))
                {
                    if (g is MonsterGrabPoint) { left.Grab(g); return; }
                    if (Held != null) ForceRelease();
                    Grab(g);
                    return;
                }

                // держим продукт/миску — положить туда, куда смотрим (доска, мойка, гриль, окно раздачи, котёл)
                if (Held is GrabItem gi)
                {
                    var mouth = hit.collider.GetComponent<CauldronMouth>();
                    var pos = mouth != null ? mouth.transform.position + Vector3.up * 0.25f : hit.point + hit.normal * 0.1f;
                    // Только миска и реальная полка окна в таверне. Луч может попасть
                    // в бок полки: hit.point + normal*0.1 тогда оказывался вне зоны Bell.
                    if (gi is Bowl && !InDungeonNow && Kitchen.I != null && hit.collider.name == "WindowShelf")
                    {
                        pos = Kitchen.I.WindowSlot(Kitchen.I.WindowTable(hit.point));
                        foreach (var bowl in Bowl.All)
                            if (bowl != null && bowl != gi && !bowl.IsHeld &&
                                (bowl.transform.position - pos).sqrMagnitude < .28f * .28f)
                            { Hints.Show("Это место подачи занято другой миской — выбери свободное место нужного стола", HintPri.Action); return; }
                    }
                    gi.PlaceAt(pos);
                    return;
                }
            }
            // руки пусты — поднять упавший продукт с пола рядом (в любом месте кухни, даже под столом)
            if (Held == null && PickFromFloor()) return;
            // ничего не нашли — положить то, что в руке
            if (Held is GrabItem) Release(cam.forward * 0.5f);
            else if (left.Held is MonsterGrabPoint) left.Release(Vector3.zero);
        }

        void DesktopThrow(HandController left)
        {
            if (left.Held is MonsterGrabPoint) { left.Release(Vector3.up * 2.5f); return; } // «потянуть вверх»
            if (Held is GrabItem) { Release(_rig.Cam.transform.forward * 3.5f + Vector3.up * 1f); return; }
            if (HeldTool != null) ForceRelease();
        }

        // ---- процедурные взмахи (движение инструмента, не анимация персонажа) ----
        // Взмах нацелен в точку под прицелом (до 2.4 м), чтобы инструмент доставал до монстров на полу.
        Vector3 AimPoint()
        {
            var cam = _rig.Cam.transform;
            if (Rays.First(cam.position, cam.forward, 2.4f, _rig.transform, out var hit, false)) return hit.point;
            return cam.position + cam.forward * 1.2f;
        }

        Vector3 FlatForward()
        {
            var f = _rig.Cam.transform.forward; f.y = 0;
            if (f.sqrMagnitude < 1e-4f) f = _rig.transform.forward;
            return f.normalized;
        }

        void EndSwing()
        {
            transform.localPosition = _deskRest;
            transform.localRotation = Quaternion.identity;
            Swinging = false;
        }

        /// <summary>ЛКМ: удар сверху вниз (тесак — ребром, лопатка/молоток — плашмя).</summary>
        IEnumerator Chop()
        {
            Swinging = true;
            var tool = HeldTool;
            var target = AimPoint() + Vector3.up * 0.05f;
            var f = FlatForward();
            var r = Vector3.Cross(Vector3.up, f);
            var baseRot = Quaternion.LookRotation(f, Vector3.up);
            var tipLocal = tool != null ? tool.Tip.localPosition : new Vector3(0, 0, 0.3f);
            // рука стоит так, чтобы при наклоне +40° кончик оказался в точке цели
            var handPos = target - (Quaternion.AngleAxis(40f, r) * baseRot) * tipLocal;
            yield return Sweep(handPos, a => Quaternion.AngleAxis(a, r) * baseRot, -60f, -60f, 0.08f);
            yield return Sweep(handPos, a => Quaternion.AngleAxis(a, r) * baseRot, -60f, 65f, 0.14f);
            yield return new WaitForSeconds(0.08f);
            EndSwing();
        }

        /// <summary>ПКМ: горизонтальный срез справа налево.</summary>
        IEnumerator Slash()
        {
            Swinging = true;
            var tool = HeldTool;
            var f = FlatForward();
            // целимся чуть «внутрь» предмета: дуга кончика должна пройти СКВОЗЬ цель, а не скользнуть по её поверхности
            var target = AimPoint() + f * 0.1f;
            var baseRot = Quaternion.LookRotation(f, Vector3.up) * Quaternion.Euler(0, 0, 90);
            var tipLocal = tool != null ? tool.Tip.localPosition : new Vector3(0, 0, 0.3f);
            var handPos = target - baseRot * tipLocal;
            // замах начинается дальше сбоку (130°), чтобы тесак успел разогнаться ДО касания цели:
            // скорость считается за последние 0,1 с, и с короткого замаха маленькая мандрагора задевалась «медленно»
            yield return Sweep(handPos, a => Quaternion.AngleAxis(a, Vector3.up) * baseRot, 130f, 130f, 0.08f);
            yield return Sweep(handPos, a => Quaternion.AngleAxis(a, Vector3.up) * baseRot, 130f, -80f, 0.22f);
            yield return new WaitForSeconds(0.08f);
            EndSwing();
        }

        /// <summary>F / СКМ: медленный тычок вперёд (~0.4 м/с) — «постучать», «пощупать».</summary>
        IEnumerator Poke()
        {
            Swinging = true;
            var tool = HeldTool;
            var finisher = DragonFinisher.BeginDesktop(tool);
            var target = AimPoint();
            var start = _rig.Cam.transform.position + Vector3.down * 0.2f;
            var d = target - start;
            if (d.sqrMagnitude < 1e-4f) d = FlatForward();
            d.Normalize();
            var rot = Quaternion.LookRotation(d, Vector3.up);
            float len = tool != null ? tool.Tip.localPosition.magnitude : 0.3f;
            var from = target - d * (len + (finisher != null ? .50f : .25f));
            var to = target - d * (len - (finisher != null ? .10f : .05f));
            transform.rotation = rot;
            // Boss-only Shift+F: settle before a real fast forward stroke. Other pokes stay unchanged.
            if (finisher != null)
                for (float t = 0; t < .12f; t += Time.deltaTime) { transform.position = from; transform.rotation = rot; yield return null; }
            float duration = finisher != null ? .14f : .7f;
            for (float t = 0; t < duration; t += Time.deltaTime) { transform.position = Vector3.Lerp(from, to, t / duration); transform.rotation = rot; yield return null; }
            for (float t = 0; t < 0.2f; t += Time.deltaTime) { transform.position = Vector3.Lerp(to, from, t / 0.2f); transform.rotation = rot; yield return null; }
            if (finisher != null) finisher.EndDesktop();
            EndSwing();
        }

        IEnumerator Sweep(Vector3 handPos, System.Func<float, Quaternion> rotAt, float a0, float a1, float dur)
        {
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                transform.position = handPos;
                transform.rotation = rotAt(Mathf.Lerp(a0, a1, t / dur));
                yield return null;
            }
            transform.position = handPos;
            transform.rotation = rotAt(a1);
        }
    }
}
