#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
namespace Podz
{
    /// <summary>Editor description of the SAME source layout. No scene objects, no replacement AI or live spawner.</summary>
    public sealed class DungeonSourcePlan
    {
        public readonly List<Bounds> Rooms = new List<Bounds>();
        public readonly List<int> RoomTypes = new List<int>();
        public readonly List<Vector2Int> Edges = new List<Vector2Int>();
        public int Floor;
        public static DungeonSourcePlan Read(int floor,int seed,int requestedRooms=0)
        {
            var state=UnityEngine.Random.state;
            try {
                if(floor==3 && requestedRooms!=0 && (requestedRooms<6 || requestedRooms>9))throw new InvalidOperationException("Крепость: число комнат 0 (как в игре) или 6–9. Этажи1/2 сохраняют восемь исходных комнат.");
                UnityEngine.Random.InitState(seed); var p=new DungeonSourcePlan {Floor=floor};
                if(floor==1)UpperCaves.DescribeBlocksForEditor(p);
                else if(floor==2)OvergrownRuins.DescribeBlocksForEditor(p);
                else if(floor==3)FloorGenerator.DescribeFortressForEditor(p,requestedRooms);
                else if(floor==4){p.Rooms.Add(new Bounds(Vector3.up*6,new Vector3(36,12,36)));p.RoomTypes.Add(0);}
                else throw new ArgumentOutOfRangeException(nameof(floor));
                return p;
            } finally {UnityEngine.Random.state=state;}
        }
        public List<Vector3> Exits(int index)
        {
            var list=new List<Vector3>();
            foreach(var e in Edges)if(e.x==index || e.y==index)
                list.Add((Rooms[e.x==index?e.y:e.x].center-Rooms[index].center).normalized);
            return list;
        }
        public float Length(int edge)
        {
            var e=Edges[edge];var a=Rooms[e.x];var b=Rooms[e.y];var d=b.center-a.center;
            return d.magnitude-(Mathf.Abs(d.x)>.1f?a.extents.x+b.extents.x:a.extents.z+b.extents.z)-(Floor==3?FloorGenerator.Thick*2:0);
        }
        public Vector3 PassageCenter(int edge)
        {
            var e=Edges[edge];var a=Rooms[e.x];var b=Rooms[e.y];var dir=(b.center-a.center).normalized;
            float ah=Mathf.Abs(dir.x)>.5f?a.extents.x:a.extents.z,bh=Mathf.Abs(dir.x)>.5f?b.extents.x:b.extents.z;
            var p=(a.center+dir*ah+b.center-dir*bh)*.5f;p.y=0;return p;
        }
        public string Key(bool corridor,int index)
        {
            if(corridor)return "F"+Floor+"_Passage_"+Length(index).ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+"_"+index;
            int mask=0;foreach(var d in Exits(index))mask|=d.z>.5f?1:d.x>.5f?2:d.z<-.5f?4:8;
            var s=Rooms[index].size;
            return "F"+Floor+"_Room_"+index+"_"+RoomTypes[index]+"_"+s.x+"x"+s.z+"_Ports"+mask;
        }
    }
}
#endif
