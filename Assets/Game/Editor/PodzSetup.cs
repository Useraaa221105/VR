using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Podz.EditorTools
{
    /// <summary>Меню Podz → «1. Создать сцену и ассеты»: материалы, Resources/PodzGameAssets, сцена Main с GameBootstrap, Build Settings.</summary>
    public static class PodzSetup
    {
        const string Root = "Assets/Game";
        const string ResourcesPath = "Assets/PodzGame/Resources";
        const string ScenePath = Root + "/Scenes/Main.unity";

        [MenuItem("Podz/1. Создать сцену и ассеты")]
        public static void Setup()
        {
            Directory.CreateDirectory(ResourcesPath);
            Directory.CreateDirectory(Root + "/Shared/Materials");
            Directory.CreateDirectory(Root + "/Scenes");
            AssetDatabase.Refresh();
            PodzXRSetup.Setup();

            var lit = CreateMaterial(Root + "/Shared/Materials/PodzLit.mat", "Universal Render Pipeline/Lit", "Standard", true);
            var overlay = CreateMaterial(Root + "/Shared/Materials/PodzOverlay.mat", "Sprites/Default", "Unlit/Transparent", false);

            var assetPath = ResourcesPath + "/PodzGameAssets.asset";
            var ga = AssetDatabase.LoadAssetAtPath<GameAssets>(assetPath);
            if (ga == null)
            {
                ga = ScriptableObject.CreateInstance<GameAssets>();
                AssetDatabase.CreateAsset(ga, assetPath);
            }
            ga.lit = lit;
            ga.overlay = overlay;
            EditorUtility.SetDirty(ga);
            AssetDatabase.SaveAssets();

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);

            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            foreach (var s in EditorBuildSettings.scenes) if (s.path != ScenePath) scenes.Add(s);
            EditorBuildSettings.scenes = scenes.ToArray();

            Debug.Log("[Podz] Готово: сцена " + ScenePath + " создана и добавлена в Build Settings. Нажмите Play.");
            if (!Application.isBatchMode) EditorUtility.DisplayDialog("Подземелье вкусностей", "Сцена и ассеты созданы.\nОткройте Assets/Game/Scenes/Main.unity и нажмите Play.\nB — бестиарий, I — рюкзак, F2 — настройки подсказок подземелья.", "OK");
        }

        static Material CreateMaterial(string path, string shader, string fallback, bool emission)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var sh = Shader.Find(shader) ?? Shader.Find(fallback);
            if (m == null)
            {
                m = new Material(sh);
                AssetDatabase.CreateAsset(m, path);
            }
            else m.shader = sh;
            if (emission)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Color.black);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.2f);
            }
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
