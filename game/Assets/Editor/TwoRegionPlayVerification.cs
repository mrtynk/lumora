using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Opt-in integration test. It uses the actual scene, colliders and HTTP sender,
// but receives requests in memory, never in the user's backend/database.
public static class TwoRegionPlayVerification
{
    private const string Key = "Lumora.TwoRegionTest";
    private const string Report = "Assets/Editor/TwoRegionPlayVerification.results.txt";
    private static readonly ConcurrentQueue<string> requests = new ConcurrentQueue<string>();
    private static TcpListener server;
    private static string endpoint;
    private static int phase, waypoint, ticks, errors;
    private static double readyAt, timeout;
    private static bool previousBackground;
    private static Vector2[] route;
    private static PlayerController player;
    private static RegionProgressController progress;
    private static readonly Vector2[] ValleyRoute = {
        new Vector2(-6,-26), new Vector2(2,-16), new Vector2(4,-13),
        new Vector2(4,-10), new Vector2(4,0), new Vector2(4,4),
        new Vector2(-4,11), new Vector2(-15,22), new Vector2(-5,28),
        new Vector2(8,29), new Vector2(20,37) };

    [MenuItem("Tools/Lumora/Tests/Verify Two Region Flow")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!RegionSceneLoader.CanLoad("SisliOrman")) throw new Exception("Önce Sisli Orman sahnesini oluşturun.");
        EditorSceneManager.OpenScene(LumoraLevelSceneBuilder.ScenePath);
        SessionState.SetBool(Key, true);
        EditorApplication.isPlaying = true;
    }

    [InitializeOnLoadMethod]
    private static void Register()
    {
        EditorApplication.playModeStateChanged -= StateChanged;
        EditorApplication.playModeStateChanged += StateChanged;
        if (SessionState.GetBool(Key, false))
        {
            phase = errors = 0;
            readyAt = EditorApplication.timeSinceStartup + 2;
            timeout = readyAt + 150;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }
    }
    private static void StateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode) Cleanup();
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false)) Register();
    }
    private static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
    private static void Click(UnityEngine.Object owner, string field) =>
        ((Button)new SerializedObject(owner).FindProperty(field).objectReferenceValue).onClick.Invoke();
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Write(string message) => File.AppendAllText(Report, message + Environment.NewLine);
    private static void Log(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception) errors++; }
    private static void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // sceneLoaded runs after Awake and before Start: the forest's first
        // area_explored must reach the test receiver, not localhost:5000.
        foreach (GameEventSender sender in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameEventSender>(true)))
        {
            var s = new SerializedObject(sender);
            s.FindProperty("eventEndpoint").stringValue = endpoint;
            s.ApplyModifiedPropertiesWithoutUndo();
        }
    }
    private static void PrepareWalk(Vector2[] points)
    {
        player = Find<PlayerController>(); progress = Find<RegionProgressController>();
        player.enabled = false;
        route = points; waypoint = ticks = 0;
    }
    private static bool Walk()
    {
        if (waypoint >= route.Length) return true;
        Vector3 p = player.transform.position;
        Vector2 delta = route[waypoint] - new Vector2(p.x, p.z);
        if (delta.magnitude < .4f)
        {
            waypoint++;
            if (waypoint == route.Length) return true;
            delta = route[waypoint] - new Vector2(p.x, p.z);
        }
        player.GetComponent<CharacterController>().Move(new Vector3(delta.x, 0, delta.y).normalized * .1f + Vector3.down * .15f);
        Check(++ticks < 6500 && p.y > -5, "Walk blocked/fell: " + SceneManager.GetActiveScene().name + " at " + p);
        return false;
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.timeSinceStartup < readyAt) return;
        try
        {
            Check(EditorApplication.timeSinceStartup < timeout, "Integration test timed out.");
            switch (phase)
            {
                case 0:
                    File.WriteAllText(Report, "Two-region integration / Unity " + Application.unityVersion + "\n");
                    previousBackground = Application.runInBackground; Application.runInBackground = true;
                    Application.logMessageReceived += Log;
                    while (requests.TryDequeue(out _)) { }
                    server = new TcpListener(IPAddress.Loopback, 0); server.Start();
                    endpoint = "http://127.0.0.1:" + ((IPEndPoint)server.LocalEndpoint).Port + "/api/events";
                    TcpListener receiver = server; _ = Task.Run(() => Receive(receiver));
                    SceneManager.sceneLoaded += SceneLoaded;
                    SceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
                    Check(!Find<RegionProgressController>().HasStarted, "Valley started under menu.");
                    Click(Find<MainMenuUI>(), "startButton");
                    Click(Find<CharacterSelectionUI>(), "maleButton");
                    phase = 1; readyAt = EditorApplication.timeSinceStartup + 1; break;
                case 1:
                    Click(Find<IntroVideoUI>(), "skipButton");
                    Click(Find<StoryIntroUI>(), "startButton");
                    progress = Find<RegionProgressController>();
                    Check(progress.HasStarted && progress.CollectedSeedCount == 0, "New game did not reset progress.");
                    progress.TryUsePortal(0, "SisliOrman");
                    Check(SceneManager.GetActiveScene().name == "IsikliVadi", "Locked portal allowed entry.");
                    PrepareWalk(ValleyRoute); phase = 2; break;
                case 2:
                    if (SceneManager.GetActiveScene().name == "SisliOrman")
                    {
                        phase = 3; readyAt = EditorApplication.timeSinceStartup + 1; break;
                    }
                    if (player != null && player.gameObject.scene.name == "IsikliVadi") Walk();
                    break;
                case 3:
                    progress = Find<RegionProgressController>();
                    Check(progress.HasStarted && progress.ActiveRegionIndex == 1, "Forest did not begin.");
                    Check(progress.CollectedSeedCount == 1 && progress.IsSeedCollected(0) && !progress.IsSeedCollected(1), "First seed lost during transition.");
                    Check(Find<MainMenuUI>() == null && Find<IntroVideoUI>() == null && Find<StoryIntroUI>() == null, "Intro/menu repeated in forest.");
                    Check(UnityEngine.Object.FindObjectsByType<GameEventSender>(FindObjectsSortMode.None).Length == 1, "Duplicate sender.");
                    Check(UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length == 1, "Duplicate player.");
                    Check(!progress.TryCollectLightSeed(0), "Prior seed collected twice.");
                    progress.TryUsePortal(1, "KristalMagara");
                    Write("PASS actual valley walk, seed trigger, portal scene load, seed 1/4 and single manager; no second intro.");
                    PrepareWalk(SisliOrmanSceneBuilder.Route.Skip(1).ToArray()); phase = 4; break;
                case 4:
                    if (!Walk()) break;
                    player.enabled = true;
                    Check(progress.IsSeedCollected(1) && progress.CollectedSeedCount == 2, "Second seed not collected by collider.");
                    Check(!progress.TryCollectLightSeed(1), "Second seed duplicated.");
                    var portal = new SerializedObject(Find<PortalTrigger>());
                    Check(!((Collider)portal.FindProperty("blockingCollider").objectReferenceValue).enabled, "Crystal portal remained locked.");
                    progress.TryUsePortal(1, "KristalMagara");
                    progress.BeginExploration();
                    Write("PASS forest route/bridge/seed, count 2/4, crystal portal unlocked; unavailable crystal scene handled.");
                    phase = 5; readyAt = EditorApplication.timeSinceStartup + 2; break;
                case 5:
                    foreach (string region in new[] { "isikli_vadi", "sisli_orman" })
                        foreach (string type in new[] { "area_explored", "reward_collected" })
                            Check(requests.Count(x => x.Contains("\"region\":\"" + region + "\"") && x.Contains("\"eventType\":\"" + type + "\"")) == 1, "Wrong event count: " + region + "/" + type);
                    Check(errors == 0, "Console errors: " + errors);
                    Write("PASS HTTP: area_explored/reward_collected exactly once per region; Console errors=0.");
                    // A new run must not inherit these two seeds.
                    RegionProgressController.ResetAdventure();
                    Check(progress.CollectedSeedCount == 0, "Session reset failed.");
                    Write("PASS new-game reset. Database/device performance not covered by this local receiver test.");
                    Cleanup(); Debug.Log("Two-region Play verification PASSED."); EditorApplication.isPlaying = false; break;
            }
        }
        catch (Exception error)
        {
            Write("FAIL " + error); Cleanup(); Debug.LogError(error); EditorApplication.isPlaying = false;
        }
    }

    private static async Task Receive(TcpListener listener)
    {
        try
        {
            while (true)
            {
                using (TcpClient client = await listener.AcceptTcpClientAsync())
                using (NetworkStream stream = client.GetStream())
                {
                    // Read exact HTTP bytes, so Turkish UTF-8 values and braces
                    // inside JSON strings cannot truncate an event.
                    var header = new StringBuilder(); int value;
                    while ((value = stream.ReadByte()) >= 0)
                    {
                        header.Append((char)value);
                        if (header.ToString().EndsWith("\r\n\r\n")) break;
                        if (header.Length > 16384) throw new IOException("HTTP header too large.");
                    }
                    string lengthLine = header.ToString().Split('\n').First(x => x.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase));
                    int length = int.Parse(lengthLine.Substring(15).Trim());
                    if (length < 0 || length > 65536) throw new IOException("Invalid event length.");
                    var body = new byte[length]; int read = 0;
                    while (read < length) { int n = await stream.ReadAsync(body, read, length - read); if (n == 0) throw new IOException("Truncated event."); read += n; }
                    requests.Enqueue(Encoding.UTF8.GetString(body));
                    byte[] reply = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\n{}");
                    await stream.WriteAsync(reply, 0, reply.Length);
                }
            }
        }
        catch (ObjectDisposedException) { }
        catch (SocketException) { }
        catch (IOException) { }
    }
    private static void Cleanup()
    {
        SessionState.SetBool(Key, false);
        EditorApplication.update -= Tick;
        SceneManager.sceneLoaded -= SceneLoaded;
        Application.logMessageReceived -= Log;
        server?.Stop(); server = null;
        Application.runInBackground = previousBackground;
    }
}
