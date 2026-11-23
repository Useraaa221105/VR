using System.Collections.Generic;
using UnityEngine;
namespace Podz {
    /// <summary>Original low-poly stone architecture, generated locally. No third-party textures or models.</summary>
    public static class DungeonArchitecture {
        static readonly Dictionary<int,Material> Stone = new Dictionary<int,Material>();
        static Material StoneMaterial(int level) {
            if(Stone.TryGetValue(level,out var existing) && existing!=null) return existing;
            var tex=new Texture2D(256,256,TextureFormat.RGB24,true);tex.name="OriginalStoneTile";tex.wrapMode=TextureWrapMode.Repeat;
            for(int y=0;y<256;y++) for(int x=0;x<256;x++) {
                int row=y/64, xx=(x+((row&1)*64))%128;
                bool seam=y%64<3 || xx<3;
                float noise=Mathf.PerlinNoise(x*.085f,y*.085f)*.12f;
                float v=seam?.26f:.63f+noise;
                tex.SetPixel(x,y,new Color(v,v*.96f,v*.89f));
            }
            tex.Apply();var mat=new Material(GameAssets.I.lit);mat.name="OriginalStone_L"+level;
            mat.SetTexture("_BaseMap",tex);mat.SetTexture("_MainTex",tex);mat.SetFloat("_Smoothness",level==1?.24f:.08f);
            Stone[level]=mat;return mat;
        }
        public static void SurfaceAll(Transform root,int level) {
            foreach(var r in root.GetComponentsInChildren<MeshRenderer>()) {
                if (level == 3 && (r.GetComponentInParent<FortressGeometryBlock>() != null ||
                    IsSavedDecor(r.transform) || r.GetComponentInParent<TransitionDoor>()?.UsesSavedAsset == true)) continue;
                string n=r.name;
                bool surface=n.StartsWith("Wall") || n=="Floor" || n=="Ceiling" || n.StartsWith("Vault") || n.StartsWith("Arch") || n.StartsWith("Step_") || n=="FlatUpperPlatform" || n=="Pillar" || n.StartsWith("Stone");
                if(!surface) continue;
                r.sharedMaterial=StoneMaterial(level);
                Color c=level==1?new Color(.48f,.48f,.43f):level==2?new Color(.58f,.55f,.44f):new Color(.51f,.48f,.44f);
                if(n=="Floor")c*=.8f;
                Prim.SetColor(r,c);
                // Tile each face by physical dimensions, not one stretched wall texture.
                var mf=r.GetComponent<MeshFilter>();if(mf==null || mf.sharedMesh==null || n.StartsWith("Vault"))continue;
                var mesh=Object.Instantiate(mf.sharedMesh);var uv=mesh.uv;var normals=mesh.normals;var scale=r.transform.lossyScale;
                if(uv.Length!=normals.Length)continue;
                for(int i=0;i<uv.Length;i++) {
                    var a=normals[i];float u=Mathf.Abs(a.x)>.5f?scale.z:scale.x;
                    float v=Mathf.Abs(a.y)>.5f?scale.z:scale.y;
                    uv[i]=Vector2.Scale(uv[i],new Vector2(Mathf.Abs(u)*.8f,Mathf.Abs(v)*.8f));
                }
                mesh.uv=uv;mf.sharedMesh=mesh;
                var owner=r.gameObject.AddComponent<ArchitectureMeshOwner>();owner.Mesh=mesh;
            }
        }
        static bool IsSavedDecor(Transform part)
        {
            for (var parent = part.parent; parent != null; parent = parent.parent)
                if (parent.name == "SavedDecor") return true;
            return false;
        }

