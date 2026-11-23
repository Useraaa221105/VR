using System.Collections.Generic;
using UnityEngine;

namespace Podz
{
    /// <summary>Типы комнат по биомам (как в презентации): у каждого этажа 8 «именных» комнат, своя табличка, декор и любимый монстр.</summary>
    public static class RoomTypes
    {
        public static readonly string[][] Names =
        {
            new string[0],
            new[] { "Входной грот", "Слизневый пруд", "Кристальный зал", "Грибная роща", "Капельный коридор", "Затопленный грот", "Лагерь охотников", "Спуск на −2" },
            new[] { "Арка корней", "Сад мандрагор", "Курятник василиска", "Обвал", "Колодезный двор", "Разрушенный храм", "Зал колонн", "Спуск на −3" },
            new[] { "Ворота крепости", "Оружейная", "Казарма", "Рыцарский зал", "Сокровищница", "Кладовая", "Часовня", "Золотые врата" },
        };

        /// <summary>Любимый монстр комнаты (null — смешанно).</summary>
        public static MonsterKind? Favorite(int level, int type)
        {
            switch (level)
            {
                case 1: return type == 1 || type == 4 ? MonsterKind.Slime : type == 3 ? MonsterKind.Mushroom : (MonsterKind?)null;
                case 2: return type == 1 ? MonsterKind.Mandrake : type == 2 ? MonsterKind.Basilisk : (MonsterKind?)null;
                case 3: return type == 1 || type == 2 ? MonsterKind.Armor : type == 4 || type == 5 ? MonsterKind.Mimic : (MonsterKind?)null;
            }
            return null;
        }

        /// <summary>Тип комнаты по индексу: старт = 0, спуск = 7, остальные — по кругу 1..6.</summary>
        public static int Assign(int roomIndex, int start, int stairs, int order)
        {
            if (roomIndex == start) return 0;
            if (roomIndex == stairs) return 7;
            return 1 + order % 6;
        }
    }

    public static class RoomDecor
    {
        static Vector3 R(float a, float b) => new Vector3(Random.Range(-a, a), 0, Random.Range(-b, b));

        public static void Build(Transform root, int level, int type, Vector3 c, Biome b, Vector3 roomSize)
        {
            string name = RoomTypes.Names[level][type];
            // табличка на северной стене (левее проёма): читается, когда смотришь на север
            var sign = new GameObject("Sign_" + name).transform;
            sign.SetParent(root, true);
            sign.position = c + new Vector3(-2.3f, 2.3f, roomSize.z * .5f - .3f);
            Prim.Cube("Board", sign, new Vector3(0, 0, 0.03f), new Vector3(2.2f, 0.45f, 0.05f), new Color(0.35f, 0.22f, 0.12f), false);
            Prim.Text(sign, Vector3.zero, name, 0.16f, new Color(1f, 0.9f, 0.6f));

            var t = new GameObject("Decor_" + name).transform;
            t.SetParent(root, true);
            t.position = c;
            switch (level)
            {
                case 1: Caves(t, type, b); break;
                case 2: Ruins(t, type, b, roomSize); break;
                case 3:
                    var template = SavedDungeonAssets.Active?.FindFortressDecor(type, roomSize);
                    if (template != null)
                        Object.Instantiate(template, t, false).name = "SavedDecor";
                    else FortressReference.Build(t, type, roomSize);
                    break;
            }
        }

        static GameObject Glow(GameObject g, Color c) => Prim.Glow(g, c);

