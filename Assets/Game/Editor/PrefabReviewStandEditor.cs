using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Podz.EditorTools
{
    [CustomEditor(typeof(PrefabReviewStand))]
    public class PrefabReviewStandEditor : Editor
    {
        string _measurements;
        [MenuItem("Podz/Проверка/Создать лёгкую сцену одного префаба")]
        static void CreateFromMenu() => Run(() => CreateScene(Selection.activeObject as GameObject));
        static void Run(Action action)
        {
            try { action(); }
            catch (Exception ex) { Debug.LogException(ex); EditorUtility.DisplayDialog("Стенд: ошибка", ex.Message, "OK"); }
        }
        static Monster CheckPrefab(GameObject prefab)
        {
            if (prefab == null || !PrefabUtility.IsPartOfPrefabAsset(prefab) || !EditorUtility.IsPersistent(prefab))
                throw new InvalidOperationException("Выбери сохранённый prefab из Project, не объект сцены/recipe asset.");
            if (prefab.GetComponentsInChildren<GameBootstrap>(true).Length != 0 ||
                prefab.GetComponentsInChildren<DungeonManager>(true).Length != 0 ||
                prefab.GetComponentsInChildren<Kitchen>(true).Length != 0 ||
                prefab.GetComponentsInChildren<GuestManager>(true).Length != 0 ||
                prefab.GetComponentsInChildren<GameFlow>(true).Length != 0)
                throw new InvalidOperationException("В выбранном prefab есть запуск Main/кухни/этажей/профиля. Такой prefab стенд не размещает.");
            if (prefab.GetComponentsInChildren<Monster>(true).Length != 1)
                throw new InvalidOperationException("В prefab должен быть ровно один Monster на корне.");
            var monster = prefab.GetComponent<Monster>();
            bool supported = monster is Slime slime ? slime.HasSavedModel :
                monster != null && monster.HasSavedGeometry && (monster is Mushroom || monster is Mandrake || monster is Basilisk || monster is LivingArmor || monster is Mimic || monster is Dragon);
            if (!supported) throw new InvalidOperationException("Нужен сохранённый префаб одного из семи монстров, созданный инструментом Podz.");
            return monster;
        }
        static void CreateScene(GameObject selected)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Сцену создаём только вне Play.");
            if (selected != null && (!EditorUtility.IsPersistent(selected) || !PrefabUtility.IsPartOfPrefabAsset(selected))) selected = null;
            if (selected != null) CheckPrefab(selected);
            string path = EditorUtility.SaveFilePanelInProject("Новая лёгкая сцена (старые не перезаписываются)",
                "PrefabReview", "unity", "Сохрани новый вариант в Scenes. Старый стенд и ручные правки не перезаписываются.", "Assets/Game/Scenes");
            if (string.IsNullOrEmpty(path)) return;
            if (AssetDatabase.LoadMainAssetAtPath(path) != null || File.Exists(path))
                throw new InvalidOperationException("Этот путь уже занят. Выбери новое имя сцены.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("PrefabReviewStand"); var stand = root.AddComponent<PrefabReviewStand>();
            var anchor = new GameObject("ModelAnchor").transform; anchor.SetParent(root.transform, false); stand.ModelAnchor = anchor;
            var start = new GameObject("PlayerStart").transform; start.SetParent(root.transform, false);
            start.localPosition = new Vector3(0, 0, -3.5f); stand.PlayerStart = start;
            var cameraGo = new GameObject("ReviewCamera"); cameraGo.transform.SetParent(root.transform, false);
            cameraGo.transform.position = new Vector3(0, 1.8f, -3.5f); cameraGo.transform.LookAt(new Vector3(0, .3f, 0));
            var camera = cameraGo.AddComponent<Camera>(); camera.nearClipPlane = .03f; camera.farClipPlane = 30f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.12f, .13f, .15f); stand.ReviewCamera = camera;
            var lightGo = new GameObject("ReviewLight"); lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localRotation = Quaternion.Euler(45, -30, 0);
            var light = lightGo.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = .9f;
            light.shadows = LightShadows.None; light.lightmapBakeType = LightmapBakeType.Realtime;
            RenderSettings.skybox = null; RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.45f, .45f, .45f); RenderSettings.fog = false;
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube); ground.name = "ReviewGround";
            ground.transform.SetParent(root.transform, false); ground.transform.localPosition = new Vector3(0, -.1f, 0);
            ground.transform.localScale = new Vector3(12, .2f, 12); stand.Ground = ground.GetComponent<BoxCollider>();
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("Нет существующего URP/Lit: проверь исходные пакеты, не переустанавливай их автоматически.");
            var material = new Material(shader); material.SetColor("_BaseColor", new Color(.26f, .28f, .30f));
            ground.GetComponent<Renderer>().sharedMaterial = material;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string folder = AssetDatabase.GenerateUniqueAssetPath(parent + "/" + Path.GetFileNameWithoutExtension(path) + "_Resources");
            if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, Path.GetFileName(folder)))) throw new IOException("Не создана папка стенда.");
            try
            {
                // Only the new stand's floor is baked; the selected monster's resources are not rewritten.
                SavedModelResources.SaveMeshes(ground, folder); SavedModelResources.SaveMaterials(ground, folder, null);
                stand.SelectedPrefab = selected;
                if (selected != null) PlaceOne(stand);
                AssetDatabase.SaveAssets();
                if (!EditorSceneManager.SaveScene(scene, path)) throw new IOException("Unity не сохранила сцену: " + path);
                Selection.activeGameObject = root;
                Debug.Log("[PrefabReview] Сохранена отдельная сцена: " + path +
                    ". Изменение Inspector не генерирует объекты. Play сам по себе не создаёт rig/сервисы.", stand);
            }
            finally { Object.DestroyImmediate(material); }
        }
        static void PlaceOne(PrefabReviewStand stand)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Размещение только вне Play.");
            CheckPrefab(stand.SelectedPrefab);
            if (stand.ModelAnchor == null || stand.ModelAnchor.gameObject.scene != stand.gameObject.scene)
                throw new InvalidOperationException("Нужен ModelAnchor в этой сцене.");
            if (stand.PlacedMonster != null || stand.ModelAnchor.childCount != 0 ||
                Object.FindObjectsByType<Monster>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length != 0)
                throw new InvalidOperationException("Место уже занято. Ручной объект не удаляется: создай НОВУЮ сцену/вариант.");
            GameObject instance = null;
            try
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(stand.SelectedPrefab, stand.ModelAnchor);
                Undo.RegisterCreatedObjectUndo(instance, "Разместить один prefab");
                // Make a snapshot so saving edits to the source prefab does not rebuild this stand's hierarchy.
                PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.UserAction);
                instance.transform.localPosition = Vector3.zero; instance.transform.localRotation = Quaternion.identity;
                var monster = instance.GetComponent<Monster>(); SavedModelResources.Validate(instance);
                Undo.RecordObject(stand, "Ссылки стенда"); stand.RecordPlacedForEditor(monster, stand.SelectedPrefab);
                EditorUtility.SetDirty(stand); EditorSceneManager.MarkSceneDirty(stand.gameObject.scene);
            }
            catch { if (instance != null) Undo.DestroyObjectImmediate(instance); throw; }
        }
        public override void OnInspectorGUI()
        {
            var stand = (PrefabReviewStand)target;
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("Изменение полей НЕ создаёт и НЕ обновляет модель. На стенде снимок выбранного prefab: исходный asset не меняется, " +
                "его правки иерархии не применяются сюда автоматически. Обновление — НОВЫЙ вариант сцены, без удаления ручных изменений старого. " +
                "Материалы/меши остаются ссылками на сохранённые assets.", MessageType.Info);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Размещённый источник", stand.PlacedSource, typeof(GameObject), false);
                EditorGUILayout.ObjectField("Экземпляр на стенде", stand.PlacedMonster, typeof(Monster), true);
            }
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("Разместить выбранный prefab (только пустое место)")) Run(() => PlaceOne(stand));
                if (GUILayout.Button("Обновление: создать НОВЫЙ вариант сцены")) Run(() => CreateScene(stand.SelectedPrefab));
                if (GUILayout.Button("Сохранить эту сцену без генерации")) Run(() =>
                {
                    if (string.IsNullOrEmpty(stand.gameObject.scene.path)) throw new InvalidOperationException("Сначала сохрани сцену в Unity.");
                    if (!EditorSceneManager.SaveScene(stand.gameObject.scene)) throw new IOException("Сцена не сохранена.");
                });
            }
            if (GUILayout.Button("Прочитать размеры / зоны / зависимости")) Run(() => Measure(stand));
            if (!string.IsNullOrEmpty(_measurements))
                EditorGUILayout.SelectableLabel(_measurements, EditorStyles.textArea, GUILayout.MinHeight(220), GUILayout.MaxHeight(420));
            EditorGUILayout.HelpBox("Размер модели = меши × цепочка Transform. Размер BoxCollider = Size × мировой Scale; Center тоже преобразуется Transform. " +
                "HitZone использует тот же collider, а MonsterGrabPoint находится на его узле: отдельный визуальный масштаб НЕ меняет их. " +
                "Мандрагора: Leaves — видимый пучок + collider + реальный захват; Root включается прежним срезом. " +
                "Сохраняй согласованность размеров/зон вручную или исходным recipe; автоматической подгонки здесь нет.", MessageType.Info);
            EditorGUILayout.HelpBox("Для поведения: настоящие PlayerRig/HandController/PlayerHealth, DTime, Sfx, Hints/Banner/Fader, Monster.Init + Registry/Cache/AttackGate. " +
                "Кнопка ниже создаёт только эти сервисы в Play. НЕТ DungeonManager, кухни, GameFlow, профиля, выдачи инструментов и имитации хвата. " +
                "Полная охота/лут/прогрессия этим не подтверждаются. XR использует прежнюю интеграцию и требует работающего подключения устройства.", MessageType.Warning);
            using (new EditorGUI.DisabledScope(!Application.isPlaying || stand.Attempted))
                if (GUILayout.Button("Play: запустить настоящее взаимодействие ОДИН РАЗ")) Run(stand.StartInteraction);
            if (stand.Attempted) EditorGUILayout.HelpBox(stand.Running ?
                "Сессия запущена. Действуй обычным вводом; повтор/сброс — остановить Play. Не сохраняй runtime-изменения как authored-ресурсы." :
                "Была попытка запуска. При ошибке сохрани первую строку Console и выйди из Play; автоматических повторов нет.", MessageType.Info);
        }
        void Measure(PrefabReviewStand stand)
        {
            var monster = stand.PlacedMonster;
            if (monster == null) throw new InvalidOperationException("Сначала размести один prefab.");
            var b = new StringBuilder();
            b.AppendLine("Размеры, метры; чтение только по кнопке.");
            b.AppendLine("Root LocalScale: " + monster.transform.localScale.ToString("F3") + "; WorldScale: " + monster.transform.lossyScale.ToString("F3"));
            bool found = false; Bounds visual = default;
            foreach (var renderer in monster.GetComponentsInChildren<Renderer>(true))
                if (renderer.enabled && renderer.gameObject.activeInHierarchy && renderer.GetComponent<TextMesh>() == null)
                { if (!found) { visual = renderer.bounds; found = true; } else visual.Encapsulate(renderer.bounds); }
            b.AppendLine("Видимый AABB: " + (found ? visual.size.ToString("F3") : "нет активных Renderer"));
            foreach (var collider in monster.GetComponentsInChildren<Collider>(true))
            {
                var zone = collider.GetComponent<HitZone>(); var grab = collider.GetComponent<MonsterGrabPoint>();
                b.Append(collider.name).Append(": ").Append(collider.GetType().Name).Append("; enabled=").Append(collider.enabled);
                if (collider is BoxCollider box)
                {
                    var m = box.transform.localToWorldMatrix;
                    var size = new Vector3(m.MultiplyVector(Vector3.right * box.size.x).magnitude,
                        m.MultiplyVector(Vector3.up * box.size.y).magnitude, m.MultiplyVector(Vector3.forward * box.size.z).magnitude);
                    b.Append("; Size local=").Append(box.size.ToString("F3")).Append(" world axes=").Append(size.ToString("F3"));
                    b.Append("; Center world=").Append(box.transform.TransformPoint(box.center).ToString("F3"));
                }
                else b.Append("; world AABB=").Append(collider.bounds.size.ToString("F3"));
                if (zone != null) b.Append("; Zone=").Append(zone.Zone).Append(" OwnerOK=").Append(zone.Owner == monster);
                if (grab != null) b.Append("; Grab Active=").Append(grab.Active).Append(" Held=").Append(grab.Held).Append(" OwnerOK=").Append(grab.Owner == monster);
                b.AppendLine();
            }
            if (monster is Mandrake) b.AppendLine("Leaf pose: вращение вершин, НЕ collider. Hold window 3s, pull up >0.25m / release travel >0.22m — прежние правила.");
            if (monster is Basilisk) b.AppendLine("Василиск: сохраняй исходный масштаб1; прежние соединители шеи используют мировые расстояния.");
            b.AppendLine("Нет автоматической коррекции/синхронизации размеров; чтение не запускает генератор.");
            _measurements = b.ToString();
        }
    }
}
