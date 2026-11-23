using UnityEngine;
using UnityEngine.Rendering;

namespace Podz
{
    public struct LightPreset
    {
        public Color Ambient, Fog, Background;
        public float FogStart, FogEnd;
    }

    public static class WorldLighting
    {
        public static readonly LightPreset Tavern = new LightPreset
        {
            Ambient = new Color(0.42f, 0.33f, 0.24f), Fog = new Color(0.12f, 0.08f, 0.05f), Background = new Color(0.08f, 0.05f, 0.03f), FogStart = 15, FogEnd = 40
        };

        public static void Apply(LightPreset p)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = p.Ambient;
            var sh = new SphericalHarmonicsL2();
            sh.AddAmbientLight(p.Ambient);
            RenderSettings.ambientProbe = sh;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = p.Fog;
            RenderSettings.fogStartDistance = p.FogStart;
            RenderSettings.fogEndDistance = p.FogEnd;
            if (PlayerRig.I != null && PlayerRig.I.Cam != null)
            {
                PlayerRig.I.Cam.clearFlags = CameraClearFlags.SolidColor;
                PlayerRig.I.Cam.backgroundColor = p.Background;
            }
        }
    }
}
