using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

// Read-only structural checks. Gameplay/analytics and actual video decoding
// still need a Play Mode test; validation never sends events or rebuilds a scene.
public static class IsikliVadiSceneValidation
{
    [MenuItem("Tools/Lumora/Tests/Validate Işıklı Vadi")]
    private static void ValidateActiveScene()
    {
        ValidateScene(SceneManager.GetActiveScene());
    }

    public static void ValidateScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            throw new InvalidOperationException("Doğrulanacak Işıklı Vadi sahnesi açık değil.");
        }

        var errors = new List<string>();
        Transform[] objects = Components<Transform>(scene);
        foreach (Transform item in objects)
        {
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject) > 0)
            {
                errors.Add("Eksik script: " + item.name);
            }
        }

        GameEventSender sender = Unique<GameEventSender>(scene, errors);
        PlayerController player = Unique<PlayerController>(scene, errors);
        RegionProgressController progress = Unique<RegionProgressController>(scene, errors);
        StoryIntroUI story = Unique<StoryIntroUI>(scene, errors);
        IntroVideoUI intro = Unique<IntroVideoUI>(scene, errors);
        MainMenuUI menu = Unique<MainMenuUI>(scene, errors);
        CharacterSelectionUI selection = Unique<CharacterSelectionUI>(scene, errors);
        PuzzlePopupUI popup = Unique<PuzzlePopupUI>(scene, errors);
        NpcDialogueUI dialogue = Unique<NpcDialogueUI>(scene, errors);
        NpcInteractionTrigger npc = Unique<NpcInteractionTrigger>(scene, errors);
        LightSeedCollectible seed = Unique<LightSeedCollectible>(scene, errors);
        PortalTrigger portal = Unique<PortalTrigger>(scene, errors);
        VideoPlayer video = Unique<VideoPlayer>(scene, errors);
        Unique<EventSystem>(scene, errors);

        Camera[] mainCameras = Components<Camera>(scene)
            .Where(camera => camera.CompareTag("MainCamera")).ToArray();
        Require(mainCameras.Length == 1, "Tam bir MainCamera etiketli kamera bulunmalı.", errors);
        if (player != null)
        {
            Require(player.CompareTag("Player"), "Oyuncunun Player etiketi eksik.", errors);
            Require(player.GetComponent<CharacterController>() != null,
                "Oyuncunun CharacterController bileşeni eksik.", errors);
        }

        RequiredReferences(menu, errors, "playerController", "menuRoot", "characterSelectionUI",
            "startButton", "continueButton", "settingsButton", "informationText");
        RequiredReferences(selection, errors, "playerController", "selectionRoot", "mainMenuUI",
            "introVideoUI", "maleButton", "femaleButton", "backButton");
        RequiredReferences(story, errors, "playerController", "introRoot", "startButton");
        RequiredReferences(intro, errors, "playerController", "storyIntroUI", "characterSelectionUI",
            "mainMenuCanvas", "characterSelectionCanvas", "videoRoot", "videoImage",
            "videoAspectFitter", "videoPlayer", "audioSource", "skipButton", "maleIntroClip", "femaleIntroClip");
        RequiredReferences(progress, errors, "eventSender", "player", "mainCamera", "regionSpawnPoints",
            "lightSeeds", "portals", "activeRegionText", "seedCountText", "portalStatusText",
            "objectiveText", "notificationText");
        RequiredReferences(popup, errors, "eventSender", "playerController", "popupRoot", "selectionPanel",
            "hiddenObjectPanel", "memoryMatchPanel", "patternPuzzlePanel", "hiddenObjectPuzzle",
            "memoryMatchPuzzle", "patternPuzzle", "startHiddenObjectButton", "startMemoryMatchButton",
            "startPatternPuzzleButton", "closeSelectionButton", "closeHiddenObjectButton",
            "closeMemoryMatchButton", "closePatternPuzzleButton");
        RequiredReferences(dialogue, errors, "eventSender", "playerController", "popupRoot", "helpButton",
            "laterButton", "closeButton");
        RequiredReferences(npc, errors, "dialogueUI", "playerController");
        RequiredReferences(seed, errors, "progressController");
        RequiredReferences(portal, errors, "progressController", "portalRenderer", "blockingCollider",
            "lockedMaterial", "openMaterial");

        RequiredReferences(Unique<HiddenObjectPuzzleUI>(scene, errors), errors,
            "puzzlePopup", "lightSeedButton", "shinyStoneButton", "goldenLeafButton", "hintButton", "feedbackText");
        MemoryMatchPuzzleUI memory = Unique<MemoryMatchPuzzleUI>(scene, errors);
        RequiredReferences(memory, errors, "puzzlePopup", "cardButtons", "statusText");
        RequireArraySize(memory, "cardButtons", 4, errors);
        RequiredReferences(Unique<PatternPuzzleUI>(scene, errors), errors,
            "puzzlePopup", "redButton", "blueButton", "yellowButton", "statusText");

        foreach (MonoBehaviour component in Components<MonoBehaviour>(scene))
        {
            if (component == null)
            {
                continue;
            }
            // Any system with these existing connections must use this scene's
            // one manager/player/popup, not a copied reference from the prototype.
            ExpectReferenceIfPresent(component, "eventSender", sender, errors);
            ExpectReferenceIfPresent(component, "playerController", player, errors);
            ExpectReferenceIfPresent(component, "progressController", progress, errors);
            ExpectReferenceIfPresent(component, "puzzlePopup", popup, errors);
        }

        PuzzleTrigger[] stations = Components<PuzzleTrigger>(scene);
        Require(stations.Length == 3, "Üç opsiyonel bulmaca noktası bulunmalı.", errors);
        var expectedTypes = new HashSet<string> { "hidden_object", "memory_match", "pattern_puzzle" };
        foreach (PuzzleTrigger station in stations)
        {
            RequiredReferences(station, errors, "puzzlePopup", "playerController");
            SerializedProperty type = Property(station, "preferredPuzzleType");
            Require(type != null && expectedTypes.Remove(type.stringValue),
                station.name + ": bulmaca türü geçersiz veya yinelenmiş.", errors);
        }
        Require(expectedTypes.Count == 0, "Hidden Object, Memory Match ve Pattern Puzzle noktalarının tümü gerekli.", errors);

        RequireBoolean(progress, "startAutomatically", false, errors);
        RequireBoolean(progress, "useSceneTransitions", true, errors);
        RequireBoolean(story, "showOnStart", false, errors);
        RequireArraySize(progress, "regionSpawnPoints", 1, errors);
        RequireArraySize(progress, "lightSeeds", 1, errors);
        RequireArraySize(progress, "portals", 1, errors);
        CheckClip(intro, "maleIntroClip", "Assets/LumoraAssets/Videos/erkek_intro.mp4", errors);
        CheckClip(intro, "femaleIntroClip", "Assets/LumoraAssets/Videos/kiz_intro.mp4", errors);
        if (video != null)
        {
            Require(!video.skipOnDrop, "VideoPlayer Skip On Drop kapalı olmalı (Unity 6.3 oynatma düzeltmesi).", errors);
            Require(!video.playOnAwake && !video.isLooping && video.waitForFirstFrame,
                "VideoPlayer başlangıç, döngü veya ilk kare ayarı hatalı.", errors);
        }

        MeshCollider terrain = Components<MeshCollider>(scene)
            .FirstOrDefault(item => item.sharedMesh != null && item.sharedMesh.name == "ValleyTerrain");
        Require(terrain != null && terrain.sharedMesh != null,
            "ValleyTerrain zemin mesh/collider bulunamadı.", errors);
        if (terrain != null && terrain.sharedMesh != null)
        {
            Vector3 size = Vector3.Scale(terrain.sharedMesh.bounds.size, terrain.transform.lossyScale);
            Require(Mathf.Abs(size.x) >= 80f && Mathf.Abs(size.z) >= 80f,
                "Işıklı Vadi zemini en az 80 x 80 birim olmalı.", errors);
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException("Işıklı Vadi doğrulaması başarısız:\n- " + string.Join("\n- ", errors));
        }

        WarnAboutSpawnOverlap(scene, player);
        Renderer[] renderers = Components<Renderer>(scene);
        Debug.Log("Işıklı Vadi sahne bağlantıları doğrulandı. Obje: " + objects.Length +
            ", renderer: " + renderers.Length + ", static renderer: " +
            renderers.Count(item => item.gameObject.isStatic) + ". Play Mode testi ayrıca yapılmalı.");
    }

    private static T[] Components<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
    }

    private static T Unique<T>(Scene scene, List<string> errors) where T : Component
    {
        T[] found = Components<T>(scene);
        Require(found.Length == 1, typeof(T).Name + " sayısı 1 olmalı; bulunan: " + found.Length, errors);
        return found.Length == 1 ? found[0] : null;
    }

    private static SerializedProperty Property(Component component, string field)
    {
        return component == null ? null : new SerializedObject(component).FindProperty(field);
    }

    private static void RequiredReferences(Component component, List<string> errors, params string[] fields)
    {
        if (component == null)
        {
            return;
        }
        foreach (string field in fields)
        {
            SerializedProperty property = Property(component, field);
            if (property == null)
            {
                errors.Add(component.GetType().Name + "." + field + " alanı bulunamadı.");
                continue;
            }
            if (property.isArray)
            {
                for (int i = 0; i < property.arraySize; i++)
                {
                    RequireReference(component, property.GetArrayElementAtIndex(i), field + "[" + i + "]", errors);
                }
            }
            else
            {
                RequireReference(component, property, field, errors);
            }
        }
    }

    private static void RequireReference(Component owner, SerializedProperty property, string field, List<string> errors)
    {
        UnityEngine.Object value = property.objectReferenceValue;
        Require(value != null, owner.GetType().Name + "." + field + " bağlantısı eksik.", errors);
        GameObject sceneObject = value is Component component ? component.gameObject : value as GameObject;
        if (sceneObject != null)
        {
            Require(sceneObject.scene == owner.gameObject.scene,
                owner.GetType().Name + "." + field + " başka bir sahneye/asset'e bağlı.", errors);
        }
    }

    private static void ExpectReferenceIfPresent(Component owner, string field, UnityEngine.Object expected, List<string> errors)
    {
        SerializedProperty property = Property(owner, field);
        if (property != null && expected != null)
        {
            Require(property.objectReferenceValue == expected,
                owner.GetType().Name + "." + field + " ortak sahne bileşenine bağlı değil.", errors);
        }
    }

    private static void RequireBoolean(Component component, string field, bool expected, List<string> errors)
    {
        SerializedProperty property = Property(component, field);
        Require(property != null && property.boolValue == expected,
            field + " = " + expected + " olmalı.", errors);
    }

    private static void RequireArraySize(Component component, string field, int count, List<string> errors)
    {
        SerializedProperty property = Property(component, field);
        Require(property != null && property.isArray && property.arraySize == count,
            field + " dizisinde " + count + " bağlantı olmalı.", errors);
    }

    private static void CheckClip(Component intro, string field, string assetPath, List<string> errors)
    {
        SerializedProperty clip = Property(intro, field);
        Require(clip != null && AssetDatabase.GetAssetPath(clip.objectReferenceValue) == assetPath,
            field + " yanlış video asset'ine bağlı.", errors);
    }

    private static void WarnAboutSpawnOverlap(Scene scene, PlayerController player)
    {
        CharacterController controller = player.GetComponent<CharacterController>();
        Physics.SyncTransforms();
        Vector3 scale = player.transform.lossyScale;
        float radius = controller.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z)) * 0.9f;
        float halfHeight = Mathf.Max(radius, controller.height * Mathf.Abs(scale.y) * 0.5f);
        Vector3 center = player.transform.TransformPoint(controller.center) + Vector3.up * 0.1f;
        Vector3 offset = player.transform.up * (halfHeight - radius);
        string[] overlaps = Physics.OverlapCapsule(center - offset, center + offset, radius,
            ~0, QueryTriggerInteraction.Ignore)
            .Where(item => item.gameObject.scene == scene &&
                !item.transform.IsChildOf(player.transform))
            .Select(item => item.name).Distinct().ToArray();
        if (overlaps.Length > 0)
        {
            Debug.LogWarning("Başlangıç kapsülü yakınındaki colliderlar Play Mode'da kontrol edilmeli: " +
                string.Join(", ", overlaps));
        }
    }

    private static void Require(bool condition, string message, List<string> errors)
    {
        if (!condition)
        {
            errors.Add(message);
        }
    }
}