        static void Caves(Transform t, int type, Biome b)
        {
            // кристаллы по углам во всех комнатах
            for (int i = 0; i < 3; i++)
            {
                var p = new Vector3((i % 2 == 0 ? -1 : 1) * 3.4f, 0, (i - 1) * 2.8f);
                var cr = Prim.Part(PrimitiveType.Cylinder, "Crystal", t, p + Vector3.up * 0.45f, new Vector3(0.22f, 0.9f, 0.22f), b.Accent, new Vector3(Random.Range(-15, 15), 0, Random.Range(-20, 20)));
                Glow(cr, b.Accent * 1.6f);
            }
            switch (type)
            {
                case 0: Camp(t, new Vector3(2.2f, 0, 2.2f)); break;
                case 1: // слизневый пруд
                    Glow(Prim.Cyl("Pond", t, new Vector3(0.5f, 0.015f, 0.5f), new Vector3(3.2f, 0.02f, 2.4f), new Color(.18f,.27f,.22f)), Color.black);
                    for (int i = 0; i < 5; i++) Prim.Sphere("SlimeBlob", t, R(1.4f, 1.0f) + new Vector3(0.5f, 0.06f, 0.5f), Vector3.one * Random.Range(0.12f, 0.25f), new Color(0.4f, 1f, 0.5f));
                    break;
                case 2: // кристальный зал
                    for (int i = 0; i < 6; i++)
                    {
                        var p = R(2.6f, 2.6f);
                        if (p.magnitude < 1.4f) p = p.normalized * 2f;
                        float h = Random.Range(0.8f, 1.8f);
                        Glow(Prim.Part(PrimitiveType.Cylinder, "BigCrystal", t, p + Vector3.up * h * 0.5f, new Vector3(0.3f, h, 0.3f), new Color(0.5f, 0.8f, 1f), new Vector3(Random.Range(-20, 20), 0, Random.Range(-20, 20))), new Color(0.3f, 0.6f, 1f) * 1.8f);
                    }
                    Prim.PointLight(t, Vector3.up * 2.5f, new Color(0.5f, 0.8f, 1f), 9f, 1.2f);
                    break;
                case 3: // грибная роща
                    for (int i = 0; i < 4; i++)
                    {
                        var p = new Vector3((i % 2 == 0 ? -1 : 1) * 2.8f, 0, (i < 2 ? -1 : 1) * 2.6f);
                        float h = Random.Range(1.4f, 2.4f);
                        Prim.Cyl("GiantStem", t, p + Vector3.up * h * 0.5f, new Vector3(0.3f, h, 0.3f), new Color(0.9f, 0.85f, 0.7f));
                        Glow(Prim.Sphere("GiantCap", t, p + Vector3.up * h, new Vector3(1.4f, 0.5f, 1.4f), new Color(0.75f, 0.35f, 0.8f)), new Color(0.4f, 0.1f, 0.5f));
                    }
                    break;
                case 4: // капельный коридор — сталактиты
                    for (int i = 0; i < 8; i++) Prim.Part(PrimitiveType.Cylinder, "Stalactite", t, R(3f, 3f) + Vector3.up * 3.1f, new Vector3(0.18f, Random.Range(0.4f, 0.9f), 0.18f), new Color(0.45f, 0.55f, 0.6f));
                    break;
                case 5: // затопленный грот
                    Glow(Prim.Part(PrimitiveType.Cube, "Water", t, new Vector3(0, 0.02f, 0), new Vector3(7.2f, 0.03f, 7.2f), new Color(0.15f, 0.4f, 0.6f)), new Color(0.05f, 0.2f, 0.35f));
                    for (int i = 0; i < 4; i++) Prim.Sphere("Stone", t, R(3f, 3f) + Vector3.up * 0.1f, new Vector3(0.6f, 0.25f, 0.5f), new Color(0.35f, 0.4f, 0.42f));
                    break;
                case 6: Camp(t, Vector3.zero); break;
                case 7: Arch(t, b); break;
            }
        }

