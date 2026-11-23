using System.Collections.Generic;
using UnityEngine;

namespace Podz
{
    public class FloorLayout
    {
        public readonly List<Bounds> Rooms = new List<Bounds>();
        public readonly List<Vector2Int> Cells = new List<Vector2Int>();
        public readonly List<int> Depth = new List<int>();
        public int Start, Stairs;
        public Vector3 Origin, TransitionEntrance, TransitionForward;
    }

    /// <summary>
    /// Генератор этажа: сетка 4×4, 6–9 комнат случайным обходом (дерево → все комнаты связаны),
    /// коридоры с проёмами, стены 3,5 м, потолки. Самая дальняя комната — лестница, стартовая — портал.
    /// </summary>
    public static class FloorGenerator
    {
        public const int Grid = 4;
        public const float Cell = 14f, RoomSize = 8f, WallH = 3.5f, Door = 2.4f, Thick = 0.3f;
        static readonly Vector2Int[] Dirs = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        public static FloorLayout Build(Transform root, Vector3 origin, Biome b)
        {
            if (b.Level == 1) return UpperCaves.Build(root, origin);
            if (b.Level == 2) return OvergrownRuins.Build(root, origin);
            var L = PlanFortress(origin, b.Level, out var links);
            var cells = L.Cells; int best = L.Stairs;

            for (int i = 0; i < cells.Count; i++)
            {
                var c = cells[i];
                var size = L.Rooms[i].size;
                int openings = 0;
                for (int side = 0; side < Dirs.Length; side++)
                    if (links.Contains((c, c + Dirs[side]))) openings |= 1 << side;

                var prefab = SavedDungeonAssets.Active?.FindFortressRoom(new Vector2(size.x, size.z), openings);
                if (prefab != null)
                {
                    var instance = Object.Instantiate(prefab, CellCenter(origin, c), Quaternion.identity, root);
                    instance.name = $"Room_{c.x}_{c.y}";
                    continue;
                }
                var room = new GameObject($"Room_{c.x}_{c.y}").transform;
                room.SetParent(root, false);
                room.position = CellCenter(origin, c);
                BuildRoom(room, b, links, c, L.Rooms[i].size.x, L.Rooms[i].size.z);
            }
            foreach (var l in links)
            {
                if (l.Item1.x > l.Item2.x || l.Item1.y > l.Item2.y) continue; // каждый коридор один раз
                BuildCorridor(root, origin, l.Item1, l.Item2, b, L);
            }
            var tc=cells[best];
            foreach(var d in Dirs) if(links.Contains((tc,tc+d))) {
                L.TransitionForward=new Vector3(-d.x,0,-d.y);
                var rb=L.Rooms[best];float half=d.x!=0?rb.extents.x:rb.extents.z;
                L.TransitionEntrance=CellCenter(origin,tc)-L.TransitionForward*(half+Thick*.5f);break;
            }
            return L;
        }

        static FloorLayout PlanFortress(Vector3 origin, int level, out HashSet<(Vector2Int, Vector2Int)> links, int requestedRooms = 0)
        {
            float height = FortressReference.HeightFor(level);
            var L = new FloorLayout { Origin = origin };
            int target = Random.Range(6, 10);
            if (requestedRooms != 0) target = requestedRooms; // editor opt-in, runtime always default0; consume the same original random draw
            var cells = new List<Vector2Int>();
            links = new HashSet<(Vector2Int, Vector2Int)>();
            var start = new Vector2Int(Random.Range(0, Grid), 0);
            cells.Add(start);
            int guard = 0;
            while (cells.Count < target && guard++ < 1000)
            {
                var from = cells[Random.Range(0, cells.Count)];
                var to = from + Dirs[Random.Range(0, 4)];
                if (to.x < 0 || to.y < 0 || to.x >= Grid || to.y >= Grid || cells.Contains(to)) continue;
                cells.Add(to);
                links.Add((from, to)); links.Add((to, from));
            }
            // BFS — глубина от старта, самая дальняя комната = лестница
            var depth = new Dictionary<Vector2Int, int> { [start] = 0 };
            var q = new Queue<Vector2Int>(); q.Enqueue(start);
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                foreach (var d in Dirs)
                {
                    var n = c + d;
                    if (links.Contains((c, n)) && !depth.ContainsKey(n)) { depth[n] = depth[c] + 1; q.Enqueue(n); }
                }
            }
            int best = 0;
            for (int i = 0; i < cells.Count; i++)
            {
                L.Cells.Add(cells[i]);
                L.Depth.Add(depth[cells[i]]);
                var center = CellCenter(origin, cells[i]);
                float w=i%3==1?12f:8f, d=i%3==2?11f:8f;
                L.Rooms.Add(new Bounds(center + Vector3.up * height * 0.5f, new Vector3(w, height, d)));
                if (depth[cells[i]] > depth[cells[best]]) best = i;
            }
            L.Start = 0; L.Stairs = best;

