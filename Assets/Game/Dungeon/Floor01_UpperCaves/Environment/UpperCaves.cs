using System.Collections.Generic;
using UnityEngine;
namespace Podz {
    /// <summary>Только игровой этаж −1. Восемь связанных пещер; другие биомы не используют этот строитель.</summary>
    public static class UpperCaves {
        public static readonly string[] Names={"Стартовая пещера","Зал луж","Грибная роща","Корневой лабиринт","Затопленный коридор","Пещера сталактитов","Лагерь приключенцев","Комната спуска"};
        public static readonly int[] Types={0,1,3,2,5,4,6,7};
        static readonly Vector2Int[] Cells={new Vector2Int(0,0),new Vector2Int(0,1),new Vector2Int(1,1),new Vector2Int(1,2),new Vector2Int(0,2),new Vector2Int(-1,2),new Vector2Int(-1,1),new Vector2Int(-1,0)};
        static readonly Vector2[] Sizes={new Vector2(11,9),new Vector2(12,11),new Vector2(13,12),new Vector2(13,11),new Vector2(8,13),new Vector2(11,12),new Vector2(12,10),new Vector2(11,10)};
        static readonly int[,] Links={{0,1},{1,2},{2,3},{1,4},{4,5},{5,6},{6,7}};
        static Material _plants,_water,_roots;static FloorLayout _current;static readonly List<Vector3> Reserved=new List<Vector3>();
        public static Material Water {get{if(_water==null){_water=new Material(Shader.Find("Universal Render Pipeline/Lit"));_water.name="UpperCavesShallowWater";_water.SetColor("_BaseColor",new Color(.14f,.3f,.32f));_water.SetFloat("_Smoothness",.52f);_water.SetFloat("_Metallic",0);_water.SetFloat("_EnvironmentReflections",0);_water.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");_water.EnableKeyword("_EMISSION");_water.SetColor("_EmissionColor",new Color(.022f,.055f,.06f));}return _water;}}
        public static void Reserve(Vector3 p){Reserved.Add(p);}
        public static bool Free(Vector3 p,int ri,float margin=.75f) {
            if(_current==null || ri<0 || ri>=8)return false;var r=_current.Rooms[ri];var v=p-r.center;float x=Mathf.Abs(v.x),z=Mathf.Abs(v.z);
            if(x>r.extents.x-margin || z>r.extents.z-margin)return false;
            if(x>r.extents.x-1.65f && z>r.extents.z-1.65f)return false;
            if(ri==7 && Vector3.Dot(new Vector3(v.x,0,v.z),_current.TransitionForward)>-.3f && Mathf.Abs(Vector3.Dot(new Vector3(v.x,0,v.z),Vector3.Cross(Vector3.up,_current.TransitionForward)))<2.1f)return false;
            foreach(var q in Reserved){var d=q-p;d.y=0;if(d.sqrMagnitude<4.84f)return false;}
            return true;
        }
        static bool Clear(Vector3 p,int ri,float radius=.45f) => Free(p,ri) && !Physics.CheckSphere(p+Vector3.up*(radius+.10f),radius,~0,QueryTriggerInteraction.Ignore);
        public static Vector3 Point(int ri) {
            var r=_current.Rooms[ri];
            for(int k=0;k<50;k++){var p=new Vector3(Random.Range(r.min.x+1.2f,r.max.x-1.2f),_current.Origin.y,Random.Range(r.min.z+1.2f,r.max.z-1.2f));if(Clear(p,ri))return p;}
            for(float x=r.min.x+1.2f;x<r.max.x-1.2f;x+=.65f)for(float z=r.min.z+1.2f;z<r.max.z-1.2f;z+=.65f){var p=new Vector3(x,_current.Origin.y,z);if(Clear(p,ri))return p;}
            // Запасной путь — свободная стартовая пещера, а не портал/стена.
            var start=_current.Rooms[0];return new Vector3(start.center.x,_current.Origin.y,start.center.z);
        }
        public static Vector3 SafeDrop(Vector3 pos) {
            if(_current==null)return pos;int ri=0;float nearest=float.MaxValue;
            for(int i=0;i<8;i++){var d=pos-_current.Rooms[i].center;d.y=0;if(d.sqrMagnitude<nearest){nearest=d.sqrMagnitude;ri=i;}}
            var ground=new Vector3(pos.x,_current.Origin.y,pos.z);
            if(Clear(ground,ri,.18f))return pos;
            for(int ring=1;ring<=6;ring++)for(int j=0;j<12;j++){float a=j*Mathf.PI/6;var q=ground+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*(ring*.45f);if(Clear(q,ri,.18f)){q.y=pos.y;return q;}}
            var fallback=Point(ri);fallback.y=pos.y;return fallback;
        }
#if UNITY_EDITOR
        public static void DescribeBlocksForEditor(DungeonSourcePlan plan) {
            for(int i=0;i<8;i++){plan.Rooms.Add(new Bounds(new Vector3(Cells[i].x*16,2,Cells[i].y*16),new Vector3(Sizes[i].x,4,Sizes[i].y)));plan.RoomTypes.Add(Types[i]);}
            for(int k=0;k<7;k++){plan.Edges.Add(new Vector2Int(Links[k,0],Links[k,1]));}
        }
        public static void BuildRoomForEditor(Transform root,int index) {
            if(Application.isPlaying || index<0 || index>=8 || root.childCount!=0)throw new System.InvalidOperationException("Нужна пустая Editor-заготовка и индекс 0–7.");
            var exits=new List<Vector3>();
            for(int k=0;k<7;k++){int a=Links[k,0],b=Links[k,1];if(a==index || b==index){var d=Cells[a==index?b:a]-Cells[index];exits.Add(new Vector3(d.x,0,d.y).normalized);}}
            Shell(root,Sizes[index].x,Sizes[index].y,exits,index);Physics.SyncTransforms();Decor(root,index,Sizes[index]);
        }
        public static void BuildPassageForEditor(Transform root,float length,int sourceIndex) {
            if(Application.isPlaying || root.childCount!=0 || length<=0)throw new System.InvalidOperationException("Нужна пустая Editor-заготовка коридора.");
            Tunnel(root,Vector3.back*length*.5f,Vector3.forward*length*.5f,sourceIndex);
        }
#endif
        public static FloorLayout Build(Transform root,Vector3 origin) {
            Reserved.Clear();var L=new FloorLayout{Origin=origin,Start=0,Stairs=7};_current=L;
            for(int i=0;i<8;i++){var p=origin+new Vector3(Cells[i].x*16,0,Cells[i].y*16);L.Cells.Add(Cells[i]);L.Depth.Add(new[]{0,1,2,3,2,3,4,5}[i]);L.Rooms.Add(new Bounds(p+Vector3.up*2,new Vector3(Sizes[i].x,4,Sizes[i].y)));}
            L.TransitionForward=Vector3.back;L.TransitionEntrance=new Vector3(L.Rooms[7].center.x,origin.y,L.Rooms[7].max.z);
            if(SavedDungeonAssets.TryBuildFixed(SavedDungeonAssets.Active?.Caves,1,root,L,Links))return L;
            for(int i=0;i<8;i++) {
                var t=new GameObject("UpperCave_"+i+"_"+Names[i]).transform;t.SetParent(root,false);t.position=new Vector3(L.Rooms[i].center.x,origin.y,L.Rooms[i].center.z);
                var exits=new List<Vector3>();for(int k=0;k<7;k++){int a=Links[k,0],b=Links[k,1];if(a==i || b==i)exits.Add((L.Rooms[a==i?b:a].center-L.Rooms[i].center).normalized);}
                Shell(t,Sizes[i].x,Sizes[i].y,exits,i);Physics.SyncTransforms();Decor(t,i,Sizes[i]);
            }
            for(int k=0;k<7;k++){int a=Links[k,0],b=Links[k,1];var delta=L.Rooms[b].center-L.Rooms[a].center;delta.y=0;var dir=delta.normalized;float ah=Mathf.Abs(dir.x)>.5f?L.Rooms[a].extents.x:L.Rooms[a].extents.z,bh=Mathf.Abs(dir.x)>.5f?L.Rooms[b].extents.x:L.Rooms[b].extents.z;var p=L.Rooms[a].center+dir*ah;p.y=origin.y;var q=L.Rooms[b].center-dir*bh;q.y=origin.y;Tunnel(root,p,q,k);}
            Physics.SyncTransforms();return L;
        }
        static void Shell(Transform root,float width,float depth,List<Vector3> exits,int seed) {
            var edge=new List<Vector3>();var sides=new List<int>();float wx=width*.5f,dz=depth*.5f;
            for(int s=0;s<4;s++) {
                float half=s%2==0?wx:dz;var coords=new List<float>{-half+.72f,-half+1.3f,-2.4f,-1.4f,-.7f,0,.7f,1.4f,2.4f,half-1.3f,half-.72f};coords.Sort();
                foreach(float c in coords){Vector3 v=s==0?new Vector3(c,0,dz):s==1?new Vector3(wx,0,-c):s==2?new Vector3(-c,0,-dz):new Vector3(-wx,0,c);if(Mathf.Abs(c)>2.4f)v+=v.normalized*(.22f*Mathf.Sin(c*1.3f+seed+s));edge.Add(v);sides.Add(s);}
            }
            int n=edge.Count;var vs=new List<Vector3>{Vector3.zero};vs.AddRange(edge);var tr=new List<int>();for(int i=0;i<n;i++)tr.AddRange(new[]{0,i+1,(i+1)%n+1});CavePrototype.MeshObject("CaveStoneFloor",root,vs,tr,CavePrototype.Rock,true);
            var outer=new List<Vector3>();var wv=new List<Vector3>();var wt=new List<int>();
            for(int i=0;i<n;i++)outer.Add(edge[i]+Vector3.up*(3.6f+.19f*Mathf.Sin(i*.41f+seed)));
            Vector3[] normals={Vector3.forward,Vector3.right,Vector3.back,Vector3.left};
            for(int i=0;i<n;i++) {
                int j=(i+1)%n;int s=sides[i];bool opening=false;foreach(var e in exits)if(Vector3.Dot(e,normals[s])>.9f)opening=true;
                float ca=s%2==0?edge[i].x:edge[i].z,cb=s%2==0?edge[j].x:edge[j].z;
                bool door=opening && sides[j]==s && Mathf.Abs((ca+cb)*.5f)<1.4f;
                if(door) {int k=wv.Count;float ha=2.1f+.75f*Mathf.Sqrt(Mathf.Max(0,1-ca*ca/(1.4f*1.4f))),hb=2.1f+.75f*Mathf.Sqrt(Mathf.Max(0,1-cb*cb/(1.4f*1.4f)));wv.Add(edge[i]+Vector3.up*ha);wv.Add(edge[j]+Vector3.up*hb);wv.Add(outer[j]);wv.Add(outer[i]);wt.AddRange(new[]{k,k+2,k+1,k,k+3,k+2});}
                else for(int band=0;band<4;band++) {int k=wv.Count;float t0=band/4f,t1=(band+1)/4f;Vector3 A(float t,int id){var p=Vector3.Lerp(edge[id]-Vector3.up*.035f,outer[id],t);if(t>0 && t<1)p+=new Vector3(p.x,0,p.z).normalized*(.14f*Mathf.Sin(id*.73f+t*7f+seed)*Mathf.Sin(t*Mathf.PI));return p;}wv.Add(A(t0,i));wv.Add(A(t0,j));wv.Add(A(t1,j));wv.Add(A(t1,i));wt.AddRange(new[]{k,k+2,k+1,k,k+3,k+2});}
            }
            CavePrototype.MeshObject("OrganicRockWalls",root,wv,wt,CavePrototype.Rock,true,true);
            var cv=new List<Vector3>();var ct=new List<int>();for(int ring=0;ring<3;ring++)for(int i=0;i<n;i++){var p=outer[i];float f=ring==0?1:ring==1?.65f:.24f;p.x*=f;p.z*=f;if(ring>0)p.y=(ring==1?4.65f:5.4f)+.15f*Mathf.Sin(i*.4f+seed);cv.Add(p);}for(int ring=0;ring<2;ring++)for(int i=0;i<n;i++){int a=ring*n+i,b=ring*n+(i+1)%n;ct.AddRange(new[]{a,a+n,b,b,a+n,b+n});}int center=cv.Count;cv.Add(new Vector3(0,5.75f,0));for(int i=0;i<n;i++)ct.AddRange(new[]{center,2*n+(i+1)%n,2*n+i});CavePrototype.MeshObject("ContinuousCaveVault",root,cv,ct,CavePrototype.Rock,true);
            Prim.PointLight(root,new Vector3(-width*.5f+1.2f,1.6f,depth*.2f),new Color(.42f,.78f,.65f),9f,3.5f);Prim.PointLight(root,new Vector3(width*.5f-1.4f,1.7f,-depth*.15f),new Color(.35f,.65f,.73f),9f,3.2f);
            foreach(var e in exits)if(seed==1 || seed==6 || seed==7)Arch(root,e*(Mathf.Abs(e.x)>.5f?wx-.2f:dz-.2f),e);
        }
        static void Tunnel(Transform root,Vector3 p,Vector3 q,int seed) {
            var t=new GameObject("CavePassage_"+seed).transform;t.SetParent(root,true);t.position=(p+q)*.5f;t.rotation=Quaternion.LookRotation(q-p);float len=Vector3.Distance(p,q)+.16f;
            var v=new List<Vector3>();var tr=new List<int>();const int count=13;
            for(int r=0;r<2;r++)for(int j=0;j<count;j++){float x=j==0?-1.4f:j==count-1?1.4f:-1.4f+(j-1)*2.8f/10;float y=j==0||j==count-1?0:2.1f+.75f*Mathf.Sqrt(Mathf.Max(0,1-x*x/1.96f));v.Add(new Vector3(x,y+.015f,(r==0?-1:1)*len*.5f));}
            for(int j=0;j<count-1;j++)tr.AddRange(new[]{j,j+1,j+count,j+1,j+1+count,j+count});CavePrototype.MeshObject("NaturalPassageWallsAndRoof",t,v,tr,CavePrototype.Rock,true,true);
            var floor=Prim.Cube("PassageFloor",t,new Vector3(0,-.1f,0),new Vector3(2.86f,.2f,len),Color.white);floor.GetComponent<Renderer>().sharedMaterial=CavePrototype.Rock;floor.GetComponent<Renderer>().SetPropertyBlock(null);
            Prim.PointLight(t,new Vector3(0,1.9f,0),new Color(.4f,.73f,.68f),5f,2.5f);
        }
        static void Arch(Transform t,Vector3 p,Vector3 forward) {
            var a=new GameObject("MossyAncientArchFragment").transform;a.SetParent(t,false);a.localPosition=p;a.localRotation=Quaternion.LookRotation(forward);
            for(int s=-1;s<=1;s+=2)for(int j=0;j<4;j++)StoneBox(a,new Vector3(s*1.65f,j*.44f+.22f,0),new Vector3(.5f,.43f,.55f));
            for(int j=0;j<11;j++){float r=j*Mathf.PI/10;var g=StoneBox(a,new Vector3(Mathf.Cos(r)*1.65f,1.7f+Mathf.Sin(r)*1.65f,0),new Vector3(.55f,.45f,.6f));g.transform.localRotation=Quaternion.Euler(0,0,r*Mathf.Rad2Deg);}
            CavePrototype.MossPatch(a,new Vector3(-1.65f,.7f,-.3f),Vector3.back,3);CavePrototype.MossPatch(a,new Vector3(1.65f,1.2f,-.3f),Vector3.back,5);
        }
        static GameObject StoneBox(Transform p,Vector3 at,Vector3 size){var g=Prim.Cube("AncientStone",p,at,size,Color.white,false);g.GetComponent<Renderer>().sharedMaterial=CavePrototype.Rock;g.GetComponent<Renderer>().SetPropertyBlock(null);return g;}
        static void Plant(Transform root,Vector3 p,int seed) {
            if(_plants==null){_plants=new Material(Shader.Find("Universal Render Pipeline/Lit"));_plants.SetColor("_BaseColor",new Color(.2f,.38f,.17f));_plants.SetFloat("_Cull",0);}
            var vs=new List<Vector3>();var ts=new List<int>();for(int j=0;j<8;j++){float a=j*Mathf.PI/4+seed;var dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var side=Vector3.Cross(dir,Vector3.up);int k=vs.Count;vs.Add(p);vs.Add(p+dir*.22f+Vector3.up*.23f-side*.07f);vs.Add(p+dir*.42f+Vector3.up*(.4f+(j%3)*.05f));vs.Add(p+dir*.22f+Vector3.up*.23f+side*.07f);ts.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});}CavePrototype.MeshObject("FernFronds",root,vs,ts,_plants,false);
        }
        static void Roots(Transform t,Vector3 p,float length,int seed) {
            var v=new List<Vector3>();var tri=new List<int>();const int bands=9,sides=6;for(int b=0;b<bands;b++){float f=b/(bands-1f);var c=p+new Vector3(Mathf.Sin(f*3+seed)*.13f,-length*f,Mathf.Cos(f*3+seed)*.1f);for(int j=0;j<sides;j++){float a=j*Mathf.PI*2/sides,r=.07f*(1-f*.8f);v.Add(c+new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r));}}for(int b=0;b<bands-1;b++)for(int j=0;j<sides;j++){int a=b*sides+j,c=b*sides+(j+1)%sides;tri.AddRange(new[]{a,c,a+sides,c,c+sides,a+sides});}for(int branch=0;branch<2;branch++){float start=.38f+branch*.25f;var begin=p+new Vector3(Mathf.Sin(start*3+seed)*.13f,-length*start,Mathf.Cos(start*3+seed)*.1f);int first=v.Count;for(int b=0;b<4;b++){float f=b/3f;var c=begin+new Vector3((branch==0?-1:1)*f*.26f,-length*f*.22f,f*.1f);for(int j=0;j<sides;j++){float a=j*Mathf.PI*2/sides,r=.03f*(1-f*.8f);v.Add(c+new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r));}}for(int b=0;b<3;b++)for(int j=0;j<sides;j++){int a=first+b*sides+j,c=first+b*sides+(j+1)%sides;tri.AddRange(new[]{a,c,a+sides,c,c+sides,a+sides});}}if(_roots==null){_roots=new Material(GameAssets.I.lit);_roots.SetColor("_BaseColor",new Color(.24f,.22f,.13f));}CavePrototype.MeshObject("AttachedNaturalRoot",t,v,tri,_roots,false);
        }
        static Vector3 RoofPoint(Transform t,Vector3 p) {
            var roof=t.Find("ContinuousCaveVault").GetComponent<MeshCollider>();
            if(roof.Raycast(new Ray(t.TransformPoint(new Vector3(p.x,1,p.z)),Vector3.up),out var hit,8))p.y=t.InverseTransformPoint(hit.point).y+.02f;return p;
        }
        static void DecorativeMushroom(Transform t,Vector3 p,float h,float cap,int variation) {
            int first=t.childCount;CavePrototype.Mushroom(t,p,h,cap,variation);
            for(int k=first;k<t.childCount;k++){var part=t.GetChild(k);var r=part.GetComponent<Renderer>();if(r==null)continue;
                if(part.name=="MushroomDomeCap")Prim.SetColor(r,variation%2==0?new Color(.27f,.43f,.49f):new Color(.38f,.32f,.46f),new Color(.04f,.13f,.15f));
                if(part.name=="MushroomPaleGills")Prim.SetColor(r,new Color(.43f,.62f,.61f),new Color(.045f,.14f,.13f));
            }
        }
        static void Decor(Transform t,int type,Vector2 size) {
            for(int j=0;j<5;j++){float sx=j%2==0?-1:1;var p=new Vector3(sx*(size.x*.5f-1.0f),.2f,-size.y*.3f+j*size.y*.13f);CavePrototype.Boulder(t,p,new Vector3(1.25f,.75f,1.05f),j+type*13);CavePrototype.MossPatch(t,p+Vector3.up*.38f,Vector3.up,j+type*5);Plant(t,new Vector3(p.x-sx*.4f,.025f,p.z+.5f),j);Roots(t,RoofPoint(t,new Vector3(sx*(size.x*.5f-1.3f),3.8f,p.z)),1.3f+(j%2)*.5f,type+j);}
            for(int j=0;j<4;j++){float x=j%2==0?-size.x*.5f+.65f:size.x*.5f-.65f;var p=new Vector3(x,.5f+(j%2)*.7f,-size.y*.25f+j*.9f);var normal=x<0?Vector3.right:Vector3.left;var wall=t.Find("OrganicRockWalls").GetComponent<MeshCollider>();if(wall.Raycast(new Ray(t.TransformPoint(new Vector3(0,p.y,p.z)),-normal),out var hit,10))p=t.InverseTransformPoint(hit.point)+normal*.015f;CavePrototype.MossPatch(t,p,normal,j+20);}
            int mushrooms=type==2?12:4;for(int j=0;j<mushrooms;j++){float sx=j%2==0?-1:1;var p=new Vector3(sx*(size.x*.5f-1.7f),.02f,-size.y*.32f+(j/2)*.75f);DecorativeMushroom(t,p,type==2?1.45f+(j%4)*.4f:.25f+(j%3)*.12f,type==2?1.3f+(j%3)*.48f:.4f+(j%3)*.14f,j);}
            if(type==1){CavePrototype.Pool(t,new Vector3(-2.9f,.018f,.3f),1.65f,1.4f,2);CavePrototype.Pool(t,new Vector3(2.8f,.018f,-1.6f),1.4f,1.15f,7);}
            if(type==4){CavePrototype.Pool(t,new Vector3(0,.018f,0),size.x*.38f,size.y*.39f,8);for(int j=0;j<4;j++)CavePrototype.Boulder(t,new Vector3(j%2==0?-2.3f:2.3f,.09f,-2.4f+j*1.4f),new Vector3(.7f,.23f,.6f),30+j);}
            if(type==3){for(int s=-1;s<=1;s+=2){var collision=Prim.Cube("RootCoverCollision",t,new Vector3(s*3.35f,.9f,.5f),new Vector3(3.0f,1.8f,.4f),Color.black);collision.GetComponent<Renderer>().enabled=false;for(int j=0;j<14;j++){var at=RoofPoint(t,new Vector3(s*(1.9f+j*.22f),3.7f,.5f));Roots(t,at,at.y-.04f,j+50);}} }
            if(type==5)for(int j=0;j<12;j++)CavePrototype.Stalactite(t,RoofPoint(t,new Vector3(-3.5f+(j%4)*2.3f,4.0f+(j%3)*.15f,-2+(j/4)*1.9f)),.5f+(j%3)*.27f,j);
            if(type==6){var camp=new GameObject("AdventurerCamp").transform;camp.SetParent(t,false);camp.localPosition=new Vector3(-3,0,-2.5f);for(int s=-1;s<=1;s+=2){Prim.Cube("Bedroll",camp,new Vector3(s*.75f,.09f,0),new Vector3(.7f,.18f,1.5f),new Color(.32f,.28f,.20f),false);Prim.Cube("Pack",camp,new Vector3(s*1.2f,.18f,.9f),new Vector3(.35f,.36f,.27f),new Color(.25f,.2f,.12f),false);}for(int j=0;j<7;j++){float a=j*Mathf.PI*2/7;CavePrototype.Boulder(camp,new Vector3(Mathf.Cos(a)*.35f,.09f,Mathf.Sin(a)*.35f),new Vector3(.2f,.18f,.17f),j);}Prim.Glow(Prim.Sphere("SmallCampEmber",camp,new Vector3(0,.12f,0),new Vector3(.32f,.09f,.32f),new Color(.42f,.2f,.065f)),new Color(.18f,.08f,.02f));Prim.PointLight(camp,new Vector3(0,.5f,0),new Color(.85f,.58f,.35f),4,1.2f);}
        }
    }
}
