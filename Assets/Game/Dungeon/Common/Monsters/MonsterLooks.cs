using System.Collections.Generic;
using UnityEngine;

namespace Podz
{
    /// <summary>
    /// «Живая» внешность монстров поверх greybox-кубов. Игровые кубы (коллайдеры, зоны попадания) остаются,
    /// но их рендеры скрываются, а вместо них рисуются сферы/цилиндры/капсулы: глаза, шляпки, крылья, шлем, пасть.
    /// Все детали — без коллайдеров и дочерние к монстру: умирают и чистятся вместе с ним (и попадают в его кеш).
    /// </summary>
    public static class MonsterLooks
    {
        static readonly Color White = new Color(0.97f, 0.97f, 0.95f), Black = new Color(0.05f, 0.05f, 0.06f);

        public static void Decorate(Monster m)
        {
            var anim = m.gameObject.AddComponent<MonsterAnim>();
            anim.Owner = m;
            switch (m.Kind)
            {
                case MonsterKind.Slime: Slime(m, anim); break;
                case MonsterKind.Mushroom: Mushroom(m, anim); break;
                case MonsterKind.Mandrake: Mandrake(m, anim); break;
                case MonsterKind.Basilisk: Basilisk(m, anim); break;
                case MonsterKind.Armor: FortressCreatures.Armor(m.transform, true, anim); break;
                case MonsterKind.Mimic: FortressCreatures.Mimic(m, anim); break;
                case MonsterKind.Dragon: DragonReference.Build(m, anim); break;
            }
        }

