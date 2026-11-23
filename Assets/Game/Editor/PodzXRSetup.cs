using System.IO;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

namespace Podz.EditorTools
{
    /// <summary>
    /// Настройка VR одной кнопкой: XR Plug-in Management → OpenXR (ПК и Android), профиль контроллеров Oculus Touch,
    /// группа Meta Quest для Android. Меню: Podz → 4. Настроить VR.
    /// </summary>
    public static class PodzXRSetup
    {
        const string SimKey = "PodzVrSimulator";

        [MenuItem("Podz/4. Настроить VR (OpenXR + Meta Quest 3)")]
        public static void Setup()
        {
            if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.settingsKey, out XRGeneralSettingsPerBuildTarget per) || per == null)
            {
                Directory.CreateDirectory("Assets/XR");
                per = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(per, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.settingsKey, per, true);
            }
            foreach (var g in new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android })
            {
                if (!per.HasSettingsForBuildTarget(g)) per.CreateDefaultSettingsForBuildTarget(g);
                if (!per.HasManagerSettingsForBuildTarget(g)) per.CreateDefaultManagerSettingsForBuildTarget(g);
                var gs = per.SettingsForBuildTarget(g);
                // ASSUMPTION: на ПК шлем (Quest Link) запускается только по галочке в меню, чтобы без шлема не было ошибок OpenXR
                gs.InitManagerOnStart = g == BuildTargetGroup.Android || EditorPrefs.GetBool("PodzVrLink", false);
                XRPackageMetadataStore.AssignLoader(gs.Manager, typeof(OpenXRLoader).FullName, g);
                EditorUtility.SetDirty(gs);
                UnityEditor.XR.OpenXR.Features.FeatureHelpers.RefreshFeatures(g);
                var ox = OpenXRSettings.GetSettingsForBuildTargetGroup(g);
                if (ox == null) continue;
                foreach (var f in ox.GetFeatures<OculusTouchControllerProfile>()) f.enabled = true;
                if (g == BuildTargetGroup.Android) foreach (var f in ox.GetFeatures<MetaQuestFeature>()) f.enabled = true;
                EditorUtility.SetDirty(ox);
            }
            EditorUtility.SetDirty(per);
            AssetDatabase.SaveAssets();
            Debug.Log("[Podz] VR настроен: OpenXR + Oculus Touch (ПК), OpenXR + Meta Quest (Android)");
        }

        [MenuItem("Podz/5. VR без шлема: XR Device Simulator (вкл-выкл)")]
        static void ToggleSim()
        {
            bool v = !EditorPrefs.GetBool(SimKey, false);
            EditorPrefs.SetBool(SimKey, v);
            Debug.Log(v ? "[Podz] При Play запустится XR Device Simulator (VR-режим без шлема)" : "[Podz] XR Device Simulator выключен — обычный режим ПК");
        }

        [MenuItem("Podz/5. VR без шлема: XR Device Simulator (вкл-выкл)", true)]
        static bool ToggleSimCheck() { Menu.SetChecked("Podz/5. VR без шлема: XR Device Simulator (вкл-выкл)", EditorPrefs.GetBool(SimKey, false)); return true; }

        [MenuItem("Podz/6. VR через Quest Link на ПК (вкл-выкл)")]
        static void ToggleLink()
        {
            bool v = !EditorPrefs.GetBool("PodzVrLink", false);
            EditorPrefs.SetBool("PodzVrLink", v);
            Setup();
        }

        [MenuItem("Podz/6. VR через Quest Link на ПК (вкл-выкл)", true)]
        static bool ToggleLinkCheck() { Menu.SetChecked("Podz/6. VR через Quest Link на ПК (вкл-выкл)", EditorPrefs.GetBool("PodzVrLink", false)); return true; }
    }
}