            return L;
        }
#if UNITY_EDITOR
        public static void DescribeFortressForEditor(DungeonSourcePlan plan, int requestedRooms)
        {
            var layout = PlanFortress(Vector3.zero, 3, out var links, requestedRooms);
            plan.Rooms.AddRange(layout.Rooms);
            int order=0;
            for(int i=0;i<layout.Rooms.Count;i++) {
                plan.RoomTypes.Add(RoomTypes.Assign(i,layout.Start,layout.Stairs,order));
                if(i!=layout.Start && i!=layout.Stairs)order++;
            }
            foreach(var edge in links)
                if(edge.Item1.x<edge.Item2.x || (edge.Item1.x==edge.Item2.x && edge.Item1.y<edge.Item2.y))
                    plan.Edges.Add(new Vector2Int(layout.Cells.IndexOf(edge.Item1),layout.Cells.IndexOf(edge.Item2)));
        }
        public static void BuildFortressRoomForEditor(Transform root, DungeonSourcePlan plan, int index)
        {
            var links=new HashSet<(Vector2Int,Vector2Int)>();var cell=Vector2Int.zero;
            foreach(var exit in plan.Exits(index)) {var d=new Vector2Int(Mathf.RoundToInt(exit.x),Mathf.RoundToInt(exit.z));links.Add((cell,d));}
            var size=plan.Rooms[index].size; var b=DungeonManager.GetBiome(3);
            BuildRoom(root,b,links,cell,size.x,size.z);
            RoomDecor.Build(root,3,plan.RoomTypes[index],root.position,b,size);
            DungeonArchitecture.SurfaceAll(root,3);
        }
        public static void BuildFortressShellForEditor(Transform root, Vector2 size, int openings)
        {
            var links = new HashSet<(Vector2Int, Vector2Int)>();
            for (int side = 0; side < Dirs.Length; side++)
                if ((openings & (1 << side)) != 0) links.Add((Vector2Int.zero, Dirs[side]));
            BuildRoom(root, DungeonManager.GetBiome(3), links, Vector2Int.zero, size.x, size.y);
            DungeonArchitecture.SurfaceAll(root, 3);
        }

        public static void BuildFortressPassageForEditor(Transform root,float length)
        {
            // Reuse the old corridor builder with two virtual adjacent rooms: their half extents determine EXACT length.
            var layout=new FloorLayout();layout.Cells.Add(Vector2Int.zero);layout.Cells.Add(Vector2Int.up);
            float half=(Cell-length)*.5f-Thick;
            layout.Rooms.Add(new Bounds(Vector3.zero,new Vector3(8,4,half*2)));
            layout.Rooms.Add(new Bounds(Vector3.forward*Cell,new Vector3(8,4,half*2)));
            BuildCorridor(root,Vector3.back*(Cell*.5f),Vector2Int.zero,Vector2Int.up,DungeonManager.GetBiome(3),layout, false);
            DungeonArchitecture.SurfaceAll(root,3);
        }
#endif

        public static Vector3 CellCenter(Vector3 origin, Vector2Int c) => origin + new Vector3(c.x * Cell, 0, c.y * Cell);