        public static void Vault(Transform parent,float width,float length,float baseHeight,int level) {
            // Explicit inward normals: opposite triangles sharing vertices used to cancel lighting normals.
            // End caps seal the space above the room walls. Original locally generated mesh.
            float half=width*.5f, rise=level==1?1.15f:width*.18f;
            var vs=new List<Vector3>();var ns=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>();const int segments=16;
            for(int i=0;i<=segments;i++) {
                float a=Mathf.PI*i/segments,x=-half*Mathf.Cos(a),y=baseHeight+Mathf.Sin(a)*rise;
                Vector3 normal=new Vector3(rise*Mathf.Cos(a),-half*Mathf.Sin(a),0).normalized;
                vs.Add(new Vector3(x,y,-length*.5f));vs.Add(new Vector3(x,y,length*.5f));ns.Add(normal);ns.Add(normal);
                uv.Add(new Vector2(width*i/segments*.8f,0));uv.Add(new Vector2(width*i/segments*.8f,length*.8f));
                if(i<segments){int k=i*2;tr.AddRange(new[]{k+2,k+1,k,k+2,k+3,k+1});}
            }
            for(int side=-1;side<=1;side+=2) {
                int center=vs.Count;var normal=Vector3.back*side;
                vs.Add(new Vector3(0,baseHeight,side*length*.5f));ns.Add(normal);uv.Add(new Vector2(half*.8f,0));
                for(int i=0;i<=segments;i++) {
                    float a=Mathf.PI*i/segments,x=-half*Mathf.Cos(a),y=Mathf.Sin(a)*rise;
                    vs.Add(new Vector3(x,baseHeight+y,side*length*.5f));ns.Add(normal);uv.Add(new Vector2((x+half)*.8f,y*.8f));
                    if(i>0) {if(side>0)tr.AddRange(new[]{center,center+i,center+i+1});else tr.AddRange(new[]{center,center+i+1,center+i});}
                }
            }
            var mesh=new Mesh {name="OriginalClosedVault"};mesh.SetVertices(vs);mesh.SetNormals(ns);mesh.SetUVs(0,uv);mesh.SetTriangles(tr,0);mesh.RecalculateBounds();
            var go=new GameObject("VaultStone");go.transform.SetParent(parent,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=StoneMaterial(level);
            go.AddComponent<MeshCollider>().sharedMesh=mesh;go.AddComponent<ArchitectureMeshOwner>().Mesh=mesh;
        }
        public static void Arch(Transform t,Vector3 center,float opening,int level) {
            for(int i=0;i<=10;i++) {
                float a=Mathf.PI*i/10f;
                var stone=Prim.Cube("ArchStone",t,center+new Vector3(Mathf.Cos(a)*(opening*.5f+.2f),2.3f+Mathf.Sin(a)*.8f,0),new Vector3(.38f,.35f,.4f),Color.gray,false);
                stone.transform.localRotation=Quaternion.Euler(0,0,a*Mathf.Rad2Deg-90f);
            }
            for(int side=-1;side<=1;side+=2)
                Prim.Cube("ArchSupport",t,center+new Vector3(side*(opening*.5f+.2f),1.1f,0),new Vector3(.3f,2.2f,.4f),Color.gray,false);
        }
        public static void RoomEdges(Transform room,float w,float d,int level) {
            // Clipped corners replace the identical rectangular silhouette, without blocking doorways or central routes.
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2) {
                if(level==1) {
                    var rock=Prim.Sphere("StoneCorner",room,new Vector3(x*(w*.5f-.4f),.9f,z*(d*.5f-.4f)),new Vector3(1.6f,2f,1.6f),new Color(.38f,.4f,.36f));
                    rock.AddComponent<BoxCollider>().size=new Vector3(.55f,.8f,.55f);
                } else {
                    var cut=Prim.Cube("StoneCorner",room,new Vector3(x*(w*.5f-.45f),1.75f,z*(d*.5f-.45f)),new Vector3(1.55f,3.5f,.3f),Color.gray);
                    cut.transform.localRotation=Quaternion.Euler(0,x*z*45f,0);
                    if(level==2) {
                        Prim.Cyl("MossRoot",room,new Vector3(x*(w*.5f-.7f),1f,z*(d*.5f-.7f)),new Vector3(.09f,2.3f,.09f),new Color(.19f,.23f,.11f),new Vector3(0,0,12*x));
                    }
                }
            }
            if(level==1) for(int i=0;i<2;i++) {
                var puddle=Prim.Cyl("ShallowPuddle",room,new Vector3(-w*.32f+i*.65f,.005f,d*.3f),new Vector3(1.2f,.008f,.7f),new Color(.15f,.22f,.22f));
            }
            if(level>=2) {
                var pillar=Prim.Shape(PrimitiveType.Cylinder,"Pillar",room,new Vector3(-w*.5f+.7f,1.4f,0),new Vector3(.65f,1.4f,.65f),Color.gray);
                Prim.Cube("StonePillarBase",room,new Vector3(-w*.5f+.7f,.15f,0),new Vector3(.95f,.3f,.95f),Color.gray);
            }
        }
    }

}
