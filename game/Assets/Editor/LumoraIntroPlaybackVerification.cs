using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

// Explicit, opt-in regression check for the technical prototype.
public static class LumoraIntroPlaybackVerification
{
    private const string Key = "Lumora.VideoVerification.SkipOff.1";
    private const string Report = "Assets/Editor/LumoraIntroPlaybackVerification.results.txt";
    private static double deadline;
    private static int phase;
    private static int testIndex;
    private static VideoPlayer video;
    private static StoryIntroUI story;
    private static CharacterSelectionUI selection;
    private static PlayerController player;
    private static long maximumFrame;
    private static int frameEvents;
    private static int completions;
    private static int overflowWarnings;
    private static uint firstPixels;
    private static bool hasPixels;
    private static bool changedPixels;
    private static double nextSample;
    private static double testStarted;
    private static bool originalRunInBackground;

    [MenuItem("Tools/Lumora/Tests/Verify Prototype Intro Playback")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (SceneManager.GetActiveScene().path != "Assets/Scenes/PrototypeScene.unity")
        {
            Debug.LogWarning("Video kontrolü için PrototypeScene'i açın.");
            return;
        }
        SessionState.SetInt(Key, 0);
        phase = testIndex = 0;
        deadline = EditorApplication.timeSinceStartup + 3;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    [InitializeOnLoadMethod]
    private static void ResumeRunningCheck()
    {
        if (SessionState.GetInt(Key, 0) != 1) return;
        deadline = EditorApplication.timeSinceStartup + 3;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    private static T Find<T>() where T : UnityEngine.Object =>
        UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

    private static void Click(UnityEngine.Object owner, string field)
    {
        var reference = new SerializedObject(owner).FindProperty(field);
        ((Button)reference.objectReferenceValue).onClick.Invoke();
    }

    private static void Write(string message)
    {
        File.AppendAllText(Report, DateTime.Now.ToString("HH:mm:ss") + " " + message + Environment.NewLine);
    }

    private static void Tick()
    {
        try
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (EditorApplication.timeSinceStartup < deadline) return;
            if (SessionState.GetInt(Key, 0) == 0)
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (SceneManager.GetActiveScene().path != "Assets/Scenes/PrototypeScene.unity") return;
                if (SceneManager.GetActiveScene().isDirty) return;
                SessionState.SetInt(Key, 1);
                File.WriteAllText(Report, "Unity " + Application.unityVersion + " intro integration check\n");
                Write("Starting Play Mode with the existing PrototypeScene.");
                EditorApplication.isPlaying = true;
                return;
            }
            if (!EditorApplication.isPlaying) return;
            if (phase == 0)
            {
                video = Find<VideoPlayer>();
                story = Find<StoryIntroUI>();
                selection = Find<CharacterSelectionUI>();
                player = Find<PlayerController>();
                if (video == null || story == null || selection == null || player == null)
                    throw new InvalidOperationException("Required scene references missing.");
                originalRunInBackground = Application.runInBackground;
                Application.runInBackground = true;
                video.frameReady += OnFrame;
                video.loopPointReached += OnCompleted;
                Application.logMessageReceived += OnLog;
                phase = 1;
                StartCase();
                return;
            }
            if (phase != 1) return;
            if (video.clip != null && video.frame >= 0)
            {
                maximumFrame = Math.Max(maximumFrame, video.frame);
                if (EditorApplication.timeSinceStartup >= nextSample)
                {
                    SamplePixels();
                    Write("sample case=" + testIndex + " clip=" + video.clip.name + " frame=" + video.frame + " time=" + video.time.ToString("F2") + " playing=" + video.isPlaying + " skipOnDrop=" + video.skipOnDrop);
                    nextSample = EditorApplication.timeSinceStartup + 1;
                }
                if (player.enabled) throw new InvalidOperationException("Player movement unlocked during intro.");
                if (story.IsOpen) throw new InvalidOperationException("Story visible before video finishes.");
                if (testIndex == 2 && maximumFrame >= 20)
                {
                    SamplePixels();
                    Click(Find<IntroVideoUI>(), "skipButton");
                    Click(Find<IntroVideoUI>(), "skipButton");
                }
            }
            if (story.IsOpen)
            {
                bool passed = maximumFrame >= 20 && changedPixels && !player.enabled &&
                    (testIndex == 2 ? completions == 0 : completions == 1);
                Write((passed ? "PASS" : "FAIL") + " case=" + testIndex + " maxFrame=" + maximumFrame + " frameEvents=" + frameEvents + " changedPixels=" + changedPixels + " completed=" + completions + " overflowWarnings=" + overflowWarnings + " elapsed=" + (EditorApplication.timeSinceStartup - testStarted).ToString("F2"));
                if (!passed) { Finish(); return; }
                Click(story, "startButton");
                if (!player.enabled || story.IsOpen) throw new InvalidOperationException("Story start did not unlock gameplay.");
                testIndex++;
                if (testIndex == 3) { Write("ALL CASES PASSED: male, female, skip; gameplay unlocked."); Finish(); return; }
                StartCase();
            }
            else if (EditorApplication.timeSinceStartup - testStarted > 25)
            {
                Write("FAIL timeout case=" + testIndex + " maxFrame=" + maximumFrame);
                Finish();
            }
        }
        catch (Exception exception)
        {
            Write("FAIL " + exception);
            Finish();
        }
    }

