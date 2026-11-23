using UnityEngine;
namespace Podz
{
    /// <summary>Круглая неэмиссивная поверхность, очень небольшая рябь. Один общий вид супа для котлов и миски.</summary>
    public class SoupVisual : MonoBehaviour
    {
        Mesh _mesh; Vector3[] _vertices; float _radius, _next; const int Segments=40;
        public static Renderer Disc(Transform parent,Vector3 position,float radius,Color color)
        {
            var go=new GameObject("SoupSurface");go.transform.SetParent(parent,false);go.transform.localPosition=position;
            var vis=go.AddComponent<SoupVisual>();vis._radius=radius;
            vis._vertices=new Vector3[Segments+1];var indices=new int[Segments*3];
            for(int i=0;i<Segments;i++){float a=i*Mathf.PI*2/Segments;vis._vertices[i+1]=new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);indices[i*3]=0;indices[i*3+1]=(i+1)%Segments+1;indices[i*3+2]=i+1;}
            vis._mesh=new Mesh {name="RoundSoup"};vis._mesh.MarkDynamic();vis._mesh.vertices=vis._vertices;vis._mesh.triangles=indices;vis._mesh.RecalculateNormals();vis._mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh=vis._mesh;
            var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=GameAssets.I.lit;
            Prim.SetColor(r,color,Color.black);return r;
        }
        public static bool IsStarter(Recipe recipe) => recipe != null && (recipe == Recipes.All[0] || recipe == Recipes.All[1]);
        public static Color CookingColor(Recipe recipe, float progress)
        {
            var water = recipe.Index == 0 ? new Color(.76f,.78f,.52f) : new Color(.80f,.71f,.49f);
            return Color.Lerp(Color.Lerp(water,recipe.SoupColor,.35f),recipe.SoupColor,Mathf.Clamp01(progress));
        }
        static bool StarterItems(ItemId[] items) => items == Recipes.All[0].Items || items == Recipes.All[1].Items;

        // Только два начальных супа. Остальные используют прежние Chunks/материалы без изменения.
        static GameObject Piece(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Color color)
        {
            var go = Prim.Shape(type, name, parent, pos, scale, color, false);
            var renderer = go.GetComponent<Renderer>(); var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
            block.SetFloat("_Smoothness", type == PrimitiveType.Cube && name == "JellyCube" ? .7f : .25f);
            renderer.SetPropertyBlock(block); return go;
        }

        static GameObject Ring(Transform parent, Vector3 pos, float size, Color color, string name)
        {
            const int n = 24;
            // Настоящее отверстие, а не шарик с именем «кольцо»: светлый срез грибной ножки/лука.
            var vertices = new Vector3[n * 4]; var triangles = new int[n * 24];
            for (int i = 0; i < n; i++)
            {
                float angle = i * Mathf.PI * 2 / n; var d = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                vertices[i*4] = d*size + Vector3.up*size*.12f;
                vertices[i*4+1] = d*size*.52f + Vector3.up*size*.12f;
                vertices[i*4+2] = d*size - Vector3.up*size*.12f;
                vertices[i*4+3] = d*size*.52f - Vector3.up*size*.12f;
                int a = i*4, b = ((i+1)%n)*4, t = i*24;
                int[] face = { a,a+1,b, b,a+1,b+1, a+2,b+2,a+3, b+2,b+3,a+3, a,b,a+2,b,b+2,a+2, a+1,a+3,b+1,b+1,a+3,b+3 };
                for (int k=0;k<24;k++) triangles[t+k] = face[k];
            }
            var go = new GameObject(name); go.transform.SetParent(parent,false); go.transform.localPosition=pos;
            var owner = go.AddComponent<SoupVisual>(); owner._mesh = new Mesh { name = name }; owner._mesh.vertices=vertices; owner._mesh.triangles=triangles; owner._mesh.RecalculateNormals(); owner._mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh=owner._mesh;
            var renderer=go.AddComponent<MeshRenderer>(); renderer.sharedMaterial=GameAssets.I.lit; Prim.SetColor(renderer,color,Color.black); return go;
        }