        static void Ruins(Transform t, int type, Biome b, Vector3 roomSize)
        {
            // Корни прикреплены к настоящей стене комнаты; центральный проход свободен.
            for (int side=-1;side<=1;side+=2) for(int branch=0;branch<2;branch++) {
                float x=side*(roomSize.x*.5f-.7f-branch*.38f), wallZ=roomSize.z*.5f-.12f;
                Vector3 previous=new Vector3(x,.08f,wallZ);
                for(int segment=1;segment<=5;segment++) {
                    var next=new Vector3(x+side*.12f*Mathf.Sin(segment*.8f+branch),segment*.6f,wallZ);
                    var delta=next-previous;
                    var root=Prim.Part(PrimitiveType.Cylinder,"WallRoot",t,(next+previous)*.5f,new Vector3(.1f,delta.magnitude+.03f,.1f),new Color(.27f,.19f,.12f));
                    root.transform.localRotation=Quaternion.FromToRotation(Vector3.up,delta.normalized);previous=next;
                }
            }
            switch (type)
            {
                case 0: Arch(t, b); break;
                case 1: // сад мандрагор — грядки
                    for (int i = -1; i <= 1; i++) Prim.Part(PrimitiveType.Cube, "Bed", t, new Vector3(i * 2.2f, 0.06f, -2.5f), new Vector3(1.4f, 0.12f, 1.0f), new Color(0.3f, 0.2f, 0.12f));
                    Prim.Part(PrimitiveType.Cube, "Fence", t, new Vector3(0, 0.3f, -3.3f), new Vector3(6.5f, 0.6f, 0.06f), new Color(0.45f, 0.32f, 0.18f));
                    break;
                case 2: // курятник василиска
                    Prim.Part(PrimitiveType.Cube, "Coop", t, new Vector3(2.6f, 0.6f, 2.6f), new Vector3(1.6f, 1.2f, 1.2f), new Color(0.5f, 0.35f, 0.2f));
                    Prim.Part(PrimitiveType.Cube, "CoopRoof", t, new Vector3(2.6f, 1.3f, 2.6f), new Vector3(1.8f, 0.12f, 1.4f), new Color(0.6f, 0.2f, 0.12f), new Vector3(0, 0, 10));
                    for (int i = 0; i < 4; i++) Prim.Sphere("Egg", t, R(2.5f, 2.5f) + Vector3.up * 0.1f, new Vector3(0.14f, 0.18f, 0.14f), new Color(0.95f, 0.9f, 0.75f));
                    for (int i = 0; i < 4; i++) Prim.Sphere("Statue", t, new Vector3(-2.8f + i * 0.5f, 0.3f, -3f), new Vector3(0.3f, 0.6f, 0.3f), new Color(0.55f, 0.55f, 0.55f)); // окаменевшие гости
                    break;
                case 3: // обвал
                    for (int i = 0; i < 7; i++) Prim.Sphere("Rubble", t, R(3f, 3f) + Vector3.up * 0.15f, Vector3.one * Random.Range(0.3f, 0.7f), new Color(0.45f, 0.43f, 0.35f));
                    break;
                case 4: // колодезный двор
                    Prim.Part(PrimitiveType.Cube, "Tiles", t, new Vector3(0, 0.01f, 0), new Vector3(5f, 0.02f, 5f), new Color(0.5f, 0.48f, 0.4f));
                    break;
                case 5: // храм
                    Prim.Part(PrimitiveType.Cube, "TempleStep", t, new Vector3(0, 0.15f, 3f), new Vector3(3f, 0.3f, 1.2f), new Color(0.6f, 0.58f, 0.5f));
                    Prim.Sphere("Idol", t, new Vector3(0, 0.9f, 3.2f), new Vector3(0.6f, 1.0f, 0.5f), new Color(0.65f, 0.6f, 0.45f));
                    Glow(Prim.Sphere("IdolEye", t, new Vector3(0, 1.1f, 2.95f), Vector3.one * 0.1f, b.Accent), b.Accent * 2f);
                    break;
                case 6: // зал колонн
                    for (int i = 0; i < 4; i++)
                    {
                        var p = new Vector3((i % 2 == 0 ? -1 : 1) * 2.6f, 0, (i < 2 ? -1 : 1) * 2.6f);
                        var col = Prim.Shape(PrimitiveType.Cylinder, "Column", t, p + Vector3.up * (i == 3 ? 0.8f : 1.75f), new Vector3(0.6f, i == 3 ? 0.8f : 1.75f, 0.6f), new Color(0.6f, 0.58f, 0.48f), true);
                        col.transform.position = t.position + p + Vector3.up * (i == 3 ? 0.8f : 1.75f);
                    }
                    break;
                case 7: Arch(t, b); break;
            }
        }

