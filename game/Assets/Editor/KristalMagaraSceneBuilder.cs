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

public static class KristalMagaraSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/KristalMagara.unity";
    private const string ArtPath = "Assets/Scenes/KristalMagaraData";
    private const string MatPath = "Assets/Materials/KristalMagara";
    public static readonly Vector2[] Route = {
        new Vector2(-38,-44), new Vector2(-38,-24), new Vector2(-14,-20),
        new Vector2(-14,-12), new Vector2(-14,-2), new Vector2(-14,8),
        new Vector2(-14,18), new Vector2(-8,20), new Vector2(8,20),
        new Vector2(8,30), new Vector2(8,44), new Vector2(30,48) };
    private static Material stone, floor, cyan, violet, pink, gold, water, beamMaterial;
    private static Mesh crystalMesh;

    [MenuItem("Tools/Lumora/Build Kristal Mağara Level")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            EditorSceneManager.OpenScene(ScenePath);
            Debug.Log("Mevcut Kristal Mağara açıldı; elle yapılan düzenlemeler korunuyor.");
            return;
        }
        Folder(MatPath); Folder(ArtPath);
        if (!AssetDatabase.CopyAsset(LumoraLevelSceneBuilder.ScenePath, ScenePath))
            throw new InvalidOperationException("Işıklı Vadi bağlantı şablonu kopyalanamadı.");
        Scene scene = EditorSceneManager.OpenScene(ScenePath);
        // Only reuse player/UI wiring. All environment geometry below is cave-specific.
        var keep = new HashSet<string> { "Player", "GameManager", "Main Camera", "RegionProgressCanvas",
            "Directional Light", "EventSystem", "PuzzleCanvas", "NpcCanvas", "ValleyInteractions" };
        foreach (GameObject root in scene.GetRootGameObjects())
            if (!keep.Contains(root.name)) Object.DestroyImmediate(root);
        stone = Mat("CaveStone", new Color(.19f,.20f,.32f));
        floor = Mat("StonePath", new Color(.43f,.47f,.62f));
        cyan = Mat("TurquoiseCrystal", new Color(.2f,.9f,1), true);
        violet = Mat("VioletCrystal", new Color(.55f,.38f,.92f), true);
        pink = Mat("PinkCrystal", new Color(1,.4f,.78f), true);
        gold = Mat("SeedGold", new Color(1,.84f,.3f), true);
        water = Mat("UndergroundWater", new Color(.12f,.37f,.49f));
        beamMaterial = new Material(Shader.Find("Sprites/Default")) { name = "CrystalBeam" };
        AssetDatabase.CreateAsset(beamMaterial, MatPath + "/CrystalBeam.mat");
        crystalMesh = MakeCrystal();
        BuildEnvironment();
        WireGameplay(scene);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.58f,.61f,.78f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(.13f,.17f,.28f);
        RenderSettings.fogStartDistance = 24; RenderSettings.fogEndDistance = 65;
        Camera camera = One<Camera>(scene);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = RenderSettings.fogColor;
        camera.farClipPlane = 100;
        One<Light>(scene).intensity = .75f;
        Validate(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Kristal Mağara oluşturuldu. Teste Işıklı Vadi'den başlayın.");
    }

    public static float Height(float z) => Mathf.Clamp01((z - 8) / 8) * 2;
    private static Vector3 At(float x, float z, float up = 0) => new Vector3(x, Height(z) + up, z);

    private static void BuildEnvironment()
    {
        Transform entry = Root("01_MagaraGirisi");
        Transform corridor = Root("02_ParlayanKristalKoridoru");
        Transform pool = Root("03_YeraltiSuyuTasGecisi");
        Transform room = Root("04_KristalIsikOdasi");
        Transform sanctuary = Root("05_IsikTohumuTapinagi");
        // Two terraces and a broad slope; only one terrain collider.
        var vertices = new List<Vector3>(); var triangles = new List<int>();
        for (int z = -55; z < 55; z += 2)
            for (int x = -55; x < 55; x += 2)
                Quad(vertices, triangles, At(x,z), At(x,z+2), At(x+2,z+2), At(x+2,z));
        Mesh groundMesh = SaveMesh("CaveFloor", vertices, triangles);
        var ground = MeshObject("CaveFloor_110x110", entry, groundMesh, stone);
        ground.AddComponent<MeshCollider>().sharedMesh = groundMesh;

        // Wide bends and tall partitions conceal the next chamber from spawn.
        Block("OuterWest", entry, new Vector3(-56,8,0), new Vector3(3,18,114), stone, true);
        Block("OuterEast", room, new Vector3(56,8,0), new Vector3(3,18,114), stone, true);
        Block("OuterSouth", entry, new Vector3(0,8,-56), new Vector3(114,18,3), stone, true);
        Block("OuterNorth", sanctuary, new Vector3(0,8,56), new Vector3(114,18,3), stone, true);
        Block("EntryBendWall", corridor, new Vector3(15,6,-28), new Vector3(80,12,5), stone, true);
        Block("PoolWestWall", pool, new Vector3(-41,6,5), new Vector3(28,12,4), stone, true);
        Block("PoolEastWall", pool, new Vector3(25,6,5), new Vector3(60,12,4), stone, true);
        Block("SanctuaryWestWall", sanctuary, new Vector3(-26,8,34), new Vector3(58,12,3), stone, true);
        Block("SanctuaryEastWall", sanctuary, new Vector3(34,8,34), new Vector3(42,12,3), stone, true);
        for (int i = 1; i < Route.Length; i++)
        {
            Vector2 a = Route[i-1], b = Route[i];
            int count = Mathf.CeilToInt(Vector2.Distance(a,b));
            for (int j = 0; j < count; j++)
            {
                Vector2 p = Vector2.Lerp(a,b,(j+.5f)/count);
                if (p.y > -12 && p.y < -2) continue;
                var tile = Block("Path", corridor, At(p.x,p.y,.04f), new Vector3(5,.06f,1.3f), floor);
                tile.transform.rotation = Quaternion.Euler(0, Mathf.Atan2(b.x-a.x,b.y-a.y)*Mathf.Rad2Deg,0);
            }
        }
        Block("ShallowPool", pool, new Vector3(0,.06f,-7), new Vector3(106,.08f,10), water);
        // Overlapping, broad stepping stones: no jumping and no precision gaps.
        for (int i = 0; i < 5; i++)
            Block("SteppingStone", pool, new Vector3(-14,.12f,-12+i*2.5f), new Vector3(7,.24f,3), floor, true);
        Arch(entry, At(-38,-49), cyan);
        Arch(corridor, At(-38,-25), violet);
        Arch(room, At(-14,12), pink);
        Sign(entry, At(-32,-40,2), "KRİSTAL MAĞARA\nParlayan taşları takip et");
        Sign(pool, At(-23,-14,2), "Taşların üzerinden yürü\nZıplamana gerek yok");
        Sign(room, At(-14,17,2), "Işığı hedeflere yönelt\nKristalin yanında E'ye bas");
        Sign(sanctuary, At(8,40,2), "IŞIK TOHUMU");

        var random = new System.Random(3013);
        for (int i = 0; i < 120; i++)
        {
            float x = (float)random.NextDouble()*100-50, z = (float)random.NextDouble()*100-50;
            if (RouteDistance(new Vector2(x,z)) < 7 || (z > 15 && z < 34 && x > -20 && x < 20)) continue;
            Transform parent = z < -28 ? entry : z < 5 ? corridor : z < 34 ? room : sanctuary;
            Material m = i%3 == 0 ? cyan : i%3 == 1 ? violet : pink;
            Crystal(parent, At(x,z), new Vector3(1.1f,3+(float)random.NextDouble()*5,1.1f), m);
            Crystal(parent, At(x+1.5f,z+.5f), new Vector3(.7f,2,.7f), m);
        }
        // Emissive route markers, not individual realtime lights.
        foreach (Vector2 p in Route)
            Crystal(corridor, At(p.x-3,p.y), new Vector3(.3f,1.1f,.3f), cyan);
        foreach (Transform area in new[] { entry, corridor, pool, room, sanctuary }) CombineDecor(area);
    }

    private static void WireGameplay(Scene scene)
    {
        PlayerController player = One<PlayerController>(scene);
        RegionProgressController progress = One<RegionProgressController>(scene);
        GameEventSender sender = One<GameEventSender>(scene);
        LevelFollowCamera camera = One<LevelFollowCamera>(scene);
        Transform spawn = Root("CaveSpawn"); spawn.position = At(-38,-44,1.15f);
        player.transform.position = spawn.position;
        Edit(camera, s => { s.FindProperty("offset").vector3Value = new Vector3(0,20,-8); s.FindProperty("lookOffset").vector3Value = new Vector3(0,1,3); });
        camera.SnapToTarget();
        Transform interactions = scene.GetRootGameObjects().Single(r => r.name == "ValleyInteractions").transform;
        interactions.name = "CaveOptionalInteractions"; interactions.gameObject.SetActive(true);
        foreach (LightSeedCollectible old in interactions.GetComponentsInChildren<LightSeedCollectible>(true)) Object.DestroyImmediate(old.gameObject);
        foreach (PortalTrigger old in interactions.GetComponentsInChildren<PortalTrigger>(true)) Object.DestroyImmediate(old.gameObject);
        PuzzlePopupUI popup = One<PuzzlePopupUI>(scene);
        NpcDialogueUI dialogue = One<NpcDialogueUI>(scene);
        foreach (Object ui in new Object[] { popup, dialogue })
            Edit(ui, s => { s.FindProperty("region").stringValue = "kristal_magara"; s.FindProperty("demoFlowController").objectReferenceValue = null; });
        foreach (PuzzleTrigger station in interactions.GetComponentsInChildren<PuzzleTrigger>(true))
        {
            string type = new SerializedObject(station).FindProperty("preferredPuzzleType").stringValue;
            if (type == "memory_match") { Object.DestroyImmediate(station.gameObject); continue; }
            station.transform.position = type == "hidden_object" ? At(-46,-16) : At(25,22);
            foreach (TextMesh label in station.GetComponentsInChildren<TextMesh>()) label.color = Color.white;
        }
        NpcInteractionTrigger npc = One<NpcInteractionTrigger>(scene);
        npc.transform.position = At(-30,-19,1);
        npc.name = "KristalBekcisi";
        foreach (TextMesh label in npc.GetComponentsInChildren<TextMesh>()) { label.text = "Kristal Bekçisi · E"; label.color = Color.white; }
        foreach (Text text in dialogue.GetComponentsInChildren<Text>(true))
        {
            if (text.name == "Title") text.text = "Kristal Bekçisi";
            if (text.name == "DialogueText") text.text = "Kristaller ışığı birbirine aktarır. Parlayan yolu takip edersen tohumu bulabilirsin.";
            if (text.text == "Orman Dostu") text.text = "Kristal Bekçisi";
        }

        var seedObject = new GameObject("ThirdLightSeed"); seedObject.transform.position = At(8,44,1.2f);
        seedObject.AddComponent<SphereCollider>().radius = 1.2f; Trigger(seedObject);
        var seed = seedObject.AddComponent<LightSeedCollectible>();
        Set(seed,"progressController",progress); Edit(seed,s=>s.FindProperty("regionIndex").intValue=2);
        Crystal(seedObject.transform, seedObject.transform.position, new Vector3(.8f,1.6f,.8f), gold).isStatic = false;
        Transform portalRoot = Root("PortalToKaranlikTepe"); portalRoot.position = At(30,48);
        var trigger = portalRoot.gameObject.AddComponent<BoxCollider>(); trigger.center = Vector3.up*2; trigger.size = new Vector3(6,4,4); Trigger(portalRoot.gameObject);
        GameObject gate = Block("PortalGate", portalRoot, At(30,48,2), new Vector3(4,4,.3f), stone,true); gate.isStatic=false;
        Arch(portalRoot, At(30,48), violet);
        PortalTrigger portal = portalRoot.gameObject.AddComponent<PortalTrigger>();
        Set(portal,"progressController",progress); Set(portal,"portalRenderer",gate.GetComponent<Renderer>());
        Set(portal,"blockingCollider",gate.GetComponent<Collider>()); Set(portal,"lockedMaterial",stone); Set(portal,"openMaterial",violet);
        Edit(portal,s=>{s.FindProperty("regionIndex").intValue=2;s.FindProperty("destinationSceneName").stringValue="KaranlikTepe";});
        Sign(portalRoot, At(30,48,6), "KARANLIK TEPE");
        Edit(progress,s=>{
            s.FindProperty("startAutomatically").boolValue=false; s.FindProperty("useSceneTransitions").boolValue=true;
            s.FindProperty("sceneRegionIndex").intValue=2; s.FindProperty("regionOneGuide").objectReferenceValue=null;
            Array(s,"regionSpawnPoints",spawn); Array(s,"lightSeeds",seed); Array(s,"portals",portal);
        });
        GameObject door = Block("LightRoomStoneDoor", null, At(8,34,4), new Vector3(10,8,3), stone,true); door.isStatic=false;
        Transform receiver = Root("LightReceiver"); receiver.position = At(8,32);
        Crystal(receiver, receiver.position, new Vector3(.8f,2,.8f), gold);
        CrystalLightNode first = Node("Crystal_A", At(-8,22), 0, 1);
        CrystalLightNode second = Node("Crystal_B", At(8,22), 3, 0);
        Set(first,"target",second.transform); Set(second,"target",receiver);
        // Ground arrows make correct directions readable without trial-and-error penalties.
        Sign(first.transform, At(-8,22,3), "1 · E ile çevir →");
        Sign(second.transform, At(8,22,3), "2 · E ile çevir ↑");
        var level = new GameObject("KristalMagaraLevel").AddComponent<KristalMagaraLevelController>();
        Set(level,"progress",progress);Set(level,"eventSender",sender);Set(level,"player",player);
        Set(level,"followCamera",camera);Set(level,"spawnPoint",spawn);Set(level,"stoneDoor",door);Set(level,"lightSeed",seed);
        Edit(level,s=>{var p=s.FindProperty("nodes");p.arraySize=2;p.GetArrayElementAtIndex(0).objectReferenceValue=first;p.GetArrayElementAtIndex(1).objectReferenceValue=second;});
        Text instruction = new GameObject("CrystalInstruction",typeof(RectTransform)).AddComponent<Text>();
        instruction.transform.SetParent(progress.transform,false); instruction.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        instruction.fontSize=23; instruction.alignment=TextAnchor.MiddleCenter; instruction.color=Color.cyan; instruction.raycastTarget=false;
        RectTransform rect=instruction.rectTransform;rect.anchorMin=new Vector2(.27f,.86f);rect.anchorMax=new Vector2(.97f,.98f);rect.offsetMin=rect.offsetMax=Vector2.zero;
        instruction.text="Parlayan iki kristale yaklaş ve E ile ışığı yönelt.";Set(level,"instruction",instruction);
        foreach(Text t in progress.GetComponentsInChildren<Text>(true))
        {
            if(t.name=="ActiveRegionText") t.text="Aktif Bölge: Kristal Mağara";
            if(t.name=="SeedCountText") t.text="Işık Tohumu: 2 / 4";
            if(t.name=="ObjectiveText") t.text="Kristal Mağara’daki Işık Tohumu’nu bul.";
        }
    }

    private static CrystalLightNode Node(string name, Vector3 position, int initial, int correct)
    {
        Transform root=Root(name);root.position=position;
        GameObject crystal=Crystal(root,position,new Vector3(.9f,2.3f,.9f),pink);crystal.isStatic=false;
        Transform pointer=new GameObject("DirectionPointer").transform;pointer.SetParent(root,false);
        GameObject arrow=Block("Arrow",pointer,position+new Vector3(0,1.8f,1.4f),new Vector3(.25f,.25f,2),gold);arrow.isStatic=false;
        var beam=root.gameObject.AddComponent<LineRenderer>();beam.positionCount=2;beam.useWorldSpace=true;beam.widthMultiplier=.16f;
        beam.sharedMaterial=beamMaterial;beam.shadowCastingMode=ShadowCastingMode.Off;beam.receiveShadows=false;
        var node=root.gameObject.AddComponent<CrystalLightNode>();
        Set(node,"pointer",pointer);Set(node,"crystal",crystal.GetComponent<Renderer>());Set(node,"beam",beam);
        Set(node,"waitingMaterial",pink);Set(node,"connectedMaterial",cyan);
        Edit(node,s=>{s.FindProperty("initialDirection").intValue=initial;s.FindProperty("correctDirection").intValue=correct;});
        return node;
    }

    public static void Validate(Scene scene)
    {
        One<PlayerController>(scene);One<Camera>(scene);One<AudioListener>(scene);One<EventSystem>(scene);
        One<GameEventSender>(scene);One<RegionProgressController>(scene);One<LightSeedCollectible>(scene);One<PortalTrigger>(scene);
        One<KristalMagaraLevelController>(scene);One<PuzzlePopupUI>(scene);One<NpcDialogueUI>(scene);
        foreach(Transform t in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)))
            if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new Exception("Missing script: "+t.name);
    }
    private static float RouteDistance(Vector2 point)
    {
        float distance=float.MaxValue;
        for(int i=1;i<Route.Length;i++){Vector2 d=Route[i]-Route[i-1];float t=Mathf.Clamp01(Vector2.Dot(point-Route[i-1],d)/d.sqrMagnitude);distance=Mathf.Min(distance,Vector2.Distance(point,Route[i-1]+d*t));}
        return distance;
    }
    private static Transform Root(string name)=>new GameObject(name).transform;
    private static T One<T>(Scene scene) where T:Component=>scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).Single();
    private static void Edit(Object obj,Action<SerializedObject> change){var s=new SerializedObject(obj);change(s);s.ApplyModifiedPropertiesWithoutUndo();}
    private static void Set(Object obj,string field,Object value)=>Edit(obj,s=>s.FindProperty(field).objectReferenceValue=value);
    private static void Array(SerializedObject s,string field,Object value){var p=s.FindProperty(field);p.arraySize=1;p.GetArrayElementAtIndex(0).objectReferenceValue=value;}
    private static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;int i=path.LastIndexOf('/');Folder(path.Substring(0,i));AssetDatabase.CreateFolder(path.Substring(0,i),path.Substring(i+1));}
    private static Material Mat(string name,Color color,bool emission=false){var m=new Material(Shader.Find("Standard")){name=name,color=color};m.SetFloat("_Glossiness",.15f);if(emission){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*.7f);}AssetDatabase.CreateAsset(m,MatPath+"/"+name+".mat");return m;}
    private static GameObject Block(string name,Transform parent,Vector3 position,Vector3 size,Material material,bool solid=false)
    {
        var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(parent,true);obj.transform.position=position;obj.transform.localScale=size;
        obj.GetComponent<Renderer>().sharedMaterial=material;if(!solid)Object.DestroyImmediate(obj.GetComponent<Collider>());obj.isStatic=true;return obj;
    }
    private static void Trigger(GameObject obj){obj.GetComponent<Collider>().isTrigger=true;var rb=obj.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;}
    private static void Arch(Transform parent,Vector3 p,Material accent){Block("ArchLeft",parent,p+new Vector3(-5,4,0),new Vector3(1.5f,8,2),stone);Block("ArchRight",parent,p+new Vector3(5,4,0),new Vector3(1.5f,8,2),stone);Block("ArchTop",parent,p+Vector3.up*8,new Vector3(11.5f,1,2),accent);}
    private static void Sign(Transform parent,Vector3 position,string message){var obj=new GameObject("LumoraInscription");obj.transform.SetParent(parent,true);obj.transform.position=position;obj.transform.rotation=Quaternion.Euler(40,0,0);var t=obj.AddComponent<TextMesh>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");obj.GetComponent<Renderer>().sharedMaterial=t.font.material;t.text=message;t.fontSize=48;t.characterSize=.07f;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=new Color(.8f,.96f,1);}
    private static GameObject Crystal(Transform parent,Vector3 position,Vector3 size,Material material){GameObject obj=MeshObject("CrystalCluster",parent,crystalMesh,material);obj.transform.position=position;obj.transform.localScale=size;return obj;}
    private static GameObject MeshObject(string name,Transform parent,Mesh mesh,Material material){var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(parent,false);obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<Renderer>().sharedMaterial=material;obj.isStatic=true;return obj;}
    private static Mesh MakeCrystal()
    {
        var v=new List<Vector3>();var t=new List<int>();
        for(int i=0;i<6;i++){float a=i*Mathf.PI/3,b=(i+1)*Mathf.PI/3;Vector3 p=new Vector3(Mathf.Cos(a),.6f,Mathf.Sin(a)),q=new Vector3(Mathf.Cos(b),.6f,Mathf.Sin(b));Quad(v,t,p,Vector3.up*1.4f,q,q);Quad(v,t,q,Vector3.zero,p,p);}
        return SaveMesh("HexCrystal",v,t);
    }
    private static void Quad(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c,Vector3 d){int n=v.Count;v.AddRange(new[]{a,b,c,a,c,d});for(int i=0;i<6;i++)t.Add(n+i);}
    private static Mesh SaveMesh(string name,List<Vector3> v,List<int> t){var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,ArtPath+"/"+name+".asset");return mesh;}
    private static void CombineDecor(Transform area)
    {
        // Retain simple colliders, batch opaque static rendering by chamber/material.
        var renderers=area.GetComponentsInChildren<MeshRenderer>().Where(r => r.GetComponent<MeshFilter>() != null).ToArray();
        foreach(var group in renderers.GroupBy(r=>r.sharedMaterial))
        {
            var mesh=new Mesh{name=area.name+"_"+group.Key.name,indexFormat=IndexFormat.UInt32};
            mesh.CombineMeshes(group.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=r.transform.localToWorldMatrix}).ToArray());
            AssetDatabase.CreateAsset(mesh,ArtPath+"/"+mesh.name+".asset");
            foreach(var renderer in group){Object.DestroyImmediate(renderer.GetComponent<MeshFilter>());Object.DestroyImmediate(renderer);}
            MeshObject("Static_"+mesh.name,area,mesh,group.Key);
        }
    }
}
