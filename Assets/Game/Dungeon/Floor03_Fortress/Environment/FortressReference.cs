using System.Collections.Generic;
using UnityEngine;

namespace Podz
{
    /// <summary>Floor -3 only. Static, wall-side reference detail, without new traps.</summary>
    public static class FortressReference
    {
        public const float WallHeight = 4.8f;
        public static float HeightFor(int level) => level == 3 ? WallHeight : FloorGenerator.WallH;
        static readonly Color Stone = new Color(.48f, .46f, .40f), Iron = new Color(.34f, .31f, .27f),
            Gold = new Color(.65f, .45f, .16f), Wood = new Color(.31f, .20f, .10f), Cloth = new Color(.29f, .12f, .085f);

        public static void Build(Transform t, int type, Vector3 size)
        {
            float x = size.x * .5f, z = size.z * .5f;
            for (int s = -1; s <= 1; s += 2)
            {
                for (int k = -1; k <= 1; k += 2)
                {
                    var bay = ReferenceMesh.Node("FortressRibBay", t, new Vector3(s * (x - .28f), 0, k * (z - 1.2f)));
                    ReferenceMesh.Shell("StoneFlutedPilaster", bay, Vector3.zero,
                        new[] { 0f, .2f, .3f, 4.45f, 4.6f, 4.8f },
                        new[] { .32f, .32f, .22f, .22f, .32f, .32f },
                        new[] { .32f, .32f, .22f, .22f, .32f, .32f }, Stone, "stone", 10);
                    ReferenceMesh.Box("StoneCapital", bay, new Vector3(0, 4.65f, 0), new Vector3(.7f, .22f, .7f), Stone, "stone");
                }
                Banner(t, new Vector3(s * (x - .16f), 2.7f, -1.1f), s < 0 ? 90 : -90);
                Torch(t, new Vector3(s * (x - .18f), 2.5f, 1.7f), s < 0 ? 90 : -90);
            }
            for (int k = -1; k <= 1; k += 2) for (int j = 0; j < 14; j++)
            {
                float a = (j + .5f) * Mathf.PI / 14, rise = (size.x + .6f) * .18f;
                var rib = ReferenceMesh.Box("StoneVaultRib", t,
                    new Vector3(-x * Mathf.Cos(a), WallHeight + Mathf.Sin(a) * rise - .05f, k * (z - 1.1f)),
                    new Vector3(size.x / 14 * .95f, .18f, .25f), Stone * .9f, "stone");
                rib.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(rise * Mathf.Cos(a), x * Mathf.Sin(a)) * Mathf.Rad2Deg);
            }
            switch (type)
            {
                case 0: case 7:
                    for (int s = -1; s <= 1; s += 2)
                    {
                        var gate = ReferenceMesh.Node("GateButtress", t, new Vector3(s * (x - .55f), 0, z - .8f));
                        ReferenceMesh.Box("StoneGatePost", gate, new Vector3(0, 1.8f, 0), new Vector3(.65f, 3.6f, .7f), Stone, "stone");
                        ReferenceMesh.Box("GateGoldInlay", gate, new Vector3(0, 1.8f, -.38f), new Vector3(.13f, 3.4f, .04f), Gold, "iron");
                    }
                    break;
                case 1:
                    Rack(t, new Vector3(-x + .5f, 0, -z + 1.3f));
                    Rack(t, new Vector3(x - .5f, 0, z - 1.3f)); break;
                case 2:
                    for (int s = -1; s <= 1; s += 2)
                    {
                        ReferenceMesh.Box("BarracksBed", t, new Vector3(s * (x - .8f), .25f, -z + 1.4f), new Vector3(1.1f, .4f, 2), Wood);
                        ReferenceMesh.Box("WornBedRoll", t, new Vector3(s * (x - .8f), .51f, -z + 1.4f), new Vector3(1, .16f, 1.9f), Cloth, "cloth");
                    }
                    break;
                case 3:
                    for (int s = -1; s <= 1; s += 2)
                    {
                        ReferenceMesh.Box("StoneArmourPlinth", t, new Vector3(s * (x - .9f), .1f, z - 1), new Vector3(1.1f, .2f, 1.1f), Stone, "stone");
                        Banner(t, new Vector3(s * 2, 2.8f, z - .16f), 180);
                    }
                    break;
                case 4:
                    for (int s = -1; s <= 1; s += 2)
                    {
                        var pile = ReferenceMesh.Node("TreasureCoins", t, new Vector3(s * (x - 1.2f), 0, z - 1.2f));
                        for (int j = 0; j < 34; j++)
                        {
                            float a = j * 2.4f, r = .08f * Mathf.Sqrt(j);
                            ReferenceMesh.Shell("Coin", pile, new Vector3(Mathf.Cos(a) * r, .02f + (j % 4) * .024f, Mathf.Sin(a) * r),
                                new[] { 0f, .014f, .014f }, new[] { .07f, .07f, 0f }, new[] { .07f, .07f, 0f }, Gold, "iron", 8);
                        }
                    }
                    break;
                case 5:
                    for (int s = -1; s <= 1; s += 2)
                    {
                        ReferenceMesh.Shell("BarrelStaves", t, new Vector3(s * (x - .7f), 0, z - 1),
                            new[] { 0f, .08f, .5f, .9f, .96f }, new[] { .27f, .30f, .35f, .30f, 0f },
                            new[] { .27f, .30f, .35f, .30f, 0f }, Wood, "wood");
                        ReferenceMesh.Box("StoredCrate", t, new Vector3(s * (x - .7f), .4f, -z + 1), new Vector3(.8f, .8f, .8f), Wood);
                    }
                    break;
                case 6:
                    var altar = ReferenceMesh.Node("ChapelAltar", t, new Vector3(x - 1, 0, z - 1));
                    ReferenceMesh.Box("StoneAltar", altar, new Vector3(0, .65f, 0), new Vector3(1.3f, 1.3f, .7f), Stone, "stone");
                    ReferenceMesh.Box("AltarCloth", altar, new Vector3(0, 1.32f, 0), new Vector3(1.38f, .03f, .75f), Cloth, "cloth");
                    ReferenceMesh.Box("ChapelCross", altar, new Vector3(0, 1.85f, 0), new Vector3(.06f, .8f, .06f), Gold, "iron");
                    ReferenceMesh.Box("CrossArm", altar, new Vector3(0, 2.05f, 0), new Vector3(.4f, .06f, .06f), Gold, "iron"); break;
            }
            ReferenceMesh.CombineStatic(t, "FortressBatchedDetail");
        }
        static void Banner(Transform p, Vector3 at, float yaw)
        {
            var t = ReferenceMesh.Node("FadedTapestry", p, at); t.localRotation = Quaternion.Euler(0, yaw, 0);
            ReferenceMesh.Box("TapestryRod", t, new Vector3(0, .72f, 0), new Vector3(1.15f, .05f, .08f), Iron, "iron");
            var v = new List<Vector3>(); var tr = new List<int>();
            for (int y = 0; y <= 8; y++) for (int x = 0; x <= 8; x++)
                v.Add(new Vector3((x - 4) * .12f, .66f - y * .22f, .04f * Mathf.Sin(x * 1.5f + y * .2f)));
            for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++)
            {
                int i = y * 9 + x; tr.AddRange(new[] { i, i + 9, i + 1, i + 1, i + 9, i + 10 });
            }
            ReferenceMesh.Draw("WornFabric", t, ReferenceMesh.Make("FoldedBanner", v, tr), Cloth, "cloth");
            ReferenceMesh.Box("HeraldicStripe", t, new Vector3(0, -.1f, -.06f), new Vector3(.13f, 1.3f, .01f), Gold * .8f, "cloth");
            ReferenceMesh.Box("HeraldicCrossbar", t, new Vector3(0, .15f, -.065f), new Vector3(.5f, .12f, .01f), Gold * .8f, "cloth");
        }
        static void Torch(Transform p, Vector3 at, float yaw)
        {
            var t = ReferenceMesh.Node("WallTorchSconce", p, at); t.localRotation = Quaternion.Euler(0, yaw, 0);
            ReferenceMesh.Tube("ForgedTorchBracket", t,
                new[] { new Vector3(0, -.4f, 0), new Vector3(0, -.25f, -.23f), new Vector3(0, .08f, -.23f) },
                new[] { .055f, .055f, .07f }, Iron, "iron");
            var flame = ReferenceMesh.Tube("SmallAmberFlame", t,
                new[] { new Vector3(0, .06f, -.23f), new Vector3(.035f, .2f, -.23f), new Vector3(0, .37f, -.23f) },
                new[] { .09f, .06f, .003f }, new Color(.9f, .55f, .16f), "stone");
            Prim.SetEmission(flame.GetComponent<Renderer>(), new Color(.3f, .13f, .025f));
            Prim.PointLight(t, new Vector3(0, .2f, -.28f), new Color(1, .75f, .43f), 5, .75f);
        }
        static void Rack(Transform p, Vector3 at)
        {
            var t = ReferenceMesh.Node("WeaponRack", p, at);
            ReferenceMesh.Box("RackBeam", t, new Vector3(0, 1.4f, 0), new Vector3(.16f, .14f, 1.8f), Wood);
            for (int j = -1; j <= 1; j++)
            {
                var basePos = new Vector3(0, .65f, j * .48f);
                ReferenceMesh.Tube("RackBlade", t, new[] { basePos, basePos + Vector3.up * .6f, basePos + Vector3.up * .85f }, new[] { .05f, .05f, 0f }, Iron, "iron", 4);
                ReferenceMesh.Box("RackHilt", t, basePos - Vector3.up * .16f, new Vector3(.07f, .27f, .07f), Wood);
            }
        }
    }
}