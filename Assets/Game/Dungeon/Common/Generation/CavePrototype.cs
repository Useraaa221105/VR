using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Podz {
    /// <summary>Одна показательная пещера «Зал луж». Не подключена к генератору этажей до одобрения.</summary>
    public static class CavePrototype {
        static Material _rock;
        static readonly Color Stone = new Color(.52f,.55f,.56f);
        public static Material Rock {
            get {
                if(_rock!=null)return _rock;
                _rock=new Material(Shader.Find("Universal Render Pipeline/Lit"));_rock.name="WetGreyCaveRock";
                const int n=512;var tex=new Texture2D(n,n,TextureFormat.RGB24,true);tex.name="CaveStoneAlbedo";
                for(int y=0;y<n;y++)for(int x=0;x<n;x++) {
                    float a=Mathf.PerlinNoise(x*.018f,y*.026f),b=Mathf.PerlinNoise(x*.065f+13,y*.075f),c=Mathf.PerlinNoise(x*.19f,y*.19f);
                    float seam=Mathf.Abs(Mathf.PerlinNoise(x*.033f+5,y*.033f)-.48f);float v=.29f+a*.3f+b*.16f+c*.1f-Mathf.Clamp01((.022f-seam)/.022f)*.16f;
                    tex.SetPixel(x,y,new Color(v*.98f,v,v*1.02f));

                }
                tex.wrapMode=TextureWrapMode.Repeat;tex.Apply();
                _rock.SetColor("_BaseColor",new Color(.60f,.62f,.63f));_rock.SetTexture("_BaseMap",tex);_rock.SetFloat("_Smoothness",.36f);return _rock;
            }
        }
        public static GameObject MeshObject(string name,Transform root,List<Vector3> vs,List<int> ts,Material mat,bool collider,bool wall=false) {
            var mesh=new Mesh{name=name};mesh.SetVertices(vs);mesh.SetTriangles(ts,0);var uv=new List<Vector2>();
            foreach(var v in vs)uv.Add(wall?new Vector2((Mathf.Atan2(v.z,v.x)+Mathf.PI)*2.8f,v.y*.5f):new Vector2(v.x*.38f,v.z*.38f));
            mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();var go=new GameObject(name);go.transform.SetParent(root,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;if(collider)go.AddComponent<MeshCollider>().sharedMesh=mesh;return go;
        }
        public static GameObject Boulder(Transform root,Vector3 p,Vector3 size,int seed) {
            var g=Prim.Sphere("NaturalFracturedRock",root,p,size,Color.white);var mf=g.GetComponent<MeshFilter>();var mesh=Object.Instantiate(mf.sharedMesh);var vs=mesh.vertices;
            for(int i=0;i<vs.Length;i++){var v=vs[i];float n=Mathf.PerlinNoise(v.x*4+seed,v.z*4+v.y*3+seed*.7f);vs[i]=v*(.85f+n*.32f);}
            mesh.vertices=vs;mesh.RecalculateNormals();mf.sharedMesh=mesh;g.GetComponent<Renderer>().sharedMaterial=Rock;g.GetComponent<Renderer>().SetPropertyBlock(null);g.AddComponent<SphereCollider>();return g;
        }
        static float OpeningHeight(float x) {return 2.02f+1.03f*Mathf.Sqrt(Mathf.Max(0,1-(x/1.8f)*(x/1.8f)));}
        public static void MossPatch(Transform root,Vector3 p,Vector3 normal,int seed) {
            const int n=18;var vs=new List<Vector3>{Vector3.zero};var ts=new List<int>();for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n,r=.68f*(1+.18f*Mathf.Sin(i*1.9f+seed));vs.Add(new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r*.45f,0));}for(int i=0;i<n;i++)ts.AddRange(new[]{0,i+1,(i+1)%n+1});
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.SetColor("_BaseColor",new Color(.18f,.33f,.16f));m.SetFloat("_Smoothness",.26f);m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",new Color(.015f,.055f,.025f));var g=MeshObject("SubtleWallMoss",root,vs,ts,m,false);g.transform.localPosition=p;g.transform.localRotation=Quaternion.LookRotation(normal);
            for(int i=0;i<5;i++){float a=i*2.4f;var leaf=Prim.Sphere("MossSmallLeaves",g.transform,new Vector3(Mathf.Cos(a)*.16f,Mathf.Sin(a)*.07f,.008f),new Vector3(.095f,.045f,.025f),new Color(.22f,.38f,.18f));}
        }
        static void Fern(Transform root,Vector3 p,int seed) {
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetColor("_BaseColor",new Color(.18f,.38f,.21f));material.SetFloat("_Cull",0);material.SetFloat("_Smoothness",.22f);
            for(int f=0;f<7;f++){float a=f*Mathf.PI*2/7+seed*.13f;var dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var side=Vector3.Cross(Vector3.up,dir);float h=.33f+(f%3)*.09f;var v=new List<Vector3>();var tr=new List<int>();Vector3 prev=p;
                for(int j=0;j<=6;j++){float t=j/6f;var c=p+dir*(t*.40f)+Vector3.up*(Mathf.Sin(t*1.5f)*h);float w=Mathf.Sin(t*Mathf.PI)*.075f;v.Add(c-side*w);v.Add(c+side*w);if(j>0){int k=j*2-2;tr.AddRange(new[]{k,k+2,k+1,k+1,k+2,k+3});}if(j>0)Tube(root,prev,c,.014f,"FernSpine",new Color(.18f,.31f,.15f));prev=c;}
                MeshObject("NaturalFernFrond",root,v,tr,material,false);
            }
        }
        static void Tube(Transform root,Vector3 a,Vector3 b,float width,string name,Color col) {
            var d=b-a;var g=Prim.Cyl(name,root,(a+b)*.5f,new Vector3(width,d.magnitude+.025f,width),col);g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,d.normalized);
        }
        static Vector3 CeilingAt(GameObject roof,Vector3 p) {
            Physics.SyncTransforms();if(roof.GetComponent<MeshCollider>().Raycast(new Ray(new Vector3(p.x,.1f,p.z),Vector3.up),out var hit,12f))return hit.point;
            return p+Vector3.up*4.5f;
        }
        public static void Stalactite(Transform root,Vector3 p,float length,int seed) {
            const int sides=9;var vs=new List<Vector3>();var ts=new List<int>();
            for(int k=0;k<4;k++)for(int j=0;j<sides;j++){float a=j*Mathf.PI*2/sides;float r=(1-k/3f)*(.2f+length*.13f)*(1+.14f*Mathf.Sin(j*3+seed));vs.Add(p+new Vector3(Mathf.Cos(a)*r,-k*length/3f,Mathf.Sin(a)*r));}
            for(int k=0;k<3;k++)for(int j=0;j<sides;j++){int a=k*sides+j,b=k*sides+(j+1)%sides;ts.AddRange(new[]{a,b,a+sides,b,b+sides,a+sides});}
            MeshObject("StoneStalactite",root,vs,ts,Rock,false);
        }
        public static void Pool(Transform root,Vector3 p,float rx,float rz,int seed) {
            const int n=40;var vs=new List<Vector3>{p};var ts=new List<int>();
            for(int j=0;j<n;j++){float a=j*Mathf.PI*2/n,r=1+.12f*Mathf.Sin(a*3+seed)+.045f*Mathf.Cos(a*5+seed);vs.Add(p+new Vector3(Mathf.Cos(a)*rx*r,0,Mathf.Sin(a)*rz*r));}
            for(int j=0;j<n;j++)ts.AddRange(new[]{0,(j+1)%n+1,j+1});
            var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.name="ShallowCaveWater";mat.SetColor("_BaseColor",new Color(.19f,.37f,.39f));mat.SetFloat("_Smoothness",.48f);mat.SetFloat("_Metallic",0);mat.SetFloat("_EnvironmentReflections",0);mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",new Color(.025f,.065f,.07f));
            MeshObject("IrregularShallowPool",root,vs,ts,mat,false);
            // Каменные края пологие; центральная полоса ходьбы остаётся свободной.
            for(int j=0;j<5;j++){float a=(j*.9f+seed);Boulder(root,p+new Vector3(Mathf.Cos(a)*rx,0,Mathf.Sin(a)*rz),new Vector3(.28f,.12f,.24f),seed+j);}
        }
        public static void Mushroom(Transform root,Vector3 p,float h,float cap,int variation) {
            Prim.Cyl("MushroomStem",root,p+Vector3.up*h*.48f,new Vector3(h*.13f,h*.94f,h*.13f),new Color(.46f,.49f,.4f));
            var g=Prim.Sphere("MushroomDomeCap",root,p+Vector3.up*h,new Vector3(cap,cap*.42f,cap*.9f),variation%2==0?new Color(.26f,.43f,.37f):new Color(.4f,.45f,.32f));
            Prim.SetEmission(g.GetComponent<Renderer>(),new Color(.06f,.2f,.15f));
            Prim.Sphere("MushroomPaleGills",root,p+Vector3.up*(h-.035f),new Vector3(cap*.86f,.045f,cap*.78f),new Color(.36f,.5f,.42f));
            for(int j=0;j<6;j++){float a=j*Mathf.PI*2/6;Prim.Sphere("CapNaturalSpot",root,p+new Vector3(Mathf.Sin(a)*cap*.27f,h+cap*.13f,Mathf.Cos(a)*cap*.24f),new Vector3(cap*.085f,.025f,cap*.07f),new Color(.53f,.62f,.48f));}
        }
        public static void Build(Transform root) {
            var corners=new[]{new Vector3(-1.8f,0,5),new Vector3(1.8f,0,5),new Vector3(4.3f,0,4.6f),new Vector3(5.9f,0,2.8f),new Vector3(6.2f,0,.4f),new Vector3(5.8f,0,-2.4f),new Vector3(4.1f,0,-4.2f),new Vector3(1.5f,0,-5.2f),new Vector3(-1.5f,0,-5.4f),new Vector3(-4.2f,0,-4.3f),new Vector3(-5.9f,0,-2.6f),new Vector3(-6.4f,0,.2f),new Vector3(-5.6f,0,3),new Vector3(-3.7f,0,4.8f)};
            var edge=new List<Vector3>();const int sub=5;for(int i=0;i<corners.Length;i++)for(int j=0;j<sub;j++) {var v=Vector3.Lerp(corners[i],corners[(i+1)%corners.Length],j/(float)sub);if(i>0)v+=v.normalized*(.10f*Mathf.Sin((i*sub+j)*1.7f));edge.Add(v);}
            int n=edge.Count;var floor=new List<Vector3>{Vector3.zero};floor.AddRange(edge);var ft=new List<int>();for(int i=0;i<n;i++)ft.AddRange(new[]{0,i+1,(i+1)%n+1});MeshObject("NaturalCaveFloor",root,floor,ft,Rock,true);
            var wall=new List<Vector3>();var wt=new List<int>();var outer=new List<Vector3>();
            for(int i=0;i<n;i++)outer.Add(edge[i]+Vector3.up*(3.7f+.19f*Mathf.Sin(i*.51f)));
            for(int i=0;i<n;i++) {int ni=(i+1)%n;
                if(i<sub){int k=wall.Count;wall.Add(edge[i]+Vector3.up*OpeningHeight(edge[i].x));wall.Add(edge[ni]+Vector3.up*OpeningHeight(edge[ni].x));wall.Add(outer[ni]);wall.Add(outer[i]);wt.AddRange(new[]{k,k+2,k+1,k,k+3,k+2});continue;}
                for(int j=0;j<5;j++){float t=j/4f;var v=Vector3.Lerp(edge[i]-Vector3.up*.04f,outer[i],t);v+=edge[i].normalized*(Mathf.Sin(i*.93f+j*1.75f)*.19f*Mathf.Sin(t*Mathf.PI));wall.Add(v);}
                for(int j=0;j<5;j++){float t=j/4f;var v=Vector3.Lerp(edge[ni]-Vector3.up*.04f,outer[ni],t);v+=edge[ni].normalized*(Mathf.Sin(ni*.93f+j*1.75f)*.19f*Mathf.Sin(t*Mathf.PI));wall.Add(v);}
                int k2=wall.Count-10;for(int j=0;j<4;j++)wt.AddRange(new[]{k2+j,k2+j+6,k2+j+5,k2+j,k2+j+1,k2+j+6});
            }
            var wallObject=MeshObject("UnevenContinuousRockWalls",root,wall,wt,Rock,true,true);
            var ceiling=new List<Vector3>();var ct=new List<int>();for(int ring=0;ring<3;ring++)for(int i=0;i<n;i++){var v=outer[i];float r=ring==0?1: ring==1?.65f:.28f;v.x*=r;v.z*=r;v.y=ring==0?v.y:ring==1?5.05f+.22f*Mathf.Sin(i*.43f):6f+.15f*Mathf.Sin(i*.51f);ceiling.Add(v);}
            for(int ring=0;ring<2;ring++)for(int i=0;i<n;i++){int a=ring*n+i,b=ring*n+(i+1)%n;ct.AddRange(new[]{a,b,a+n,b,b+n,a+n});}int center=ceiling.Count;ceiling.Add(new Vector3(0,6.4f,0));for(int i=0;i<n;i++)ct.AddRange(new[]{center,2*n+i,2*n+(i+1)%n});
            for(int i=0;i<ct.Count;i+=3){int swap=ct[i+1];ct[i+1]=ct[i+2];ct[i+2]=swap;}
            var roof=MeshObject("SealedArchedCaveRoof",root,ceiling,ct,Rock,true);
            // Вход и короткий поворачивающий проход: единый камень и ломаный естественный свод, без коробки.
            var passage=new List<Vector3>();var pt=new List<int>();const int arch=13;
            for(int ring=0;ring<5;ring++)for(int j=0;j<arch;j++) {
                float a=j*Mathf.PI/(arch-1);float x=-Mathf.Cos(a)*(1.8f-ring*.055f),y=3.02f*Mathf.Sin(a);
                float bend=ring<2?0:(ring-1)*(ring-1)*.15f;
                float irregular=j==0||j==arch-1?0:.06f*Mathf.Sin(j*1.7f+ring*.8f);
                passage.Add(new Vector3(x+bend,y+irregular,5f+ring*1.15f));
            }
            for(int ring=0;ring<4;ring++)for(int j=0;j<arch-1;j++){int a=ring*arch+j,b=(ring+1)*arch+j;pt.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
            for(int i=0;i<pt.Count;i+=3){int swap=pt[i+1];pt[i+1]=pt[i+2];pt[i+2]=swap;}
            MeshObject("CurvedRockPassage",root,passage,pt,Rock,true,true);
            var pv=new List<Vector3>();var pf=new List<int>();for(int ring=0;ring<5;ring++){pv.Add(passage[ring*arch]);pv.Add(passage[ring*arch+arch-1]);}for(int ring=0;ring<4;ring++){int k=ring*2;pf.AddRange(new[]{k,k+2,k+1,k+1,k+2,k+3});}MeshObject("PassageConnectedFloor",root,pv,pf,Rock,true);
            var end=new List<Vector3>();for(int j=0;j<arch;j++)end.Add(passage[4*arch+j]);end.Add(new Vector3(1.35f,0,9.6f));var et=new List<int>();for(int j=0;j<arch-1;j++)et.AddRange(new[]{arch,j,j+1});MeshObject("PassageBendFarRock",root,end,et,Rock,true,true);
            Boulder(root,new Vector3(-1.96f,1.02f,5.04f),new Vector3(.75f,2.1f,.7f),102);
            Boulder(root,new Vector3(1.96f,1.05f,5.04f),new Vector3(.8f,2.15f,.74f),103);
            Boulder(root,new Vector3(-1.55f,2.16f,5.04f),new Vector3(.65f,.85f,.65f),106);
            Boulder(root,new Vector3(1.55f,2.16f,5.04f),new Vector3(.67f,.85f,.65f),107);
            foreach(var p in new[]{new Vector3(-5.4f,.3f,.8f),new Vector3(5.4f,.28f,2.6f),new Vector3(-4.7f,.23f,-3.1f),new Vector3(4.8f,.2f,-3.5f)})Boulder(root,p,new Vector3(1.6f,.9f,1.15f),(int)(p.x*10+60));
            // Крупные слоистые образования по периферии, не одинаковые коробки.
            for(int i=0;i<12;i++){int idx=7+i*5;var p=edge[idx%n]*.985f;p.y=.35f+(i%3)*.45f;Boulder(root,p,new Vector3(1.3f,.7f,1.1f),i+90);}
            Pool(root,new Vector3(-3.0f,.018f,-.5f),1.5f,1.15f,2);Pool(root,new Vector3(3.1f,.018f,1.1f),1.45f,.9f,7);Pool(root,new Vector3(-1.5f,.018f,3.25f),1.15f,.6f,11);
            for(int i=0;i<12;i++){float x=i<6?-4.75f:4.75f,z=-2.8f+(i%6)*1.22f;Mushroom(root,new Vector3(x,.03f,z),.36f+(i%3)*.13f,.5f+(i%3)*.15f,i);Mushroom(root,new Vector3(x+(i<6?.34f:-.34f),.03f,z+.3f),.22f,.32f,i+1);}
            Mushroom(root,new Vector3(-3.75f,0,3.65f),1.22f,1.52f,0);Mushroom(root,new Vector3(3.8f,0,3.75f),.95f,1.2f,0);
            for(int i=0;i<18;i++){var p=new Vector3(i<9?-4.0f-(i%3)*.25f:4.2f+(i%3)*.24f,0,-3.3f+(i%9)*.88f);var anchor=CeilingAt(roof,p);var prev=anchor+Vector3.up*.025f;for(int j=1;j<=10;j++){var next=anchor+new Vector3(.15f*Mathf.Sin(j*.58f+i),-j*(.17f+(i%3)*.035f),.1f*Mathf.Sin(j*.7f));Tube(root,prev,next,.075f*(1-j*.065f),"AttachedHangingRoot",new Color(.25f,.23f,.15f));if(j==6)Tube(root,next,next+new Vector3(.25f,-.4f,.13f),.025f,"RootBranch",new Color(.25f,.23f,.15f));prev=next;}}
            for(int i=0;i<8;i++){var p=new Vector3(-3.6f+i*1.05f,0,2.4f+(i%3)*.65f);Stalactite(root,CeilingAt(roof,p)+Vector3.up*.015f,.5f+(i%4)*.18f,i);}
            for(int i=0;i<30;i++){var p=edge[(8+i*2)%n]*.94f;p.y=.04f+(i%4)*.035f;Prim.Sphere("DecorativeNonHealingMoss",root,p,new Vector3(.56f,.12f,.35f),new Color(.22f,.39f,.20f));}
            for(int i=0;i<12;i++){var p=edge[(7+i*5)%n]*.965f;p.y=.4f+(i%4)*.32f;var moss=Prim.Sphere("RockCreviceMoss",root,p,new Vector3(.8f,.24f,.28f),new Color(.24f,.44f,.20f));moss.transform.localRotation=Quaternion.Euler(0,Mathf.Atan2(p.x,p.z)*Mathf.Rad2Deg,0);Prim.SetEmission(moss.GetComponent<Renderer>(),new Color(.035f,.11f,.065f));}
            // Маленькие влажные мшистые пятна в реальных щелях стен, без лечебной механики.
            Physics.SyncTransforms();for(int i=0;i<18;i++){var ep=edge[(7+i*3)%n];var ray=new Ray(new Vector3(0,.25f+(i%3)*.34f,0),new Vector3(ep.x,0,ep.z).normalized);if(wallObject.GetComponent<MeshCollider>().Raycast(ray,out var hit,12f))MossPatch(root,hit.point+hit.normal*.016f,hit.normal,i);}
            foreach(var p in new[]{new Vector3(-4.1f,.034f,-.6f),new Vector3(-3.0f,.034f,-1.65f),new Vector3(4.15f,.034f,1.1f),new Vector3(2.8f,.034f,2.05f),new Vector3(-1.9f,.034f,3.8f)}) {MossPatch(root,p,Vector3.up,(int)(p.x*10+60));Fern(root,p+new Vector3(.25f,0,.2f),(int)(p.x*10+60));}
            for(int i=0;i<8;i++){var p=new Vector3(i<4?-5.0f:5.05f,.0f,-2.1f+(i%4)*1.45f);var ray=new Ray(p+Vector3.up*3,Vector3.down);if(Physics.Raycast(ray,out var hit,4f))MossPatch(root,hit.point+Vector3.up*.024f,hit.normal,i+70);Fern(root,p+new Vector3(i<4?.35f:-.35f,0,.25f),i+20);}
            // Только отдельные грибные/мшистые островки дают мягкую локальную подсветку.
            Prim.PointLight(root,new Vector3(-3.85f,1.3f,3.5f),new Color(.27f,.78f,.59f),5.5f,5f);
            Prim.PointLight(root,new Vector3(3.7f,1.0f,3.5f),new Color(.24f,.65f,.72f),5f,4.5f);
            Prim.PointLight(root,new Vector3(-3.1f,.7f,-.5f),new Color(.29f,.68f,.52f),4.5f,3f);
            var well=new GameObject("WallWell").transform;well.SetParent(root,false);well.localPosition=new Vector3(-4.9f,0,2.0f);
            for(int i=0;i<12;i++){float a=i*Mathf.PI*2/12;var g=Prim.Cube("WellRingStone",well,new Vector3(Mathf.Sin(a)*.57f,.3f,Mathf.Cos(a)*.57f),new Vector3(.31f,.6f,.28f),Color.white);g.transform.localRotation=Quaternion.Euler(0,a*Mathf.Rad2Deg,0);g.GetComponent<Renderer>().sharedMaterial=Rock;g.GetComponent<Renderer>().SetPropertyBlock(null);}
            Prim.Cyl("WellWater",well,new Vector3(0,.14f,0),new Vector3(.88f,.02f,.88f),new Color(.08f,.18f,.2f));for(int side=-1;side<=1;side+=2)Prim.Cube("WellSupport",well,new Vector3(side*.7f,.85f,0),new Vector3(.1f,1.7f,.1f),new Color(.29f,.23f,.15f));Prim.Cube("WellBeam",well,new Vector3(0,1.65f,0),new Vector3(1.55f,.13f,.15f),new Color(.29f,.23f,.15f));Tube(well,new Vector3(0,1.62f,0),new Vector3(0,.37f,0),.025f,"WellRope",new Color(.4f,.33f,.21f));
            Prim.PointLight(root,new Vector3(-2.5f,3.1f,1.2f),new Color(.43f,.82f,.71f),10f,7f);Prim.PointLight(root,new Vector3(3.4f,2.6f,1.5f),new Color(.36f,.72f,.78f),9f,6f);Prim.PointLight(root,new Vector3(0,2f,7.4f),new Color(.38f,.72f,.70f),7f,4f);
            var light=new GameObject("SoftCaveAmbientKey");light.transform.SetParent(root,false);light.transform.localRotation=Quaternion.Euler(48,-30,0);var l=light.AddComponent<Light>();l.type=LightType.Directional;l.color=new Color(.69f,.79f,.78f);l.intensity=.56f;l.shadows=LightShadows.None;
        }
    }
}
