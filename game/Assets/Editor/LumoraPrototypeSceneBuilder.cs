using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class LumoraPrototypeSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/PrototypeScene.unity";
    private const string EventEndpoint = "http://localhost:5000/api/events";
    private const string ChildId = "demo-child-001";
    private const string MainMenuConceptPath =
        "Assets/LumoraAssets/UI/main_menu_concept.png";
    private const string MaleCharacterPath =
        "Assets/LumoraAssets/Characters/erkek_tam_boy.png";
    private const string FemaleCharacterPath =
        "Assets/LumoraAssets/Characters/kiz_tam_boy.png";
    private const string MaleIntroVideoPath =
        "Assets/LumoraAssets/Videos/erkek_intro.mp4";
    private const string FemaleIntroVideoPath =
        "Assets/LumoraAssets/Videos/kiz_intro.mp4";

    [MenuItem("Tools/Lumora/Build Prototype Scene")]
    public static void BuildPrototypeScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        EnsureFolder("Assets/Editor");
        EnsureFolder("Assets/Scenes");
        EnsureFolder("Assets/Materials");
        EnsureFolder("Assets/UI");

        if (!ConfigureUiTextureImports())
        {
            return;
        }

        if (!TryLoadMenuAssets(
                out Texture2D mainMenuTexture,
                out Texture2D maleCharacterTexture,
                out Texture2D femaleCharacterTexture))
        {
            return;
        }

        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single
        );

        Material groundMaterial = GetOrCreateMaterial(
            "Assets/Materials/GroundMaterial.mat",
            new Color(0.55f, 0.85f, 0.45f)
        );
        Material puzzleMaterial = GetOrCreateMaterial(
            "Assets/Materials/PuzzlePaperMaterial.mat",
            new Color(1f, 0.82f, 0.15f)
        );
        Material npcMaterial = GetOrCreateMaterial(
            "Assets/Materials/ForestFriendMaterial.mat",
            new Color(0.25f, 0.68f, 0.58f)
        );
        Material treeTrunkMaterial = GetOrCreateMaterial(
            "Assets/Materials/LightTreeTrunkMaterial.mat",
            new Color(0.38f, 0.22f, 0.12f)
        );
        Material treeCrownMaterial = GetOrCreateMaterial(
            "Assets/Materials/LightTreeCrownMaterial.mat",
            new Color(0.28f, 0.36f, 0.24f)
        );
        Material rewardMaterial = GetOrCreateMaterial(
            "Assets/Materials/LightSeedRewardMaterial.mat",
            new Color(1f, 0.78f, 0.15f)
        );
        Material mistyForestMaterial = GetOrCreateMaterial(
            "Assets/Materials/MistyForestGroundMaterial.mat",
            new Color(0.24f, 0.34f, 0.28f)
        );
        Material crystalCaveMaterial = GetOrCreateMaterial(
            "Assets/Materials/CrystalCaveGroundMaterial.mat",
            new Color(0.28f, 0.34f, 0.58f)
        );
        Material darkHillMaterial = GetOrCreateMaterial(
            "Assets/Materials/DarkHillGroundMaterial.mat",
            new Color(0.2f, 0.18f, 0.28f)
        );
        Material portalLockedMaterial = GetOrCreateMaterial(
            "Assets/Materials/PortalLockedMaterial.mat",
            new Color(0.32f, 0.34f, 0.4f)
        );
        Material portalOpenMaterial = GetOrCreateMaterial(
            "Assets/Materials/PortalOpenMaterial.mat",
            new Color(0.28f, 0.8f, 1f)
        );

        Material[] regionMaterials =
        {
            groundMaterial,
            mistyForestMaterial,
            crystalCaveMaterial,
            darkHillMaterial
        };

        GameObject player = CreatePlayer();
        PlayerController playerController = player.GetComponent<PlayerController>();
        GameEventSender eventSender = CreateGameManager();
        Camera mainCamera = CreateOrConfigureMainCamera();
        DemoFlowController demoFlow = CreateDemoFlow(
            treeTrunkMaterial,
            treeCrownMaterial
        );
        CreateRegionSystem(
            eventSender,
            player.transform,
            mainCamera,
            demoFlow.gameObject,
            regionMaterials,
            rewardMaterial,
            portalLockedMaterial,
            portalOpenMaterial
        );
        StoryIntroUI storyIntro = CreateStoryIntro(playerController);
        CreateMainMenuFlow(
            playerController,
            storyIntro,
            mainMenuTexture,
            maleCharacterTexture,
            femaleCharacterTexture
        );
        PuzzlePopupUI puzzlePopup = CreatePuzzlePopup(
            eventSender,
            playerController,
            demoFlow
        );
        CreatePuzzlePaper(puzzleMaterial, puzzlePopup);
        CreateNpc(
            npcMaterial,
            eventSender,
            playerController,
            demoFlow
        );
        CreateDirectionalLight();

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            Debug.LogError("Lumora prototype scene could not be saved: " + ScenePath);
            return;
        }

        Selection.activeGameObject = player;
        EditorGUIUtility.PingObject(player);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Lumora prototype scene created successfully.");
    }

    private static bool TryLoadMenuAssets(
        out Texture2D mainMenuTexture,
        out Texture2D maleCharacterTexture,
        out Texture2D femaleCharacterTexture)
    {
        mainMenuTexture = null;
        maleCharacterTexture = null;
        femaleCharacterTexture = null;

        string[] requiredAssetPaths =
        {
            MainMenuConceptPath,
            MaleCharacterPath,
            FemaleCharacterPath,
            MaleIntroVideoPath,
            FemaleIntroVideoPath
        };

        bool hasMissingAsset = false;
        foreach (string assetPath in requiredAssetPaths)
        {
            if (AssetDatabase.LoadMainAssetAtPath(assetPath) == null)
            {
                Debug.LogError("Zorunlu Lumora asseti bulunamadı: " + assetPath);
                hasMissingAsset = true;
            }
        }

        if (hasMissingAsset)
        {
            Debug.LogError(
                "PrototypeScene kurulmadı. Eksik Lumora assetlerini ekleyip " +
                "yeniden deneyin."
            );
            return false;
        }

        mainMenuTexture =
            AssetDatabase.LoadAssetAtPath<Texture2D>(MainMenuConceptPath);
        maleCharacterTexture =
            AssetDatabase.LoadAssetAtPath<Texture2D>(MaleCharacterPath);
        femaleCharacterTexture =
            AssetDatabase.LoadAssetAtPath<Texture2D>(FemaleCharacterPath);

        return mainMenuTexture != null &&
               maleCharacterTexture != null &&
               femaleCharacterTexture != null;
    }

    private static bool ConfigureUiTextureImports()
    {
        bool menuConfigured = ConfigureUiTextureImport(
            MainMenuConceptPath,
            false
        );
        bool maleConfigured = ConfigureUiTextureImport(
            MaleCharacterPath,
            true
        );
        bool femaleConfigured = ConfigureUiTextureImport(
            FemaleCharacterPath,
            true
        );

        return menuConfigured && maleConfigured && femaleConfigured;
    }

    private static bool ConfigureUiTextureImport(
        string assetPath,
        bool alphaIsTransparency)
    {
        TextureImporter importer =
            AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError(
                "UI görseli için TextureImporter bulunamadı: " + assetPath
            );
            return false;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.streamingMipmaps = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.crunchedCompression = false;
        importer.compressionQuality = 100;
        importer.maxTextureSize = 4096;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.sRGBTexture = true;
        importer.alphaIsTransparency = alphaIsTransparency;
        importer.isReadable = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.anisoLevel = 1;
        importer.wrapMode = TextureWrapMode.Clamp;

        ConfigureTexturePlatform(importer, "DefaultTexturePlatform", false);
        ConfigureTexturePlatform(importer, "Standalone", true);
        ConfigureTexturePlatform(importer, "Android", true);
        importer.SaveAndReimport();
        return true;
    }

    private static void ConfigureTexturePlatform(
        TextureImporter importer,
        string platformName,
        bool overridden)
    {
        TextureImporterPlatformSettings settings =
            importer.GetPlatformTextureSettings(platformName);
        settings.name = platformName;
        settings.overridden = overridden;
        settings.maxTextureSize = 4096;
        settings.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
        settings.textureCompression = TextureImporterCompression.Uncompressed;
        settings.compressionQuality = 100;
        settings.crunchedCompression = false;
        settings.allowsAlphaSplitting = false;
        settings.format = overridden
            ? TextureImporterFormat.RGBA32
            : TextureImporterFormat.Automatic;
        importer.SetPlatformTextureSettings(settings);
    }

    private static GameObject CreateGround(
        string name,
        Vector3 position,
        Material material)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = name;
        ground.transform.position = position;
        ground.transform.localScale = new Vector3(1.6f, 1f, 1.6f);
        ground.GetComponent<Renderer>().sharedMaterial = material;
        return ground;
    }

    private static GameObject CreatePlayer()
    {
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1f, 0f);

        if (EnsureTagExists("Player"))
        {
            player.tag = "Player";
        }
        else
        {
            Debug.LogWarning(
                "Player tag could not be created. Add a 'Player' tag from " +
                "Project Settings > Tags and Layers, then assign it to the Player object."
            );
        }

        CapsuleCollider capsuleCollider = player.GetComponent<CapsuleCollider>();
        if (capsuleCollider != null)
        {
            UnityEngine.Object.DestroyImmediate(capsuleCollider);
        }

        player.AddComponent<CharacterController>();
        player.AddComponent<PlayerController>();
        return player;
    }

    private static GameEventSender CreateGameManager()
    {
        GameObject gameManager = new GameObject("GameManager");
        GameEventSender eventSender = gameManager.AddComponent<GameEventSender>();
        SetStringProperty(eventSender, "eventEndpoint", EventEndpoint);
        return eventSender;
    }

    private static void CreateRegionSystem(
        GameEventSender eventSender,
        Transform player,
        Camera mainCamera,
        GameObject regionOneGuide,
        Material[] regionMaterials,
        Material seedMaterial,
        Material portalLockedMaterial,
        Material portalOpenMaterial)
    {
        GameObject canvasObject = new GameObject(
            "RegionProgressCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 4;

        CanvasScaler canvasScaler = canvasObject.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasScaler.matchWidthOrHeight = 0.5f;

        GameObject progressPanel = CreateUiObject(
            "RegionProgressPanel",
            canvasObject.transform
        );
        RectTransform progressRect = progressPanel.GetComponent<RectTransform>();
        progressRect.anchorMin = new Vector2(0f, 1f);
        progressRect.anchorMax = new Vector2(0f, 1f);
        progressRect.pivot = new Vector2(0f, 1f);
        progressRect.sizeDelta = new Vector2(520f, 300f);
        progressRect.anchoredPosition = new Vector2(20f, -20f);

        Image progressImage = progressPanel.AddComponent<Image>();
        progressImage.color = new Color(0.08f, 0.12f, 0.18f, 0.9f);
        progressImage.raycastTarget = false;

        Text activeRegionText = CreateText(
            "ActiveRegionText",
            progressPanel.transform,
            "Aktif Bölge: Işıklı Vadi",
            27,
            FontStyle.Bold,
            new Vector2(470f, 42f),
            new Vector2(0f, 115f)
        );
        Text seedCountText = CreateText(
            "SeedCountText",
            progressPanel.transform,
            "Işık Tohumu: 0 / 4",
            23,
            FontStyle.Normal,
            new Vector2(470f, 38f),
            new Vector2(0f, 75f)
        );
        Text portalStatusText = CreateText(
            "PortalStatusText",
            progressPanel.transform,
            "Portal: Kilitli",
            23,
            FontStyle.Normal,
            new Vector2(470f, 38f),
            new Vector2(0f, 37f)
        );
        Text objectiveText = CreateText(
            "ObjectiveText",
            progressPanel.transform,
            "Hedef: Bu bölgedeki ışık tohumunu bul.",
            21,
            FontStyle.Bold,
            new Vector2(470f, 55f),
            new Vector2(0f, -15f)
        );
        Text notificationText = CreateText(
            "NotificationText",
            progressPanel.transform,
            "Bu bölgedeki ışık tohumunu bul ve portalı aç.",
            20,
            FontStyle.Normal,
            new Vector2(470f, 80f),
            new Vector2(0f, -95f)
        );

        Text[] progressTexts =
        {
            activeRegionText,
            seedCountText,
            portalStatusText,
            objectiveText,
            notificationText
        };
        foreach (Text progressText in progressTexts)
        {
            progressText.alignment = TextAnchor.MiddleLeft;
            progressText.raycastTarget = false;
        }

        RegionProgressController progressController =
            canvasObject.AddComponent<RegionProgressController>();
        Transform[] spawnPoints = new Transform[4];
        LightSeedCollectible[] lightSeeds = new LightSeedCollectible[4];
        PortalTrigger[] portals = new PortalTrigger[4];

        string[] regionObjectNames =
        {
            "Region1_IsikliVadi",
            "Region2_SisliOrman",
            "Region3_KristalMagara",
            "Region4_KaranlikTepe"
        };

        for (int i = 0; i < regionObjectNames.Length; i++)
        {
            Vector3 center = new Vector3(i * 24f, 0f, 0f);
            GameObject regionRoot = new GameObject(regionObjectNames[i]);

            GameObject ground = CreateGround(
                i == 0 ? "Ground" : regionObjectNames[i] + "_Ground",
                center,
                regionMaterials[i]
            );
            ground.transform.SetParent(regionRoot.transform);

            GameObject spawnPoint = new GameObject("PlayerSpawnPoint");
            spawnPoint.transform.SetParent(regionRoot.transform);
            spawnPoint.transform.position = center + new Vector3(-5f, 1f, 0f);
            spawnPoints[i] = spawnPoint.transform;

            GameObject seed = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            seed.name = "LightSeed_" + (i + 1);
            seed.transform.SetParent(regionRoot.transform);
            seed.transform.position = center + new Vector3(0f, 0.75f, 3.2f);
            seed.transform.localScale = new Vector3(0.7f, 1f, 0.7f);
            seed.GetComponent<Renderer>().sharedMaterial = seedMaterial;

            SphereCollider seedCollider = seed.GetComponent<SphereCollider>();
            seedCollider.isTrigger = true;
            Rigidbody seedRigidbody = seed.AddComponent<Rigidbody>();
            seedRigidbody.useGravity = false;
            seedRigidbody.isKinematic = true;

            LightSeedCollectible collectible =
                seed.AddComponent<LightSeedCollectible>();
            SerializedObject seedObject = new SerializedObject(collectible);
            SetObjectProperty(
                seedObject,
                "progressController",
                progressController
            );
            SetIntegerProperty(seedObject, "regionIndex", i);
            seedObject.ApplyModifiedPropertiesWithoutUndo();
            lightSeeds[i] = collectible;

            GameObject portal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            portal.name = i == regionObjectNames.Length - 1
                ? "FinalLightGoal"
                : "PortalToRegion" + (i + 2);
            portal.transform.SetParent(regionRoot.transform);
            portal.transform.position = center + new Vector3(6f, 1.4f, 0f);
            portal.transform.localScale = new Vector3(0.6f, 2.8f, 3.2f);
            Renderer portalRenderer = portal.GetComponent<Renderer>();
            portalRenderer.sharedMaterial = portalLockedMaterial;

            BoxCollider portalTriggerCollider = portal.GetComponent<BoxCollider>();
            portalTriggerCollider.isTrigger = true;
            portalTriggerCollider.size = new Vector3(2f, 1.2f, 1.2f);
            Rigidbody portalRigidbody = portal.AddComponent<Rigidbody>();
            portalRigidbody.useGravity = false;
            portalRigidbody.isKinematic = true;

            GameObject barrier = new GameObject("PortalBarrier");
            barrier.transform.SetParent(portal.transform, false);
            BoxCollider barrierCollider = barrier.AddComponent<BoxCollider>();
            barrierCollider.size = new Vector3(0.8f, 1f, 1f);

            PortalTrigger portalTrigger = portal.AddComponent<PortalTrigger>();
            SerializedObject portalObject = new SerializedObject(portalTrigger);
            SetObjectProperty(
                portalObject,
                "progressController",
                progressController
            );
            SetIntegerProperty(portalObject, "regionIndex", i);
            SetBooleanProperty(
                portalObject,
                "isFinalPortal",
                i == regionObjectNames.Length - 1
            );
            SetObjectProperty(portalObject, "portalRenderer", portalRenderer);
            SetObjectProperty(portalObject, "blockingCollider", barrierCollider);
            SetObjectProperty(
                portalObject,
                "lockedMaterial",
                portalLockedMaterial
            );
            SetObjectProperty(
                portalObject,
                "openMaterial",
                portalOpenMaterial
            );
            portalObject.ApplyModifiedPropertiesWithoutUndo();
            portals[i] = portalTrigger;

            CreateRegionDecorations(
                regionRoot.transform,
                center,
                i,
                regionMaterials[i]
            );
        }

        SerializedObject progressObject =
            new SerializedObject(progressController);
        SetObjectProperty(progressObject, "eventSender", eventSender);
        SetObjectProperty(progressObject, "player", player);
        SetObjectProperty(progressObject, "mainCamera", mainCamera);
        SetObjectArrayProperty(
            progressObject,
            "regionSpawnPoints",
            spawnPoints
        );
        SetObjectArrayProperty(progressObject, "lightSeeds", lightSeeds);
        SetObjectArrayProperty(progressObject, "portals", portals);
        SetObjectProperty(progressObject, "regionOneGuide", regionOneGuide);
        SetObjectProperty(progressObject, "activeRegionText", activeRegionText);
        SetObjectProperty(progressObject, "seedCountText", seedCountText);
        SetObjectProperty(progressObject, "portalStatusText", portalStatusText);
        SetObjectProperty(progressObject, "objectiveText", objectiveText);
        SetObjectProperty(progressObject, "notificationText", notificationText);
        progressObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateRegionDecorations(
        Transform parent,
        Vector3 center,
        int regionIndex,
        Material material)
    {
        Vector3[] offsets =
        {
            new Vector3(-5.5f, 0f, -5.5f),
            new Vector3(-5.5f, 0f, 5.5f),
            new Vector3(4.8f, 0f, -5.2f)
        };

        foreach (Vector3 offset in offsets)
        {
            PrimitiveType primitiveType = regionIndex == 1
                ? PrimitiveType.Cylinder
                : regionIndex == 2
                    ? PrimitiveType.Cube
                    : PrimitiveType.Sphere;
            GameObject decoration = GameObject.CreatePrimitive(primitiveType);
            decoration.name = "RegionDecoration";
            decoration.transform.SetParent(parent);
            decoration.transform.position = center + offset + Vector3.up * 0.65f;
            decoration.transform.localScale = regionIndex == 2
                ? new Vector3(0.8f, 1.8f, 0.8f)
                : regionIndex == 1
                    ? new Vector3(0.7f, 1.3f, 0.7f)
                    : new Vector3(1.2f, 1.2f, 1.2f);
            decoration.transform.rotation = regionIndex == 2
                ? Quaternion.Euler(0f, 35f, 35f)
                : Quaternion.identity;
            decoration.GetComponent<Renderer>().sharedMaterial = material;
        }
    }

    private static void CreatePuzzlePaper(Material material, PuzzlePopupUI puzzlePopup)
    {
        GameObject puzzlePaper = GameObject.CreatePrimitive(PrimitiveType.Cube);
        puzzlePaper.name = "PuzzlePaper";
        puzzlePaper.transform.position = new Vector3(3f, 0.25f, 0f);
        puzzlePaper.transform.localScale = new Vector3(1.5f, 0.1f, 1f);
        puzzlePaper.GetComponent<Renderer>().sharedMaterial = material;

        BoxCollider boxCollider = puzzlePaper.GetComponent<BoxCollider>();
        boxCollider.isTrigger = true;

        Rigidbody rigidbody = puzzlePaper.AddComponent<Rigidbody>();
        rigidbody.useGravity = false;
        rigidbody.isKinematic = true;

        PuzzleTrigger puzzleTrigger = puzzlePaper.AddComponent<PuzzleTrigger>();
        SerializedObject triggerObject = new SerializedObject(puzzleTrigger);
        SetObjectProperty(triggerObject, "puzzlePopup", puzzlePopup);
        triggerObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static DemoFlowController CreateDemoFlow(
        Material trunkMaterial,
        Material crownMaterial)
    {
        GameObject environmentRoot = new GameObject("IsikliVadiEnvironment");

        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "LightTreeTrunk";
        trunk.transform.SetParent(environmentRoot.transform);
        trunk.transform.position = new Vector3(0f, 1.25f, 5f);
        trunk.transform.localScale = new Vector3(0.55f, 1.25f, 0.55f);
        trunk.GetComponent<Renderer>().sharedMaterial = trunkMaterial;

        GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crown.name = "LightTreeCrown";
        crown.transform.SetParent(environmentRoot.transform);
        crown.transform.position = new Vector3(0f, 3.4f, 5f);
        crown.transform.localScale = new Vector3(2.3f, 2f, 2.3f);
        crown.GetComponent<Renderer>().sharedMaterial = crownMaterial;

        GameObject canvasObject = new GameObject(
            "DemoGuideCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;

        CanvasScaler canvasScaler = canvasObject.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasScaler.matchWidthOrHeight = 0.5f;

        GameObject guidePanel = CreateUiObject(
            "DemoGuidePanel",
            canvasObject.transform
        );
        RectTransform guideRect = guidePanel.GetComponent<RectTransform>();
        guideRect.anchorMin = new Vector2(0.5f, 1f);
        guideRect.anchorMax = new Vector2(0.5f, 1f);
        guideRect.pivot = new Vector2(0.5f, 1f);
        guideRect.sizeDelta = new Vector2(760f, 125f);
        guideRect.anchoredPosition = new Vector2(0f, -20f);

        Image guideImage = guidePanel.AddComponent<Image>();
        guideImage.color = new Color(0.08f, 0.14f, 0.18f, 0.88f);
        guideImage.raycastTarget = false;

        Text titleText = CreateText(
            "RegionTitle",
            guidePanel.transform,
            "Opsiyonel Mini Görev",
            28,
            FontStyle.Bold,
            new Vector2(700f, 42f),
            new Vector2(0f, 30f)
        );
        titleText.raycastTarget = false;

        Text instructionText = CreateText(
            "InstructionText",
            guidePanel.transform,
            "İstersen PuzzlePaper'daki mini görevleri deneyebilirsin.",
            22,
            FontStyle.Normal,
            new Vector2(700f, 55f),
            new Vector2(0f, -22f)
        );
        instructionText.raycastTarget = false;

        DemoFlowController demoFlow =
            canvasObject.AddComponent<DemoFlowController>();
        SerializedObject flowObject = new SerializedObject(demoFlow);
        SetObjectProperty(flowObject, "instructionText", instructionText);
        flowObject.ApplyModifiedPropertiesWithoutUndo();

        return demoFlow;
    }

    private static StoryIntroUI CreateStoryIntro(
        PlayerController playerController)
    {
        GameObject canvasObject = new GameObject(
            "StoryIntroCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;

        CanvasScaler canvasScaler = canvasObject.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasScaler.matchWidthOrHeight = 0.5f;

        GameObject introRoot = CreateUiObject(
            "StoryIntroRoot",
            canvasObject.transform
        );
        RectTransform rootRect = introRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image backdrop = introRoot.AddComponent<Image>();
        backdrop.color = new Color(0.02f, 0.04f, 0.08f, 0.92f);

        GameObject storyPanel = CreatePopupPanel(
            "StoryPanel",
            introRoot.transform,
            new Vector2(900f, 520f)
        );

        CreateText(
            "Title",
            storyPanel.transform,
            "Lumora Karardı",
            44,
            FontStyle.Bold,
            new Vector2(800f, 65f),
            new Vector2(0f, 185f)
        );
        CreateText(
            "StoryText",
            storyPanel.transform,
            "Işık Ağacı gücünü kaybetti. Işık tohumları dört farklı " +
            "bölgeye dağıldı. İlk ışık tohumu Işıklı Vadi'de saklı. " +
            "Onu bul ve Lumora'ya ışığı geri getir.",
            27,
            FontStyle.Normal,
            new Vector2(780f, 170f),
            new Vector2(0f, 65f)
        );

        Text objectiveText = CreateText(
            "ObjectiveText",
            storyPanel.transform,
            "İlk hedef: Işıklı Vadi'deki ışık tohumunu bul.",
            25,
            FontStyle.Bold,
            new Vector2(780f, 65f),
            new Vector2(0f, -70f)
        );
        objectiveText.color = new Color(1f, 0.82f, 0.28f);

        Button startButton = CreateButton(
            "StartAdventureButton",
            storyPanel.transform,
            "Göreve Başla",
            new Vector2(0f, -175f),
            new Color(0.25f, 0.65f, 0.35f)
        );
        RectTransform buttonRect = startButton.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(220f, 64f);
        RectTransform buttonTextRect =
            startButton.GetComponentInChildren<Text>().rectTransform;
        buttonTextRect.sizeDelta = new Vector2(220f, 64f);

        StoryIntroUI storyIntro = canvasObject.AddComponent<StoryIntroUI>();
        SerializedObject introObject = new SerializedObject(storyIntro);
        SetObjectProperty(introObject, "playerController", playerController);
        SetObjectProperty(introObject, "introRoot", introRoot);
        SetObjectProperty(introObject, "startButton", startButton);
        SetBooleanProperty(introObject, "showOnStart", false);
        introObject.ApplyModifiedPropertiesWithoutUndo();

        introRoot.SetActive(false);
        return storyIntro;
    }

    private static void CreateMainMenuFlow(
        PlayerController playerController,
        StoryIntroUI storyIntro,
        Texture2D mainMenuTexture,
        Texture2D maleCharacterTexture,
        Texture2D femaleCharacterTexture)
    {
        GameObject mainMenuCanvas = new GameObject(
            "MainMenuCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );
        Vector2 menuReferenceResolution = new Vector2(
            mainMenuTexture.width,
            mainMenuTexture.height
        );
        ConfigureOverlayCanvas(mainMenuCanvas, 50, menuReferenceResolution);

        GameObject menuRoot = CreateFullScreenUiObject(
            "MainMenuRoot",
            mainMenuCanvas.transform
        );
        Image menuFallback = menuRoot.AddComponent<Image>();
        menuFallback.color = new Color(0.04f, 0.12f, 0.12f, 1f);
        menuFallback.raycastTarget = false;

        RawImage mainMenuConcept = CreateAspectFittedRawImage(
            "MainMenuConcept",
            menuRoot.transform,
            mainMenuTexture,
            menuReferenceResolution,
            Vector2.zero
        );

        Button startButton = CreateAnchoredTransparentButton(
            "StartButton",
            mainMenuConcept.transform,
            new Vector2(0.581f, 0.48f),
            new Vector2(0.851f, 0.644f)
        );
        Button continueButton = CreateAnchoredTransparentButton(
            "ContinueButton",
            mainMenuConcept.transform,
            new Vector2(0.584f, 0.312f),
            new Vector2(0.851f, 0.456f)
        );
        Button settingsButton = CreateAnchoredTransparentButton(
            "SettingsButton",
            mainMenuConcept.transform,
            new Vector2(0.581f, 0.124f),
            new Vector2(0.853f, 0.284f)
        );

        float menuScaleX = mainMenuTexture.width / 1920f;
        float menuScaleY = mainMenuTexture.height / 1080f;
        Text informationText = CreateText(
            "InformationText",
            menuRoot.transform,
            string.Empty,
            Mathf.Max(6, Mathf.RoundToInt(23f * menuScaleY)),
            FontStyle.Bold,
            new Vector2(760f * menuScaleX, 65f * menuScaleY),
            new Vector2(430f * menuScaleX, -455f * menuScaleY)
        );
        informationText.color = new Color(1f, 0.94f, 0.68f);
        informationText.raycastTarget = false;

        GameObject characterSelectCanvas = new GameObject(
            "CharacterSelectCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );
        ConfigureOverlayCanvas(
            characterSelectCanvas,
            55,
            new Vector2(1920f, 1080f)
        );

        GameObject selectionRoot = CreateFullScreenUiObject(
            "CharacterSelectRoot",
            characterSelectCanvas.transform
        );
        Image selectionBackdrop = selectionRoot.AddComponent<Image>();
        selectionBackdrop.color = new Color(0.05f, 0.16f, 0.15f, 1f);

        CreateText(
            "Title",
            selectionRoot.transform,
            "Karakterini Seç",
            46,
            FontStyle.Bold,
            new Vector2(900f, 70f),
            new Vector2(0f, 455f)
        );
        CreateText(
            "Description",
            selectionRoot.transform,
            "Lumora macerasına kiminle başlayacaksın?",
            27,
            FontStyle.Normal,
            new Vector2(900f, 55f),
            new Vector2(0f, 395f)
        );

        Button maleButton = CreateCharacterCard(
            "MaleCharacterButton",
            selectionRoot.transform,
            maleCharacterTexture,
            "Erkek Karakter",
            new Vector2(-300f, 10f),
            new Color(0.17f, 0.38f, 0.42f, 1f)
        );
        Button femaleButton = CreateCharacterCard(
            "FemaleCharacterButton",
            selectionRoot.transform,
            femaleCharacterTexture,
            "Kız Karakter",
            new Vector2(300f, 10f),
            new Color(0.35f, 0.25f, 0.4f, 1f)
        );
        Button backButton = CreateSizedButton(
            "BackButton",
            selectionRoot.transform,
            "Geri",
            new Vector2(210f, 65f),
            new Vector2(0f, -445f),
            new Color(0.36f, 0.43f, 0.46f, 1f)
        );

        MainMenuUI mainMenu = mainMenuCanvas.AddComponent<MainMenuUI>();
        CharacterSelectionUI characterSelection =
            characterSelectCanvas.AddComponent<CharacterSelectionUI>();

        SerializedObject mainMenuObject = new SerializedObject(mainMenu);
        SetObjectProperty(
            mainMenuObject,
            "playerController",
            playerController
        );
        SetObjectProperty(mainMenuObject, "menuRoot", menuRoot);
        SetObjectProperty(
            mainMenuObject,
            "characterSelectionUI",
            characterSelection
        );
        SetObjectProperty(mainMenuObject, "startButton", startButton);
        SetObjectProperty(mainMenuObject, "continueButton", continueButton);
        SetObjectProperty(mainMenuObject, "settingsButton", settingsButton);
        SetObjectProperty(mainMenuObject, "informationText", informationText);
        mainMenuObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject selectionObject =
            new SerializedObject(characterSelection);
        SetObjectProperty(
            selectionObject,
            "playerController",
            playerController
        );
        SetObjectProperty(selectionObject, "selectionRoot", selectionRoot);
        SetObjectProperty(selectionObject, "mainMenuUI", mainMenu);
        SetObjectProperty(selectionObject, "storyIntroUI", storyIntro);
        SetObjectProperty(selectionObject, "maleButton", maleButton);
        SetObjectProperty(selectionObject, "femaleButton", femaleButton);
        SetObjectProperty(selectionObject, "backButton", backButton);
        selectionObject.ApplyModifiedPropertiesWithoutUndo();

        menuRoot.SetActive(true);
        characterSelectCanvas.SetActive(false);
    }

    private static void CreateNpc(
        Material material,
        GameEventSender eventSender,
        PlayerController playerController,
        DemoFlowController demoFlow)
    {
        NpcDialogueUI dialogueUI = CreateNpcDialogueUi(
            eventSender,
            playerController,
            demoFlow
        );

        GameObject npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        npc.name = "ForestFriendNPC";
        npc.transform.position = new Vector3(-3f, 1f, 0f);
        npc.GetComponent<Renderer>().sharedMaterial = material;

        CapsuleCollider collider = npc.GetComponent<CapsuleCollider>();
        collider.isTrigger = true;
        collider.radius = 1.25f;
        collider.height = 2.5f;

        Rigidbody rigidbody = npc.AddComponent<Rigidbody>();
        rigidbody.useGravity = false;
        rigidbody.isKinematic = true;

        NpcInteractionTrigger interaction =
            npc.AddComponent<NpcInteractionTrigger>();
        SerializedObject interactionObject = new SerializedObject(interaction);
        SetObjectProperty(interactionObject, "dialogueUI", dialogueUI);
        interactionObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static NpcDialogueUI CreateNpcDialogueUi(
        GameEventSender eventSender,
        PlayerController playerController,
        DemoFlowController demoFlow)
    {
        GameObject canvasObject = new GameObject(
            "NpcCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler canvasScaler = canvasObject.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasScaler.matchWidthOrHeight = 0.5f;

        GameObject popupRoot = CreatePopupPanel(
            "NpcDialoguePanel",
            canvasObject.transform,
            new Vector2(680f, 360f)
        );
        CreateText(
            "Title",
            popupRoot.transform,
            "Orman Dostu",
            34,
            FontStyle.Bold,
            new Vector2(600f, 50f),
            new Vector2(0f, 120f)
        );
        CreateText(
            "DialogueText",
            popupRoot.transform,
            "Işık tohumunu bulmama yardım eder misin?",
            24,
            FontStyle.Normal,
            new Vector2(600f, 70f),
            new Vector2(0f, 45f)
        );

        Button helpButton = CreateButton(
            "HelpButton",
            popupRoot.transform,
            "Yardım Et",
            new Vector2(-180f, -55f),
            new Color(0.25f, 0.65f, 0.35f)
        );
        Button laterButton = CreateButton(
            "LaterButton",
            popupRoot.transform,
            "Sonra",
            new Vector2(0f, -55f),
            new Color(0.38f, 0.5f, 0.72f)
        );
        Button closeButton = CreateButton(
            "CloseButton",
            popupRoot.transform,
            "Kapat",
            new Vector2(180f, -55f),
            new Color(0.38f, 0.43f, 0.5f)
        );

        NpcDialogueUI dialogueUI = canvasObject.AddComponent<NpcDialogueUI>();
        SerializedObject dialogueObject = new SerializedObject(dialogueUI);
        SetStringProperty(dialogueObject, "childId", ChildId);
        SetStringProperty(dialogueObject, "region", "isikli_vadi");
        SetObjectProperty(dialogueObject, "eventSender", eventSender);
        SetObjectProperty(dialogueObject, "playerController", playerController);
        SetObjectProperty(dialogueObject, "demoFlowController", demoFlow);
        SetObjectProperty(dialogueObject, "popupRoot", popupRoot);
        SetObjectProperty(dialogueObject, "helpButton", helpButton);
        SetObjectProperty(dialogueObject, "laterButton", laterButton);
        SetObjectProperty(dialogueObject, "closeButton", closeButton);
        dialogueObject.ApplyModifiedPropertiesWithoutUndo();

        popupRoot.SetActive(false);
        return dialogueUI;
    }

    private static PuzzlePopupUI CreatePuzzlePopup(
        GameEventSender eventSender,
        PlayerController playerController,
        DemoFlowController demoFlow)
    {
        GameObject canvasObject = new GameObject(
            "PuzzleCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler canvasScaler = canvasObject.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasScaler.matchWidthOrHeight = 0.5f;

        GameObject popupRoot = CreateUiObject("PuzzlePopupRoot", canvasObject.transform);
        RectTransform popupRootRect = popupRoot.GetComponent<RectTransform>();
        popupRootRect.anchorMin = Vector2.zero;
        popupRootRect.anchorMax = Vector2.one;
        popupRootRect.offsetMin = Vector2.zero;
        popupRootRect.offsetMax = Vector2.zero;
        GameObject selectionPanel = CreatePopupPanel(
            "PuzzleSelectionPanel",
            popupRoot.transform,
            new Vector2(800f, 360f)
        );

        CreateText(
            "Title",
            selectionPanel.transform,
            "Bulmaca Seç",
            34,
            FontStyle.Bold,
            new Vector2(540f, 50f),
            new Vector2(0f, 115f)
        );
        CreateText(
            "Description",
            selectionPanel.transform,
            "Oynamak istediğin bulmacayı seç.",
            24,
            FontStyle.Normal,
            new Vector2(540f, 50f),
            new Vector2(0f, 55f)
        );
        Button startHiddenObjectButton = CreateButton(
            "StartHiddenObjectButton",
            selectionPanel.transform,
            "Hidden Object",
            new Vector2(-210f, -20f),
            new Color(0.92f, 0.72f, 0.18f)
        );
        Button startMemoryMatchButton = CreateButton(
            "StartMemoryMatchButton",
            selectionPanel.transform,
            "Memory Match",
            new Vector2(0f, -20f),
            new Color(0.35f, 0.58f, 0.78f)
        );
        Button startPatternPuzzleButton = CreateButton(
            "StartPatternPuzzleButton",
            selectionPanel.transform,
            "Pattern Puzzle",
            new Vector2(210f, -20f),
            new Color(0.65f, 0.4f, 0.72f)
        );
        Button closeSelectionButton = CreateButton(
            "CloseSelectionButton",
            selectionPanel.transform,
            "Kapat",
            new Vector2(0f, -105f),
            new Color(0.38f, 0.43f, 0.5f)
        );

        GameObject hiddenObjectPanel = CreatePopupPanel(
            "HiddenObjectPanel",
            popupRoot.transform,
            new Vector2(700f, 440f)
        );
        CreateText(
            "Title",
            hiddenObjectPanel.transform,
            "Hidden Object",
            34,
            FontStyle.Bold,
            new Vector2(620f, 50f),
            new Vector2(0f, 165f)
        );
        CreateText(
            "Description",
            hiddenObjectPanel.transform,
            "Gizli nesneler arasından Işık Tohumu'nu bul.",
            24,
            FontStyle.Normal,
            new Vector2(620f, 55f),
            new Vector2(0f, 110f)
        );

        Button lightSeedButton = CreateButton(
            "LightSeedButton",
            hiddenObjectPanel.transform,
            "Işık Tohumu",
            new Vector2(-210f, 25f),
            new Color(0.92f, 0.72f, 0.18f)
        );
        Button shinyStoneButton = CreateButton(
            "ShinyStoneButton",
            hiddenObjectPanel.transform,
            "Parlak Taş",
            new Vector2(0f, 25f),
            new Color(0.35f, 0.58f, 0.78f)
        );
        Button goldenLeafButton = CreateButton(
            "GoldenLeafButton",
            hiddenObjectPanel.transform,
            "Altın Yaprak",
            new Vector2(210f, 25f),
            new Color(0.75f, 0.52f, 0.2f)
        );

        Text feedbackText = CreateText(
            "FeedbackText",
            hiddenObjectPanel.transform,
            "Işık Tohumu'nu bul.",
            21,
            FontStyle.Normal,
            new Vector2(620f, 50f),
            new Vector2(0f, -55f)
        );

        Button hintButton = CreateButton(
            "HintButton",
            hiddenObjectPanel.transform,
            "İpucu",
            new Vector2(-95f, -140f),
            new Color(0.38f, 0.5f, 0.72f)
        );
        Button closeHiddenObjectButton = CreateButton(
            "CloseHiddenObjectButton",
            hiddenObjectPanel.transform,
            "Kapat",
            new Vector2(95f, -140f),
            new Color(0.38f, 0.43f, 0.5f)
        );

        GameObject memoryMatchPanel = CreatePopupPanel(
            "MemoryMatchPanel",
            popupRoot.transform,
            new Vector2(700f, 500f)
        );
        CreateText(
            "Title",
            memoryMatchPanel.transform,
            "Memory Match",
            34,
            FontStyle.Bold,
            new Vector2(620f, 50f),
            new Vector2(0f, 195f)
        );
        CreateText(
            "Description",
            memoryMatchPanel.transform,
            "Aynı sembolleri eşleştir.",
            24,
            FontStyle.Normal,
            new Vector2(620f, 50f),
            new Vector2(0f, 145f)
        );

        Button[] cardButtons =
        {
            CreateButton(
                "Card1",
                memoryMatchPanel.transform,
                "?",
                new Vector2(-105f, 65f),
                new Color(0.28f, 0.48f, 0.7f)
            ),
            CreateButton(
                "Card2",
                memoryMatchPanel.transform,
                "?",
                new Vector2(105f, 65f),
                new Color(0.28f, 0.48f, 0.7f)
            ),
            CreateButton(
                "Card3",
                memoryMatchPanel.transform,
                "?",
                new Vector2(-105f, -15f),
                new Color(0.28f, 0.48f, 0.7f)
            ),
            CreateButton(
                "Card4",
                memoryMatchPanel.transform,
                "?",
                new Vector2(105f, -15f),
                new Color(0.28f, 0.48f, 0.7f)
            )
        };

        Text memoryStatusText = CreateText(
            "StatusText",
            memoryMatchPanel.transform,
            "Aynı sembolleri eşleştir.",
            21,
            FontStyle.Normal,
            new Vector2(620f, 50f),
            new Vector2(0f, -90f)
        );
        Button closeMemoryMatchButton = CreateButton(
            "CloseMemoryMatchButton",
            memoryMatchPanel.transform,
            "Kapat",
            new Vector2(0f, -175f),
            new Color(0.38f, 0.43f, 0.5f)
        );

        GameObject patternPuzzlePanel = CreatePopupPanel(
            "PatternPuzzlePanel",
            popupRoot.transform,
            new Vector2(720f, 460f)
        );
        CreateText(
            "Title",
            patternPuzzlePanel.transform,
            "Pattern Puzzle",
            34,
            FontStyle.Bold,
            new Vector2(640f, 50f),
            new Vector2(0f, 175f)
        );
        CreateText(
            "Description",
            patternPuzzlePanel.transform,
            "Örüntüyü tamamla.",
            24,
            FontStyle.Normal,
            new Vector2(640f, 50f),
            new Vector2(0f, 125f)
        );
        CreateText(
            "PatternText",
            patternPuzzlePanel.transform,
            "Kırmızı - Mavi - Kırmızı - ?",
            26,
            FontStyle.Bold,
            new Vector2(640f, 55f),
            new Vector2(0f, 65f)
        );

        Button redButton = CreateButton(
            "RedButton",
            patternPuzzlePanel.transform,
            "Kırmızı",
            new Vector2(-190f, -15f),
            new Color(0.78f, 0.25f, 0.25f)
        );
        Button blueButton = CreateButton(
            "BlueButton",
            patternPuzzlePanel.transform,
            "Mavi",
            new Vector2(0f, -15f),
            new Color(0.25f, 0.45f, 0.82f)
        );
        Button yellowButton = CreateButton(
            "YellowButton",
            patternPuzzlePanel.transform,
            "Sarı",
            new Vector2(190f, -15f),
            new Color(0.9f, 0.72f, 0.18f)
        );
        Text patternStatusText = CreateText(
            "StatusText",
            patternPuzzlePanel.transform,
            "Doğru rengi seç.",
            21,
            FontStyle.Normal,
            new Vector2(640f, 50f),
            new Vector2(0f, -85f)
        );
        Button closePatternPuzzleButton = CreateButton(
            "ClosePatternPuzzleButton",
            patternPuzzlePanel.transform,
            "Kapat",
            new Vector2(0f, -155f),
            new Color(0.38f, 0.43f, 0.5f)
        );

        PuzzlePopupUI popup = canvasObject.AddComponent<PuzzlePopupUI>();
        HiddenObjectPuzzleUI hiddenObjectPuzzle =
            canvasObject.AddComponent<HiddenObjectPuzzleUI>();
        MemoryMatchPuzzleUI memoryMatchPuzzle =
            canvasObject.AddComponent<MemoryMatchPuzzleUI>();
        PatternPuzzleUI patternPuzzle =
            canvasObject.AddComponent<PatternPuzzleUI>();

        SerializedObject popupObject = new SerializedObject(popup);
        SetStringProperty(popupObject, "childId", ChildId);
        SetStringProperty(popupObject, "region", "isikli_vadi");
        SetObjectProperty(popupObject, "eventSender", eventSender);
        SetObjectProperty(popupObject, "playerController", playerController);
        SetObjectProperty(popupObject, "demoFlowController", demoFlow);
        SetObjectProperty(popupObject, "popupRoot", popupRoot);
        SetObjectProperty(popupObject, "selectionPanel", selectionPanel);
        SetObjectProperty(popupObject, "hiddenObjectPanel", hiddenObjectPanel);
        SetObjectProperty(popupObject, "memoryMatchPanel", memoryMatchPanel);
        SetObjectProperty(popupObject, "patternPuzzlePanel", patternPuzzlePanel);
        SetObjectProperty(popupObject, "hiddenObjectPuzzle", hiddenObjectPuzzle);
        SetObjectProperty(popupObject, "memoryMatchPuzzle", memoryMatchPuzzle);
        SetObjectProperty(popupObject, "patternPuzzle", patternPuzzle);
        SetObjectProperty(
            popupObject,
            "startHiddenObjectButton",
            startHiddenObjectButton
        );
        SetObjectProperty(
            popupObject,
            "startMemoryMatchButton",
            startMemoryMatchButton
        );
        SetObjectProperty(
            popupObject,
            "startPatternPuzzleButton",
            startPatternPuzzleButton
        );
        SetObjectProperty(popupObject, "closeSelectionButton", closeSelectionButton);
        SetObjectProperty(
            popupObject,
            "closeHiddenObjectButton",
            closeHiddenObjectButton
        );
        SetObjectProperty(
            popupObject,
            "closeMemoryMatchButton",
            closeMemoryMatchButton
        );
        SetObjectProperty(
            popupObject,
            "closePatternPuzzleButton",
            closePatternPuzzleButton
        );
        popupObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject hiddenObject = new SerializedObject(hiddenObjectPuzzle);
        SetObjectProperty(hiddenObject, "puzzlePopup", popup);
        SetObjectProperty(hiddenObject, "lightSeedButton", lightSeedButton);
        SetObjectProperty(hiddenObject, "shinyStoneButton", shinyStoneButton);
        SetObjectProperty(hiddenObject, "goldenLeafButton", goldenLeafButton);
        SetObjectProperty(hiddenObject, "hintButton", hintButton);
        SetObjectProperty(hiddenObject, "feedbackText", feedbackText);
        hiddenObject.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject memoryMatch = new SerializedObject(memoryMatchPuzzle);
        SetObjectProperty(memoryMatch, "puzzlePopup", popup);
        SetObjectArrayProperty(memoryMatch, "cardButtons", cardButtons);
        SetObjectProperty(memoryMatch, "statusText", memoryStatusText);
        memoryMatch.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject pattern = new SerializedObject(patternPuzzle);
        SetObjectProperty(pattern, "puzzlePopup", popup);
        SetObjectProperty(pattern, "redButton", redButton);
        SetObjectProperty(pattern, "blueButton", blueButton);
        SetObjectProperty(pattern, "yellowButton", yellowButton);
        SetObjectProperty(pattern, "statusText", patternStatusText);
        pattern.ApplyModifiedPropertiesWithoutUndo();

        popupRoot.SetActive(false);
        CreateEventSystem();
        return popup;
    }

    private static void ConfigureOverlayCanvas(
        GameObject canvasObject,
        int sortingOrder,
        Vector2 referenceResolution)
    {
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        canvas.pixelPerfect = true;

        CanvasScaler canvasScaler = canvasObject.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = referenceResolution;
        canvasScaler.screenMatchMode =
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;
        canvasScaler.referencePixelsPerUnit = 100f;
    }

    private static GameObject CreateFullScreenUiObject(
        string name,
        Transform parent)
    {
        GameObject uiObject = CreateUiObject(name, parent);
        RectTransform rectTransform = uiObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        return uiObject;
    }

    private static RawImage CreateAspectFittedRawImage(
        string name,
        Transform parent,
        Texture2D texture,
        Vector2 areaSize,
        Vector2 position)
    {
        GameObject imageArea = CreateUiObject(name + "Area", parent);
        SetCenteredRect(
            imageArea.GetComponent<RectTransform>(),
            areaSize,
            position
        );

        GameObject imageObject = CreateUiObject(name, imageArea.transform);
        RawImage rawImage = imageObject.AddComponent<RawImage>();
        rawImage.texture = texture;
        rawImage.color = Color.white;
        rawImage.raycastTarget = false;
        rawImage.uvRect = new Rect(0f, 0f, 1f, 1f);

        AspectRatioFitter aspectFitter =
            imageObject.AddComponent<AspectRatioFitter>();
        aspectFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        aspectFitter.aspectRatio = (float)texture.width / texture.height;
        return rawImage;
    }

    private static Button CreateAnchoredTransparentButton(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        GameObject buttonObject = CreateUiObject(name, parent);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = anchorMin;
        buttonRect.anchorMax = anchorMax;
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.01f);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        return button;
    }

    private static Button CreateCharacterCard(
        string name,
        Transform parent,
        Texture2D characterTexture,
        string label,
        Vector2 position,
        Color color)
    {
        GameObject cardObject = CreateUiObject(name, parent);
        SetCenteredRect(
            cardObject.GetComponent<RectTransform>(),
            new Vector2(430f, 740f),
            position
        );

        Image cardImage = cardObject.AddComponent<Image>();
        cardImage.color = color;
        Button button = cardObject.AddComponent<Button>();
        button.targetGraphic = cardImage;

        CreateAspectFittedRawImage(
            "CharacterImage",
            cardObject.transform,
            characterTexture,
            GetNativeSizeWithin(
                characterTexture,
                new Vector2(350f, 600f)
            ),
            new Vector2(0f, 38f)
        );
        Text labelText = CreateText(
            "CharacterLabel",
            cardObject.transform,
            label,
            27,
            FontStyle.Bold,
            new Vector2(390f, 55f),
            new Vector2(0f, -325f)
        );
        labelText.raycastTarget = false;
        return button;
    }

    private static Vector2 GetNativeSizeWithin(
        Texture2D texture,
        Vector2 maximumSize)
    {
        Vector2 nativeSize = new Vector2(texture.width, texture.height);
        float fitScale = Mathf.Min(
            maximumSize.x / nativeSize.x,
            maximumSize.y / nativeSize.y
        );
        fitScale = Mathf.Min(1f, fitScale);
        return nativeSize * fitScale;
    }

    private static Button CreateSizedButton(
        string name,
        Transform parent,
        string label,
        Vector2 size,
        Vector2 position,
        Color color)
    {
        GameObject buttonObject = CreateUiObject(name, parent);
        SetCenteredRect(
            buttonObject.GetComponent<RectTransform>(),
            size,
            position
        );

        Image image = buttonObject.AddComponent<Image>();
        image.color = color;
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        Text buttonText = CreateText(
            "Text",
            buttonObject.transform,
            label,
            23,
            FontStyle.Bold,
            size,
            Vector2.zero
        );
        buttonText.raycastTarget = false;
        return button;
    }

    private static GameObject CreatePopupPanel(
        string name,
        Transform parent,
        Vector2 size)
    {
        GameObject panel = CreateUiObject(name, parent);
        SetCenteredRect(panel.GetComponent<RectTransform>(), size, Vector2.zero);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.12f, 0.16f, 0.22f, 0.97f);
        return panel;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject uiObject = new GameObject(name, typeof(RectTransform));
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    private static Text CreateText(
        string name,
        Transform parent,
        string content,
        int fontSize,
        FontStyle fontStyle,
        Vector2 size,
        Vector2 position)
    {
        GameObject textObject = CreateUiObject(name, parent);
        SetCenteredRect(textObject.GetComponent<RectTransform>(), size, position);

        Text text = textObject.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        return text;
    }

    private static Button CreateButton(
        string name,
        Transform parent,
        string label,
        Vector2 position,
        Color color)
    {
        GameObject buttonObject = CreateUiObject(name, parent);
        SetCenteredRect(
            buttonObject.GetComponent<RectTransform>(),
            new Vector2(145f, 58f),
            position
        );

        Image image = buttonObject.AddComponent<Image>();
        image.color = color;
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        Text buttonText = CreateText(
            "Text",
            buttonObject.transform,
            label,
            21,
            FontStyle.Bold,
            new Vector2(145f, 58f),
            Vector2.zero
        );
        buttonText.raycastTarget = false;
        return button;
    }

    private static void SetCenteredRect(
        RectTransform rectTransform,
        Vector2 size,
        Vector2 position)
    {
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;
        rectTransform.anchoredPosition = position;
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystemObject = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(StandaloneInputModule)
        );
        eventSystemObject.transform.position = Vector3.zero;
    }

    private static Camera CreateOrConfigureMainCamera()
    {
        Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        GameObject cameraObject;

        if (camera == null)
        {
            cameraObject = new GameObject("Main Camera");
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }
        else
        {
            cameraObject = camera.gameObject;
            cameraObject.name = "Main Camera";
        }

        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 8f, -8f);
        cameraObject.transform.rotation = Quaternion.Euler(35f, 0f, 0f);
        return camera;
    }

    private static void CreateDirectionalLight()
    {
        Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(
            FindObjectsSortMode.None
        );

        foreach (Light existingLight in lights)
        {
            if (existingLight.type == LightType.Directional)
            {
                return;
            }
        }

        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        light.shadows = LightShadows.Soft;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private static Material GetOrCreateMaterial(string path, Color color)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            Shader shader = FindCompatibleLitShader();
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static Shader FindCompatibleLitShader()
    {
        string[] shaderNames =
        {
            "Universal Render Pipeline/Lit",
            "HDRP/Lit",
            "Standard"
        };

        foreach (string shaderName in shaderNames)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader != null)
            {
                return shader;
            }
        }

        throw new InvalidOperationException(
            "No compatible Lit shader was found for the prototype materials."
        );
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parentPath = folderPath.Substring(0, folderPath.LastIndexOf('/'));
        string folderName = folderPath.Substring(folderPath.LastIndexOf('/') + 1);
        EnsureFolder(parentPath);
        AssetDatabase.CreateFolder(parentPath, folderName);
    }

    private static bool EnsureTagExists(string tagName)
    {
        foreach (string existingTag in InternalEditorUtility.tags)
        {
            if (existingTag == tagName)
            {
                return true;
            }
        }

        try
        {
            UnityEngine.Object tagManager = AssetDatabase.LoadAllAssetsAtPath(
                "ProjectSettings/TagManager.asset"
            )[0];
            SerializedObject tagManagerObject = new SerializedObject(tagManager);
            SerializedProperty tags = tagManagerObject.FindProperty("tags");
            int newIndex = tags.arraySize;
            tags.InsertArrayElementAtIndex(newIndex);
            tags.GetArrayElementAtIndex(newIndex).stringValue = tagName;
            tagManagerObject.ApplyModifiedProperties();
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "Could not create the '" + tagName + "' tag automatically: " +
                exception.Message
            );
            return false;
        }
    }

    private static void SetStringProperty(
        UnityEngine.Object target,
        string propertyName,
        string value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        SetStringProperty(serializedObject, propertyName, value);
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetStringProperty(
        SerializedObject serializedObject,
        string propertyName,
        string value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            throw new InvalidOperationException(
                "Serialized field was not found: " + propertyName
            );
        }

        property.stringValue = value;
    }

    private static void SetIntegerProperty(
        SerializedObject serializedObject,
        string propertyName,
        int value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            throw new InvalidOperationException(
                "Serialized field was not found: " + propertyName
            );
        }

        property.intValue = value;
    }

    private static void SetBooleanProperty(
        SerializedObject serializedObject,
        string propertyName,
        bool value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            throw new InvalidOperationException(
                "Serialized field was not found: " + propertyName
            );
        }

        property.boolValue = value;
    }

    private static void SetObjectProperty(
        SerializedObject serializedObject,
        string propertyName,
        UnityEngine.Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            throw new InvalidOperationException(
                "Serialized field was not found: " + propertyName
            );
        }

        property.objectReferenceValue = value;
    }

    private static void SetObjectArrayProperty(
        SerializedObject serializedObject,
        string propertyName,
        UnityEngine.Object[] values)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || !property.isArray)
        {
            throw new InvalidOperationException(
                "Serialized array field was not found: " + propertyName
            );
        }

        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
