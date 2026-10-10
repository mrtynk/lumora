using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Only player/UI wiring is reused. Terrain, art and route are authored here.
public static class KaranlikTepeSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/KaranlikTepe.unity";
    private const string ArtPath = "Assets/Scenes/KaranlikTepeData";
    private const string MatPath = "Assets/Materials/KaranlikTepe";
    public static readonly Vector2[] Route = {
        new Vector2(-42,-48), new Vector2(-42,-34), new Vector2(-30,-24),
        new Vector2(-20,-8), new Vector2(-20,2), new Vector2(-25,6),
        new Vector2(-8,20), new Vector2(20,22), new Vector2(8,29),
        new Vector2(8,44), new Vector2(30,50) };
    private static Material stone, path, wood, dormant, gold, purple;
    private static Mesh rockMesh;

    [MenuItem("Tools/Lumora/Build Karanlık Tepe Level")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            EditorSceneManager.OpenScene(ScenePath);
            Debug.Log("Mevcut Karanlık Tepe açıldı; manuel düzenlemeler korunuyor.");
            return;
        }
        Folder(MatPath); Folder(ArtPath);
        Scene scene = CopyWiring(ScenePath, true);
        stone = Mat(MatPath, "VioletRock", new Color(.25f,.22f,.35f));
        path = Mat(MatPath, "PaleStone", new Color(.48f,.47f,.59f));
        wood = Mat(MatPath, "FadedWood", new Color(.29f,.26f,.35f));
        dormant = Mat(MatPath, "SleepingLantern", new Color(.25f,.35f,.48f));
        gold = Mat(MatPath, "WarmLight", new Color(1,.74f,.27f), true);
        purple = Mat(MatPath, "HopeViolet", new Color(.63f,.43f,.76f), true);
        rockMesh = MakeRock();
        BuildEnvironment();
        Wire(scene);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.42f,.43f,.59f);
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(.16f,.17f,.29f);
        RenderSettings.fogStartDistance = 35; RenderSettings.fogEndDistance = 95;
        Camera camera = One<Camera>(scene);
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = RenderSettings.fogColor;
        camera.farClipPlane = 160;
        One<Light>(scene).intensity = .75f;
        Validate(scene);
        EditorSceneManager.SaveScene(scene, ScenePath); AssetDatabase.SaveAssets();
        Debug.Log("Karanlık Tepe hazır. Maceraya Işıklı Vadi'den başlayın.");
    }

    public static float Height(float z) => Mathf.Clamp01((z + 24) / 24) * 4 + Mathf.Clamp01((z - 12) / 20) * 6;
    public static Vector3 At(float x, float z, float y = 0) => new Vector3(x, Height(z) + y, z);

    private static void BuildEnvironment()
    {
        Transform entry = Root("01_KaranlikTepeGirisi"), trail = Root("02_KayipIsikPatikasi"),
            pass = Root("03_RuzgarliKayaGecidi"), field = Root("04_SonmusIsikAlani"),
            summit = Root("05_TepeZirvesi"), exit = Root("06_LumorayaDonus");
        var vertices = new List<Vector3>(); var triangles = new List<int>();
        for (int z=-60; z<60; z+=2)
            for (int x=-60; x<60; x+=2)
                Quad(vertices,triangles,At(x,z),At(x,z+2),At(x+2,z+2),At(x+2,z));
        Mesh terrain = SaveMesh("HillGround_120x120", vertices, triangles);
        GameObject ground = MeshObject("HillGround_120x120", entry, terrain, stone);
        ground.AddComponent<MeshCollider>().sharedMesh = terrain;
        // Wide ramped terrain, not a fall-prone series of isolated platforms.
        for (int i=1; i<Route.Length; i++)
        {
            Vector2 a=Route[i-1], b=Route[i];
            int steps=Mathf.CeilToInt(Vector2.Distance(a,b));
            for(int j=0;j<steps;j++)
            {
                Vector2 p=Vector2.Lerp(a,b,(j+.5f)/steps);
                var tile=Block("OldStoneRoad",trail,At(p.x,p.y,.035f),new Vector3(6,.05f,1.4f),path);
                tile.transform.rotation=Quaternion.Euler(0,Mathf.Atan2(b.x-a.x,b.y-a.y)*Mathf.Rad2Deg,0);
            }
        }
        // Wind-shaped rock shoulders obscure the next area without a maze.
        Rock(entry,At(-7,-34),new Vector3(20,15,13),stone,true);
        Rock(trail,At(-44,-12),new Vector3(10,13,16),stone,true);
        Rock(pass,At(0,0),new Vector3(12,14,15),stone,true);
        Rock(pass,At(-38,13),new Vector3(10,11,10),stone,true);
        for(int i=0;i<5;i++)
        {
            // Broad walkable ledges beside the ramp: decorative surface, no jump gaps.
            Rock(pass,At(-28-i*2,-6+i*3,-1),new Vector3(6,2,5),path);
        }
        // A visible ridge, with a single broad gate leading to the protected summit.
        Block("SummitRidgeWest",summit,At(-28,35,4),new Vector3(62,8,4),stone,true);
        Block("SummitRidgeEast",summit,At(38,35,4),new Vector3(44,8,4),stone,true);
        Block("SummitRidgeJoin",summit,At(14.5f,35,4),new Vector3(3,8,4),stone,true);
        // Landmark behind the goal, visible from the approach, never on the route.
        Rock(summit,At(8,57),new Vector3(8,17,5),purple);
        Sign(summit,At(8,53,12),"SON IŞIK TOHUMU",.12f);
        for(int i=0;i<16;i++)
        {
            float x=-60+i*8;
            Rock(entry,At(x,-61),new Vector3(6,6,4),stone,true);
            Rock(exit,At(x,61),new Vector3(6,7,4),stone,true);
            Rock(trail,At(-61,x),new Vector3(4,7,6),stone,true);
            Rock(field,At(61,x),new Vector3(4,7,6),stone,true);
        }
        var random=new System.Random(404);
        for(int i=0;i<70;i++)
        {
            float x=(float)random.NextDouble()*108-54,z=(float)random.NextDouble()*98-48;
            if(RouteDistance(new Vector2(x,z))<9 || z>30 || (z>4&&z<28&&x>-36&&x<30))continue;
            Transform area=z<0?trail:field;
            Vector3 p=At(x,z);
            Block("FadedTrunk",area,p+Vector3.up*2,new Vector3(.7f,4,.7f),wood);
            var branch=Block("BentBranch",area,p+new Vector3(.8f,3,0),new Vector3(2.5f,.35f,.35f),wood);
            branch.transform.rotation=Quaternion.Euler(0,20,25);
            Rock(area,p+Vector3.up*4.4f,new Vector3(2.2f,1.3f,1.7f),dormant);
        }
        foreach(Vector2 p in Route)
        {
            Rock(trail,At(p.x-4,p.y,.25f),new Vector3(.45f,.55f,.45f),gold);
            Sign(trail,At(p.x+4,p.y,.3f),"✧",.1f);
        }
        Sign(entry,At(-35,-44,2),"KARANLIK TEPE\nKüçük ışıkları takip et");
        Sign(pass,At(-28,-3,3),"Rüzgârlı Kaya Geçidi\nGeniş taş yoldan ilerle");
        Sign(field,At(-17,13,3),"Üç eski feneri uyandır\nYaklaş ve E'ye bas");
        foreach(Transform area in new[]{entry,trail,pass,field,summit,exit}) Combine(area);
    }

    private static void Wire(Scene scene)
    {
        PlayerController player=One<PlayerController>(scene);
        RegionProgressController progress=One<RegionProgressController>(scene);
        GameEventSender sender=One<GameEventSender>(scene);
        LevelFollowCamera camera=One<LevelFollowCamera>(scene);
        Transform spawn=Root("HillSpawn"); spawn.position=At(-42,-48,1.15f);
        player.transform.position=spawn.position;
        Edit(camera,s=>{s.FindProperty("offset").vector3Value=new Vector3(0,20,-10);s.FindProperty("lookOffset").vector3Value=new Vector3(0,1,4);});
        camera.SnapToTarget();
        Transform interactions=scene.GetRootGameObjects().Single(r=>r.name=="ValleyInteractions").transform;
        interactions.name="HillOptionalInteractions"; interactions.gameObject.SetActive(true);
        foreach(LightSeedCollectible old in interactions.GetComponentsInChildren<LightSeedCollectible>(true))Object.DestroyImmediate(old.gameObject);
        foreach(PortalTrigger old in interactions.GetComponentsInChildren<PortalTrigger>(true))Object.DestroyImmediate(old.gameObject);
        foreach(Object ui in new Object[]{One<PuzzlePopupUI>(scene),One<NpcDialogueUI>(scene)})
            Edit(ui,s=>{s.FindProperty("region").stringValue="karanlik_tepe";s.FindProperty("demoFlowController").objectReferenceValue=null;});
        foreach(PuzzleTrigger station in interactions.GetComponentsInChildren<PuzzleTrigger>(true))
        {
            string type=new SerializedObject(station).FindProperty("preferredPuzzleType").stringValue;
            if(type=="hidden_object"){Object.DestroyImmediate(station.gameObject);continue;}
            station.transform.position=type=="memory_match"?At(-38,20):At(37,23);
        }
        NpcInteractionTrigger npc=One<NpcInteractionTrigger>(scene);
        npc.name="IsikKoruyucusu";npc.transform.position=At(-35,-20,1);
        foreach(TextMesh label in npc.GetComponentsInChildren<TextMesh>())label.text="Işık Koruyucusu · E";
        foreach(Text text in One<NpcDialogueUI>(scene).GetComponentsInChildren<Text>(true))
        {
            if(text.name=="Title"||text.text=="Orman Dostu")text.text="Işık Koruyucusu";
            if(text.name=="DialogueText")text.text="Son ışık tohumu zirvede. Eski fenerleri uyandırırsan yol sana görünecek.";
        }
        var seedObject=new GameObject("FourthLightSeed");seedObject.transform.position=At(8,44,1.2f);
        seedObject.AddComponent<SphereCollider>().radius=1.2f;Trigger(seedObject);
        var seed=seedObject.AddComponent<LightSeedCollectible>();
        Set(seed,"progressController",progress);Edit(seed,s=>s.FindProperty("regionIndex").intValue=3);
        Rock(seedObject.transform,seedObject.transform.position,new Vector3(.8f,1.5f,.8f),gold).isStatic=false;
        Transform portalRoot=Root("PortalToLumoraFinal");portalRoot.position=At(30,50);
        var trigger=portalRoot.gameObject.AddComponent<BoxCollider>();trigger.center=Vector3.up*2;trigger.size=new Vector3(6,4,4);Trigger(portalRoot.gameObject);
        var barrier=Block("FinalPortalBarrier",portalRoot,At(30,50,2),new Vector3(4,4,.3f),dormant,true);barrier.isStatic=false;
        Block("PortalPillarLeft",portalRoot,At(27,50,3),new Vector3(1,6,1),gold);
        Block("PortalPillarRight",portalRoot,At(33,50,3),new Vector3(1,6,1),gold);
        Block("PortalCrown",portalRoot,At(30,50,6),new Vector3(7,1,1),gold);
        Sign(portalRoot,At(30,50,8),"IŞIK AĞACI'NA DÖN");
        var portal=portalRoot.gameObject.AddComponent<PortalTrigger>();
        Set(portal,"progressController",progress);Set(portal,"portalRenderer",barrier.GetComponent<Renderer>());
        Set(portal,"blockingCollider",barrier.GetComponent<Collider>());Set(portal,"lockedMaterial",dormant);Set(portal,"openMaterial",gold);
        Edit(portal,s=>{s.FindProperty("regionIndex").intValue=3;s.FindProperty("isFinalPortal").boolValue=true;s.FindProperty("destinationSceneName").stringValue="LumoraFinal";});
        Edit(progress,s=>{
            s.FindProperty("startAutomatically").boolValue=false;s.FindProperty("useSceneTransitions").boolValue=true;
            s.FindProperty("sceneRegionIndex").intValue=3;s.FindProperty("regionOneGuide").objectReferenceValue=null;
            Array(s,"regionSpawnPoints",spawn);Array(s,"lightSeeds",seed);Array(s,"portals",portal);
        });
        GameObject gate=Block("SummitLightGate",null,At(8,35,4),new Vector3(10,8,4),dormant,true);gate.isStatic=false;
        LightBeacon[] beacons={ Beacon("1",At(-25,8)),Beacon("2",At(-8,22)),Beacon("3",At(20,24)) };
        var level=new GameObject("KaranlikTepeLevel").AddComponent<KaranlikTepeLevelController>();
        Set(level,"progress",progress);Set(level,"eventSender",sender);Set(level,"player",player);Set(level,"followCamera",camera);
        Set(level,"spawnPoint",spawn);Set(level,"summitGate",gate);Set(level,"lightSeed",seed);
        Edit(level,s=>{var a=s.FindProperty("beacons");a.arraySize=3;for(int i=0;i<3;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=beacons[i];});
        Text instruction=Label(progress.transform,"BeaconInstruction","Üç eski feneri uyandır.",new Vector2(.3f,.87f),new Vector2(.98f,.98f),25);
        Set(level,"instruction",instruction);
        foreach(Text t in progress.GetComponentsInChildren<Text>(true))
        {
            if(t.name=="ActiveRegionText")t.text="Aktif Bölge: Karanlık Tepe";
            if(t.name=="SeedCountText")t.text="Işık Tohumu: 3 / 4";
            if(t.name=="ObjectiveText")t.text="Son Işık Tohumu'nu bul.";
        }
    }

    private static LightBeacon Beacon(string id,Vector3 p)
    {
        Transform root=Root("LightBeacon_"+id);root.position=p;
        Block("AncientPedestal",root,p+Vector3.up*.5f,new Vector3(1.5f,1,1.5f),stone);
        GameObject lantern=Rock(root,p+Vector3.up*1.8f,new Vector3(.8f,1.4f,.8f),dormant);lantern.isStatic=false;
        Transform lit=new GameObject("IlluminatedRoute").transform;lit.SetParent(root,false);
        for(int i=0;i<5;i++)Rock(lit,At(p.x+3,p.z+i*1.4f,.15f),new Vector3(.5f,.3f,.5f),gold).isStatic=false;
        lit.gameObject.SetActive(false);
        Sign(root,p+Vector3.up*3.8f,"FENER "+id+" · E");
        var beacon=root.gameObject.AddComponent<LightBeacon>();
        Edit(beacon,s=>s.FindProperty("beaconId").stringValue=id);
        Set(beacon,"lantern",lantern.GetComponent<Renderer>());Set(beacon,"unlitMaterial",dormant);Set(beacon,"litMaterial",gold);Set(beacon,"illuminatedRoute",lit.gameObject);
        return beacon;
    }

    public static void Validate(Scene scene)
    {
        One<PlayerController>(scene);One<Camera>(scene);One<AudioListener>(scene);One<EventSystem>(scene);
        One<GameEventSender>(scene);One<RegionProgressController>(scene);One<LightSeedCollectible>(scene);One<PortalTrigger>(scene);
        One<KaranlikTepeLevelController>(scene);One<PuzzlePopupUI>(scene);One<NpcDialogueUI>(scene);
        if(scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<LightBeacon>(true)).Count()!=3)throw new Exception("Üç fener gerekli.");
        foreach(Transform t in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)))
            if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new Exception("Missing script: "+t.name);
    }

    public static Scene CopyWiring(string destination,bool optionalContent)
    {
        if(!AssetDatabase.CopyAsset(LumoraLevelSceneBuilder.ScenePath,destination))throw new Exception("Sahne bağlantı şablonu kopyalanamadı.");
        Scene scene=EditorSceneManager.OpenScene(destination);
        var keep=new HashSet<string>{"Player","GameManager","Main Camera","RegionProgressCanvas","Directional Light","EventSystem"};
        if(optionalContent){keep.Add("PuzzleCanvas");keep.Add("NpcCanvas");keep.Add("ValleyInteractions");}
        foreach(GameObject root in scene.GetRootGameObjects())if(!keep.Contains(root.name))Object.DestroyImmediate(root);
        return scene;
    }
    public static T One<T>(Scene scene) where T:Component=>scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).Single();
    public static Transform Root(string name)=>new GameObject(name).transform;
    public static void Edit(Object obj,Action<SerializedObject> change){var s=new SerializedObject(obj);change(s);s.ApplyModifiedPropertiesWithoutUndo();}
    public static void Set(Object obj,string field,Object value)=>Edit(obj,s=>s.FindProperty(field).objectReferenceValue=value);
    public static void Array(SerializedObject s,string field,Object value){var a=s.FindProperty(field);a.arraySize=1;a.GetArrayElementAtIndex(0).objectReferenceValue=value;}
    public static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;int i=path.LastIndexOf('/');Folder(path.Substring(0,i));AssetDatabase.CreateFolder(path.Substring(0,i),path.Substring(i+1));}
    public static Material Mat(string directory,string name,Color color,bool emission=false)
    {
        string asset=directory+"/"+name+".mat";
        Material existing=AssetDatabase.LoadAssetAtPath<Material>(asset);if(existing!=null)return existing;
        var material=new Material(Shader.Find("Standard")){name=name,color=color};material.SetFloat("_Glossiness",.1f);
        if(emission){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",color*.65f);}
        AssetDatabase.CreateAsset(material,asset);return material;
    }
    public static GameObject Block(string name,Transform parent,Vector3 p,Vector3 size,Material material,bool solid=false)
    {
        var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(parent,true);obj.transform.position=p;obj.transform.localScale=size;
        obj.GetComponent<Renderer>().sharedMaterial=material;if(!solid)Object.DestroyImmediate(obj.GetComponent<Collider>());obj.isStatic=true;return obj;
    }
    public static void Sign(Transform parent,Vector3 p,string message,float size=.07f)
    {
        var obj=new GameObject("LumoraSign");obj.transform.SetParent(parent,true);obj.transform.position=p;obj.transform.rotation=Quaternion.Euler(40,0,0);
        var text=obj.AddComponent<TextMesh>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        obj.GetComponent<Renderer>().sharedMaterial=text.font.material;text.text=message;text.fontSize=48;text.characterSize=size;
        text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=new Color(1,.87f,.59f);
    }
    public static Text Label(Transform parent,string name,string content,Vector2 min,Vector2 max,int fontSize)
    {
        var text=new GameObject(name,typeof(RectTransform)).AddComponent<Text>();text.transform.SetParent(parent,false);
        text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=fontSize;text.alignment=TextAnchor.MiddleCenter;
        text.color=new Color(1,.88f,.6f);text.text=content;text.raycastTarget=false;
        var rect=text.rectTransform;rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;return text;
    }
    private static void Trigger(GameObject obj){obj.GetComponent<Collider>().isTrigger=true;var rb=obj.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;}
    private static GameObject Rock(Transform parent,Vector3 p,Vector3 size,Material material,bool solid=false)
    {
        GameObject obj=MeshObject("WindSculptedRock",parent,rockMesh,material);obj.transform.position=p;obj.transform.localScale=size;
        if(solid){var c=obj.AddComponent<BoxCollider>();c.size=new Vector3(1.4f,1.4f,1.4f);}
        return obj;
    }
    private static GameObject MeshObject(string name,Transform parent,Mesh mesh,Material material)
    {
        var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(parent,false);
        obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<Renderer>().sharedMaterial=material;obj.isStatic=true;return obj;
    }
    private static Mesh MakeRock()
    {
        Vector3[] p={Vector3.up,Vector3.down,Vector3.left,Vector3.forward,Vector3.right,Vector3.back};
        var v=new List<Vector3>();var t=new List<int>();
        for(int i=0;i<4;i++){int a=2+i,b=2+(i+1)%4;Quad(v,t,p[0],p[a],p[b],p[b]);Quad(v,t,p[1],p[b],p[a],p[a]);}
        return SaveMesh("LowPolyRock",v,t);
    }
    private static void Quad(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c,Vector3 d){int n=v.Count;v.AddRange(new[]{a,b,c,a,c,d});for(int i=0;i<6;i++)t.Add(n+i);}
    private static Mesh SaveMesh(string name,List<Vector3> v,List<int> t)
    {
        var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,ArtPath+"/"+name+".asset");return mesh;
    }
    private static float RouteDistance(Vector2 p)
    {
        float result=float.MaxValue;
        for(int i=1;i<Route.Length;i++){Vector2 d=Route[i]-Route[i-1];float f=Mathf.Clamp01(Vector2.Dot(p-Route[i-1],d)/d.sqrMagnitude);result=Mathf.Min(result,Vector2.Distance(p,Route[i-1]+d*f));}
        return result;
    }
    private static void Combine(Transform area)
    {
        foreach(var group in area.GetComponentsInChildren<MeshRenderer>().Where(r=>r.GetComponent<MeshFilter>()!=null).GroupBy(r=>r.sharedMaterial).ToArray())
        {
            var mesh=new Mesh{name=area.name+"_"+group.Key.name,indexFormat=IndexFormat.UInt32};
            mesh.CombineMeshes(group.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=r.transform.localToWorldMatrix}).ToArray());
            AssetDatabase.CreateAsset(mesh,ArtPath+"/"+mesh.name+".asset");
            foreach(var r in group){Object.DestroyImmediate(r.GetComponent<MeshFilter>());Object.DestroyImmediate(r);}
            MeshObject("Static_"+mesh.name,area,mesh,group.Key);
        }
    }
}
