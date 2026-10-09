using UnityEngine;
using UnityEngine.SceneManagement;

// Build uses its included scene list. Editor may also test authored scenes
// without changing project-wide Build Profiles from an Assets-only task.
public static class RegionSceneLoader
{
    public static bool CanLoad(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return false;
        if (Application.CanStreamedLevelBeLoaded(sceneName)) return true;
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(Path(sceneName)) != null;
#else
        return false;
#endif
    }

    public static AsyncOperation Load(string sceneName)
    {
        if (!CanLoad(sceneName)) return null;
#if UNITY_EDITOR
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
            return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                Path(sceneName), new LoadSceneParameters(LoadSceneMode.Single));
#endif
        return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
    }

    private static string Path(string sceneName) => "Assets/Scenes/" + sceneName + ".unity";
}
