using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Podz {
    /// <summary>Визуальные модели −1 по слайдам 22–24. Оригинальные зоны и поведение не заменяются.</summary>
    public static class UpperCaveCreatures {
        static Material _gel,_skin;static Mesh _blob,_body,_cap;
        static readonly Color Gel=new Color(.35f,.68f,.22f,.62f),Cream=new Color(.78f,.66f,.47f),Cap=new Color(.52f,.19f,.105f),Dark=new Color(.07f,.045f,.025f);
        static Material Lit(Color c,float smooth){var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.SetColor("_BaseColor",c);m.SetFloat("_Smoothness",smooth);m.EnableKeyword("_EMISSION");m.enableInstancing=true;return m;}
        static Transform Node(string name,Transform parent,Vector3 p){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=p;return t;}
        static void Hide(Monster m,MonsterAnim a,params string[] names){foreach(var t in m.GetComponentsInChildren<Transform>())foreach(var n in names)if(t.name==n && t.GetComponent<Renderer>() is Renderer r)a.Hidden.Add(r);}
        static Mesh Lathe(float[] ys,float[] rs,bool jelly=false){
            const int n=32;var v=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>();
            for(int k=0;k<ys.Length;k++)for(int j=0;j<n;j++){float a=j*Mathf.PI*2/n;float r=rs[k]*(jelly?1+.07f*Mathf.Cos(a*5)*(1-k/(float)ys.Length):1);v.Add(new Vector3(Mathf.Cos(a)*r,ys[k],Mathf.Sin(a)*r));uv.Add(new Vector2(j/(float)n,k/(float)(ys.Length-1)));}
            for(int k=0;k<ys.Length-1;k++)for(int j=0;j<n;j++){int a=k*n+j,b=k*n+(j+1)%n;tr.AddRange(new[]{a,a+n,b,b,a+n,b+n});}var mesh=new Mesh{name=jelly?"GroundedJellyDome":"RoundedCreatureProfile"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tr,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        static void Draw(string name,Transform parent,Mesh mesh,Material mat,Color c){var g=new GameObject(name);g.transform.SetParent(parent,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.Off;Prim.SetColor(r,c);}
        static GameObject Ball(string name,Transform t,Vector3 at,Vector3 size,Color c){var g=Prim.Sphere(name,t,at,size,c);if(_skin==null)_skin=Lit(Color.white,.4f);g.GetComponent<Renderer>().sharedMaterial=_skin;return g;}
        static void Face(Transform t,float y,float z,float spread,float size){for(int s=-1;s<=1;s+=2){Ball("DarkEye",t,new Vector3(s*spread,y,z),new Vector3(size,size*1.13f,size*.42f),Dark);Ball("EyeGlint",t,new Vector3(s*spread-.006f,y+.009f,z+size*.2f),Vector3.one*size*.2f,new Color(.92f,.92f,.8f));}}
        public static void Slime(Monster m,MonsterAnim a){
            Hide(m,a,"Body","Core");
            if(_gel==null){var saved=Resources.Load<Material>("UpperCaveSlime");_gel=saved!=null?Object.Instantiate(saved):Lit(Gel,.88f);_gel.name="URP_TransparentJelly";_gel.SetFloat("_Surface",1);_gel.SetOverrideTag("RenderType","Transparent");_gel.SetFloat("_SrcBlendAlpha",1);_gel.SetFloat("_DstBlendAlpha",10);_gel.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);_gel.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);_gel.SetFloat("_ZWrite",0);_gel.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");_gel.renderQueue=3000;}
            if(_blob==null)_blob=Lathe(new[]{0f,.02f,.07f,.16f,.25f,.33f,.39f,.42f},new[]{0f,.29f,.255f,.23f,.195f,.15f,.085f,0f},true);
            var t=Node("JellyVisual",m.transform,Vector3.zero);a.Squash=t;Draw("TranslucentGroundedSlime",t,_blob,_gel,Gel);
            for(int j=0;j<8;j++){float y=.075f+j*.032f,angle=j*2.4f,r=.07f+(j%3)*.015f;Ball("InternalBubble",t,new Vector3(Mathf.Cos(angle)*r,y,Mathf.Sin(angle)*r),Vector3.one*(.025f+(j%3)*.012f),new Color(.49f,.69f,.3f));}
            Face(t,.225f,.197f,.068f,.049f);Ball("OpenMouth",t,new Vector3(0,.137f,.24f),new Vector3(.07f,.074f,.025f),new Color(.09f,.23f,.065f));Ball("MouthInterior",t,new Vector3(0,.131f,.253f),new Vector3(.028f,.024f,.008f),new Color(.39f,.53f,.16f));
            Ball("WetShine",t,new Vector3(-.09f,.32f,.13f),new Vector3(.037f,.065f,.014f),new Color(.76f,.85f,.64f));Ball("WetShineSmall",t,new Vector3(.12f,.27f,.14f),new Vector3(.022f,.022f,.014f),new Color(.76f,.85f,.64f));
        }
        public static void Mushroom(Monster m,MonsterAnim a){
            Hide(m,a,"Stem","Cap","Dot1","Dot2");if(_skin==null)_skin=Lit(Color.white,.4f);
            if(_body==null)_body=Lathe(new[]{.1f,.14f,.22f,.33f,.43f,.51f,.54f},new[]{.08f,.135f,.174f,.167f,.125f,.085f,0f});
            if(_cap==null)_cap=Lathe(new[]{-.017f,0f,.045f,.1f,.16f,.21f,.24f},new[]{0f,.33f,.321f,.29f,.235f,.145f,0f});
            Draw("PlumpPaleBody",m.transform,_body,_skin,Cream);
            var cap=Node("RoundedRedBrownCap",m.transform,new Vector3(0,.45f,0));a.Wobble=cap;Draw("DomedCap",cap,_cap,_skin,Cap);
            Ball("PaleGillUnderside",cap,new Vector3(0,-.015f,0),new Vector3(.6f,.024f,.6f),new Color(.53f,.39f,.25f));
            // Пластинки объединены в один меш, а не отдельные цилиндры/коллайдеры.
            var v=new List<Vector3>();var ts=new List<int>();for(int j=0;j<20;j++){float ang=j*Mathf.PI/10;var d=new Vector3(Mathf.Cos(ang),0,Mathf.Sin(ang));var side=new Vector3(-d.z,0,d.x)*.003f;int k=v.Count;v.Add(d*.10f+Vector3.down*.028f);v.Add(d*.29f-side+Vector3.down*.028f);v.Add(d*.29f+side+Vector3.down*.028f);ts.AddRange(new[]{k,k+1,k+2});}var mesh=new Mesh{name="CapGills"};mesh.SetVertices(v);mesh.SetTriangles(ts,0);mesh.RecalculateNormals();Draw("CapGills",cap,mesh,_skin,new Color(.37f,.26f,.16f));
            for(int j=0;j<10;j++){float ang=j*2.399f,r=.10f+(j%3)*.065f;float y=r<.145f?Mathf.Lerp(.24f,.21f,r/.145f):r<.235f?Mathf.Lerp(.21f,.16f,(r-.145f)/.09f):Mathf.Lerp(.16f,.10f,(r-.235f)/.055f);var spot=Ball("CreamSpot",cap,new Vector3(Mathf.Cos(ang)*r,y,Mathf.Sin(ang)*r),new Vector3(.047f+(j%2)*.023f,.014f,.045f+(j%3)*.008f),new Color(.86f,.8f,.64f));spot.transform.localRotation=Quaternion.FromToRotation(Vector3.up,new Vector3(Mathf.Cos(ang)*r,y,Mathf.Sin(ang)*r).normalized);}
            Face(m.transform,.36f,.151f,.063f,.033f);Ball("SmallMouth",m.transform,new Vector3(0,.29f,.172f),new Vector3(.029f,.017f,.013f),Dark);
            for(int s=-1;s<=1;s+=2){var leg=Node("ShortWalkingLeg",m.transform,new Vector3(s*.105f,.12f,0));a.Legs.Add(leg);Ball("Foot",leg,new Vector3(0,-.055f,.02f),new Vector3(.13f,.13f,.18f),Cream);}
        }
    }
}