        static void Fortress(Transform t, int type, Biome b)
        {
            // факелы на стенах и знамёна
            for (int s = -1; s <= 1; s += 2)
            {
                var torch = Prim.Cyl("Torch", t, new Vector3(s * 3.8f, 1.8f, 1.5f), new Vector3(0.08f, 0.45f, 0.08f), new Color(0.35f, 0.2f, 0.1f));
                var flame = Glow(Prim.Sphere("TorchFlame", t, new Vector3(s * 3.8f, 2.1f, 1.5f), new Vector3(0.14f, 0.22f, 0.14f), new Color(1f, 0.6f, 0.2f)), new Color(1f, 0.5f, 0.1f) * 3f);
                flame.AddComponent<Flicker>().Init(Prim.PointLight(flame.transform, Vector3.zero, b.Light, 6f, 1.2f));
            }
            Prim.Part(PrimitiveType.Cube, "Banner", t, new Vector3(2.2f, 2.0f, 3.85f), new Vector3(0.9f, 1.4f, 0.03f), new Color(0.6f, 0.08f, 0.08f));
            Glow(Prim.Sphere("BannerSign", t, new Vector3(2.2f, 2.1f, 3.82f), new Vector3(0.35f, 0.35f, 0.02f), new Color(1f, 0.8f, 0.2f)), new Color(0.6f, 0.4f, 0.05f));
            switch (type)
            {
                case 0: // ворота
                    for (int s = -1; s <= 1; s += 2) Prim.Part(PrimitiveType.Cube, "GateTower", t, new Vector3(s * 1.7f, 1.5f, -3.4f), new Vector3(0.7f, 3f, 0.7f), b.Wall * 1.2f);
                    break;
                case 1: // оружейная — стойки с оружием
                    for (int i = 0; i < 3; i++)
                    {
                        Prim.Part(PrimitiveType.Cube, "Rack", t, new Vector3(-3.4f, 0.7f, -1.5f + i * 1.3f), new Vector3(0.3f, 1.4f, 1.0f), new Color(0.4f, 0.28f, 0.15f));
                        Prim.Part(PrimitiveType.Cube, "Spear", t, new Vector3(-3.2f, 1.1f, -1.5f + i * 1.3f), new Vector3(0.04f, 1.9f, 0.04f), new Color(0.75f, 0.75f, 0.8f));
                    }
                    break;
                case 2: // казарма — койки
                    for (int i = 0; i < 3; i++) Prim.Part(PrimitiveType.Cube, "Bunk", t, new Vector3(3f, 0.3f, -2.5f + i * 1.7f), new Vector3(1.0f, 0.3f, 1.6f), new Color(0.45f, 0.35f, 0.25f));
                    break;
                case 3: // коридор знамён
                    for (int i = -1; i <= 1; i++) Prim.Part(PrimitiveType.Cube, "Banner", t, new Vector3(-3.85f, 2.0f, i * 2.2f), new Vector3(0.03f, 1.4f, 0.8f), new Color(0.15f, 0.2f, 0.6f));
                    break;
                case 4: // сокровищница — монеты
                    for (int i = 0; i < 12; i++) Glow(Prim.Cyl("Coin", t, R(2.5f, 2.5f) + Vector3.up * 0.02f, new Vector3(0.18f, 0.03f, 0.18f), new Color(1f, 0.8f, 0.2f)), new Color(0.5f, 0.35f, 0f));
                    break;
                case 5: // кладовая — бочки и мешки
                    for (int i = 0; i < 4; i++) Prim.Cyl("Barrel", t, new Vector3(3.2f, 0.45f, -2.6f + i * 0.9f), new Vector3(0.7f, 0.9f, 0.7f), new Color(0.5f, 0.32f, 0.16f));
                    for (int i = 0; i < 3; i++) Prim.Sphere("Sack", t, new Vector3(-3.1f, 0.25f, -2f + i * 0.8f), new Vector3(0.6f, 0.5f, 0.5f), new Color(0.75f, 0.65f, 0.45f));
                    break;
                case 6: // часовня
                    Prim.Part(PrimitiveType.Cube, "Pew", t, new Vector3(-1.2f, 0.25f, -1f), new Vector3(1.8f, 0.5f, 0.4f), new Color(0.4f, 0.28f, 0.15f));
                    Prim.Part(PrimitiveType.Cube, "Pew", t, new Vector3(1.2f, 0.25f, -1f), new Vector3(1.8f, 0.5f, 0.4f), new Color(0.4f, 0.28f, 0.15f));
                    break;
                case 7: // золотые врата к логову
                    Arch(t, b);
                    Glow(Prim.Part(PrimitiveType.Cube, "GoldGate", t, new Vector3(0, 1.4f, 3.7f), new Vector3(2.0f, 2.8f, 0.1f), new Color(1f, 0.75f, 0.2f)), new Color(0.6f, 0.4f, 0.05f));
                    break;
            }
        }

        static void Camp(Transform t, Vector3 p)
        {
            Prim.Part(PrimitiveType.Cube, "Bedroll", t, p + new Vector3(0.8f, 0.05f, 0), new Vector3(0.7f, 0.1f, 1.6f), new Color(0.55f, 0.2f, 0.15f));
            for (int i = 0; i < 5; i++) Prim.Part(PrimitiveType.Cube, "Log", t, p + Quaternion.Euler(0, i * 72f, 0) * new Vector3(0.25f, 0.06f, 0), new Vector3(0.5f, 0.08f, 0.08f), new Color(0.35f, 0.22f, 0.1f), new Vector3(0, i * 72f, 0));
            var fire = Glow(Prim.Sphere("Campfire", t, p + Vector3.up * 0.15f, new Vector3(0.3f, 0.35f, 0.3f), new Color(1f, 0.55f, 0.15f)), new Color(1f, 0.45f, 0.1f) * 3f);
            fire.AddComponent<Flicker>().Init(Prim.PointLight(fire.transform, Vector3.up * 0.3f, new Color(1f, 0.6f, 0.3f), 6f, 1.3f));
        }

        static void Arch(Transform t, Biome b)
        {
            for (int s = -1; s <= 1; s += 2) Prim.Cyl("ArchPost", t, new Vector3(s * 1.2f, 1.3f, 2.4f), new Vector3(0.35f, 2.6f, 0.35f), b.Wall * 1.25f);
            Prim.Part(PrimitiveType.Cube, "ArchTop", t, new Vector3(0, 2.7f, 2.4f), new Vector3(2.8f, 0.35f, 0.4f), b.Wall * 1.25f);
            Glow(Prim.Sphere("ArchGem", t, new Vector3(0, 2.7f, 2.18f), Vector3.one * 0.2f, b.Accent), b.Accent * 2f);
        }
    }
}
