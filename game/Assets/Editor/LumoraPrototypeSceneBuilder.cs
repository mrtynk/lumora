using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        CreateGround(groundMaterial);
        GameObject player = CreatePlayer();
        GameEventSender eventSender = CreateGameManager();
        CreatePuzzlePaper(puzzleMaterial, eventSender);
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

    private static void CreatePuzzlePaper(Material material, GameEventSender eventSender)
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
        SetStringProperty(triggerObject, "childId", ChildId);
        SetStringProperty(triggerObject, "eventType", "puzzle_started");
        SetStringProperty(triggerObject, "region", "isikli_vadi");
        SetStringProperty(triggerObject, "puzzleType", "hidden_object");
        SetStringProperty(
            triggerObject,
            "value",
            "Unity üzerinden puzzle kağıdı tetiklendi"
        );
        SetObjectProperty(triggerObject, "eventSender", eventSender);
        triggerObject.ApplyModifiedPropertiesWithoutUndo();
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
}
