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

        CreateGround(groundMaterial);
        GameObject player = CreatePlayer();
        PlayerController playerController = player.GetComponent<PlayerController>();
        GameEventSender eventSender = CreateGameManager();
        DemoFlowController demoFlow = CreateDemoFlow(
            eventSender,
            treeTrunkMaterial,
            treeCrownMaterial,
            rewardMaterial
        );
        CreateStoryIntro(playerController);
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
        CreateOrConfigureMainCamera();
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

    private static void CreateGround(Material material)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(3f, 1f, 3f);
        ground.GetComponent<Renderer>().sharedMaterial = material;
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
        GameEventSender eventSender,
        Material trunkMaterial,
        Material crownMaterial,
        Material rewardMaterial)
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

        GameObject reward = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        reward.name = "LightSeedReward";
        reward.transform.SetParent(environmentRoot.transform);
        reward.transform.position = new Vector3(0f, 0.55f, 3.5f);
        reward.transform.localScale = new Vector3(0.65f, 0.9f, 0.65f);
        reward.GetComponent<Renderer>().sharedMaterial = rewardMaterial;
        reward.SetActive(false);

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
            "Işıklı Vadi",
            28,
            FontStyle.Bold,
            new Vector2(700f, 42f),
            new Vector2(0f, 30f)
        );
        titleText.raycastTarget = false;

        Text instructionText = CreateText(
            "InstructionText",
            guidePanel.transform,
            "PuzzlePaper'a git ve E'ye bas.",
            22,
            FontStyle.Normal,
            new Vector2(700f, 55f),
            new Vector2(0f, -22f)
        );
        instructionText.raycastTarget = false;

        DemoFlowController demoFlow =
            canvasObject.AddComponent<DemoFlowController>();
        SerializedObject flowObject = new SerializedObject(demoFlow);
        SetObjectProperty(flowObject, "eventSender", eventSender);
        SetObjectProperty(flowObject, "instructionText", instructionText);
        SetObjectProperty(flowObject, "rewardObject", reward);
        flowObject.ApplyModifiedPropertiesWithoutUndo();

        return demoFlow;
    }

    private static void CreateStoryIntro(PlayerController playerController)
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
        introObject.ApplyModifiedPropertiesWithoutUndo();
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

    private static void CreateOrConfigureMainCamera()
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
