using System.Collections.Generic;
using UnityEngine;
namespace Podz
{
    /// <summary>Один физический объект/стек. В хранилище поступает исключительно при Collect; повторный сбор блокируется.</summary>
    public class DungeonLoot : GrabItem
    {
        public static readonly List<DungeonLoot> All = new List<DungeonLoot>();
        [field: SerializeField] public ItemId Id { get; private set; }
        [field: SerializeField] public int Amount { get; private set; }
        bool _collecting;
        [SerializeField, HideInInspector] bool _savedPhysics;
#if UNITY_EDITOR
        public void PrepareSavedPhysics() { _savedPhysics=true; }
#endif
        void Awake() {
            if(!_savedPhysics)return;
            Rb=GetComponent<Rigidbody>();
            if(Rb==null)throw new System.InvalidOperationException("DungeonLoot prefab lost its saved Rigidbody.");
            if(!All.Contains(this))All.Add(this);
        }
        public static DungeonLoot Create(ItemId id, int amount, Transform parent, Vector3 pos)
        {
            if (DungeonManager.I != null && DungeonManager.I.InDungeon && DungeonManager.I.Level == 1 && parent == DungeonManager.I.WorldRoot) pos = UpperCaves.SafeDrop(pos);
            if (DungeonManager.I != null && DungeonManager.I.InDungeon && DungeonManager.I.Level == 2 && parent == DungeonManager.I.WorldRoot) pos = OvergrownRuins.SafeDrop(pos);
            var prefab = SavedDungeonAssets.Active?.FindLoot(id)?.LastPrefab;
            if (prefab != null && prefab.GetComponentInChildren<DungeonLoot>() != null)
            {
                var wrapper = Instantiate(prefab, pos, Quaternion.identity, parent);
                var saved = wrapper.GetComponentInChildren<DungeonLoot>();
                saved.transform.SetParent(parent, true);
                Destroy(wrapper);
                saved.Amount = amount;
                saved.name = "Loot_" + id;
                return saved;
            }
            var go = new GameObject("Loot_" + id); go.transform.SetParent(parent, true); go.transform.position = pos;
            var loot = go.AddComponent<DungeonLoot>(); loot.Id = id; loot.Amount = amount;
            FoodModels.Build(id,go.transform,.22f);
            var col = go.AddComponent<BoxCollider>(); col.size = new Vector3(.3f,.25f,.3f); col.center = new Vector3(0,.1f,0);
            loot.SetupPhysics(); All.Add(loot); return loot;
        }
        public override bool CanGrab(HandController h) => !_collecting && Amount > 0 && Tools.InDungeon && base.CanGrab(h);
        public bool Collect(HandController hand = null)
        {
            if (_collecting || Amount <= 0 || !Tools.InDungeon) return false;
            _collecting = true;
            int added = Inventory.Backpack.Add(Id,Amount);
            if (added <= 0) { _collecting = false; Banner.Show(false,"Рюкзак полон",Items.Name(Id),""); return false; }
            Amount -= added; GameEvents.RaiseLoot(Id,added);
            Banner.Show(true,"Подобрано",Items.Name(Id) + " ×" + added,"");
            InteractionFeedback.Emit(transform.position, hand, true);
            if (Amount == 0)
            {
                if (Holder != null) Holder.ClearIfHolding(this);
                foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
                All.Remove(this); Destroy(gameObject);
            }
            else _collecting = false;
            return true;
        }
        void Update()
        {
            if (!IsHeld || BackpackView.I == null || !BackpackView.I.IsHeld || BackpackView.I.Holder == Holder) return;
            if (Vector3.Distance(transform.position,BackpackView.I.Opening) < .24f) Collect(Holder);
        }
        void OnDestroy() { All.Remove(this); if (Holder != null) Holder.ClearIfHolding(this); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset() { All.Clear(); }
    }
}