    private static void StartCase()
    {
        maximumFrame = -1;
        frameEvents = completions = overflowWarnings = 0;
        hasPixels = changedPixels = false;
        testStarted = nextSample = EditorApplication.timeSinceStartup;
        if (testIndex == 0) Click(Find<MainMenuUI>(), "startButton");
        else selection.ShowSelection();
        Click(selection, testIndex == 1 ? "femaleButton" : "maleButton");
        string expected = testIndex == 1 ? "kiz_intro" : "erkek_intro";
        if (video.clip == null || video.clip.name != expected)
            throw new InvalidOperationException("Character-to-clip mismatch.");
        Write("START case=" + testIndex + " clip=" + video.clip.name + " frameCount=" + video.clip.frameCount + " audio=" + video.audioOutputMode);
    }

    private static void OnFrame(VideoPlayer source, long frame) { frameEvents++; maximumFrame = Math.Max(maximumFrame, frame); }
    private static void OnCompleted(VideoPlayer source) { completions++; }
    private static void OnLog(string message, string stack, LogType type)
    {
        if (message.Contains("AudioSampleProvider")) overflowWarnings++;
        if (type == LogType.Error && message.Contains("Intro")) Write("VIDEO ERROR " + message);
    }

    private static void SamplePixels()
    {
        if (video.targetTexture == null) return;
        var previous = RenderTexture.active;
        var sample = RenderTexture.GetTemporary(32, 18, 0);
        var pixels = new Texture2D(32, 18, TextureFormat.RGB24, false);
        try
        {
            Graphics.Blit(video.targetTexture, sample);
            RenderTexture.active = sample;
            pixels.ReadPixels(new Rect(0, 0, 32, 18), 0, 0);
            pixels.Apply();
            uint hash = 2166136261;
            foreach (var value in pixels.GetRawTextureData<byte>()) hash = unchecked((hash ^ value) * 16777619);
            if (!hasPixels) { firstPixels = hash; hasPixels = true; }
            else if (hash != firstPixels) changedPixels = true;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(sample);
            UnityEngine.Object.DestroyImmediate(pixels);
        }
    }

    private static void Finish()
    {
        SessionState.SetInt(Key, 2);
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;
        if (video != null) { video.frameReady -= OnFrame; video.loopPointReached -= OnCompleted; }
        Application.runInBackground = originalRunInBackground;
        phase = 2;
        EditorApplication.isPlaying = false;
    }
}
