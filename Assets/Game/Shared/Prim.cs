using UnityEngine;

namespace Podz
{
    /// <summary>Greybox-конструктор: кубы, сферы, цилиндры, текст. Цвет — через MaterialPropertyBlock (без копий материалов => без утечек).</summary>
    public static class Prim
    {
        static MaterialPropertyBlock _mpb;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        static Font _font;

        public static GameObject Shape(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, Color color, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (!collider)
            {
                var c = go.GetComponent<Collider>();
                if (c != null) Object.DestroyImmediate(c);
            }
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = GameAssets.I.lit;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            SetColor(r, color);
            return go;
        }

        /// <summary>Деталь в МЕТРАХ под (возможно масштабированным) родителем: компенсирует масштаб родителя. Без коллайдера.</summary>
        public static GameObject Part(PrimitiveType type, string name, Transform parent, Vector3 posMeters, Vector3 sizeMeters, Color color, Vector3? euler = null)
        {
            var ls = parent != null ? parent.lossyScale : Vector3.one;
            Vector3 Div(Vector3 a) => new Vector3(a.x / Mathf.Max(1e-4f, Mathf.Abs(ls.x)), a.y / Mathf.Max(1e-4f, Mathf.Abs(ls.y)), a.z / Mathf.Max(1e-4f, Mathf.Abs(ls.z)));
            if (type == PrimitiveType.Cylinder || type == PrimitiveType.Capsule) sizeMeters.y *= 0.5f; // примитив высотой 2 м
            if (!euler.HasValue)
            {
                var go = Shape(type, name, parent, Div(posMeters), Div(sizeMeters), color, false);
                return go;
            }
            // с поворотом: промежуточный узел снимает масштаб родителя, чтобы поворот не «перекашивал» деталь
            var holder = new GameObject(name + "_Holder").transform;
            holder.SetParent(parent, false);
            holder.localPosition = Div(posMeters);
            holder.localScale = Div(Vector3.one);
            var g = Shape(type, name, holder, Vector3.zero, sizeMeters, color, false);
            g.transform.localRotation = Quaternion.Euler(euler.Value);
            return g;
        }

        public static GameObject Sphere(string name, Transform parent, Vector3 posMeters, Vector3 sizeMeters, Color color) => Part(PrimitiveType.Sphere, name, parent, posMeters, sizeMeters, color);
        public static GameObject Cyl(string name, Transform parent, Vector3 posMeters, Vector3 sizeMeters, Color color, Vector3? euler = null) => Part(PrimitiveType.Cylinder, name, parent, posMeters, sizeMeters, color, euler);

        /// <summary>Светящаяся деталь (глаза, кристаллы, лава).</summary>
        public static GameObject Glow(GameObject go, Color emission) { SetEmission(go.GetComponent<Renderer>(), emission); return go; }

        public static GameObject Cube(string name, Transform parent, Vector3 localPos, Vector3 scale, Color color, bool collider = true)
            => Shape(PrimitiveType.Cube, name, parent, localPos, scale, color, collider);

        public static void SetColor(Renderer r, Color color) => SetColor(r, color, Color.black);

        public static void SetColor(Renderer r, Color color, Color emission)
        {
            if (r == null) return;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorId, color);
            _mpb.SetColor(ColorId, color);
            _mpb.SetColor(EmissionId, emission);
            r.SetPropertyBlock(_mpb);
        }

        public static void SetEmission(Renderer r, Color emission)
        {
            if (r == null) return;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(EmissionId, emission);
            r.SetPropertyBlock(_mpb);
        }

        public static void SetOverlayColor(Renderer r, Color color)
        {
            if (r == null) return;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(ColorId, color);
            r.SetPropertyBlock(_mpb);
        }

        /// <summary>Прозрачный квад (оверлей / подложка).</summary>
        public static Renderer Quad(string name, Transform parent, Vector3 localPos, Vector2 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = GameAssets.I.overlay;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            SetOverlayColor(r, color);
            return r;
        }

        static Material _hud;
        /// <summary>Материал HUD поверх мира (ZTest Always). Если шейдер недоступен — обычный оверлей.</summary>
        public static Material HudMaterial
        {
            get
            {
                if (_hud == null)
                {
                    var sh = Resources.Load<Shader>("PodzHudOverlay");
                    _hud = sh != null && sh.isSupported ? new Material(sh) { name = "HudOverlay" } : new Material(GameAssets.I.overlay);
                    _hud.renderQueue = 3500; // после всего мира (3000): таблички и подписи в мире не рисуются поверх подложек HUD
                }
                return _hud;
            }
        }
        /// <summary>Сделать квад/диск частью HUD: рисуется поверх предметов в руках и стен.</summary>
        public static Renderer Hud(Renderer r) { var c = r.sharedMaterial; r.sharedMaterial = HudMaterial; return r; }