        public static void StarterBowl(Transform parent)
        {
            // Деревянная глубокая миска по41–42, без чужой текстуры/ресурса и без изменения поздних блюд.
            var profile = new Vector2[] { new Vector2(0,-.018f),new Vector2(.06f,-.018f),new Vector2(.10f,0),new Vector2(.133f,.072f),new Vector2(.145f,.077f),new Vector2(.137f,.066f),new Vector2(.109f,.021f),new Vector2(.08f,.005f),new Vector2(0,.005f) };
            const int n=40; var vertices=new Vector3[n*profile.Length]; var triangles=new int[n*(profile.Length-1)*6];
            for(int i=0;i<n;i++) for(int j=0;j<profile.Length;j++)
            { float a=i*Mathf.PI*2/n;vertices[i*profile.Length+j]=new Vector3(Mathf.Cos(a)*profile[j].x,profile[j].y,Mathf.Sin(a)*profile[j].x); }
            int t=0;
            for(int i=0;i<n;i++) for(int j=0;j<profile.Length-1;j++)
            { int a=i*profile.Length+j,b=((i+1)%n)*profile.Length+j;triangles[t++]=a;triangles[t++]=a+1;triangles[t++]=b;triangles[t++]=b;triangles[t++]=a+1;triangles[t++]=b+1; }
            var go=new GameObject("WoodenSoupBowl");go.transform.SetParent(parent,false);
            var owner=go.AddComponent<SoupVisual>();owner._mesh=new Mesh {name="StarterWoodenBowl"};owner._mesh.vertices=vertices;owner._mesh.triangles=triangles;owner._mesh.RecalculateNormals();owner._mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh=owner._mesh;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=GameAssets.I.lit;Prim.SetColor(renderer,new Color(.42f,.24f,.10f),Color.black);
        }

        public static void Chunks(Transform parent,ItemId[] items,Vector3 center,float radius)
        {
            if (StarterItems(items))
            {
                int serial=0;
                foreach (var id in items)
                {
                    int count=id==ItemId.SlimeJelly ? 6 : id==ItemId.Cabbage ? 6 : id==ItemId.MushroomLegs ? 5 : 4;
                    for(int j=0;j<count;j++)
                    {
                        float angle=serial++*2.399f, distance=radius*(.3f+.12f*(j%5));
                        var pos=center+new Vector3(Mathf.Cos(angle)*distance,.002f,Mathf.Sin(angle)*distance);
                        var size=radius*.22f;
                        if(id==ItemId.SlimeJelly)
                        { var part=Piece(PrimitiveType.Cube,"JellyCube",parent,pos+Vector3.up*size*.28f,new Vector3(size,size*.8f,size),new Color(.30f,.65f,.12f));part.transform.localRotation=Quaternion.Euler(8,angle*Mathf.Rad2Deg,5); }
                        else if(id==ItemId.Cabbage)
                        { var part=Piece(PrimitiveType.Cube,"CabbageRibbon",parent,pos,new Vector3(size*1.7f,size*.13f,size*.35f),new Color(.64f,.72f,.30f));part.transform.localRotation=Quaternion.Euler(5,angle*Mathf.Rad2Deg,14); }
                        else if(id==ItemId.MushroomLegs)
                        { var part=Ring(parent,pos+Vector3.up*size*.15f,size*.65f,new Color(.72f,.57f,.34f),"MushroomStemRing");part.transform.localRotation=Quaternion.Euler(9,angle*Mathf.Rad2Deg,7); }
                        else if(id==ItemId.Onion) Ring(parent,pos,size*.60f,new Color(.86f,.79f,.59f),"OnionRing");
                        else if(id==ItemId.Potato)
                        { var part=Piece(PrimitiveType.Cube,"PotatoPiece",parent,pos+Vector3.up*size*.2f,new Vector3(size*.85f,size*.5f,size*.65f),new Color(.83f,.68f,.40f));part.transform.localRotation=Quaternion.Euler(6,angle*Mathf.Rad2Deg,10); }
                    }
                }
                return;
            }
            for(int i=0;i<items.Length;i++)for(int j=0;j<2;j++)
            {
                float a=(i*2+j)*2.399f;float rad=radius*(j==0?.45f:.8f);
                var c=Items.Get(items[i]).Color;
                Prim.Shape(PrimitiveType.Sphere,"SoupChunk_"+items[i],parent,center+new Vector3(Mathf.Cos(a)*rad,.001f,Mathf.Sin(a)*rad),new Vector3(.024f,.012f,.018f),c,false);
            }
        }
        void Update()
        {
            if(_mesh==null || _vertices==null || Time.time<_next)return;_next=Time.time+.1f;
            _vertices[0]=new Vector3(0,Mathf.Sin(Time.time*1.4f)*.0015f,0);
            for(int i=1;i<_vertices.Length;i++){var v=_vertices[i];v.y=Mathf.Sin(Time.time*1.8f+i*.5f)*.0008f;_vertices[i]=v;}
            _mesh.vertices=_vertices;_mesh.RecalculateNormals();
        }
        void OnDestroy(){if(_mesh!=null)Destroy(_mesh);}
    }
}