        static void BuildRoom(Transform room, Biome b, HashSet<(Vector2Int, Vector2Int)> links, Vector2Int c, float width, float depthSize)
        {
            float height = FortressReference.HeightFor(b.Level);
            Prim.Cube("Floor", room, new Vector3(0, -0.1f, 0), new Vector3(width + Thick * 2, 0.2f, depthSize + Thick * 2), b.Floor);
            DungeonArchitecture.Vault(room,width+Thick*2,depthSize+Thick*2,height,b.Level);
            for (int s = 0; s < 4; s++)
            {
                var d = Dirs[s];
                bool open = links.Contains((c, c + d));
                var normal = new Vector3(d.x, 0, d.y);
                var along = new Vector3(Mathf.Abs(d.y), 0, Mathf.Abs(d.x));
                float sideHalf=d.x!=0?width*.5f:depthSize*.5f;
                var center = normal * (sideHalf + Thick * 0.5f) + Vector3.up * height * 0.5f;
                float len = (d.x!=0?depthSize:width) + Thick * 2;
                if (!open) Wall(room, center, along, len, b.Wall, height);
                else
                {
                    float seg = (len - Door) * 0.5f;
                    Wall(room, center + along * (Door * 0.5f + seg * 0.5f), along, seg, b.Wall, height);
                    Wall(room, center - along * (Door * 0.5f + seg * 0.5f), along, seg, b.Wall, height);
                    Prim.Cube("Lintel", room, center + Vector3.up * (height * 0.5f - 0.3f), Size(along, Door, 0.6f), b.Wall * 0.9f);
                }
            }
            DungeonArchitecture.RoomEdges(room,width,depthSize,b.Level);
            // освещение биома: главный цветной свет + акценты
            // исправление 5: главный свет ярче, дальше и теплее (смесь цвета биома с тёплым)
            Prim.PointLight(room, new Vector3(0, height - 0.4f, 0), Color.Lerp(b.Light, new Color(1f, 0.82f, 0.6f), 0.35f), 14f, b.Level == 3 ? .85f : b.LightIntensity * 1.6f);
            // + два тёплых мерцающих факела на боковых стенах (на −3 факелы уже есть в декоре крепости)
            if (b.Level <= 2)
                for (int s = 1; s <= 3; s += 2)
                {
                    var n = new Vector3(Dirs[s].x, 0, Dirs[s].y);
                    var along = new Vector3(Mathf.Abs(Dirs[s].y), 0, Mathf.Abs(Dirs[s].x));
                    GlowDecor.Torch(room, n * (width*.5f - 0.12f) + along * (s == 1 ? 2.8f : -2.8f) + Vector3.up * 2.0f, n);
                }
        }

        static Vector3 Size(Vector3 along, float len, float height)
        {
            return new Vector3(along.x > 0.5f ? len : Thick, height, along.z > 0.5f ? len : Thick);
        }

        static void Wall(Transform parent, Vector3 localCenter, Vector3 along, float len, Color c, float height)
        {
            Prim.Cube("Wall", parent, localCenter, Size(along, len, height), c);
        }

        static void BuildCorridor(Transform root, Vector3 origin, Vector2Int a, Vector2Int b2, Biome b, FloorLayout layout, bool useSaved = true)
        {
            float height = FortressReference.HeightFor(b.Level);
            var pa = CellCenter(origin, a); var pb = CellCenter(origin, b2);
            int ai=layout.Cells.IndexOf(a), bi=layout.Cells.IndexOf(b2);
            bool xAxis=a.x!=b2.x;
            float ah=(xAxis?layout.Rooms[ai].size.x:layout.Rooms[ai].size.z)*.5f+Thick;
            float bh=(xAxis?layout.Rooms[bi].size.x:layout.Rooms[bi].size.z)*.5f+Thick;
            var axis=(pb-pa).normalized;
            var mid = (pa+axis*ah+pb-axis*bh) * .5f;
            var dir = (pb - pa).normalized;
            float len = Cell - ah - bh;
            string name = $"Corridor_{a.x}{a.y}_{b2.x}{b2.y}";
            var prefab = useSaved ? SavedDungeonAssets.Active?.FindFortressPassage(len) : null;
            if (prefab != null)
            {
                Object.Instantiate(prefab, mid, Quaternion.LookRotation(dir, Vector3.up), root).name = name;
                return;
            }
            var t = new GameObject(name).transform;
            t.SetParent(root, false);
            t.position = mid;
            t.rotation = Quaternion.LookRotation(dir, Vector3.up);
            float hw = Door * 0.5f + Thick * 0.5f;
            Prim.Cube("Floor", t, new Vector3(0, -0.1f, 0), new Vector3(Door + Thick * 2, 0.2f, len + 0.1f), b.Floor * 0.9f);
            DungeonArchitecture.Vault(t,Door+Thick*2,len+.1f,height,b.Level);
            DungeonArchitecture.Arch(t,Vector3.zero,Door,b.Level);
            Prim.Cube("WallL", t, new Vector3(-hw, height * 0.5f, 0), new Vector3(Thick, height, len + 0.1f), b.Wall);
            Prim.Cube("WallR", t, new Vector3(hw, height * 0.5f, 0), new Vector3(Thick, height, len + 0.1f), b.Wall);
        }
    }
}
