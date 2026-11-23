using UnityEngine;

namespace Podz
{
    /// <summary>Запускает мировые системы и подключает сохранённые игровые ресурсы.</summary>
    [DefaultExecutionOrder(-1000)]
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] Transform tavernArchitecture;
        [SerializeField] SavedDungeonAssets savedWorld;
        [SerializeField] SavedMonsterPrefabs savedMonsters = new SavedMonsterPrefabs();

        void Awake()
        {
            DTime.Paused = false;
            Application.targetFrameRate = 90;
            QualitySettings.vSyncCount = 0;
            SavedDungeonAssets.Active = savedWorld;
            Recipes.ApplyTuning(savedWorld?.RecipeParameters);
            var kitchen = Kitchen.Create(tavernArchitecture);
            var rig = PlayerRig.Create(kitchen.Spawn, 0f);
            var hints = new GameObject("Hints").AddComponent<Hints>();
            Hints.I = hints;
            hints.AttachTo(rig.Cam.transform);
            var banner = new GameObject("Banner").AddComponent<Banner>();
            Banner.I = banner;
            banner.AttachTo(rig.Cam.transform);
            var fader = new GameObject("Fader").AddComponent<Fader>();
            Fader.I = fader;
            fader.AttachTo(rig.Cam.transform);
            var systems = new GameObject("Systems");
            systems.AddComponent<Sfx>(); // звуки, сгенерированные кодом
            systems.AddComponent<DungeonManager>().SavedMonsters = savedMonsters;
            systems.AddComponent<GuestManager>();
            systems.AddComponent<GameFlow>();
            systems.AddComponent<DebugOverlay>().AttachTo(rig.Cam.transform);
            Guide.Create(rig.Cam.transform);
            HuntCoach.Create(rig.Cam.transform); // пункт 3: шаги охоты крупно по центру + контур цели
            KitchenCoach.Create(rig.Cam.transform); // баг-лист, пункт 6: тренер на кухне
            new GameObject("HintsToggle").AddComponent<HintsToggle>().AttachTo(rig.Cam.transform);
            BackpackView.Create(rig); // представление прежнего Inventory.Backpack
            HudPanel.Create(rig.Cam.transform); // слева сверху: HP, монеты, место
            Hotbar.Create(rig.Cam.transform); // справа сверху: инструменты
            new GameObject("ClickToPlay").AddComponent<ClickToPlay>().AttachTo(rig.Cam.transform); // «Щёлкни по игре, чтобы управлять»
            WorldLighting.Apply(WorldLighting.Tavern);
            GameFlow.I.Init();
            GuestManager.I.Init();
#if UNITY_EDITOR
            // VR без шлема: XR Device Simulator из XR Interaction Toolkit (меню Podz → 5)
            if (UnityEditor.EditorPrefs.GetBool("PodzVrSimulator", false))
            {
                var sim = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Samples/XR Interaction Toolkit/3.0.11/XR Device Simulator/XR Device Simulator.prefab");
                if (sim != null) Instantiate(sim).name = "XR Device Simulator";
            }
#endif
        }

        void Update() { DTime.Tick(); }
    }
}
