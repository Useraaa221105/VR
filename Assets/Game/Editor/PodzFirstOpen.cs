using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Podz.EditorTools
{
    /// <summary>Сохраняет явно открытую сцену. Main предлагается только для пустого стартового редактора, без автогенерации.</summary>
    [InitializeOnLoad]
    public static class PodzFirstOpen
    {
        const string ScenePath = "Assets/Game/Scenes/Main.unity";

        static PodzFirstOpen()
        {
            if (Application.isBatchMode) return;
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                var current = EditorSceneManager.GetActiveScene();
                // Respect any explicitly saved scene (including the lightweight prefab review).
                // No rebuild/open of Main on top of the user's authored scene after restart.
                if (!string.IsNullOrEmpty(current.path)) return;
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                { Debug.LogWarning("Main не найдена. Создание сцены — только вручную через меню Podz; автогенерация отключена."); return; }
                if (SessionState.GetBool("PodzOpenedOnce", false)) return;
                SessionState.SetBool("PodzOpenedOnce", true);
                var active = EditorSceneManager.GetActiveScene();
                if (active.path != ScenePath && !active.isDirty) EditorSceneManager.OpenScene(ScenePath);
            };
        }
    }
}
