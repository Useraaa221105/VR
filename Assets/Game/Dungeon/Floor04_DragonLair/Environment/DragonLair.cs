using System.Collections.Generic;
using UnityEngine;

namespace Podz
{
    /// <summary>Dimensions local to the lair; never used by floors -1/-2.</summary>
    public static class DragonLair
    {
        public const float InteriorSize = 36f, WallHeight = 12f, CoverWidth = 3.2f, CoverDepth = 2.8f, CoverHeight = 3.8f;
        public static readonly Vector3[] CoverPoints = {
            new Vector3(-10,0,-4), new Vector3(10,0,-4), new Vector3(-10,0,5), new Vector3(10,0,5) };
        public static readonly Vector3[] GroundPoints = {
            new Vector3(0,0,-3), new Vector3(-1,0,6), new Vector3(1,0,6) };
        public static readonly Vector3[] Perches = {
            new Vector3(-10,6,10), new Vector3(10,6,10), new Vector3(0,6,11) };
        public static void Build(Transform root)
        {
            var stone = new Color(.43f, .42f, .38f);
            float outer = InteriorSize + .8f, edge = InteriorSize * .5f + .2f;
            Prim.Cube("Floor", root, new Vector3(0,-.1f,0), new Vector3(outer,.2f,outer), stone*.8f);
            Prim.Cube("WallN", root, new Vector3(0,WallHeight*.5f,edge), new Vector3(outer,WallHeight,.4f), stone);
            Prim.Cube("WallS", root, new Vector3(0,WallHeight*.5f,-edge), new Vector3(outer,WallHeight,.4f), stone);
            Prim.Cube("WallE", root, new Vector3(edge,WallHeight*.5f,0), new Vector3(.4f,WallHeight,outer), stone);
            Prim.Cube("WallW", root, new Vector3(-edge,WallHeight*.5f,0), new Vector3(.4f,WallHeight,outer), stone);
            DungeonArchitecture.Vault(root, outer, outer, WallHeight, 4);
            var details = ReferenceMesh.Node("LairStaticArchitecture", root, Vector3.zero);
            for (int s=-1; s<=1; s+=2) for (int j=-1; j<=1; j++)
            {
                var t=ReferenceMesh.Node("MassiveWallColumn",details,new Vector3(s*17.25f,0,j*10));
                ReferenceMesh.Shell("StoneWallColumn",t,Vector3.zero,
                    new[]{0f,.3f,.5f,11.3f,11.5f,12f},
                    new[]{.7f,.7f,.45f,.45f,.7f,.7f},new[]{.7f,.7f,.45f,.45f,.7f,.7f},stone,"stone",12);
                ReferenceMesh.Box("StoneWallColumnCap",t,new Vector3(0,11.7f,0),new Vector3(1.5f,.35f,1.5f),stone,"stone");
            }
            foreach (var p in Perches)
            {
                // Platform faces inward and leaves the centre of the arena unobstructed.
                var t=ReferenceMesh.Node("SixMetrePerch",details,p);
                ReferenceMesh.Box("StonePerchPlatform",t,new Vector3(0,-.3f,0),new Vector3(4.6f,.6f,3.2f),stone,"stone");
                ReferenceMesh.Tube("StonePerchCorbel",t,new[]{new Vector3(0,-5.8f,0),new Vector3(0,-1.5f,0),new Vector3(0,-.55f,0)},new[]{.25f,.45f,1.3f},stone,"stone");
            }
            // A narrow perimeter fissure, no red wash across the play space.
            for (int s=-1;s<=1;s+=2)
            {
                var fissure=ReferenceMesh.Box("PeripheralEmberSeam",details,new Vector3(s*17.5f,.011f,0),new Vector3(.08f,.015f,22),new Color(.40f,.16f,.035f),"stone");
                Prim.SetEmission(fissure.GetComponent<Renderer>(),new Color(.13f,.04f,.005f));
                Prim.PointLight(root,new Vector3(s*10,7,-4),new Color(.92f,.80f,.63f),22,1.1f);
            }
            Prim.PointLight(root,new Vector3(0,10,9),new Color(.69f,.76f,.79f),25,1.0f);
            var gate=ReferenceMesh.Node("ClosedEntranceGate",details,new Vector3(0,0,-17.95f));
            for(int j=-5;j<=5;j++) ReferenceMesh.Box("GateIronBar",gate,new Vector3(j*.3f,2,0),new Vector3(.075f,4,.1f),new Color(.18f,.17f,.15f),"iron");
            ReferenceMesh.Box("GateCrossbeam",gate,new Vector3(0,2.5f,0),new Vector3(3.4f,.12f,.12f),stone,"iron");
            ReferenceMesh.CombineStatic(details,"LairBatchedArchitecture");
            // Dedicated physical perches remain after render batching.
            foreach(var p in Perches)
            {
                var t=ReferenceMesh.Node("PerchCollision",root,p);var c=t.gameObject.AddComponent<BoxCollider>();
                c.center=new Vector3(0,-.3f,0);c.size=new Vector3(4.6f,.6f,3.2f);
            }
            foreach(var p in CoverPoints) FireCover.Create(root,p);
            DungeonArchitecture.SurfaceAll(root,4);
        }
    }
    /// <summary>Same convex mesh for visible boulder and physical occlusion. Three intercepted fireballs.</summary>

}