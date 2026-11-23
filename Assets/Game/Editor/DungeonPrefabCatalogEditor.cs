using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace Podz.EditorTools
{
    [CustomEditor(typeof(DungeonPrefabCatalog))]
    public class DungeonPrefabCatalogEditor : Editor
    {
        internal static string FloorFolder(int floor) => "Assets/Game/Dungeon/"+(floor==1?"Floor01_UpperCaves":floor==2?"Floor02_OvergrownRuins":floor==3?"Floor03_Fortress":"Floor04_DragonLair");
        internal static void Folder(string path) {
            if(AssetDatabase.IsValidFolder(path))return;
            string parent=Path.GetDirectoryName(path).Replace('\\','/');Folder(parent);
            if(string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent,Path.GetFileName(path))))throw new IOException(path);
        }
        [MenuItem("Podz/Подземелье/Создать конфигурацию блоков")]
        static void CreateConfig() {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            string path=EditorUtility.SaveFilePanelInProject("Новая конфигурация","FloorConfig","asset","Новое имя; старые assets не перезаписываются.","Assets/Game/Dungeon");
            if(string.IsNullOrEmpty(path))return;
            if(AssetDatabase.LoadMainAssetAtPath(path)!=null)throw new InvalidOperationException("Выбери новое имя.");
            var config=CreateInstance<DungeonPrefabCatalog>();AssetDatabase.CreateAsset(config,path);AssetDatabase.SaveAssets();Selection.activeObject=config;
        }
        public override void OnInspectorGUI() {
            serializedObject.Update();DrawPropertiesExcluding(serializedObject,"FortressRooms");
            if(serializedObject.FindProperty("Floor").intValue==3)EditorGUILayout.PropertyField(serializedObject.FindProperty("FortressRooms"));
            serializedObject.ApplyModifiedProperties();var c=(DungeonPrefabCatalog)target;
            EditorGUILayout.HelpBox("Ничего не создаётся при изменении полей. Один блок за нажатие. Floor1/2 — исходные связанные восемь комнат, Floor3 — прежний случайный план 6–9, Floor4 — исходная арена. Это НЕ готовая замена Main, спавнера добычи или переходов.",MessageType.Info);
            using(new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode)) {
                if(GUILayout.Button("Показать состав исходного плана (без геометрии)"))Run(()=>Describe(c));
                if(GUILayout.Button("Создать и сохранить ОДИН новый блок"))Run(()=>Generate(c));
                if(GUILayout.Button("Новая лёгкая сцена: последний блок"))Run(()=>Review(c));
                if(GUILayout.Button("Новый связный участок: две комнаты и коридор"))Run(()=>Section(c,false));
                if(GUILayout.Button("Новый этаж из назначенных блоков (только по подтверждению)"))
                    if(EditorUtility.DisplayDialog("Большая сцена","Только по твоей кнопке будет создан весь план. На слабом ноутбуке сначала проверь один блок или участок. Монстры/ловушки/прогрессия НЕ запускаются.","Создать","Отмена"))Run(()=>Section(c,true));
                if(c.LastPrefab!=null && GUILayout.Button("Проверить сохранённые ссылки последнего блока"))Run(()=>Check(c.LastPrefab));
            }
        }
        internal static void Run(Action action) {try{action();}catch(Exception ex){Debug.LogException(ex);EditorUtility.DisplayDialog("Подземелье: не завершено",ex.Message,"OK");}}
        static void Describe(DungeonPrefabCatalog c) {
            var p=DungeonSourcePlan.Read(c.Floor,c.Seed,c.FortressRooms);var text=new StringBuilder("[SourcePlan] Seed="+c.Seed+"; комнаты="+p.Rooms.Count+"; коридоры="+p.Edges.Count+"\n");
            for(int i=0;i<p.Rooms.Count;i++)text.AppendLine("Room "+i+": "+p.Key(false,i));
            for(int i=0;i<p.Edges.Count;i++)text.AppendLine("Corridor "+i+": "+p.Key(true,i)+"; rooms "+p.Edges[i]);
            Debug.Log(text.ToString(),c);
        }
        internal static void Generate(DungeonPrefabCatalog c) {
            var p=DungeonSourcePlan.Read(c.Floor,c.Seed,c.FortressRooms);int i=c.SourceIndex;
            if(i<0 || i>=(c.Corridor?p.Edges.Count:p.Rooms.Count))throw new InvalidOperationException("Индекс вне плана. Сначала нажми «Показать состав». У арены нет отдельных исходных коридоров.");
            string parent=FloorFolder(c.Floor)+"/Blocks";Folder(parent);
            string folder=AssetDatabase.GenerateUniqueAssetPath(parent+"/"+p.Key(c.Corridor,i)+"_Generated");Folder(folder);
            var scene=EditorSceneManager.NewPreviewScene();var state=UnityEngine.Random.state;GameObject root=null;
            try {
                UnityEngine.Random.InitState(c.Seed);root=new GameObject(p.Key(c.Corridor,i));SceneManager.MoveGameObjectToScene(root,scene);
                if(c.Floor==4)DragonLair.Build(root.transform);
                else if(c.Corridor) {
                    if(c.Floor==1)UpperCaves.BuildPassageForEditor(root.transform,p.Length(i),i);
                    else if(c.Floor==2)OvergrownRuins.BuildPassageForEditor(root.transform,p.Length(i),i);
                    else FloorGenerator.BuildFortressPassageForEditor(root.transform,p.Length(i));
                } else {
                    if(c.Floor==1)UpperCaves.BuildRoomForEditor(root.transform,i);
                    else if(c.Floor==2)OvergrownRuins.BuildRoomForEditor(root.transform,i);
                    else FloorGenerator.BuildFortressRoomForEditor(root.transform,p,i);
                }
                var block=root.AddComponent<DungeonBlock>();block.SourceKey=p.Key(c.Corridor,i);block.Floor=c.Floor;block.Corridor=c.Corridor;
                var ports=new List<DungeonBlock.Port>();float width=c.Floor==3?2.4f:c.Floor==1?2.8f:2.9f;
                if(c.Corridor) {
                    float len=p.Length(i);block.Footprint=new Bounds(new Vector3(0,2,0),new Vector3(width,4,len));
                    ports.Add(new DungeonBlock.Port{Position=Vector3.back*len*.5f,Forward=Vector3.back,Clearance=new Vector2(width,2.1f)});
                    ports.Add(new DungeonBlock.Port{Position=Vector3.forward*len*.5f,Forward=Vector3.forward,Clearance=new Vector2(width,2.1f)});
                } else {
                    var size=p.Rooms[i].size;block.Footprint=new Bounds(Vector3.up*size.y*.5f,size);
                    foreach(var d in p.Exits(i))ports.Add(new DungeonBlock.Port{Position=d*((Mathf.Abs(d.x)>.5f?size.x*.5f:size.z*.5f)+(c.Floor==3?FloorGenerator.Thick:0)),Forward=d,Clearance=new Vector2(width,2.1f)});
                    block.PlacementSearchZone=new Bounds(new Vector3(0,.5f,0),new Vector3(Mathf.Max(0,size.x-3),1,Mathf.Max(0,size.z-3)));
                }
                block.Connections=ports.ToArray();
                if(c.Floor==4) {
                    var points=new GameObject("ExistingLairPlacementPoints").transform;points.SetParent(root.transform,false);
                    AddPoints(points,"Ground",DragonLair.GroundPoints);AddPoints(points,"Perch",DragonLair.Perches);AddPoints(points,"Cover",DragonLair.CoverPoints);
                }
                SavedModelResources.SaveMeshes(root,folder);SavedModelResources.SaveMaterials(root,folder,null);
                var asset=PrefabUtility.SaveAsPrefabAsset(root,folder+"/"+root.name+".prefab",out bool success);
                if(!success || asset==null)throw new IOException("Unity не сохранила блок.");
                AssetDatabase.SaveAssets();Check(asset);
                Undo.RecordObject(c,"Добавить сохранённый блок");c.LastPrefab=asset;
                var list=new List<GameObject>(c.Blocks??new GameObject[0]);list.Add(asset);c.Blocks=list.ToArray();EditorUtility.SetDirty(c);AssetDatabase.SaveAssets();EditorGUIUtility.PingObject(asset);
            } catch {AssetDatabase.DeleteAsset(folder);throw;}
            finally {if(root!=null)Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);UnityEngine.Random.state=state;}
        }
        static void AddPoints(Transform parent,string name,Vector3[] points) {for(int i=0;i<points.Length;i++){var t=new GameObject(name+"_"+i).transform;t.SetParent(parent,false);t.localPosition=points[i];}}
        static void Check(GameObject prefab) {
            string path=AssetDatabase.GetAssetPath(prefab);var root=PrefabUtility.LoadPrefabContents(path);
            try {SavedModelResources.Validate(root);if(root.GetComponent<DungeonBlock>()==null)throw new InvalidOperationException("Нет DungeonBlock.");Debug.Log("[DungeonBlock] Persistent references read: "+path+". Not play/VR/clearance verification.");}
            finally {PrefabUtility.UnloadPrefabContents(root);}
        }
        static GameObject Resolve(DungeonPrefabCatalog c,DungeonSourcePlan p,bool corridor,int index) {
            string key=p.Key(corridor,index);GameObject found=null;
            foreach(var prefab in c.Blocks??new GameObject[0]) {
                if(prefab==null || !EditorUtility.IsPersistent(prefab))continue;var b=prefab.GetComponent<DungeonBlock>();
                if(b==null || b.SourceKey!=key)continue;
                if(found!=null)throw new InvalidOperationException("В конфигурации два варианта для "+key+". Оставь один назначенный вариант; ресурсы не удаляй.");
                if(b.Floor!=c.Floor || b.Corridor!=corridor)throw new InvalidOperationException("Несовместимые метаданные "+key);
                ValidatePorts(b,p,corridor,index);found=prefab;
            }
            if(found==null)throw new InvalidOperationException("Не назначен блок "+key+". Создай только этот индекс или назначь совместимый prefab. Автоматической подмены процедурной геометрией нет.");
            return found;
        }
        static void ValidatePorts(DungeonBlock b,DungeonSourcePlan p,bool corridor,int index) {
            var expected=corridor?new List<Vector3>{Vector3.back,Vector3.forward}:p.Exits(index);
            if(b.Connections==null || b.Connections.Length!=expected.Count)throw new InvalidOperationException("Изменено число соединений: "+b.SourceKey);
            var size=corridor?new Vector3(b.Footprint.size.x,b.Footprint.size.y,p.Length(index)):p.Rooms[index].size;
            if(Mathf.Abs(b.Footprint.size.x-size.x)>.01f || Mathf.Abs(b.Footprint.size.z-size.z)>.01f)throw new InvalidOperationException("Изменён размер блока: "+b.SourceKey);
            foreach(var d in expected) {
                var position=d*((Mathf.Abs(d.x)>.5f?size.x*.5f:size.z*.5f)+(!corridor && p.Floor==3?FloorGenerator.Thick:0));bool found=false;
                foreach(var port in b.Connections)if(Vector3.Dot(port.Forward.normalized,d)>.999f && Vector3.Distance(port.Position,position)<.01f && port.Clearance.x>=2.4f && port.Clearance.y>=2.1f)found=true;
                if(!found)throw new InvalidOperationException("Порт не совпадает с исходным проёмом: "+b.SourceKey);
            }
        }
        static void Review(DungeonPrefabCatalog c) {
            if(c.LastPrefab==null)throw new InvalidOperationException("Сначала создай один блок.");
            SaveScene("BlockReview",(scene,root)=>Place(c.LastPrefab,root,Vector3.zero,Quaternion.identity),c.Floor==4?36:12);
        }
        static void Section(DungeonPrefabCatalog c,bool whole) {
            var p=DungeonSourcePlan.Read(c.Floor,c.Seed,c.FortressRooms);var rooms=new List<int>();var edges=new List<int>();
            if(whole){for(int i=0;i<p.Rooms.Count;i++)rooms.Add(i);for(int i=0;i<p.Edges.Count;i++)edges.Add(i);}
            else {
                if(p.Edges.Count==0)throw new InvalidOperationException("У логова нет исходного участка из двух комнат. Проверяй арену последним блоком.");
                int edge=c.SourceIndex;if(edge<0 || edge>=p.Edges.Count)throw new InvalidOperationException("SourceIndex должен указывать коридор для участка.");
                edges.Add(edge);rooms.Add(p.Edges[edge].x);rooms.Add(p.Edges[edge].y);
            }
            // Resolve ALL dependencies before changing the current scene; no partial layout/fallback on missing catalog entries.
            var prefabs=new List<GameObject>();foreach(int i in rooms)prefabs.Add(Resolve(c,p,false,i));foreach(int i in edges)prefabs.Add(Resolve(c,p,true,i));
            for(int a=0;a<rooms.Count;a++)for(int b=a+1;b<rooms.Count;b++)if(p.Rooms[rooms[a]].Intersects(p.Rooms[rooms[b]]))throw new InvalidOperationException("Пересечение исходных комнат: генерация отменена.");
            var offset=p.Rooms[rooms[0]].center;offset.y=0;
            SaveScene(whole?"AssignedFloor":"TwoRoomReview",(scene,root)=> {
                int j=0;foreach(int i in rooms){var pos=p.Rooms[i].center-offset;pos.y=0;Place(prefabs[j++],root,pos,Quaternion.identity);}
                foreach(int i in edges){var e=p.Edges[i];var dir=p.Rooms[e.y].center-p.Rooms[e.x].center;Place(prefabs[j++],root,p.PassageCenter(i)-offset,Quaternion.LookRotation(dir));}
                PlaceReviewObjects(c,p,rooms,root,offset);
            },whole?50:18);
        }
        static void PlaceReviewObjects(DungeonPrefabCatalog c,DungeonSourcePlan p,List<int> rooms,Transform root,Vector3 offset)
        {
            foreach(var entry in c.ReviewPlacements??new DungeonPrefabCatalog.Placement[0])
            {
                if(entry==null || entry.Prefab==null || !EditorUtility.IsPersistent(entry.Prefab))throw new InvalidOperationException("Review placement requires a saved prefab.");
                if(entry.RoomIndex<0 || entry.RoomIndex>=p.Rooms.Count)throw new InvalidOperationException("Review placement room index outside source plan.");
                if(!rooms.Contains(entry.RoomIndex))continue;
                if(entry.Prefab.GetComponentInChildren<GameBootstrap>(true)!=null || entry.Prefab.GetComponentInChildren<DungeonManager>(true)!=null ||
                    entry.Prefab.GetComponentInChildren<PlayerRig>(true)!=null || entry.Prefab.GetComponentInChildren<Kitchen>(true)!=null || entry.Prefab.GetComponentInChildren<GameFlow>(true)!=null)
                    throw new InvalidOperationException("A review prefab must not contain Main/rig/services.");
                var room=p.Rooms[entry.RoomIndex];var local=entry.Position;
                if(Mathf.Abs(local.x)>room.extents.x-1.3f || Mathf.Abs(local.z)>room.extents.z-1.3f || local.y<0 || local.y>room.size.y)
                    throw new InvalidOperationException("Placement outside the inner room area; choose an accessible floor position.");
                foreach(var d in p.Exits(entry.RoomIndex))
                {
                    var side=Vector3.Cross(Vector3.up,d);float along=Vector3.Dot(local,d);
                    float half=Mathf.Abs(d.x)>.5f?room.extents.x:room.extents.z;
                    if(along>half-2.5f && Mathf.Abs(Vector3.Dot(local,side))<1.8f)
                        throw new InvalidOperationException("Placement obstructs an opening; move it away from the connection.");
                }
                var go=(GameObject)PrefabUtility.InstantiatePrefab(entry.Prefab,root);var at=room.center-offset;at.y=0;
                go.transform.localPosition=at+local;go.transform.localRotation=Quaternion.Euler(0,entry.Yaw,0);
                Physics.SyncTransforms();
                var others=root.GetComponentsInChildren<Collider>(true);
                foreach(var collider in go.GetComponentsInChildren<Collider>(true))
                {
                    if(!collider.enabled || collider.isTrigger || !collider.gameObject.activeInHierarchy)continue;
                    if(collider is MeshCollider mesh && !mesh.convex)throw new InvalidOperationException("Review placement needs a convex/primitive movable collider for overlap checks; no unsafe placement is accepted.");
                    foreach(var other in others)
                    {
                        if(other.transform.IsChildOf(go.transform) || !other.enabled || other.isTrigger || !other.gameObject.activeInHierarchy)continue;
                        if(Physics.ComputePenetration(collider,collider.transform.position,collider.transform.rotation,other,other.transform.position,other.transform.rotation,out var direction,out float distance) && distance>.02f)
                            throw new InvalidOperationException("Review placement intersects "+other.name+"; scene not saved. Move it or inspect a single object first.");
                    }
                }
            }
        }
        static void Place(GameObject prefab,Transform root,Vector3 position,Quaternion rotation) {
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root);go.transform.localPosition=position;go.transform.localRotation=rotation;
        }
        internal static void SaveScene(string name,Action<Scene,Transform> build,float distance) {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            string path=EditorUtility.SaveFilePanelInProject("Новая отдельная сцена",name,"unity","Старые сцены не перезаписываются.","Assets/Game/Scenes");
            if(string.IsNullOrEmpty(path))return;if(File.Exists(path))throw new InvalidOperationException("Выбери новое имя сцены.");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject("ExplicitPrefabPreview_NoMain").transform;build(scene,root);
            var owner=root.gameObject.AddComponent<DungeonPrefabPreview>();owner.OwnedInstances=new GameObject[root.childCount];
            for(int i=0;i<root.childCount;i++)owner.OwnedInstances[i]=root.GetChild(i).gameObject;
            var camera=new GameObject("ReviewCamera").AddComponent<Camera>();camera.transform.position=new Vector3(0,Mathf.Max(4,distance*.65f),-distance);camera.transform.LookAt(new Vector3(0,1,0));camera.farClipPlane=200;
            var light=new GameObject("ReviewLight").AddComponent<Light>();light.type=LightType.Directional;light.shadows=LightShadows.None;light.intensity=.8f;light.transform.rotation=Quaternion.Euler(45,-25,0);
            if(!EditorSceneManager.SaveScene(scene,path))throw new IOException("Сцена не сохранена.");
            Debug.Log("[DungeonPreview] "+path+". Geometry/catalog preview only. No Main, rig, AI Init, profile, loot distribution or portal callbacks. No automatic generation in Play.");
        }
    }
    [CustomEditor(typeof(DungeonPrefabPreview))]
    public class DungeonPrefabPreviewEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();var owner=(DungeonPrefabPreview)target;
            EditorGUILayout.HelpBox("Only recorded generated direct children can be cleared. Unrecorded hand-authored siblings, scene cameras, assets and other generator roots are left alone.",MessageType.Info);
            using(new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                if(GUILayout.Button("Очистить только свои записанные экземпляры"))
                    if(EditorUtility.DisplayDialog("Очистка своих экземпляров","Удалить перечисленные сгенерированные экземпляры В ЭТОЙ СЦЕНЕ? Ручные изменения внутри них тоже будут удалены. Исходные assets и посторонние объекты не меняются. Можно Undo.","Удалить","Отмена"))
                    {
                        foreach(var go in owner.OwnedInstances)if(go!=null && go.transform.parent==owner.transform)Undo.DestroyObjectImmediate(go);
                        Undo.RecordObject(owner,"Очистить список владельца");owner.OwnedInstances=new GameObject[0];EditorUtility.SetDirty(owner);EditorSceneManager.MarkSceneDirty(owner.gameObject.scene);
                    }
        }
    }

}
