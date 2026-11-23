using System;
using UnityEngine;

namespace Podz
{
    public enum ToolType { Spatula, Net, Cleaver, Hammer, Shield, Knife, Ladle }

    /// <summary>Параметры удара инструментом: скорость кончика (буфер 0,1 с), направление, нормаль лезвия.</summary>
    public struct HitInfo
    {
        public Tool Tool;
        public ToolType Type;
        public Vector3 Velocity;
        public float Speed;
        public Vector3 Point;
        public Vector3 BladeNormal;
        public Collider Collider;

        public Vector3 Dir => Speed > 1e-4f ? Velocity / Speed : Vector3.zero;
        float NormalDot => Mathf.Abs(Vector3.Dot(Dir, BladeNormal));
        /// <summary>Плашмя: движение вдоль нормали лопасти (±45°).</summary>
        public bool Flat => NormalDot > 0.7f;
        /// <summary>Ребром/лезвием: движение поперёк нормали.</summary>
        public bool Edge => NormalDot < 0.5f;
        public bool Down => Dir.y < -0.5f;
        public bool Horizontal => Mathf.Abs(Dir.y) < 0.5f;
        public bool IsHuntingTool => Type == ToolType.Spatula || Type == ToolType.Net || Type == ToolType.Cleaver || Type == ToolType.Hammer;

        public override string ToString() => $"{Tools.Name(Type)} {Speed:0.0} м/с{(Flat ? " плашмя" : Edge ? " ребром" : "")}{(Down ? " вниз" : "")}";
    }

    public interface IHittable { void OnToolHit(HitInfo h); }

    public interface IGrabbable
    {
        bool CanGrab(HandController hand);
        void Grab(HandController hand);
        void Release(HandController hand, Vector3 throwVelocity);
        Transform transform { get; }
    }

    public static class Tools
    {
        /// <summary>Клавиша приседания (ПК) — одно место для всех подсказок и карточек.</summary>
        public const string CrouchKey = "C"; // пункт 6: основная C, запасная — левый Ctrl (нажал — сел, ещё раз — встал)

        /// <summary>Свой цвет у каждого инструмента: одинаковый в руке, на панели внизу и в карточках бестиария.</summary>
        public static Color ColorOf(ToolType t)
        {
            switch (t)
            {
                case ToolType.Spatula: return new Color(0.3f, 0.9f, 0.35f);  // зелёная
                case ToolType.Net: return new Color(0.3f, 0.75f, 1f);       // голубой
                case ToolType.Cleaver: return new Color(1f, 0.55f, 0.15f);  // оранжевый
                case ToolType.Hammer: return new Color(0.72f, 0.45f, 1f);   // фиолетовый
                case ToolType.Shield: return new Color(1f, 0.8f, 0.25f);    // золотой
                case ToolType.Knife: return new Color(0.85f, 0.88f, 0.95f); // серебристый
                default: return new Color(0.8f, 0.5f, 0.3f);                // медный половник
            }
        }
        public static string ColorName(ToolType t)
        {
            switch (t)
            {
                case ToolType.Spatula: return "зелёная";
                case ToolType.Net: return "голубой";
                case ToolType.Cleaver: return "оранжевый";
                case ToolType.Hammer: return "фиолетовый";
                case ToolType.Shield: return "золотой";
                case ToolType.Knife: return "серебристый";
                default: return "медный";
            }
        }
        /// <summary>Где инструмент висит в VR.</summary>
        public static string VrSlot(ToolType t)
        {
            switch (t)
            {
                case ToolType.Spatula: return "левое бедро";
                case ToolType.Net: return "за спиной";
                case ToolType.Cleaver: return "правое бедро";
                case ToolType.Hammer: return "пояс сзади";
                case ToolType.Shield: return "левое плечо";
                default: return "кухня";
            }
        }
        /// <summary>
        /// Пункт 7: номера инструментов свои для каждого места, всегда с 1.
        /// Подземелье: 1 лопатка, 2 сачок, 3 тесак, 4 молоток, 5 щит, 6 эликсир. Кухня: 1 нож, 2 половник, 3 тесак, 4 молоток.
        /// Key — номер для ТЕКУЩЕГО места (охотничий инструмент на кухне — его номер в подземелье, и наоборот).
        /// </summary>
        public static string Key(ToolType t) => InDungeon ? DungeonKey(t) : KitchenKey(t);
        public static bool InDungeon => DungeonManager.I != null && DungeonManager.I.InDungeon;
        public static readonly ToolType[] DungeonOrder = { ToolType.Spatula, ToolType.Net, ToolType.Cleaver, ToolType.Hammer, ToolType.Shield };
        public static readonly ToolType[] KitchenOrder = { ToolType.Knife, ToolType.Ladle, ToolType.Cleaver, ToolType.Hammer };
        public const string ElixirKey = "6";
        /// <summary>Номер в подземелье (карточки монстров и подсказки охоты).</summary>
        public static string DungeonKey(ToolType t) { int i = System.Array.IndexOf(DungeonOrder, t); return i >= 0 ? (i + 1).ToString() : KitchenKey(t); }
        /// <summary>Номер на кухне (рецепты, подсказки кухни).</summary>
        public static string KitchenKey(ToolType t) { int i = System.Array.IndexOf(KitchenOrder, t); return i >= 0 ? (i + 1).ToString() : DungeonKeyOnly(t); }
        static string DungeonKeyOnly(ToolType t) { int i = System.Array.IndexOf(DungeonOrder, t); return i >= 0 ? (i + 1).ToString() : "?"; }
        /// <summary>Какой инструмент на клавише n (1..) в текущем месте; null — клавиша свободна.</summary>
        public static ToolType? AtKey(int n, bool dungeon)
        {
            var order = dungeon ? DungeonOrder : KitchenOrder;
            return n >= 1 && n <= order.Length ? order[n - 1] : (ToolType?)null;
        }

