using System.Collections.Generic;
using UnityEngine;

namespace Podz
{
    /// <summary>Visual meshes follow existing hit/grab nodes. No replacement of hunt rules.</summary>
    public static class FortressCreatures
    {
        static readonly Color Steel = new Color(.48f, .46f, .40f), Rust = new Color(.30f, .17f, .085f),
            Dark = new Color(.065f, .055f, .04f), Wood = new Color(.43f, .28f, .13f),
            Crab = new Color(.51f, .39f, .20f), Bone = new Color(.72f, .62f, .40f);
        static Transform Find(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }
        static Transform Replace(Transform root, string name, MonsterAnim a)
        {
            var t = Find(root, name); if (t == null) return null;
            if (t.GetComponent<Renderer>() is Renderer r) { r.enabled = false; if (a != null) a.HiddenAlways.Add(r); }
            return ReferenceMesh.Neutral(name + "ReferenceVisual", t);
        }
        public static void Armor(Transform root, bool alive, MonsterAnim a = null)
        {
            var torso = Replace(root, "Torso", a);
            ReferenceMesh.Shell("ForgedBreastplate", torso, Vector3.zero,
                new[] { -.43f, -.24f, .12f, .43f, .48f },
                new[] { .24f, .22f, .32f, .26f, .10f }, new[] { .18f, .16f, .21f, .18f, .09f }, Steel);
            for (int j = 0; j < 4; j++)
                ReferenceMesh.Shell("OverlappingFauldPlate", torso, new Vector3(0, -.31f - j * .067f, 0),
                    new[] { -.065f, 0f, .02f }, new[] { .30f + j * .009f, .29f, .28f },
                    new[] { .20f, .185f, .18f }, Steel * (.84f + j * .025f));
            for (int s = -1; s <= 1; s += 2)
            {
                ReferenceMesh.Shell("LayeredPauldron", torso, new Vector3(s * .34f, .36f, 0),
                    new[] { -.11f, 0f, .13f, .16f }, new[] { .15f, .18f, .13f, 0f }, new[] { .18f, .22f, .17f, 0f }, Steel);
                ReferenceMesh.Tube("ArticulatedArm", torso,
                    new[] { new Vector3(s * .36f, .3f, 0), new Vector3(s * .39f, -.03f, .06f), new Vector3(s * .4f, -.16f, .30f) },
                    new[] { .09f, .075f, .07f }, Steel, "iron");
                ReferenceMesh.Ball("ElbowRivet", torso, new Vector3(s * .44f, -.035f, .07f), Vector3.one * .095f, Rust, "iron");
                for (int j = 0; j < 3; j++)
                    ReferenceMesh.Ball("RustPatch", torso, new Vector3(s * (.13f + j * .03f), .28f - j * .19f, .19f), new Vector3(.07f, .09f, .012f), Rust, "iron");
            }
            ReferenceMesh.CombineStatic(torso, "BatchedRustyCuirass");
            var helm = Replace(root, "Helmet", a);
            ReferenceMesh.Shell("GreatHelmCrown", helm, Vector3.zero,
                new[] { -.17f, -.10f, .12f, .19f, .23f }, new[] { .17f, .18f, .165f, .10f, 0f },
                new[] { .16f, .19f, .16f, .12f, 0f }, Steel * .9f);
            ReferenceMesh.Box("DarkVisorInterior", helm, new Vector3(0, -.04f, .191f), new Vector3(.27f, .17f, .012f), Dark, "iron");
            for (int j = -3; j <= 3; j++)
                ReferenceMesh.Box("VerticalVisorBar", helm, new Vector3(j * .037f, -.06f, .205f), new Vector3(.014f, .19f, .018f), Steel, "iron");
            ReferenceMesh.Box("VisorBrow", helm, new Vector3(0, .063f, .18f), new Vector3(.30f, .03f, .025f), Steel, "iron");
            if (alive)
            {
                var slime = ReferenceMesh.Tube("MolluscSlimeInHelm", helm,
                    new[] { new Vector3(-.09f, .044f, .193f), new Vector3(0, .037f, .201f), new Vector3(.095f, .047f, .191f) },
                    new[] { .014f, .019f, .012f }, new Color(.33f, .48f, .23f), "scale", 8);
                Prim.SetEmission(slime.GetComponent<Renderer>(), new Color(.018f, .035f, .008f));
            }
            var glint = Find(root, "SlimeGlint"); if (glint != null && glint.GetComponent<Renderer>() is Renderer gr)
            { gr.enabled = false; if (a != null) a.HiddenAlways.Add(gr); }
            ReferenceMesh.CombineStatic(helm, "BatchedGreatHelm");
            var sword = Replace(root, "Sword", a);
            ReferenceMesh.Tube("TaperedSwordBlade", sword,
                new[] { new Vector3(0, 0, -.31f), new Vector3(0, 0, .38f), new Vector3(0, 0, .48f) },
                new[] { .038f, .025f, 0f }, new Color(.63f, .62f, .57f), "iron", 4);
            ReferenceMesh.Box("SwordCrossguard", sword, new Vector3(0, 0, -.34f), new Vector3(.23f, .045f, .035f), Rust, "iron");
            ReferenceMesh.Tube("SwordLeatherGrip", sword, new[] { new Vector3(0, 0, -.35f), new Vector3(0, 0, -.5f) }, new[] { .028f, .028f }, Dark, "wood");
            ReferenceMesh.CombineStatic(sword, "BatchedSword");
            // Shield's original yz orientation and hit collider are retained.
            var shield = Replace(root, "Shield", a);
            var verts = new List<Vector3> { new Vector3(-.05f, .35f, -.23f), new Vector3(-.05f, .35f, .23f),
                new Vector3(-.05f, -.12f, .23f), new Vector3(-.05f, -.35f, 0), new Vector3(-.05f, -.12f, -.23f), new Vector3(-.09f, .06f, 0) };
            var shieldTr = new List<int> { 5, 1, 0, 5, 2, 1, 5, 3, 2, 5, 4, 3, 5, 0, 4 };
            for (int j = 0; j < 6; j++) { var v = verts[j]; v.x = -v.x; verts.Add(v); }
            for (int j = 0; j < 15; j += 3) shieldTr.AddRange(new[] { shieldTr[j] + 6, shieldTr[j + 2] + 6, shieldTr[j + 1] + 6 });
            ReferenceMesh.Draw("PointedRivetedShield", shield, ReferenceMesh.Make("KiteShield", verts, shieldTr), Steel * .65f, "iron");
            ReferenceMesh.Ball("ShieldBoss", shield, new Vector3(-.09f, .08f, 0), new Vector3(.07f, .12f, .12f), Rust, "iron");
            for (int s = -1; s <= 1; s += 2)
            {
                var leg = Replace(root, s < 0 ? "LegL" : "LegR", a);
                ReferenceMesh.Shell("SculptedGreave", leg, Vector3.zero, new[] { -.18f, -.02f, .28f, .41f },
                    new[] { .075f, .08f, .10f, .08f }, new[] { .09f, .08f, .09f, .08f }, Steel * .8f);
                ReferenceMesh.Ball("ArmouredSabatons", leg, new Vector3(0, -.15f, .095f), new Vector3(.17f, .14f, .34f), Steel, "iron");
                ReferenceMesh.CombineStatic(leg, "BatchedGreave");
                if (a != null) a.Legs.Add(leg.parent);
            }
        }
        static Mesh LidMesh()
        {
            var v = new List<Vector3>(); var tr = new List<int>(); const int count = 12;
            for (int side = -1; side <= 1; side += 2) for (int j = 0; j <= count; j++)
            { float ang = Mathf.PI * j / count; v.Add(new Vector3(side * .36f, -.045f + .16f * Mathf.Sin(ang), .235f * Mathf.Cos(ang))); }
            for (int j = 0; j < count; j++) { int a = j, b = j + count + 1; tr.AddRange(new[] { a, a + 1, b, b, a + 1, b + 1 }); }
            for (int side = 0; side < 2; side++)
            { int center = v.Count; v.Add(new Vector3(side == 0 ? -.36f : .36f, -.045f, 0)); for (int j = 0; j < count; j++) { int k = side * (count + 1) + j; tr.AddRange(side == 0 ? new[] { center, k + 1, k } : new[] { center, k, k + 1 }); } }
            return ReferenceMesh.Make("VaultedWoodenChestLid", v, tr);
        }
        public static void Mimic(Monster m, MonsterAnim a)
        {
            var box = Replace(m.transform, "Box", a);
            ReferenceMesh.Box("ChestPlankBody", box, Vector3.zero, new Vector3(.70f, .44f, .45f), Wood);
            for (int s = -1; s <= 1; s += 2)
            {
                ReferenceMesh.Box("RivetedChestBand", box, new Vector3(s * .24f, 0, 0), new Vector3(.045f, .45f, .47f), Steel * .5f, "iron");
                for (int j = 0; j < 4; j++) ReferenceMesh.Ball("BandRivet", box, new Vector3(s * .24f, -.16f + j * .1f, .244f), Vector3.one * .021f, Rust, "iron");
            }
            ReferenceMesh.CombineStatic(box, "BatchedChest");
            var lid = Replace(m.transform, "Lid", a);
            ReferenceMesh.Draw("CurvedWoodenLid", lid, LidMesh(), Wood * 1.1f, "wood");
            for (int s = -1; s <= 1; s += 2)
            {
                var path = new Vector3[13]; var radii = new float[13];
                for (int j = 0; j < path.Length; j++)
                { float ang = Mathf.PI * j / 12; path[j] = new Vector3(s * .24f, -.042f + .165f * Mathf.Sin(ang), .241f * Mathf.Cos(ang)); radii[j] = .016f; }
                ReferenceMesh.Tube("ArchedIronBand", lid, path, radii, Steel * .55f, "iron", 6);
            }
            ReferenceMesh.CombineStatic(lid, "BatchedLid");
            for (int j = 0; j < 6; j++)
            {
                var original = Find(m.transform, "Leg" + j); if (original == null) continue;
                var leg = Replace(m.transform, "Leg" + j, a); float s = j % 2 == 0 ? -1 : 1;
                ReferenceMesh.Tube("JointedCrustaceanLeg", leg,
                    new[] { new Vector3(0, .15f, 0), new Vector3(s * .19f, .17f, -.04f), new Vector3(s * .30f, -.12f, .06f), new Vector3(s * .35f, -.15f, .13f) },
                    new[] { .035f, .038f, .022f, .005f }, Crab);
                ReferenceMesh.CombineStatic(leg, "BatchedCrabLeg"); a.Legs.Add(original);
            }
            for (int s = -1; s <= 1; s += 2)
            {
                var claw = Replace(m.transform, s < 0 ? "ClawL" : "ClawR", a);
                ReferenceMesh.Tube("CrabForearm", claw, new[] { new Vector3(0, 0, -.12f), new Vector3(s * .05f, .02f, .03f) }, new[] { .047f, .065f }, Crab);
                for (int finger = -1; finger <= 1; finger += 2)
                    ReferenceMesh.Tube("CurvedPincer", claw,
                        new[] { new Vector3(s * .05f, .02f, .02f), new Vector3(s * .05f + finger * .078f, .02f, .16f), new Vector3(s * .05f + finger * .025f, .01f, .25f) },
                        new[] { .061f, .04f, .006f }, Crab * .95f);
                ReferenceMesh.CombineStatic(claw, "BatchedPincer");
            }
            var eyes = ReferenceMesh.Node("HiddenHermitCrab", m.transform, new Vector3(0, .46f, .08f));
            ReferenceMesh.Ball("CrabCarapace", eyes, Vector3.zero, new Vector3(.31f, .16f, .26f), Crab);
            for (int s = -1; s <= 1; s += 2)
            {
                ReferenceMesh.Tube("EyeStalk", eyes, new[] { new Vector3(s * .07f, .02f, .08f), new Vector3(s * .10f, .13f, .10f) }, new[] { .012f, .011f }, Crab);
                ReferenceMesh.Ball("CrabEye", eyes, new Vector3(s * .10f, .13f, .10f), Vector3.one * .035f, Dark);
            }
            ReferenceMesh.CombineStatic(eyes, "BatchedHermitCrab");
            var follow = m.gameObject.AddComponent<MimicReferencePose>(); follow.Owner = m; follow.Interior = eyes; follow.Lid = Find(m.transform, "Lid");
            eyes.gameObject.SetActive(false);
        }
    }
}
