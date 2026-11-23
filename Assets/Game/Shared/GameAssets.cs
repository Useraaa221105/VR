using UnityEngine;

namespace Podz
{
    /// <summary>Общие материалы. Создаётся меню Podz/1 в папке Resources; при отсутствии — фолбэк в рантайме.</summary>
    [CreateAssetMenu(menuName = "Podz/Game Assets")]
    public class GameAssets : ScriptableObject
    {
        public Material lit;      // URP Lit с включённой эмиссией (цвет задаём через MaterialPropertyBlock)
        public Material overlay;  // прозрачный (Sprites/Default) — затемнение, оверлеи статусов, подложки текста

        static GameAssets _i;

        public static GameAssets I
        {
            get
            {
                if (_i == null)
                {
                    _i = Resources.Load<GameAssets>("PodzGameAssets");
                    if (_i == null) _i = CreateInstance<GameAssets>();
                    _i.EnsureFallbacks();
                }
                return _i;
            }
        }

        void EnsureFallbacks()
        {
            if (lit == null)
            {
                lit = new Material(FindShader("Universal Render Pipeline/Lit", "Standard"));
                lit.EnableKeyword("_EMISSION");
            }
            if (overlay == null) overlay = new Material(FindShader("Sprites/Default", "Unlit/Transparent"));
        }

        static Shader FindShader(params string[] names)
        {
            foreach (var n in names)
            {
                var s = Shader.Find(n);
                if (s != null) return s;
            }
            return Shader.Find("Hidden/InternalErrorShader");
        }
    }
}