        public static string Name(ToolType t)
        {
            switch (t)
            {
                case ToolType.Spatula: return "Лопатка";
                case ToolType.Net: return "Сачок";
                case ToolType.Cleaver: return "Тесак";
                case ToolType.Hammer: return "Молоток";
                case ToolType.Shield: return "Котёл-щит";
                case ToolType.Knife: return "Нож";
                default: return "Половник";
            }
        }
    }

    /// <summary>Кнопка в мире: XR — коснуться рукой и нажать курок; ПК — навести и нажать E.</summary>


    /// <summary>Физический предмет, который можно взять (ингредиент, миска).</summary>
    public class GrabItem : MonoBehaviour, IGrabbable
    {
        protected Rigidbody Rb;
        public HandController Holder { get; private set; }
        public bool IsHeld => Holder != null;
        Transform _worldParent;

        protected void SetupPhysics(float mass = 0.3f)
        {
            Rb = gameObject.AddComponent<Rigidbody>();
            Rb.mass = mass;
            Rb.interpolation = RigidbodyInterpolation.Interpolate;
            Rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        public virtual bool CanGrab(HandController hand) => Holder == null;

        public virtual void Grab(HandController hand)
        {
            Holder = hand;
            InteractionFeedback.Emit(transform.position,hand);
            _worldParent = transform.parent;
            Rb.isKinematic = true;
            Rb.interpolation = RigidbodyInterpolation.None; // в руке предмет ведёт transform, а не физика
            transform.SetParent(hand.transform, false);
            transform.localPosition = new Vector3(0, 0, 0.06f);
            transform.localRotation = Quaternion.identity;
        }

        public virtual void Release(HandController hand, Vector3 v)
        {
            Holder = null;
            transform.SetParent(_worldParent, true);
            Rb.position = transform.position;
            Rb.rotation = transform.rotation;
            Rb.isKinematic = false;
            Rb.interpolation = RigidbodyInterpolation.Interpolate;
            SetVelocity(Rb, v);
        }

        public static void SetVelocity(Rigidbody rb, Vector3 v)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = v;
#else
            rb.velocity = v;
#endif
        }

        /// <summary>Положить предмет в точку (например, на доску).</summary>
        public void PlaceAt(Vector3 pos)
        {
            if (Holder != null) Holder.ForceRelease();
            // У Rigidbody с интерполяцией одной записи в transform мало: физика может вернуть
            // старую позицию на следующем шаге. Телепортируем и тело, и transform.
            Rb.isKinematic = false;
            Rb.position = pos;
            Rb.rotation = Quaternion.identity;
            transform.SetPositionAndRotation(pos, Quaternion.identity);
            SetVelocity(Rb, Vector3.zero);
            Physics.SyncTransforms();
        }
    }

    public static class Rays
    {
        static readonly RaycastHit[] _hits = new RaycastHit[24];

        /// <summary>Первое попадание луча, игнорируя объекты внутри ignoreRoot.</summary>
        public static bool First(Vector3 origin, Vector3 dir, float dist, Transform ignoreRoot, out RaycastHit best, bool triggers = true)
        {
            int n = Physics.RaycastNonAlloc(origin, dir, _hits, dist, ~0, triggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore);
            best = default;
            float bd = float.MaxValue;
            bool found = false;
            for (int i = 0; i < n; i++)
            {
                var h = _hits[i];
                if (ignoreRoot != null && h.collider.transform.IsChildOf(ignoreRoot)) continue;
                if (h.collider.gameObject.layer == 2) continue; // невидимые стенки кухни (Ignore Raycast)
                if (h.distance < bd) { bd = h.distance; best = h; found = true; }
            }
            return found;
        }

        /// <summary>Есть ли препятствие (не триггер, не игрок, не ignore) между точками.</summary>
        public static bool Blocked(Vector3 from, Vector3 to, Transform ignore)
        {
            var d = to - from;
            float dist = d.magnitude;
            if (dist < 0.01f) return false;
            int n = Physics.RaycastNonAlloc(from, d / dist, _hits, dist, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var t = _hits[i].collider.transform;
                if (ignore != null && t.IsChildOf(ignore)) continue;
                if (PlayerRig.I != null && t.IsChildOf(PlayerRig.I.transform)) continue;
                if (t.GetComponentInParent<Monster>() != null) continue; // монстры не перекрывают обзор друг другу
                return true;
            }
            return false;
        }
    }
}
