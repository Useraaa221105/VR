using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace Podz.EditorTools
{
    [CustomEditor(typeof(DungeonObjectRecipe))]
    public class DungeonObjectRecipeEditor : Editor
    {
        [MenuItem("Podz/Подземелье/Создать параметры одного объекта")]
        static void CreateRecipe() {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            string folder="Assets/Game/Dungeon/Common";
            string path=EditorUtility.SaveFilePanelInProject("Параметры существующего объекта","DungeonObject","asset","Новое имя; старые assets не перезаписываются.",folder);
            if(string.IsNullOrEmpty(path))return;if(AssetDatabase.LoadMainAssetAtPath(path)!=null)throw new InvalidOperationException("Выбери новое имя.");
            var recipe=CreateInstance<DungeonObjectRecipe>();AssetDatabase.CreateAsset(recipe,path);AssetDatabase.SaveAssets();Selection.activeObject=recipe;
        }
        public override void OnInspectorGUI() {
            serializedObject.Update();EditorGUILayout.PropertyField(serializedObject.FindProperty("Kind"));EditorGUILayout.PropertyField(serializedObject.FindProperty("Floor"));EditorGUILayout.PropertyField(serializedObject.FindProperty("Seed"));
            var recipe=(DungeonObjectRecipe)target;var kind=(DungeonObjectRecipe.ObjectKind)serializedObject.FindProperty("Kind").enumValueIndex;
            if(kind==DungeonObjectRecipe.ObjectKind.Stalactite || kind==DungeonObjectRecipe.ObjectKind.Rockfall)EditorGUILayout.PropertyField(serializedObject.FindProperty("CeilingHeight"));
            if(kind==DungeonObjectRecipe.ObjectKind.PhysicalLoot)EditorGUILayout.PropertyField(serializedObject.FindProperty("Loot"));serializedObject.ApplyModifiedProperties();
            EditorGUILayout.HelpBox("Исходная геометрия и поведение; генерация ОДНОГО объекта только кнопкой. Никакой выдачи лута/денег. PortalVisual сохраняет модель и зону, но НЕ callback перехода: его назначает прежний DungeonManager с проверкой доступа при будущей интеграции. Эликсир — пока исходный автоподбор, не новая редкая физическая версия.",MessageType.Info);
            using(new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode)) {
                if(GUILayout.Button("Создать и сохранить ОДИН новый префаб"))DungeonPrefabCatalogEditor.Run(()=>Generate(recipe));
                if(recipe.LastPrefab!=null) {
                    if(GUILayout.Button("Открыть последний префаб"))AssetDatabase.OpenAsset(recipe.LastPrefab);
                    if(GUILayout.Button("Проверить сохранённые ссылки"))DungeonPrefabCatalogEditor.Run(()=>Check(recipe.LastPrefab));
                    if(GUILayout.Button("Новая лёгкая сцена только этого объекта"))DungeonPrefabCatalogEditor.Run(()=>DungeonPrefabCatalogEditor.SaveScene("ObjectReview",(scene,root)=>{
                        PrefabUtility.InstantiatePrefab(recipe.LastPrefab,root);
                    },6));
                }
            }
        }
        static bool Trap(DungeonObjectRecipe.ObjectKind k)=>k<=DungeonObjectRecipe.ObjectKind.AcidDrop;
        internal static void Generate(DungeonObjectRecipe recipe) {
            if(recipe.Floor<1 || recipe.Floor>4 || !Enum.IsDefined(typeof(DungeonObjectRecipe.ObjectKind),recipe.Kind))throw new InvalidOperationException("Недопустимый этаж или тип.");
            if(!float.IsFinite(recipe.CeilingHeight) || recipe.CeilingHeight<2.5f || recipe.CeilingHeight>8f)throw new InvalidOperationException("CeilingHeight: 2.5–8 метров.");
            var kind=recipe.Kind;
            int required=kind==DungeonObjectRecipe.ObjectKind.Stalactite || kind==DungeonObjectRecipe.ObjectKind.AcidDrop || kind==DungeonObjectRecipe.ObjectKind.Puddle?1:
                kind==DungeonObjectRecipe.ObjectKind.Rockfall || kind==DungeonObjectRecipe.ObjectKind.Vines?2:
                kind==DungeonObjectRecipe.ObjectKind.Pendulum || kind==DungeonObjectRecipe.ObjectKind.FireGrate || kind==DungeonObjectRecipe.ObjectKind.FakeArmor || kind==DungeonObjectRecipe.ObjectKind.Chest?3:
                kind==DungeonObjectRecipe.ObjectKind.FireCover?4:0;
            if(required!=0 && recipe.Floor!=required)throw new InvalidOperationException("У этого объекта есть исходный вариант только на этаже "+required+". Не создаём вымышленный вариант.");
            if(kind==DungeonObjectRecipe.ObjectKind.Spores && recipe.Floor>2 || kind==DungeonObjectRecipe.ObjectKind.DartPlate && recipe.Floor!=2 && recipe.Floor!=3)
                throw new InvalidOperationException("Нет такого варианта ловушки на выбранном этаже.");
            string parent=Trap(recipe.Kind)?DungeonPrefabCatalogEditor.FloorFolder(recipe.Floor)+"/Traps":
                recipe.Kind==DungeonObjectRecipe.ObjectKind.FireCover?DungeonPrefabCatalogEditor.FloorFolder(4)+"/Environment":
                recipe.Kind==DungeonObjectRecipe.ObjectKind.FakeArmor || recipe.Kind==DungeonObjectRecipe.ObjectKind.Chest?DungeonPrefabCatalogEditor.FloorFolder(3)+"/Environment":
                "Assets/Game/Dungeon/Common/Objects";
            DungeonPrefabCatalogEditor.Folder(parent);
            string folder=AssetDatabase.GenerateUniqueAssetPath(parent+"/"+recipe.Kind+"_Floor"+recipe.Floor+"_Generated");DungeonPrefabCatalogEditor.Folder(folder);
            var scene=EditorSceneManager.NewPreviewScene();var state=UnityEngine.Random.state;GameObject root=null;
            try {
                UnityEngine.Random.InitState(recipe.Seed);root=new GameObject(recipe.Kind.ToString());SceneManager.MoveGameObjectToScene(root,scene);
                Build(recipe,root.transform);
                foreach(var hazard in root.GetComponentsInChildren<Hazard>(true))hazard.PrepareSavedState();
                foreach(var door in root.GetComponentsInChildren<TransitionDoor>(true))door.PrepareSavedBindings();
                foreach(var chest in root.GetComponentsInChildren<Chest>(true))chest.PrepareSavedBindings();
                foreach(var loot in root.GetComponentsInChildren<DungeonLoot>(true))loot.PrepareSavedPhysics();
                SavedModelResources.SaveMeshes(root,folder);SavedModelResources.SaveMaterials(root,folder,null);
                var prefab=PrefabUtility.SaveAsPrefabAsset(root,folder+"/"+root.name+".prefab",out bool success);
                if(!success || prefab==null)throw new IOException("Unity не сохранила prefab.");AssetDatabase.SaveAssets();Check(prefab);
                Undo.RecordObject(recipe,"Последний объект подземелья");recipe.LastPrefab=prefab;EditorUtility.SetDirty(recipe);AssetDatabase.SaveAssets();EditorGUIUtility.PingObject(prefab);
            } catch {AssetDatabase.DeleteAsset(folder);throw;}
            finally {if(root!=null)Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);UnityEngine.Random.state=state;}
        }
        static void Check(GameObject prefab) {
            var root=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(prefab));
            try {SavedModelResources.Validate(root);Debug.Log("[DungeonObject] Persistent references read. No gameplay/VR/portal-binding proof.",prefab);}
            finally {PrefabUtility.UnloadPrefabContents(root);}
        }
        static void Build(DungeonObjectRecipe r,Transform root) {
            var zero=Vector3.zero;var k=r.Kind;
            switch(k) {
                case DungeonObjectRecipe.ObjectKind.Stalactite:
                    var stone=Hazard.Create(root,zero,"сталактит",new Color(.48f,.62f,.65f),.9f,Dmg.TrapStalactite);stone.UpperCaveStoneVisual(r.CeilingHeight);break;
                case DungeonObjectRecipe.ObjectKind.Spores:
                    Hazard.Create(root,zero,r.Floor==2?"споровый гриб":"споры",r.Floor==1?new Color(.5f,.58f,.3f):new Color(1f,.9f,.3f),1,Dmg.TrapSpores,Status.Spores);break;
                case DungeonObjectRecipe.ObjectKind.Rockfall:
                    var rock=Hazard.Create(root,zero,"камнепад",new Color(.65f,.58f,.4f),1.1f,Dmg.TrapRockfall);rock.RuinsVisual(r.CeilingHeight);break;
                case DungeonObjectRecipe.ObjectKind.Vines:
                    var vines=Hazard.Create(root,zero,"лозы",new Color(.38f,.47f,.19f),1,Dmg.TrapVines,Status.Slowed);vines.RuinsVisual(0);break;
                case DungeonObjectRecipe.ObjectKind.DartPlate:
                    var darts=Hazard.Create(root,zero,"дротики",r.Floor==2?new Color(.62f,.51f,.30f):new Color(1f,.3f,.2f),r.Floor==2?.8f:.9f,Dmg.TrapDarts);
                    if(r.Floor==2){darts.RuinsVisual(0);OvergrownRuins.DartWallVisual(root,new Vector3(-1.47f,0,0));}break;
                case DungeonObjectRecipe.ObjectKind.Pendulum: Hazard.Create(root,zero,"маятник",new Color(.9f,.9f,1),1,Dmg.TrapPendulum);break;
                case DungeonObjectRecipe.ObjectKind.FireGrate: Hazard.Create(root,zero,"огненная решётка",new Color(1,.4f,.1f),.9f,Dmg.TrapFireGrate);break;
                case DungeonObjectRecipe.ObjectKind.AcidDrop: Hazard.Create(root,zero,"кислотная капля",new Color(.4f,1,.6f),.9f,Dmg.TrapAcid);break;
                case DungeonObjectRecipe.ObjectKind.Puddle:
                    var puddle=Puddle.Create(root,zero,1.15f,new Color(.18f,.34f,.36f));
                    foreach(var renderer in puddle.GetComponentsInChildren<Renderer>())renderer.enabled=false;break;
                case DungeonObjectRecipe.ObjectKind.Well: BuildWell(root,r.Floor);break;
                case DungeonObjectRecipe.ObjectKind.Elixir:
                    var elixir=ElixirPickup.Create(root,zero);
                    if(r.Floor<=2){elixir.transform.Find("Beam").GetComponent<Renderer>().enabled=false;foreach(var light in elixir.GetComponentsInChildren<Light>()){light.range=2;light.intensity=.35f;}}break;
                case DungeonObjectRecipe.ObjectKind.DoorWithHandles:
                    if(r.Floor==4)throw new InvalidOperationException("В логове нет отдельной исходной двери перехода. Входная решётка входит в арену.");
                    var door=TransitionDoor.Create(root,zero,r.Floor+1,DungeonManager.GetBiome(r.Floor));
                    if(r.Floor<=2)foreach(var name in new[]{"ArchL","ArchR","ArchTop"}) {var renderer=door.transform.Find(name).GetComponent<Renderer>();renderer.sharedMaterial=r.Floor==1?CavePrototype.Rock:OvergrownRuins.Stone;renderer.SetPropertyBlock(null);}
                    else DungeonArchitecture.SurfaceAll(root,3);break;
                case DungeonObjectRecipe.ObjectKind.StepsAndPlatform:
                    if(r.Floor==4)throw new InvalidOperationException("В логове нет отдельной лестницы: насесты уже входят в арену.");
                    for(int i=0;i<3;i++){float h=.2f*(i+1);SourceBox(root,r.Floor,"Step_"+(i+1),new Vector3(0,h*.5f,-1.6f+i*.6f),new Vector3(2.6f,h,.6f));}
                    SourceBox(root,r.Floor,r.Floor==2?"FlatWallLanding":"FlatUpperPlatform",new Vector3(0,.3f,.9f),new Vector3(3.2f,.6f,2));
                    if(r.Floor==3)DungeonArchitecture.SurfaceAll(root,3);break;
                case DungeonObjectRecipe.ObjectKind.PortalVisual:
                    var color=r.Floor==1?new Color(.35f,.47f,.58f):r.Floor==2?new Color(.61f,.47f,.26f):new Color(.48f,.6f,.85f);
                    if(r.Floor==4)throw new InvalidOperationException("Портал возврата логова создаётся только после победы. Не добавляем его заранее.");
                    Portal.Create(root,zero,r.Floor==3?"В логово":"На −"+(r.Floor+1),color,null,()=>false);break;
                case DungeonObjectRecipe.ObjectKind.ReturnPortalVisual:
                    if(r.Floor==4)throw new InvalidOperationException("Возврат логова создаётся только прежним OnDragonDefeated; не размещаем его заранее.");
                    Portal.Create(root,zero,"В таверну",r.Floor<=2?new Color(.65f,.53f,.32f):new Color(1f,.8f,.4f),null,()=>false);break;
                case DungeonObjectRecipe.ObjectKind.PhysicalLoot:
                    if((int)r.Loot<0 || (int)r.Loot>6)throw new InvalidOperationException("Выбери один из семи существующих трофеев, не кухонный овощ.");
                    DungeonLoot.Create(r.Loot,1,root,zero);break;
                case DungeonObjectRecipe.ObjectKind.FireCover: FireCover.Create(root,zero);break;
                case DungeonObjectRecipe.ObjectKind.FakeArmor: FakeArmor.Create(root,zero,0);break;
                case DungeonObjectRecipe.ObjectKind.Chest: Chest.Create(root,zero,0);break;
                default:throw new ArgumentOutOfRangeException();
            }
        }
        static void SourceBox(Transform root,int floor,string name,Vector3 at,Vector3 size) {
            if(floor==2){OvergrownRuins.Box(name,root,at,size,OvergrownRuins.Stone);return;}
            var go=Prim.Cube(name,root,at,size,floor==1?Color.white:new Color(.4f,.38f,.33f));
            if(floor==1){var renderer=go.GetComponent<Renderer>();renderer.sharedMaterial=CavePrototype.Rock;renderer.SetPropertyBlock(null);}
        }
        static void BuildWell(Transform root,int floor) {
            var well=HealPickup.Create(root,Vector3.zero,floor<=2?25:30,HealKind.Well);
            if(floor>2)return;
            well.transform.Find("WellRing").GetComponent<Renderer>().enabled=false;
            if(floor==2){well.transform.Find("Roof").GetComponent<Renderer>().enabled=false;foreach(var renderer in well.GetComponentsInChildren<Renderer>())if(renderer.name=="Post")renderer.enabled=false;}
            int count=floor==1?12:14;
            for(int j=0;j<count;j++) {
                float a=j*Mathf.PI/(count*.5f);var at=new Vector3(Mathf.Cos(a)*(floor==1?.43f:.47f),floor==1?.35f:.34f,Mathf.Sin(a)*(floor==1?.43f:.47f));
                GameObject stone;
                if(floor==2)stone=OvergrownRuins.Box("OldWellStone",well.transform,at,new Vector3(.22f,.68f,.20f),OvergrownRuins.Stone);
                else {stone=Prim.Cube("WellStone",well.transform,at,new Vector3(.24f,.65f,.19f),Color.white);stone.GetComponent<Renderer>().sharedMaterial=CavePrototype.Rock;stone.GetComponent<Renderer>().SetPropertyBlock(null);}
                stone.transform.localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);
            }
            var water=well.transform.Find("Water");water.localPosition=new Vector3(0,floor==1?.24f:.35f,0);
            water.GetComponent<Renderer>().sharedMaterial=floor==1?UpperCaves.Water:OvergrownRuins.Water;water.GetComponent<Renderer>().SetPropertyBlock(null);
        }
    }
}