        // ---------- помощники ----------
        static Transform Find(Monster m, string name)
        {
            foreach (var t in m.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        static Color ColorOf(Transform t)
        {
            var r = t != null ? t.GetComponent<Renderer>() : null;
            if (r == null) return Color.gray;
            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            return mpb.GetColor("_BaseColor");
        }

        /// <summary>Скрыть куб и нарисовать вместо него примитив того же размера.</summary>
        static Transform Replace(Monster m, MonsterAnim a, string part, PrimitiveType type, Color? color = null, float k = 1f)
        {
            var t = Find(m, part);
            if (t == null) return null;
            var r = t.GetComponent<Renderer>();
            var c = color ?? ColorOf(t);
            if (r != null) a.Hidden.Add(r);
            var go = Prim.Shape(type, part + "_Vis", t, Vector3.zero, type == PrimitiveType.Cylinder || type == PrimitiveType.Capsule ? new Vector3(k, 0.5f * k, k) : Vector3.one * k, c, false);
            return go.transform;
        }

        static void Eyes(Transform parent, Vector3 center, float spread, float size, Color? iris = null, float glow = 0f)
        {
            for (int s = -1; s <= 1; s += 2)
            {
                var p = center + new Vector3(s * spread, 0, 0);
                var w = Prim.Sphere("Eye", parent, p, Vector3.one * size, iris.HasValue && glow > 0 ? iris.Value : White);
                if (glow > 0) Prim.Glow(w, iris.Value * glow);
                Prim.Sphere("Pupil", parent, p + new Vector3(0, 0, size * 0.42f), Vector3.one * size * 0.45f, Black);
            }
        }

        // ---------- виды ----------
        static void Slime(Monster m, MonsterAnim a)
        {
            if (DungeonManager.I != null && DungeonManager.I.InDungeon && DungeonManager.I.Level == 1) { UpperCaveCreatures.Slime(m,a); return; }
            var body = Find(m, "Body");
            var vis = Replace(m, a, "Body", PrimitiveType.Sphere, new Color(0.35f, 0.95f, 0.45f), 1.05f);
            if (Find(m, "Core") is Transform core && core.GetComponent<Renderer>() is Renderer cr) a.Hidden.Add(cr);
            a.Squash = vis;
            // пузырьки внутри + блик
            Prim.Sphere("Bubble", body, new Vector3(0.07f, -0.06f, 0.05f), Vector3.one * 0.07f, new Color(0.6f, 1f, 0.7f));
            Prim.Sphere("Bubble", body, new Vector3(-0.09f, 0.02f, -0.06f), Vector3.one * 0.05f, new Color(0.6f, 1f, 0.7f));
            Prim.Glow(Prim.Sphere("Shine", body, new Vector3(-0.08f, 0.13f, 0.12f), new Vector3(0.08f, 0.05f, 0.04f), Color.white), Color.white * 0.6f);
            Eyes(body, new Vector3(0, 0.06f, 0.18f), 0.075f, 0.09f);
            Prim.Sphere("Mouth", body, new Vector3(0, -0.04f, 0.2f), new Vector3(0.1f, 0.03f, 0.03f), new Color(0.05f, 0.3f, 0.1f));
        }

        static void Mushroom(Monster m, MonsterAnim a)
        {
            if (DungeonManager.I != null && DungeonManager.I.InDungeon && DungeonManager.I.Level == 1) { UpperCaveCreatures.Mushroom(m,a); return; }
            var stem = Find(m, "Stem"); var cap = Find(m, "Cap");
            Replace(m, a, "Stem", PrimitiveType.Cylinder);
            var capVis = Replace(m, a, "Cap", PrimitiveType.Sphere, new Color(0.9f, 0.16f, 0.12f), 1.05f);
            foreach (var n in new[] { "Dot1", "Dot2" }) if (Find(m, n) is Transform d && d.GetComponent<Renderer>() is Renderer dr) a.Hidden.Add(dr);
            if (cap != null)
            {
                foreach (var p in new[] { new Vector3(0.14f, 0.08f, 0.1f), new Vector3(-0.13f, 0.08f, -0.1f), new Vector3(0.0f, 0.11f, -0.02f), new Vector3(-0.17f, 0.04f, 0.13f), new Vector3(0.2f, 0.03f, -0.12f) })
                    Prim.Sphere("Dot", cap, p, new Vector3(0.09f, 0.04f, 0.09f), White);
                a.Wobble = capVis;
            }
            if (stem != null)
            {
                Eyes(stem, new Vector3(0, 0.08f, 0.12f), 0.055f, 0.07f);
                Prim.Sphere("Mouth", stem, new Vector3(0, -0.01f, 0.13f), new Vector3(0.06f, 0.025f, 0.02f), new Color(0.4f, 0.2f, 0.15f));
                for (int s = -1; s <= 1; s += 2)
                    a.Legs.Add(Prim.Cyl("Foot", stem, new Vector3(s * 0.07f, -0.2f, 0), new Vector3(0.08f, 0.1f, 0.08f), new Color(0.85f, 0.75f, 0.55f)).transform);
            }
        }

        static void Mandrake(Monster m, MonsterAnim a)
        {
            if (DungeonManager.I != null && DungeonManager.I.InDungeon && DungeonManager.I.Level == 2) { RuinsCreatures.Mandrake(m,a); return; }
            var leaves = Find(m, "Leaves"); var root = Find(m, "Root");
            Replace(m, a, "Leaves", PrimitiveType.Sphere, new Color(0.2f, 0.6f, 0.18f), 0.4f);
            if (leaves != null)
            {
                for (int i = 0; i < 6; i++)
                {
                    float ang = i * 60f;
                    var q = Quaternion.Euler(0, ang, 0);
                    var leaf = Prim.Part(PrimitiveType.Sphere, "Leaf", leaves, q * new Vector3(0, 0.06f, 0.1f), new Vector3(0.09f, 0.03f, 0.26f), new Color(0.25f, 0.75f + 0.04f * (i % 2), 0.22f), new Vector3(-35f, ang, 0));
                    a.Wobble = a.Wobble ?? leaf.transform;
                }
                Prim.Sphere("Mound", m.transform, new Vector3(0, 0.0f, 0), new Vector3(0.5f, 0.12f, 0.5f), new Color(0.32f, 0.22f, 0.13f));
            }
            if (root != null)
            {
                a.HiddenAlways.Add(root.GetComponent<Renderer>());
                var c = new Color(0.9f, 0.7f, 0.45f);
                Prim.Part(PrimitiveType.Capsule, "RootBody", root, Vector3.zero, new Vector3(0.17f, 0.3f, 0.17f), c);
                Eyes(root, new Vector3(0, 0.07f, 0.075f), 0.035f, 0.045f);
                Prim.Sphere("Mouth", root, new Vector3(0, 0.01f, 0.085f), new Vector3(0.05f, 0.035f, 0.02f), new Color(0.3f, 0.1f, 0.05f));
                for (int s = -1; s <= 1; s += 2)
                {
                    Prim.Cyl("Arm", root, new Vector3(s * 0.11f, 0.0f, 0), new Vector3(0.035f, 0.14f, 0.035f), c, new Vector3(0, 0, s * 50f));
                    Prim.Cyl("Leg", root, new Vector3(s * 0.04f, -0.17f, 0), new Vector3(0.04f, 0.1f, 0.04f), c);
                }
            }
        }

        static void Basilisk(Monster m, MonsterAnim a)
        {
            if (DungeonManager.I != null && DungeonManager.I.InDungeon && DungeonManager.I.Level == 2) { RuinsCreatures.Basilisk(m,a); return; }
            var body = Find(m, "Body"); var head = Find(m, "Head"); var neck = Find(m, "Neck"); var tail = Find(m, "TailSnake");
            var gold = new Color(0.88f, 0.62f, 0.22f);
            Replace(m, a, "Body", PrimitiveType.Sphere, gold);
            Replace(m, a, "Neck", PrimitiveType.Cylinder, gold * 0.95f);
            Replace(m, a, "Head", PrimitiveType.Sphere, new Color(0.92f, 0.7f, 0.3f));
            foreach (var n in new[] { "LegL", "LegR" }) { var v = Replace(m, a, n, PrimitiveType.Cylinder, new Color(0.95f, 0.8f, 0.2f)); if (v != null) a.Legs.Add(v.parent); }
            if (body != null)
            {
                for (int s = -1; s <= 1; s += 2)
                    a.Wings.Add(Prim.Part(PrimitiveType.Sphere, "Wing", body, new Vector3(s * 0.36f, 0.05f, -0.05f), new Vector3(0.12f, 0.45f, 0.75f), gold * 0.8f, new Vector3(15f, 0, s * -10f)).transform);
                for (int i = 0; i < 3; i++)
                    Prim.Part(PrimitiveType.Sphere, "TailFeather", body, new Vector3((i - 1) * 0.12f, 0.35f, -0.45f), new Vector3(0.08f, 0.5f, 0.12f), new Color(0.15f, 0.45f + i * 0.08f, 0.3f), new Vector3(-30f, 0, (i - 1) * 20f));
            }
            if (head != null)
            {
                Prim.Part(PrimitiveType.Cylinder, "Beak", head, new Vector3(0, -0.02f, 0.25f), new Vector3(0.09f, 0.16f, 0.09f), new Color(1f, 0.85f, 0.2f), new Vector3(90, 0, 0));
                Prim.Part(PrimitiveType.Sphere, "Wattle", head, new Vector3(0, -0.13f, 0.14f), new Vector3(0.07f, 0.1f, 0.05f), new Color(0.9f, 0.1f, 0.1f));
                for (int i = 0; i < 3; i++) Prim.Sphere("CombBall", head, new Vector3(0, 0.15f + (i == 1 ? 0.03f : 0), 0.08f - i * 0.08f), new Vector3(0.05f, 0.1f, 0.07f), new Color(0.95f, 0.1f, 0.1f));
            }
            if (tail != null)
            {
                var tvis = Replace(m, a, "TailSnake", PrimitiveType.Capsule, new Color(0.2f, 0.62f, 0.27f));
                if (tvis != null) { tvis.localRotation = Quaternion.Euler(90, 0, 0); tvis.localScale = new Vector3(1f, 0.5f, 1f); }
                var snakeHead = Prim.Sphere("SnakeHead", tail, new Vector3(0, 0.03f, -0.48f), new Vector3(0.2f, 0.15f, 0.26f), new Color(0.2f, 0.55f, 0.25f));
                Prim.Glow(Prim.Sphere("SnakeEye", snakeHead.transform.parent, new Vector3(0.06f, 0.08f, -0.55f), Vector3.one * 0.04f, Color.red), Color.red * 2f);
                Prim.Glow(Prim.Sphere("SnakeEye", snakeHead.transform.parent, new Vector3(-0.06f, 0.08f, -0.55f), Vector3.one * 0.04f, Color.red), Color.red * 2f);
                a.Wobble = tail.Find("TailSnake_Vis");
            }
        }

        static void Armor(Monster m, MonsterAnim a)
        {
            var torso = Find(m, "Torso"); var helm = Find(m, "Helmet"); var shield = Find(m, "Shield"); var sword = Find(m, "Sword");
            var steel = ColorOf(torso);
            if (torso != null)
            {
                Prim.Sphere("Chest", torso, new Vector3(0, 0.22f, 0.12f), new Vector3(0.58f, 0.5f, 0.3f), steel * 1.1f);
                for (int s = -1; s <= 1; s += 2)
                {
                    Prim.Sphere("Pauldron", torso, new Vector3(s * 0.36f, 0.45f, 0), new Vector3(0.3f, 0.22f, 0.36f), steel * 1.15f);
                    Prim.Cyl("Arm", torso, new Vector3(s * 0.38f, 0.1f, 0.05f), new Vector3(0.14f, 0.5f, 0.14f), steel * 0.9f);
                }
                Prim.Cyl("Belt", torso, new Vector3(0, -0.3f, 0), new Vector3(0.64f, 0.08f, 0.44f), new Color(0.35f, 0.22f, 0.12f));
                Prim.Glow(Prim.Sphere("Buckle", torso, new Vector3(0, -0.3f, 0.22f), new Vector3(0.08f, 0.06f, 0.03f), new Color(1f, 0.8f, 0.3f)), new Color(1f, 0.8f, 0.3f) * 0.3f);
            }
            if (helm != null)
            {
                Replace(m, a, "Helmet", PrimitiveType.Cylinder, steel * 0.95f);
                Prim.Sphere("Dome", helm, new Vector3(0, 0.15f, 0), new Vector3(0.34f, 0.18f, 0.34f), steel * 0.95f);
                Prim.Part(PrimitiveType.Cube, "Visor", helm, new Vector3(0, 0.02f, 0.17f), new Vector3(0.24f, 0.04f, 0.02f), Black);
                Prim.Glow(Prim.Sphere("EyeGlow", helm, new Vector3(0.06f, 0.02f, 0.165f), Vector3.one * 0.035f, new Color(0.4f, 1f, 0.5f)), new Color(0.4f, 1f, 0.5f) * 2f);
                Prim.Glow(Prim.Sphere("EyeGlow", helm, new Vector3(-0.06f, 0.02f, 0.165f), Vector3.one * 0.035f, new Color(0.4f, 1f, 0.5f)), new Color(0.4f, 1f, 0.5f) * 2f);
                Prim.Part(PrimitiveType.Sphere, "Plume", helm, new Vector3(0, 0.28f, -0.05f), new Vector3(0.06f, 0.18f, 0.3f), new Color(0.75f, 0.1f, 0.1f));
            }
            if (shield != null) Prim.Glow(Prim.Sphere("Emblem", shield, new Vector3(-0.045f, 0, 0), new Vector3(0.02f, 0.25f, 0.2f), new Color(0.9f, 0.7f, 0.2f)), new Color(0.9f, 0.7f, 0.2f) * 0.2f);
            if (sword != null)
            {
                Prim.Part(PrimitiveType.Cube, "Guard", sword, new Vector3(0, 0, -0.38f), new Vector3(0.24f, 0.04f, 0.04f), new Color(0.6f, 0.45f, 0.2f));
                Prim.Cyl("Hilt", sword, new Vector3(0, 0, -0.47f), new Vector3(0.05f, 0.16f, 0.05f), new Color(0.35f, 0.2f, 0.1f), new Vector3(90, 0, 0));
            }
            foreach (var n in new[] { "LegL", "LegR" }) { var v = Replace(m, a, n, PrimitiveType.Cylinder); if (v != null) a.Legs.Add(v.parent); }
        }

        static void Mimic(Monster m, MonsterAnim a)
        {
            var box = Find(m, "Box"); var lid = Find(m, "Lid");
            var iron = new Color(0.25f, 0.25f, 0.28f);
            if (box != null)
            {
                for (int s = -1; s <= 1; s += 2)
                    Prim.Part(PrimitiveType.Cube, "Band", box, new Vector3(s * 0.22f, 0, 0), new Vector3(0.06f, 0.45f, 0.46f), iron);
                for (int i = 0; i < 6; i++)
                    Prim.Part(PrimitiveType.Cube, "Tooth", box, new Vector3(-0.28f + i * 0.11f, 0.24f, 0.18f), new Vector3(0.05f, 0.08f, 0.04f), White, new Vector3(0, 0, 45));
                Prim.Part(PrimitiveType.Sphere, "Tongue", box, new Vector3(0, 0.22f, 0.02f), new Vector3(0.3f, 0.05f, 0.3f), new Color(0.85f, 0.2f, 0.3f));
                Prim.Glow(Prim.Sphere("MawEye", box, new Vector3(0.12f, 0.21f, -0.1f), Vector3.one * 0.06f, new Color(1f, 0.85f, 0.2f)), new Color(1f, 0.8f, 0.2f) * 1.5f);
                Prim.Glow(Prim.Sphere("MawEye", box, new Vector3(-0.12f, 0.21f, -0.1f), Vector3.one * 0.06f, new Color(1f, 0.85f, 0.2f)), new Color(1f, 0.8f, 0.2f) * 1.5f);
            }
            if (lid != null)
            {
                for (int s = -1; s <= 1; s += 2)
                    Prim.Part(PrimitiveType.Cube, "LidBand", lid, new Vector3(s * 0.22f, 0.01f, 0), new Vector3(0.06f, 0.13f, 0.48f), iron);
                Prim.Cyl("LidRound", lid, new Vector3(0, 0.05f, 0), new Vector3(0.45f, 0.72f, 0.45f), ColorOf(lid), new Vector3(0, 0, 90));
                for (int i = 0; i < 6; i++)
                    Prim.Part(PrimitiveType.Cube, "TopTooth", lid, new Vector3(-0.28f + i * 0.11f, -0.07f, 0.18f), new Vector3(0.05f, 0.08f, 0.04f), White, new Vector3(0, 0, 45));
            }
        }

        static void Dragon(Monster m, MonsterAnim a)
        {
            var body = Find(m, "Body"); var head = Find(m, "Head"); var neck = Find(m, "Neck"); var tail = Find(m, "Tail"); var paw = Find(m, "Paw");
            var red = ColorOf(body);
            Replace(m, a, "Body", PrimitiveType.Sphere, red, 1.02f);
            if (body != null)
            {
                Prim.Sphere("Belly", body, new Vector3(0, -0.45f, 0.6f), new Vector3(2.2f, 1.4f, 3.2f), new Color(0.95f, 0.7f, 0.35f));
                for (int i = 0; i < 6; i++)
                    Prim.Part(PrimitiveType.Cube, "Spike", body, new Vector3(0, 1.15f - Mathf.Abs(i - 2.5f) * 0.08f, 1.6f - i * 0.65f), new Vector3(0.28f, 0.6f, 0.28f), new Color(0.35f, 0.08f, 0.06f), new Vector3(45, 0, 0));
            }
            foreach (var n in new[] { "WingL", "WingR" })
            {
                var w = Find(m, n);
                if (w == null) continue;
                var v = Replace(m, a, n, PrimitiveType.Sphere, red * 0.75f);
                a.Wings.Add(w);
                for (int i = 0; i < 3; i++) Prim.Part(PrimitiveType.Cube, "Bone", w, new Vector3((i - 1) * 0.6f, 0.08f, 0), new Vector3(0.08f, 0.08f, 2.4f), new Color(0.3f, 0.06f, 0.05f), new Vector3(0, (i - 1) * 25f, 0));
            }
            if (head != null)
            {
                Replace(m, a, "Head", PrimitiveType.Sphere, red * 1.1f);
                Prim.Sphere("Snout", head, new Vector3(0, -0.08f, 0.6f), new Vector3(0.6f, 0.4f, 0.7f), red * 1.05f);
                Prim.Part(PrimitiveType.Cube, "Jaw", head, new Vector3(0, -0.3f, 0.45f), new Vector3(0.55f, 0.12f, 0.7f), red * 0.8f);
                for (int i = 0; i < 4; i++) Prim.Part(PrimitiveType.Cube, "Fang", head, new Vector3(-0.2f + i * 0.13f, -0.22f, 0.8f), new Vector3(0.05f, 0.12f, 0.05f), White);
                for (int s = -1; s <= 1; s += 2)
                {
                    Prim.Part(PrimitiveType.Cylinder, "Horn", head, new Vector3(s * 0.28f, 0.45f, -0.35f), new Vector3(0.1f, 0.6f, 0.1f), new Color(0.9f, 0.85f, 0.7f), new Vector3(-40, 0, s * -20));
                    Prim.Glow(Prim.Sphere("Eye", head, new Vector3(s * 0.28f, 0.18f, 0.32f), new Vector3(0.14f, 0.1f, 0.12f), new Color(1f, 0.85f, 0.1f)), new Color(1f, 0.8f, 0.1f) * 2.5f);
                    Prim.Glow(Prim.Sphere("Nostril", head, new Vector3(s * 0.12f, 0.0f, 0.95f), Vector3.one * 0.07f, new Color(1f, 0.4f, 0.1f)), new Color(1f, 0.4f, 0.1f) * 1.5f);
                }
            }
            if (neck != null) Replace(m, a, "Neck", PrimitiveType.Sphere, red * 0.9f);
            if (paw != null)
            {
                Replace(m, a, "Paw", PrimitiveType.Sphere, red * 0.8f);
                for (int i = 0; i < 3; i++) Prim.Part(PrimitiveType.Cube, "Claw", paw, new Vector3(-0.22f + i * 0.22f, -0.3f, 0.45f), new Vector3(0.08f, 0.08f, 0.25f), White);
            }
            if (tail != null)
            {
                var tv = Replace(m, a, "Tail", PrimitiveType.Capsule, red * 0.85f);
                if (tv != null) tv.localRotation = Quaternion.Euler(90, 0, 0);
                if (tv != null) tv.localScale = new Vector3(1f, 0.5f, 1f);
                Prim.Part(PrimitiveType.Cube, "TailSpike", tail, new Vector3(0, 0.25f, -2.0f), new Vector3(0.5f, 0.5f, 0.5f), new Color(0.35f, 0.08f, 0.06f), new Vector3(45, 0, 0));
            }
        }
    }

}
