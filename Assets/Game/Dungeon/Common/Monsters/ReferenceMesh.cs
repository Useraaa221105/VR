using System.Collections.Generic;
using UnityEngine;

namespace Podz
{
    /// <summary>Locally authored meshes in metres. Shared standard URP/Lit materials.</summary>
    public static class ReferenceMesh
    {
        static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        public static Material Surface(string kind)
        {
            if (Materials.TryGetValue(kind, out var old) && old != null) return old;
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.name = "Reference_" + kind;
            m.enableInstancing = true;
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Smoothness", kind == "iron" ? .32f : .16f);
            m.SetFloat("_Metallic", kind == "iron" ? .65f : 0);
            m.EnableKeyword("_EMISSION");
            if (kind == "cloth" || kind == "wing") m.SetFloat("_Cull", 0);
            // Deterministic texture; does not consume the gameplay random stream.
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGB24, true);
            tex.name = "Authored_" + kind; tex.wrapMode = TextureWrapMode.Repeat;
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float noise = Mathf.PerlinNoise(x * .17f, y * .17f), v = .72f + noise * .24f;
                if (kind == "wood") v *= .83f + .17f * Mathf.Sin(x * .62f + Mathf.PerlinNoise(x * .02f, y * .06f) * 3);
                if (kind == "iron") v *= noise > .58f ? .68f : 1;
                if (kind == "scale")
                {
                    float xx = (x + (y / 16 % 2) * 8) % 16;
                    float d = Mathf.Pow((xx - 8) / 8, 2) + Mathf.Pow((y % 16 - 8) / 9f, 2);
                    v *= d > .75f ? .66f : 1;
                }
                if (kind == "cloth") v *= x % 3 == 0 || y % 3 == 0 ? .9f : 1;
                tex.SetPixel(x, y, new Color(v, v, v));
            }
            tex.Apply(); m.SetTexture("_BaseMap", tex); Materials[kind] = m; return m;
        }
        public static Transform Node(string name, Transform parent, Vector3 at)
        {
            var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = at; return t;
        }
        public static Transform Neutral(string name, Transform parent)
        {
            var t = Node(name, parent, Vector3.zero); var s = parent.lossyScale;
            t.localScale = new Vector3(1 / Mathf.Abs(s.x), 1 / Mathf.Abs(s.y), 1 / Mathf.Abs(s.z)); return t;
        }
        public static Mesh Make(string name, List<Vector3> vertices, List<int> triangles)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
            var uv = new List<Vector2>();
            foreach (var p in vertices) uv.Add(new Vector2(p.x * .7f + p.z * .35f, p.y * .7f + p.z * .35f));
            mesh.SetUVs(0, uv); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        public static GameObject Draw(string name, Transform parent, Mesh mesh, Color color, string surface = "scale")
        {
            var g = Node(name, parent, Vector3.zero).gameObject;
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = Surface(surface);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Prim.SetColor(r, color); g.AddComponent<ArchitectureMeshOwner>().Mesh = mesh; return g;
        }
        public static GameObject Tube(string name, Transform parent, Vector3[] path, float[] radii, Color color, string surface = "scale", int sides = 10)
        {
            var v = new List<Vector3>(); var tr = new List<int>();
            for (int k = 0; k < path.Length; k++)
            {
                var dir = k == path.Length - 1 ? path[k] - path[k - 1] : path[k + 1] - path[k];
                var q = Quaternion.FromToRotation(Vector3.up, dir.normalized);
                for (int j = 0; j < sides; j++)
                {
                    float a = j * Mathf.PI * 2 / sides;
                    v.Add(path[k] + q * new Vector3(Mathf.Cos(a) * radii[k], 0, Mathf.Sin(a) * radii[k]));
                }
            }
            for (int k = 0; k < path.Length - 1; k++) for (int j = 0; j < sides; j++)
            {
                int a = k * sides + j, b = k * sides + (j + 1) % sides;
                tr.AddRange(new[] { a, a + sides, b, b, a + sides, b + sides });
            }
            int lo = v.Count; v.Add(path[0]); int hi = v.Count; v.Add(path[path.Length - 1]);
            for (int j = 0; j < sides; j++)
                tr.AddRange(new[] { lo, j, (j + 1) % sides, hi, (path.Length - 1) * sides + (j + 1) % sides, (path.Length - 1) * sides + j });
            return Draw(name, parent, Make(name, v, tr), color, surface);
        }
        public static GameObject Shell(string name, Transform parent, Vector3 at, float[] heights, float[] widths, float[] depths, Color color, string surface = "iron", int sides = 16)
        {
            var v = new List<Vector3>(); var tr = new List<int>();
            for (int k = 0; k < heights.Length; k++) for (int j = 0; j < sides; j++)
            {
                float a = j * Mathf.PI * 2 / sides;
                v.Add(at + new Vector3(Mathf.Cos(a) * widths[k], heights[k], Mathf.Sin(a) * depths[k]));
            }
            for (int k = 0; k < heights.Length - 1; k++) for (int j = 0; j < sides; j++)
            {
                int a = k * sides + j, b = k * sides + (j + 1) % sides;
                tr.AddRange(new[] { a, a + sides, b, b, a + sides, b + sides });
            }
            return Draw(name, parent, Make(name, v, tr), color, surface);
        }
        public static GameObject Box(string name, Transform p, Vector3 at, Vector3 size, Color color, string surface = "wood", bool collider = false)
        {
            var g = Prim.Cube(name, p, at, size, color, collider);
            g.GetComponent<Renderer>().sharedMaterial = Surface(surface); return g;
        }
        public static GameObject Ball(string name, Transform p, Vector3 at, Vector3 size, Color color, string surface = "scale")
        {
            var g = Prim.Sphere(name, p, at, size, color);
            g.GetComponent<Renderer>().sharedMaterial = Surface(surface); return g;
        }
        public static void CombineStatic(Transform parent, string name, Transform except = null)
        {
            // Invoke only on static decorative roots, never gameplay or animated hierarchies.
            var groups = new Dictionary<string, List<MeshRenderer>>();
            var colors = new Dictionary<string, Color>();
            foreach (var r in parent.GetComponentsInChildren<MeshRenderer>())
            {
                if (except != null && r.transform.IsChildOf(except)) continue;
                var block = new MaterialPropertyBlock(); r.GetPropertyBlock(block);
                Color c = block.GetColor("_BaseColor"); string key = r.sharedMaterial.name + c.ToString();
                if (!groups.ContainsKey(key)) { groups[key] = new List<MeshRenderer>(); colors[key] = c; }
                groups[key].Add(r);
            }
            foreach (var pair in groups)
            {
                if (pair.Value.Count < 2) continue;
                var parts = new List<CombineInstance>();
                foreach (var r in pair.Value) parts.Add(new CombineInstance
                {
                    mesh = r.GetComponent<MeshFilter>().sharedMesh,
                    transform = parent.worldToLocalMatrix * r.transform.localToWorldMatrix
                });
                var mesh = new Mesh { name = name }; mesh.CombineMeshes(parts.ToArray(), true, true);
                var g = Draw(name, parent, mesh, colors[pair.Key]);
                g.GetComponent<Renderer>().sharedMaterial = pair.Value[0].sharedMaterial;
                foreach (var r in pair.Value) Object.DestroyImmediate(r.gameObject);
            }
        }
    }
}