using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static KaranlikTepeSceneBuilder;
using Object = UnityEngine.Object;

public static class LumoraFinalSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/LumoraFinal.unity";
    private const string Materials = "Assets/Materials/LumoraFinal";

    [MenuItem("Tools/Lumora/Build Lumora Final")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            EditorSceneManager.OpenScene(ScenePath);
            Debug.Log("Mevcut Lumora Final sahnesi açıldı; üzerine yazılmadı.");
            return;
        }
        Folder(Materials);
        Scene scene = CopyWiring(ScenePath, false);
        RegionProgressController progress = One<RegionProgressController>(scene);
        Edit(progress, s => {
            s.FindProperty("startAutomatically").boolValue = false;
            s.FindProperty("useSceneTransitions").boolValue = true;
            s.FindProperty("sceneRegionIndex").intValue = 3;
            s.FindProperty("regionSpawnPoints").arraySize = 0;
            s.FindProperty("lightSeeds").arraySize = 0;
            s.FindProperty("portals").arraySize = 0;
            s.FindProperty("regionOneGuide").objectReferenceValue = null;
        });
        progress.GetComponent<Canvas>().enabled = false;
        PlayerController player = One<PlayerController>(scene);
        player.transform.position = new Vector3(0,1.1f,-9);
        player.enabled = false;
        var camera = One<LevelFollowCamera>(scene);
        camera.enabled = false;
        camera.transform.position = new Vector3(0,12,-25);
        camera.transform.LookAt(new Vector3(0,7,2));
        Camera view = One<Camera>(scene); view.clearFlags = CameraClearFlags.SolidColor;
        view.backgroundColor = new Color(.15f,.21f,.32f); view.farClipPlane = 90;
        RenderSettings.fog = false;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.35f,.38f,.5f);
        One<Light>(scene).intensity = .8f;
        Material ground = Mat(Materials,"CentralGarden",new Color(.37f,.49f,.34f));
        Material wood = Mat(Materials,"LightTreeTrunk",new Color(.47f,.34f,.24f));
        Material leaves = Mat(Materials,"AwakeningCanopy",new Color(.25f,.32f,.35f),true);
        leaves.SetColor("_EmissionColor",Color.black);
        Material gold = Mat(Materials,"FourSeeds",new Color(1,.86f,.33f),true);
        Transform garden = Root("LumoraCentralGarden");
        Block("GardenGround",garden,new Vector3(0,-.25f,0),new Vector3(48,.5f,48),ground,true);
        Block("ReturnPath",garden,new Vector3(0,.02f,-7),new Vector3(5,.03f,20),wood);
        Transform tree = Root("IsikAgaci"); tree.position = new Vector3(0,0,4);
        Block("AncientTrunk",tree,new Vector3(0,4,4),new Vector3(2,8,2),wood);
        var left = Block("LeftBranch",tree,new Vector3(-2,6,4),new Vector3(5,.8f,1),wood);
        left.transform.rotation = Quaternion.Euler(0,0,32);
        var right = Block("RightBranch",tree,new Vector3(2,6,4),new Vector3(5,.8f,1),wood);
        right.transform.rotation = Quaternion.Euler(0,0,-32);
        Renderer[] canopy = new Renderer[5];
        Vector3[] crowns = {new Vector3(0,11,4),new Vector3(-4,9,4),new Vector3(4,9,4),new Vector3(-2,8,2),new Vector3(2,8,6)};
        for(int i=0;i<canopy.Length;i++)
        {
            var crown=GameObject.CreatePrimitive(PrimitiveType.Sphere);crown.name="LightTreeCrown_"+i;
            crown.transform.SetParent(tree,true);crown.transform.position=crowns[i];crown.transform.localScale=new Vector3(7,5,6);
            Object.DestroyImmediate(crown.GetComponent<Collider>());
            canopy[i]=crown.GetComponent<Renderer>();canopy[i].sharedMaterial=leaves;
        }
        GameObject[] seeds=new GameObject[4];
        for(int i=0;i<4;i++)
        {
            float angle=(i*90+45)*Mathf.Deg2Rad;
            seeds[i]=GameObject.CreatePrimitive(PrimitiveType.Sphere);seeds[i].name="ReturnedSeed_"+(i+1);
            seeds[i].transform.position=new Vector3(Mathf.Cos(angle)*5,2,4+Mathf.Sin(angle)*5);
            seeds[i].transform.localScale=Vector3.one*1.2f;seeds[i].GetComponent<Renderer>().sharedMaterial=gold;
            Object.DestroyImmediate(seeds[i].GetComponent<Collider>());seeds[i].SetActive(false);
        }
        var canvasObject=new GameObject("FinalCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        var panel=new GameObject("CompletionPanel",typeof(RectTransform),typeof(Image));panel.transform.SetParent(canvasObject.transform,false);
        var rect=panel.GetComponent<RectTransform>();rect.anchorMin=new Vector2(.17f,.03f);rect.anchorMax=new Vector2(.83f,.35f);rect.offsetMin=rect.offsetMax=Vector2.zero;
        panel.GetComponent<Image>().color=new Color(.08f,.15f,.2f,.95f);
        Text title=Label(panel.transform,"FinalTitle","Lumora Yeniden Parlıyor!",new Vector2(.03f,.72f),new Vector2(.97f,.98f),42);
        Text message=Label(panel.transform,"FinalMessage","Dört Işık Tohumu yeniden Işık Ağacı'na ulaştı. Lumora'nın ışığını geri getirdin!\nMacera tamamlandı.",new Vector2(.04f,.34f),new Vector2(.96f,.73f),27);
        var buttonObject=new GameObject("ReturnToMenu",typeof(RectTransform),typeof(Image),typeof(Button));buttonObject.transform.SetParent(panel.transform,false);
        var buttonRect=buttonObject.GetComponent<RectTransform>();buttonRect.anchorMin=new Vector2(.3f,.06f);buttonRect.anchorMax=new Vector2(.7f,.29f);buttonRect.offsetMin=buttonRect.offsetMax=Vector2.zero;
        buttonObject.GetComponent<Image>().color=new Color(.24f,.52f,.33f);
        Label(buttonObject.transform,"Label","Ana Menüye Dön",Vector2.zero,Vector2.one,28);
        var controller=new GameObject("LumoraFinalSequence").AddComponent<LumoraFinalController>();
        Set(controller,"progress",progress);Set(controller,"player",player);Set(controller,"completionPanel",panel);
        Set(controller,"title",title);Set(controller,"message",message);Set(controller,"returnButton",buttonObject.GetComponent<Button>());
        Edit(controller,s=>{
            var a=s.FindProperty("treeCanopy");a.arraySize=canopy.Length;for(int i=0;i<canopy.Length;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=canopy[i];
            a=s.FindProperty("symbolicSeeds");a.arraySize=4;for(int i=0;i<4;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=seeds[i];
        });
        panel.SetActive(false);
        EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
        Debug.Log("Lumora Final hazır. Dört tohum tamamlandığında Işık Ağacı uyanır.");
    }
}
