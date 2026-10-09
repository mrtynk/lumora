using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// The prototype is a one-time wiring template, never a runtime dependency.
public static class LumoraLevelSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/IsikliVadi.unity";
    private const string TemplatePath = "Assets/Scenes/PrototypeScene.unity";

    [MenuItem("Tools/Lumora/Build Işıklı Vadi Level")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Önce Play Mode'dan çıkın.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            EditorSceneManager.OpenScene(ScenePath);
            Debug.Log("Mevcut Işıklı Vadi açıldı; elle yaptığınız değişiklikler korunuyor.");
            return;
        }
        if (!AssetDatabase.CopyAsset(TemplatePath, ScenePath))
            throw new InvalidOperationException("PrototypeScene şablonu kopyalanamadı.");

        Scene scene = EditorSceneManager.OpenScene(ScenePath);
        try
        {
            Populate(scene);
            IsikliVadiSceneValidation.ValidateScene(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Işıklı Vadi oluşturuldu. Ana Menü → Karakter → Intro → Hikâye → Vadi. PrototypeScene değiştirilmedi.");
        }
        catch (Exception)
        {
            Debug.LogError("Kurulum tamamlanamadı. Açık sahnedeki hatayı düzeltip kaydedin; mevcut sahneler otomatik silinmez.");
            throw;
        }
    }

    private static void Populate(Scene scene)
    {
        PlayerController player = One<PlayerController>(scene);
        RegionProgressController progress = One<RegionProgressController>(scene);
        PuzzlePopupUI popup = One<PuzzlePopupUI>(scene);
        NpcDialogueUI dialogue = One<NpcDialogueUI>(scene);
        NpcInteractionTrigger npc = One<NpcInteractionTrigger>(scene);
        Camera camera = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Camera>(true))
            .Single(x => x.CompareTag("MainCamera"));
        Set(popup, "demoFlowController", null);
        Set(dialogue, "demoFlowController", null);
        Set(progress, "regionOneGuide", null);
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name.StartsWith("Region1_") || root.name.StartsWith("Region2_") ||
                root.name.StartsWith("Region3_") || root.name.StartsWith("Region4_") ||
                root.name == "IsikliVadiEnvironment" || root.name == "DemoGuideCanvas" || root.name == "PuzzlePaper")
                Object.DestroyImmediate(root);

        var layout = IsikliVadiEnvironmentBuilder.Build();
        var interactions = new GameObject("ValleyInteractions");
        Transform spawn = new GameObject("ValleySpawn").transform;
        spawn.position = layout.spawnPosition;
        player.transform.position = spawn.position;
        player.transform.rotation = Quaternion.identity;
        LevelFollowCamera follow = camera.gameObject.AddComponent<LevelFollowCamera>();
        Set(follow, "target", player.transform);
        follow.SnapToTarget();
        camera.farClipPlane = 180f;

        npc.transform.SetParent(interactions.transform, true);
        npc.transform.position = layout.npcPosition;
        Set(npc, "playerController", player);
        foreach (Text text in dialogue.GetComponentsInChildren<Text>(true))
            if (text.name == "DialogueText") text.text = "Işık Korusu köprünün ilerisinde! Işık tohumunu bulmama yardım eder misin?";
        Label(npc.transform, "Orman Dostu · E", new Vector3(0, 2, 0));

        Material gold = MaterialAsset("SeedGold", new Color(1f, 0.79f, 0.15f));
        Material stone = MaterialAsset("PortalLocked", new Color(0.46f, 0.54f, 0.57f));
        Material blue = MaterialAsset("PortalOpen", new Color(0.2f, 0.85f, 1f));
        var seedObject = new GameObject("MainLightSeed");
        seedObject.transform.SetParent(interactions.transform);
        seedObject.transform.position = layout.seedPosition;
        seedObject.AddComponent<SphereCollider>().radius = 1.2f;
        TriggerBody(seedObject);
        LightSeedCollectible seed = seedObject.AddComponent<LightSeedCollectible>();
        Set(seed, "progressController", progress);
        GameObject seedVisual = Visual("LightSeedVisual", seedObject.transform, Vector3.zero,
            new Vector3(0.9f, 1.5f, 0.9f), gold, PrimitiveType.Sphere);
        Label(seedObject.transform, "Işık Tohumu", new Vector3(0, 2f, 0));

        var portalObject = new GameObject("PortalToSisliOrman");
        portalObject.transform.SetParent(interactions.transform);
        portalObject.transform.position = layout.portalPosition - Vector3.up * 2f;
        BoxCollider trigger = portalObject.AddComponent<BoxCollider>();
        trigger.size = new Vector3(6, 4, 4);
        trigger.center = new Vector3(0, 2, 0);
        TriggerBody(portalObject);
        GameObject gate = Visual("PortalGate", portalObject.transform, new Vector3(0, 2, 0),
            new Vector3(4, 4, 0.3f), stone);
        BoxCollider barrier = gate.AddComponent<BoxCollider>();
        Visual("LeftPillar", portalObject.transform, new Vector3(-2.7f, 2.5f, 0), new Vector3(1, 5, 1), stone);
        Visual("RightPillar", portalObject.transform, new Vector3(2.7f, 2.5f, 0), new Vector3(1, 5, 1), stone);
        Visual("PortalArch", portalObject.transform, new Vector3(0, 5, 0), new Vector3(6.4f, 1, 1), gold);
        PortalTrigger portal = portalObject.AddComponent<PortalTrigger>();
        Set(portal, "progressController", progress);
        Set(portal, "portalRenderer", gate.GetComponent<Renderer>());
        Set(portal, "blockingCollider", barrier);
        Set(portal, "lockedMaterial", stone);
        Set(portal, "openMaterial", blue);
        Edit(portal, s => s.FindProperty("destinationSceneName").stringValue = "SisliOrman");
        Label(portalObject.transform, "Sisli Orman", new Vector3(0, 6, 0));

        string[] types = { "hidden_object", "memory_match", "pattern_puzzle" };
        string[] names = { "Gizli Nesne", "Eşleştirme", "Örüntü" };
        for (int i = 0; i < types.Length; i++)
        {
            var station = new GameObject("PuzzlePaper_" + types[i]);
            station.transform.SetParent(interactions.transform);
            station.transform.position = layout.puzzlePositions[i];
            BoxCollider area = station.AddComponent<BoxCollider>();
            area.size = new Vector3(3.5f, 3, 3.5f);
            area.center = Vector3.up;
            TriggerBody(station);
            Visual("PuzzlePaper", station.transform, new Vector3(0, 0.4f, 0), new Vector3(1.6f, 0.15f, 1.2f), gold);
            PuzzleTrigger puzzle = station.AddComponent<PuzzleTrigger>();
            Set(puzzle, "puzzlePopup", popup);
            Set(puzzle, "playerController", player);
            string type = types[i];
            Edit(puzzle, s => s.FindProperty("preferredPuzzleType").stringValue = type);
            Label(station.transform, names[i] + " · E\nİsteğe bağlı", new Vector3(0, 2, 0));
        }

        Edit(progress, s =>
        {
            s.FindProperty("startAutomatically").boolValue = false;
            s.FindProperty("useSceneTransitions").boolValue = true;
            s.FindProperty("sceneRegionIndex").intValue = 0;
            Array(s, "regionSpawnPoints", spawn);
            Array(s, "lightSeeds", seed);
            Array(s, "portals", portal);
        });
        IsikliVadiLevelController level = new GameObject("IsikliVadiLevel").AddComponent<IsikliVadiLevelController>();
        Set(level, "storyIntro", One<StoryIntroUI>(scene));
        Set(level, "progress", progress);
        Set(level, "player", player);
        Set(level, "spawnPoint", spawn);
        Set(level, "interactionsRoot", interactions);
        Set(level, "progressCanvas", progress.GetComponent<Canvas>());
        Set(level, "followCamera", follow);
        Set(level, "seedVisual", seedVisual.transform);
        RenderSettings.ambientLight = new Color(0.65f, 0.75f, 0.8f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.66f, 0.86f, 0.88f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 65;
        RenderSettings.fogEndDistance = 155;
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static T One<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
        .SelectMany(x => x.GetComponentsInChildren<T>(true)).Single();

    private static void Edit(Object target, Action<SerializedObject> apply)
    {
        var serialized = new SerializedObject(target);
        apply(serialized);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void Set(Object target, string field, Object value) =>
        Edit(target, s => s.FindProperty(field).objectReferenceValue = value);
    private static void Array(SerializedObject s, string field, Object value)
    {
        var p = s.FindProperty(field);
        p.arraySize = 1;
        p.GetArrayElementAtIndex(0).objectReferenceValue = value;
    }
    private static void TriggerBody(GameObject obj)
    {
        obj.GetComponent<Collider>().isTrigger = true;
        Rigidbody body = obj.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
    }
    private static GameObject Visual(string name, Transform parent, Vector3 position, Vector3 scale,
        Material material, PrimitiveType type = PrimitiveType.Cube)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        Object.DestroyImmediate(obj.GetComponent<Collider>());
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        return obj;
    }
    private static Material MaterialAsset(string name, Color color)
    {
        string path = "Assets/Materials/IsikliVadi/" + name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        var material = new Material(Shader.Find("Standard")) { name = name, color = color };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }
    private static void Label(Transform parent, string text, Vector3 position)
    {
        var label = new GameObject("InteractionLabel").AddComponent<TextMesh>();
        label.transform.SetParent(parent, false);
        label.transform.localPosition = position;
        label.transform.rotation = Quaternion.Euler(35, 0, 0);
        label.text = text;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.fontSize = 48;
        label.characterSize = 0.055f;
        label.color = new Color(0.1f, 0.22f, 0.2f);
    }
}