        /// <summary>
        /// Пункт 6: экран в мире (карточка монстра, ледник) рисуется ПОВЕРХ всего — стены, монстры, инструменты в руке его не перекрывают.
        /// Все детали получают материал HUD (ZTest Always) с тем же цветом; порядок: дальние по z — раньше, текст — последним.
        /// </summary>
        public const int OnTopOrder = 3000;
        public static int OnTopApplied;
        public static void OnTop(Transform root)
        {
            if (root == null) return;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                if (r.GetComponent<TextMesh>() != null)
                {
                    r.sharedMaterial = HudTextMaterial;
                    r.sortingOrder = OnTopOrder + 100 + Mathf.Clamp(r.sortingOrder, 0, 99);
                    continue;
                }
                if (r.sortingOrder >= OnTopOrder) continue;
                Color c = Color.white;
                r.GetPropertyBlock(_mpb);
                if (!_mpb.isEmpty) { c = _mpb.GetColor(ColorId); if (c.a <= 0f && c.r <= 0f && c.g <= 0f && c.b <= 0f) c = _mpb.GetColor(BaseColorId); }
                else if (r.sharedMaterial != null && r.sharedMaterial.HasProperty(ColorId)) c = r.sharedMaterial.color;
                if (c.a <= 0f) c.a = 1f;
                float z = root.InverseTransformPoint(r.bounds.center).z;
                r.sharedMaterial = HudMaterial;
                SetOverlayColor(r, c);
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.sortingOrder = OnTopOrder + Mathf.Clamp(Mathf.RoundToInt(60f - z * 1000f), 0, 99);
            }
            OnTopApplied++;
        }

        /// <summary>Пункт 6: уменьшить текст, если он шире maxW — строки не наезжают друг на друга.</summary>
        public static void FitWidth(TextMesh t, float maxW)
        {
            if (t == null || maxW <= 0) return;
            float w = Podz.HudPanel.TextWidth(t);
            if (w > maxW) t.characterSize *= maxW / w;
        }

        static Material _hudText;
        /// <summary>Текст HUD: поверх мира и поверх подложек HUD (очередь 3600).</summary>
        public static Material HudTextMaterial
        {
            get
            {
                if (_hudText == null) _hudText = new Material(Font.material) { name = "HudText", renderQueue = 3600 };
                return _hudText;
            }
        }
        static Material _depthText;
        /// <summary>Материал шрифта с ZTest LessEqual (встроенный GUI/Text Shader по умолчанию рисует поверх всего).</summary>
        public static Material DepthTextMaterial
        {
            get
            {
                if (_depthText == null)
                {
                    var sh = Resources.Load<Shader>("PodzTextDepth");
                    if (sh == null || !sh.isSupported) return Font.material; // запасной путь — как было
                    _depthText = new Material(sh) { name = "TextDepth", mainTexture = Font.material.mainTexture };
                    Font.textureRebuilt += f => { if (_depthText != null && f == _font) _depthText.mainTexture = f.material.mainTexture; };
                }
                return _depthText;
            }
        }

        public static Font Font
        {
            get
            {
                if (_font == null) _font = LoadFont();
                return _font;
            }
        }

        static Font LoadFont()
        {
            Font f = null;
            try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (f == null) { try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
            if (f == null) f = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Roboto", "DejaVu Sans" }, 64);
            return f;
        }

        /// <summary>3D-текст. lineHeight — высота строки в метрах. Текст читается, когда смотришь вдоль +Z объекта.</summary>
        public static TextMesh Text(Transform parent, Vector3 localPos, string text, float lineHeight = 0.05f, Color? color = null, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var tm = go.AddComponent<TextMesh>();
            tm.font = Font;
            go.GetComponent<MeshRenderer>().sharedMaterial = Font.material;
            tm.fontSize = 64;
            tm.characterSize = lineHeight * 10f / 64f;
            tm.anchor = anchor;
            tm.alignment = anchor == TextAnchor.MiddleLeft || anchor == TextAnchor.UpperLeft || anchor == TextAnchor.LowerLeft
                ? TextAlignment.Left : TextAlignment.Center;
            tm.color = color ?? Color.white;
            tm.text = text;
            // Текст в мире (таблички, подписи монстров, карточки) — с проверкой глубины, чтобы не просвечивал сквозь стены и панели.
            // Текст HUD на камере остаётся «поверх всего».
            if (parent == null || parent.GetComponentInParent<Camera>() == null) go.GetComponent<MeshRenderer>().sharedMaterial = DepthTextMaterial;
            else go.GetComponent<MeshRenderer>().sharedMaterial = HudTextMaterial;
            return tm;
        }

        public static Light PointLight(Transform parent, Vector3 localPos, Color color, float range, float intensity)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.range = range;
            l.intensity = intensity;
            l.shadows = LightShadows.None;
            return l;
        }

        /// <summary>Повернуть объект так, чтобы его текст был читаем с позиции viewer.</summary>
        public static void FaceTo(Transform t, Vector3 viewer)
        {
            var d = t.position - viewer;
            d.y = 0;
            if (d.sqrMagnitude > 1e-4f) t.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
        }
    }
}
