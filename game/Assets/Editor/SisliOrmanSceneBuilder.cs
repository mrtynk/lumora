using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class SisliOrmanSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/SisliOrman.unity";
    private const string ArtPath = "Assets/Scenes/SisliOrmanData";
    public static readonly Vector2[] Route = {
        new Vector2(0,-33), new Vector2(-13,-23), new Vector2(-18,-12),
        new Vector2(-18,-8), new Vector2(-18,4), new Vector2(-9,10),
        new Vector2(5,13), new Vector2(15,23), new Vector2(15,27),
        new Vector2(22,31), new Vector2(28,34) };

    [MenuItem("Tools/Lumora/Build Sisli Orman Level")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            EditorSceneManager.OpenScene(ScenePath);
            Debug.Log("Mevcut Sisli Orman açıldı; sahne yeniden yazılmadı.");
            return;
        }
        if (!AssetDatabase.CopyAsset(LumoraLevelSceneBuilder.ScenePath, ScenePath))
            throw new InvalidOperationException("Işıklı Vadi bağlantı şablonu bulunamadı.");
        Scene scene = EditorSceneManager.OpenScene(ScenePath);
        var keep = new HashSet<string> { "Player", "GameManager", "Main Camera", "RegionProgressCanvas", "Directional Light", "EventSystem" };
        foreach (GameObject root in scene.GetRootGameObjects())
            if (!keep.Contains(root.name)) Object.DestroyImmediate(root);

        EnsureFolder(ArtPath);
        EnsureFolder("Assets/Materials/SisliOrman");
        var art = new ForestArt();
        Transform meadow = Root("01_PusluGiris");
        Transform stream = Root("02_FenerliDere");
        Transform grove = Root("03_KadimAgacKorusu");
        Transform exit = Root("04_KristalGecidi");
        BuildGround(art, meadow);
        BuildTrail(art, meadow, Route, 3.4f);
        BuildTrail(art, grove, new[] { new Vector2(-9,10), new Vector2(-26,15), new Vector2(-24,27), new Vector2(5,13) }, 2f);
        art.Cube(stream, art.water, new Vector3(0,0.18f,-2), new Vector3(79,0.05f,5));
        BuildBridge(art, stream);
        var random = new System.Random(2026);
        for (int i = 0; i < 145; i++)
        {
            float x = (float)random.NextDouble()*72-36;
            float z = (float)random.NextDouble()*72-36;
            if (Mathf.Abs(z+2)<7 || DistanceToRoute(new Vector2(x,z)) < 5.2f ||
                Vector2.Distance(new Vector2(x,z),new Vector2(15,27)) < 7 ||
                Vector2.Distance(new Vector2(x,z),new Vector2(28,34)) < 6) continue;
            Transform area = z < -8 ? meadow : z < 8 ? stream : x < 18 ? grove : exit;
            Tree(art, area, x,z, 0.85f+(float)random.NextDouble()*0.6f);
        }
        // A large, readable landmark behind the seed, not on its approach.
        Tree(art, grove, 15,33, 2.2f, true);
        for (int s=1;s<Route.Length;s++)
        {
            Vector2 a=Route[s-1], b=Route[s];
            for(float t=0;t<1;t+=0.3f)
            {
                Vector2 p=Vector2.Lerp(a,b,t);
                Vector2 side=new Vector2(-(b-a).y,(b-a).x).normalized*2.8f;
                if(Mathf.Abs(p.y+2)<7) continue;
                Vector3 pos=At(p.x+side.x,p.y+side.y);
                art.Cube(grove,art.wood,pos+Vector3.up*0.55f,new Vector3(.12f,1.1f,.12f));
                art.Shape(grove,art.glow,pos+Vector3.up*1.2f,new Vector3(.35f,.5f,.35f));
                art.Shape(grove,art.mushroom,At(p.x-side.x,p.y-side.y)+Vector3.up*.3f,new Vector3(.5f,.2f,.5f));
            }
        }
        for(int i=0;i<4;i++)
        {
            float angle=i*Mathf.PI/2;
            Vector3 p=At(15+Mathf.Cos(angle)*3,27+Mathf.Sin(angle)*3);
            art.Shape(grove,art.glow,p+Vector3.up*.7f,new Vector3(.4f,1.1f,.4f));
        }
        Box("NorthBoundary",exit,new Vector3(0,5,40),new Vector3(82,12,1));
        Box("SouthBoundary",meadow,new Vector3(0,5,-40),new Vector3(82,12,1));
        Box("WestBoundary",meadow,new Vector3(-40,5,0),new Vector3(1,12,82));
        Box("EastBoundary",exit,new Vector3(40,5,0),new Vector3(1,12,82));
        for(int i=0;i<11;i++)
        {
            float p=-40+i*8;
            art.Shape(meadow,art.rock,At(p,-41),new Vector3(6,4,5));
            art.Shape(exit,art.rock,At(p,41),new Vector3(6,6,5));
            art.Shape(meadow,art.rock,At(-41,p),new Vector3(5,5,6));
            art.Shape(exit,art.rock,At(41,p),new Vector3(5,5,6));
        }
        Sign(meadow,At(3,-29),"SİSLİ ORMAN\nFenerleri takip et");
        Sign(stream,At(-23,-10),"Dereyi geç\nKadim ağacı bul");
        Sign(grove,At(10,22),"IŞIK TOHUMU\nKorunun kalbinde");
        Sign(exit,At(23,33),"KRİSTAL MAĞARA\nİkinci tohum kapıyı açar");
        art.Flush();
        WireGameplay(scene, art);
        AddEntrance(scene);
        RenderSettings.fog=true;
        RenderSettings.fogMode=FogMode.Linear;
        RenderSettings.fogColor=new Color(.57f,.72f,.77f);
        RenderSettings.fogStartDistance=18;
        RenderSettings.fogEndDistance=62;
        RenderSettings.ambientMode=AmbientMode.Flat;
        RenderSettings.ambientLight=new Color(.62f,.72f,.74f);
        Camera camera=One<Camera>(scene);
        camera.clearFlags=CameraClearFlags.SolidColor;
        camera.backgroundColor=RenderSettings.fogColor;
        Validate(scene);
        EditorSceneManager.SaveScene(scene,ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("Sisli Orman hazır. Teste Işıklı Vadi'den başlayın; tohum ve portal ile geçin.");
    }

    private static void WireGameplay(Scene scene, ForestArt art)
    {
        var player=One<PlayerController>(scene);
        var progress=One<RegionProgressController>(scene);
        Transform spawn=Root("ForestSpawn");
        spawn.position=At(0,-33)+Vector3.up*1.15f;
        player.transform.position=spawn.position;
        player.transform.rotation=Quaternion.identity;
        var camera=One<LevelFollowCamera>(scene);
        camera.SnapToTarget();
        var seedObject=new GameObject("SecondLightSeed");
        seedObject.transform.position=At(15,27)+Vector3.up*1.15f;
        seedObject.AddComponent<SphereCollider>().radius=1.2f;
        Trigger(seedObject);
        var seed=seedObject.AddComponent<LightSeedCollectible>();
        Set(seed,"progressController",progress);
        Edit(seed,s=>s.FindProperty("regionIndex").intValue=1);
        var seedVisual=Primitive("SeedVisual",PrimitiveType.Sphere,seedObject.transform,Vector3.zero,new Vector3(.85f,1.35f,.85f),art.gold);
        var portalObject=new GameObject("PortalToKristalMagara");
        portalObject.transform.position=At(28,34);
        var area=portalObject.AddComponent<BoxCollider>();
        area.center=Vector3.up*2; area.size=new Vector3(6,4,4);
        Trigger(portalObject);
        var gate=Primitive("PortalGate",PrimitiveType.Cube,portalObject.transform,Vector3.up*2,new Vector3(4,4,.3f),art.rock);
        var barrier=gate.AddComponent<BoxCollider>();
        Primitive("LeftPillar",PrimitiveType.Cube,portalObject.transform,new Vector3(-2.7f,2.5f,0),new Vector3(1,5,1),art.rock);
        Primitive("RightPillar",PrimitiveType.Cube,portalObject.transform,new Vector3(2.7f,2.5f,0),new Vector3(1,5,1),art.rock);
        Primitive("Arch",PrimitiveType.Cube,portalObject.transform,new Vector3(0,5,0),new Vector3(6.4f,1,1),art.glow);
        var portal=portalObject.AddComponent<PortalTrigger>();
        Set(portal,"progressController",progress); Set(portal,"portalRenderer",gate.GetComponent<Renderer>());
        Set(portal,"blockingCollider",barrier); Set(portal,"lockedMaterial",art.rock); Set(portal,"openMaterial",art.portal);
        Edit(portal,s=>{s.FindProperty("regionIndex").intValue=1;s.FindProperty("destinationSceneName").stringValue="KristalMagara";});
        Edit(progress,s=>{
            s.FindProperty("startAutomatically").boolValue=false;
            s.FindProperty("useSceneTransitions").boolValue=true;
            s.FindProperty("sceneRegionIndex").intValue=1;
            s.FindProperty("regionOneGuide").objectReferenceValue=null;
            Array(s,"regionSpawnPoints",spawn);Array(s,"lightSeeds",seed);Array(s,"portals",portal);
        });
        var level=new GameObject("SisliOrmanLevel").AddComponent<SisliOrmanLevelController>();
        Set(level,"progress",progress);Set(level,"player",player);Set(level,"spawnPoint",spawn);
        Set(level,"followCamera",camera);Set(level,"seedVisual",seedVisual.transform);
        foreach(Text t in progress.GetComponentsInChildren<Text>(true))
        {
            if(t.name=="ActiveRegionText")t.text="Aktif Bölge: Sisli Orman";
            if(t.name=="SeedCountText")t.text="Işık Tohumu: 1 / 4";
            if(t.name=="PortalStatusText")t.text="Portal: Kilitli";
            if(t.name=="NotificationText")t.text="Fenerleri takip et, kadim ağacın yanındaki ışık tohumunu bul.";
        }
    }

    public static void Validate(Scene scene)
    {
        One<PlayerController>(scene);One<GameEventSender>(scene);One<RegionProgressController>(scene);
        One<LightSeedCollectible>(scene);One<PortalTrigger>(scene);One<SisliOrmanLevelController>(scene);
        foreach(Transform t in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)))
            if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new Exception("Eksik script: "+t.name);
        if(scene.GetRootGameObjects().Any(r=>r.GetComponent<MainMenuUI>()!=null || r.GetComponent<IntroVideoUI>()!=null))
            throw new Exception("İkinci bölgede menü/intro tekrar oluşturulmamalı.");
    }

    public static void AddEntrance(Scene scene)
    {
        if (scene.path != ScenePath || scene.GetRootGameObjects().Any(r => r.name == "EntranceFromIsikliVadi")) return;
        Transform entrance = Root("EntranceFromIsikliVadi");
        entrance.position = At(0, -37);
        Material stone = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SisliOrman/Stone.mat");
        Material glow = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/SisliOrman/Lantern.mat");
        // Arrival landmark only: progression remains forward, not a second trigger.
        Primitive("LeftPillar", PrimitiveType.Cube, entrance, new Vector3(-2.5f, 2, 0), new Vector3(.7f, 4, .7f), stone);
        Primitive("RightPillar", PrimitiveType.Cube, entrance, new Vector3(2.5f, 2, 0), new Vector3(.7f, 4, .7f), stone);
        Primitive("ArrivalArch", PrimitiveType.Cube, entrance, Vector3.up * 4, new Vector3(5.7f, .6f, .7f), glow);
        Sign(entrance, At(0, -37), "IŞIKLI VADİ'DEN GELİŞ");
    }

    public static float Height(float x,float z)
    {
        float h=.5f+Mathf.SmoothStep(0,1.4f,Mathf.InverseLerp(3,30,z))+.12f*Mathf.Sin(x*.12f);
        float creek=Mathf.Exp(-Mathf.Pow((z+2)/2.8f,4));
        return Mathf.Lerp(h,-.15f,creek);
    }
    private static Vector3 At(float x,float z)=>new Vector3(x,Height(x,z),z);
    private static void BuildGround(ForestArt art,Transform parent)
    {
        var v=new List<Vector3>();var indices=new List<int>();
        for(int z=-40;z<40;z+=2)for(int x=-40;x<40;x+=2)
            Quad(v,indices,At(x,z),At(x,z+2),At(x+2,z+2),At(x+2,z));
        GameObject ground=MeshObject("ForestGround",parent,SaveMesh("Ground",v,indices),art.grass);
        ground.AddComponent<MeshCollider>().sharedMesh=ground.GetComponent<MeshFilter>().sharedMesh;
    }
    private static void BuildTrail(ForestArt art,Transform area,Vector2[] points,float width)
    {
        for(int i=1;i<points.Length;i++)
        {
            Vector2 d=points[i]-points[i-1];int n=Mathf.CeilToInt(d.magnitude/.7f);
            for(int j=0;j<n;j++)
            {
                Vector2 p=Vector2.Lerp(points[i-1],points[i],(j+.5f)/n);
                art.Cube(area,art.path,At(p.x,p.y)+Vector3.up*.025f,new Vector3(width,.045f,d.magnitude/n+.15f),Quaternion.Euler(0,Mathf.Atan2(d.x,d.y)*Mathf.Rad2Deg,0));
            }
        }
    }
    private static void BuildBridge(ForestArt art,Transform parent)
    {
        float[] z={-12,-8,4,8};float[] y={Height(-18,-12)+.03f,1.2f,1.2f,Height(-18,8)+.03f};
        var v=new List<Vector3>();var t=new List<int>();
        for(int i=1;i<4;i++)Quad(v,t,new Vector3(-20.5f,y[i-1],z[i-1]),new Vector3(-20.5f,y[i],z[i]),new Vector3(-15.5f,y[i],z[i]),new Vector3(-15.5f,y[i-1],z[i-1]));
        GameObject bridge=MeshObject("ForestBoardwalk",parent,SaveMesh("Bridge",v,t),art.wood);
        bridge.AddComponent<MeshCollider>().sharedMesh=bridge.GetComponent<MeshFilter>().sharedMesh;
        foreach(float x in new[]{-20.5f,-15.5f})
        {
            Box("BridgeRail",parent,new Vector3(x,1.8f,-2),new Vector3(.2f,1.1f,12));
            art.Cube(parent,art.wood,new Vector3(x,2.2f,-2),new Vector3(.2f,.2f,12));
        }
    }
    private static void Tree(ForestArt art,Transform area,float x,float z,float size,bool ancient=false)
    {
        Vector3 p=At(x,z);
        art.Cube(area,art.wood,p+Vector3.up*2*size,new Vector3(.65f,4,.65f)*size);
        for(int i=0;i<3;i++)art.Cone(area,ancient?art.mint:art.leaves,p+Vector3.up*(2+i*1.3f)*size,new Vector3(2.3f-i*.45f,3,2.3f-i*.45f)*size);
        Box("TreeTrunk",area,p+Vector3.up*1.5f*size,new Vector3(.65f,3,.65f)*size);
    }
    private static float DistanceToRoute(Vector2 p)
    {
        float nearest=float.MaxValue;
        for(int i=1;i<Route.Length;i++){Vector2 d=Route[i]-Route[i-1];float t=Mathf.Clamp01(Vector2.Dot(p-Route[i-1],d)/d.sqrMagnitude);nearest=Mathf.Min(nearest,Vector2.Distance(p,Route[i-1]+d*t));}
        return nearest;
    }
    private static void Sign(Transform parent,Vector3 p,string message)
    {
        var obj=new GameObject("ForestSign");obj.transform.SetParent(parent);obj.transform.position=p+Vector3.up*2;
        var text=obj.AddComponent<TextMesh>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text=message;text.fontSize=48;text.characterSize=.065f;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=new Color(.96f,.93f,.7f);
        obj.GetComponent<Renderer>().sharedMaterial=text.font.material;obj.transform.rotation=Quaternion.Euler(25,0,0);obj.isStatic=true;
    }
    private static Transform Root(string name)=>new GameObject(name).transform;
    private static void Box(string name,Transform parent,Vector3 p,Vector3 size){var obj=new GameObject(name);obj.transform.SetParent(parent);obj.transform.position=p;obj.AddComponent<BoxCollider>().size=size;obj.isStatic=true;}
    private static void Trigger(GameObject obj){obj.GetComponent<Collider>().isTrigger=true;var body=obj.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;}
    private static T One<T>(Scene scene) where T:Component=>scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<T>(true)).Single();
    private static void Edit(Object target,Action<SerializedObject> edit){var s=new SerializedObject(target);edit(s);s.ApplyModifiedPropertiesWithoutUndo();}
    private static void Set(Object target,string field,Object value)=>Edit(target,s=>s.FindProperty(field).objectReferenceValue=value);
    private static void Array(SerializedObject s,string field,Object value){var p=s.FindProperty(field);p.arraySize=1;p.GetArrayElementAtIndex(0).objectReferenceValue=value;}
    private static void EnsureFolder(string path){if(AssetDatabase.IsValidFolder(path))return;int i=path.LastIndexOf('/');EnsureFolder(path.Substring(0,i));AssetDatabase.CreateFolder(path.Substring(0,i),path.Substring(i+1));}
    private static GameObject Primitive(string name,PrimitiveType type,Transform parent,Vector3 p,Vector3 scale,Material material){var obj=GameObject.CreatePrimitive(type);obj.name=name;Object.DestroyImmediate(obj.GetComponent<Collider>());obj.transform.SetParent(parent,false);obj.transform.localPosition=p;obj.transform.localScale=scale;obj.GetComponent<Renderer>().sharedMaterial=material;return obj;}
    private static void Quad(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c,Vector3 d){int n=v.Count;v.AddRange(new[]{a,b,c,a,c,d});for(int i=0;i<6;i++)t.Add(n+i);}
    private static Mesh SaveMesh(string name,List<Vector3> v,List<int> t){var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,ArtPath+"/"+name+".asset");return mesh;}
    private static GameObject MeshObject(string name,Transform parent,Mesh mesh,Material material){var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(parent,false);obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<Renderer>().sharedMaterial=material;obj.isStatic=true;return obj;}

    private sealed class ForestArt
    {
        public readonly Material grass=Mat("Moss",new Color(.30f,.51f,.43f)),path=Mat("Trail",new Color(.66f,.70f,.51f)),wood=Mat("Wood",new Color(.38f,.32f,.30f)),leaves=Mat("Pine",new Color(.20f,.43f,.41f)),mint=Mat("AncientCrown",new Color(.43f,.68f,.55f)),water=Mat("Stream",new Color(.36f,.64f,.71f)),glow=Mat("Lantern",new Color(.77f,.95f,.63f),true),mushroom=Mat("Mushroom",new Color(.72f,.59f,.78f)),rock=Mat("Stone",new Color(.44f,.51f,.57f)),gold=Mat("Seed",new Color(1,.84f,.29f),true),portal=Mat("PortalOpen",new Color(.58f,.46f,.95f),true);
        private readonly Mesh cube,shape,cone;
        private readonly Dictionary<string,List<CombineInstance>> batches=new Dictionary<string,List<CombineInstance>>();
        private readonly Dictionary<string,(Transform parent,Material material)> owners=new Dictionary<string,(Transform,Material)>();
        public ForestArt(){var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);cube=obj.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(obj);shape=Octahedron();cone=ConeMesh();}
        private static Material Mat(string name,Color color,bool glow=false){string path="Assets/Materials/SisliOrman/"+name+".mat";var existing=AssetDatabase.LoadAssetAtPath<Material>(path);if(existing!=null)return existing;var m=new Material(Shader.Find("Standard")){name=name,color=color};m.SetFloat("_Glossiness",.05f);if(glow){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*.45f);}AssetDatabase.CreateAsset(m,path);return m;}
        private void Add(Transform p,Material m,Mesh mesh,Vector3 pos,Vector3 scale,Quaternion rot){string key=p.name+"_"+m.name;if(!batches.ContainsKey(key)){batches[key]=new List<CombineInstance>();owners[key]=(p,m);}batches[key].Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.TRS(pos,rot,scale)});}
        public void Cube(Transform p,Material m,Vector3 pos,Vector3 scale,Quaternion? rot=null)=>Add(p,m,cube,pos,scale,rot??Quaternion.identity);
        public void Shape(Transform p,Material m,Vector3 pos,Vector3 scale)=>Add(p,m,shape,pos,scale,Quaternion.identity);
        public void Cone(Transform p,Material m,Vector3 pos,Vector3 scale)=>Add(p,m,cone,pos,scale,Quaternion.identity);
        public void Flush(){foreach(var pair in batches){var mesh=new Mesh{name=pair.Key,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(pair.Value.ToArray());AssetDatabase.CreateAsset(mesh,ArtPath+"/"+pair.Key+".asset");var owner=owners[pair.Key];MeshObject("StaticArt_"+pair.Key,owner.parent,mesh,owner.material);}Object.DestroyImmediate(shape);Object.DestroyImmediate(cone);}
        private static Mesh ConeMesh(){var v=new List<Vector3>();var t=new List<int>();for(int i=0;i<7;i++){float a=i*Mathf.PI*2/7,b=(i+1)*Mathf.PI*2/7;Vector3 p=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)),q=new Vector3(Mathf.Cos(b),0,Mathf.Sin(b));Tri(v,t,p,Vector3.up,q);Tri(v,t,q,Vector3.zero,p);}return Make(v,t);}
        private static Mesh Octahedron(){var v=new List<Vector3>();var t=new List<int>();Vector3[] ring={Vector3.right,Vector3.forward,Vector3.left,Vector3.back};for(int i=0;i<4;i++){Tri(v,t,ring[i],Vector3.up,ring[(i+1)%4]);Tri(v,t,ring[(i+1)%4],Vector3.down,ring[i]);}return Make(v,t);}
        private static void Tri(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c){int n=v.Count;v.AddRange(new[]{a,b,c});t.AddRange(new[]{n,n+1,n+2});}
        private static Mesh Make(List<Vector3> v,List<int> t){var m=new Mesh();m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;}
    }
}
