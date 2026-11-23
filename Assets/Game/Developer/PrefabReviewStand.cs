using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Podz
{
    /// <summary>Opt-in one-prefab review harness. No Awake/Start generation and no substitute monster behavior.</summary>
    public class PrefabReviewStand : MonoBehaviour
    {
        [Header("1. Выбор — сам по себе ничего не создаёт")]
        [Tooltip("A saved prefab of one of seven monsters. Dragon is display-only here: real lair services are not imitated.")]
        public GameObject SelectedPrefab;
        [Header("2. Ссылки сцены (сохраняются кнопкой на ПК)")]
        public Transform ModelAnchor;
        public Transform PlayerStart;
        public Camera ReviewCamera;
        [Tooltip("Saved ordinary ground collider; not a procedural dungeon floor.")]
        public BoxCollider Ground;
        [SerializeField, HideInInspector] Monster _placedMonster;
        [SerializeField, HideInInspector] GameObject _placedSource;
        bool _attempted, _running;
        GameObject _services;
        public Monster PlacedMonster => _placedMonster;
        public GameObject PlacedSource => _placedSource;
        public bool Attempted => _attempted;
        public bool Running => _running;

#if UNITY_EDITOR
        public void RecordPlacedForEditor(Monster monster, GameObject source)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Размещение — только вне Play.");
            _placedMonster = monster; _placedSource = source;
        }
#endif

        static bool Exists<T>() where T : UnityEngine.Object =>
            UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length > 0;

        /// <summary>Called only by the explicit Inspector button while playing; real Init, rig and hands.</summary>
        public void StartInteraction()
        {
            if (!Application.isPlaying || _attempted) throw new InvalidOperationException("Нужен Play и ещё не запущенная сессия. Повтор — после выхода из Play.");
            if (SceneManager.sceneCount != 1 || gameObject.scene != SceneManager.GetActiveScene() ||
                Exists<GameBootstrap>() || Exists<Kitchen>() || Exists<DungeonManager>() || Exists<GuestManager>() || Exists<GameFlow>())
                throw new InvalidOperationException("Оставь открытой только сцену стенда: Main/кухня/этажи/профиль в ней недопустимы.");
            if (Exists<PlayerRig>() || Exists<Sfx>() || Exists<Hints>() || Exists<Banner>() || Exists<Fader>())
                throw new InvalidOperationException("Уже есть игровые сервисы. Не создаём второй rig/звук/UI; выйди из Play и используй чистую сцену стенда.");
            if (_placedMonster == null || _placedMonster.Initialized || _placedMonster.Dead ||
                ModelAnchor == null || PlayerStart == null || Ground == null || ReviewCamera == null ||
                !_placedMonster.transform.IsChildOf(ModelAnchor))
                throw new InvalidOperationException("Сначала размести один поддержанный prefab и проверь ссылки стенда.");
            var monsters = UnityEngine.Object.FindObjectsByType<Monster>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (monsters.Length != 1) throw new InvalidOperationException("В сцене должен быть ровно один Monster, включая неактивные.");
            MonsterKind kind;
            if (_placedMonster is Slime slime && slime.HasSavedModel) kind = MonsterKind.Slime;
            else if (_placedMonster is Mushroom && _placedMonster.HasSavedGeometry) kind = MonsterKind.Mushroom;
            else if (_placedMonster is Mandrake && _placedMonster.HasSavedGeometry) kind = MonsterKind.Mandrake;
            else if (_placedMonster is Basilisk && _placedMonster.HasSavedGeometry) kind = MonsterKind.Basilisk;
            else if (_placedMonster is LivingArmor && _placedMonster.HasSavedGeometry) kind = MonsterKind.Armor;
            else if (_placedMonster is Mimic && _placedMonster.HasSavedGeometry) kind = MonsterKind.Mimic;
            else if (_placedMonster is Dragon) throw new InvalidOperationException("Дракон: стенд разрешает осмотр/ссылки, но не бой. Нужны настоящее логово, DungeonManager.Origin, FireCover, точки полёта/посадки и переход после победы. Не имитируем их на маленькой площадке.");
            else throw new InvalidOperationException("Нужен сохранённый префаб монстра, не процедурная заготовка.");
            if (_placedMonster is Basilisk && Vector3.Distance(_placedMonster.transform.lossyScale, Vector3.one) > .001f)
                throw new InvalidOperationException("Василиск: нужен исходный мировой масштаб1, включая ModelAnchor/родителей.");
            // No tools, purchases, progress, profile loading, floor manager or AI imitation.
            _attempted = true;
            _services = new GameObject("ReviewSession_RealServices");
            _services.transform.SetParent(transform, false);
            try
            {
                DTime.Paused = false;
                _services.AddComponent<Sfx>();
                var rig = PlayerRig.Create(PlayerStart.position, PlayerStart.eulerAngles.y);
                rig.transform.SetParent(_services.transform, true);
                // Create() leaves hands unparented until its first Update; own them now for safe failure cleanup.
                rig.Left.transform.SetParent(rig.transform, true);
                rig.Right.transform.SetParent(rig.transform, true);
                var hints = new GameObject("Hints").AddComponent<Hints>(); Hints.I = hints; hints.AttachTo(rig.Cam.transform);
                var banner = new GameObject("Banner").AddComponent<Banner>(); Banner.I = banner; banner.AttachTo(rig.Cam.transform);
                var fader = new GameObject("Fader").AddComponent<Fader>(); Fader.I = fader; fader.AttachTo(rig.Cam.transform);
                // Ground bounds support the old wandering code; this is not a generated floor.
                _placedMonster.Init(kind, null, Ground.bounds);
                ReviewCamera.enabled = false;
                _running = true;
                Debug.Log("[PrefabReview] Real Monster.Init + PlayerRig/HandController + PlayerHealth + DTime + Sfx + Hints/Banner/Fader. " +
                    "No GameFlow/DungeonManager/profile/tools. Input is real; full hunt/loot progression is NOT tested by this harness.", this);
            }
            catch
            {
                if (_placedMonster != null) _placedMonster.enabled = false;
                if (_services != null) Destroy(_services);
                if (ReviewCamera != null) ReviewCamera.enabled = true;
                Debug.LogError("[PrefabReview] Запуск не завершён. Выйди из Play; повторных скрытых попыток нет.", this);
                throw;
            }
        }

        void Update() { if (_running) DTime.Tick(); }
        // No reset/respawn button: stop Play to restore the saved authored scene exactly.
    }
}
