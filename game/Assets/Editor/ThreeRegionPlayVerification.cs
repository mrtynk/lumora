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
public static class ThreeRegionPlayVerification
{
    private const string Key = "Lumora.ThreeRegionTest";
    private const string Report = "Assets/Editor/ThreeRegionPlayVerification.results.txt";
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

    [MenuItem("Tools/Lumora/Tests/Verify Three Region Flow")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!RegionSceneLoader.CanLoad("KristalMagara")) throw new Exception("Önce Sisli Orman sahnesini oluşturun.");
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
            timeout = readyAt + 240;
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
                    File.WriteAllText(Report, "Three-region integration / Unity " + Application.unityVersion + "\n");
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
                    if (SceneManager.GetActiveScene().name == "KristalMagara")
                    { phase = 5; readyAt = EditorApplication.timeSinceStartup + 1; break; }
                    if (player != null && player.gameObject.scene.name == "SisliOrman") Walk();
                    break;
                case 5:
                    progress = Find<RegionProgressController>();
                    KristalMagaraSceneBuilder.Validate(SceneManager.GetActiveScene());
                    Check(progress.HasStarted && progress.ActiveRegionIndex == 2 && progress.CollectedSeedCount == 2, "Cave progress not 2/4.");
                    Check(SelectedCharacterState.SelectedCharacter == "male", "Character lost.");
                    Check(Find<MainMenuUI>() == null && Find<StoryIntroUI>() == null && Find<IntroVideoUI>() == null, "Opening UI repeated.");
                    Check(!Find<KristalMagaraLevelController>().LightPathCompleted && !Find<LightSeedCollectible>().gameObject.activeSelf, "Seed accessible before light path.");
                    progress.TryUsePortal(2, "KaranlikTepe");
                    Check(SceneManager.GetActiveScene().name == "KristalMagara", "Locked exit allowed transition.");
                    Write("PASS actual Forest -> Cave scene transition, seed 2/4, character, unique player/camera/listener/EventSystem/sender.");
                    PrepareWalk(KristalMagaraSceneBuilder.Route.Skip(1).Take(7).ToArray());
                    phase = 6; break;
                case 6:
                    if (!Walk()) break;
                    player.enabled = true;
                    var first = UnityEngine.Object.FindObjectsByType<CrystalLightNode>(FindObjectsSortMode.None).Single(n => n.name == "Crystal_A");
                    var cave = Find<KristalMagaraLevelController>();
                    Check(cave.TryRotate(first), "First crystal did not rotate.");
                    Check(!cave.TryRotate(first), "Crystal cooldown failed.");
                    Check(!cave.LightPathCompleted, "One crystal opened door.");
                    PrepareWalk(new[] { new Vector2(8,20) }); phase = 7;
                    readyAt = EditorApplication.timeSinceStartup + .6; break;
                case 7:
                    if (!Walk()) break;
                    player.enabled = true;
                    var second = UnityEngine.Object.FindObjectsByType<CrystalLightNode>(FindObjectsSortMode.None).Single(n => n.name == "Crystal_B");
                    var controller = Find<KristalMagaraLevelController>();
                    bool turned = controller.TryRotate(second);
                    Check(turned && controller.LightPathCompleted, "Light path did not complete: turned=" + turned + " player=" + player.transform.position + " node=" + second.transform.position + " aligned=" + second.IsAligned);
                    Check(!controller.TryRotate(second), "Solved crystal accepted another action.");
                    Write("PASS cave bends, slope, stepping stones; two near-player rotations, cooldown, gate and seed activation.");
                    PrepareWalk(KristalMagaraSceneBuilder.Route.Skip(9).ToArray());
                    phase = 8; break;
                case 8:
                    if (!Walk()) break;
                    player.enabled = true;
                    Check(progress.CollectedSeedCount == 3 && progress.IsSeedCollected(2), "Third seed not collected.");
                    Check(((Text)new SerializedObject(progress).FindProperty("seedCountText").objectReferenceValue).text == "Işık Tohumu: 3 / 4", "HUD not 3/4.");
                    Check(!progress.TryCollectLightSeed(2), "Third seed duplicated.");
                    Check(!((Collider)new SerializedObject(Find<PortalTrigger>()).FindProperty("blockingCollider").objectReferenceValue).enabled, "Exit still blocked.");
                    progress.TryUsePortal(2, "KaranlikTepe");
                    Check(((Text)new SerializedObject(progress).FindProperty("notificationText").objectReferenceValue).text == "Karanlık Tepe yakında.", "Placeholder missing.");
                    Write("PASS seed 3/4, reward once, Dark Hill exit unlocked and placeholder.");
                    // Optional NPC and puzzle completion must not alter the seed count.
                    Find<NpcDialogueUI>().OpenDialogue(); Click(Find<NpcDialogueUI>(), "helpButton");
                    Find<PuzzlePopupUI>().OpenPuzzle("hidden_object");
                    Click(Find<HiddenObjectPuzzleUI>(), "hintButton");
                    Click(Find<HiddenObjectPuzzleUI>(), "lightSeedButton");
                    phase = 9; readyAt = EditorApplication.timeSinceStartup + 1; break;
                case 9:
                    Find<PuzzlePopupUI>().OpenPuzzle("pattern_puzzle");
                    Click(Find<PatternPuzzleUI>(), "blueButton");
                    phase = 10; readyAt = EditorApplication.timeSinceStartup + 2; break;
                case 10:
                    Check(progress.CollectedSeedCount == 3, "Optional content changed progression.");
                    RegionSceneLoader.Load("KristalMagara");
                    phase = 11; readyAt = EditorApplication.timeSinceStartup + 3; break;
                case 11:
                    progress = Find<RegionProgressController>();
                    Check(progress.CollectedSeedCount == 3 && Find<KristalMagaraLevelController>().LightPathCompleted, "Reload lost state.");
                    Check(!Find<LightSeedCollectible>().gameObject.activeSelf, "Reload restored collected seed.");
                    foreach (string region in new[] { "isikli_vadi", "sisli_orman", "kristal_magara" })
                        foreach (string type in new[] { "area_explored", "reward_collected" })
                            Check(requests.Count(x => x.Contains("\"region\":\"" + region + "\"") && x.Contains("\"eventType\":\"" + type + "\"")) == 1, "Wrong event count: " + region + "/" + type);
                    Check(requests.Count(x => x.Contains("\"puzzleType\":\"crystal_light\"") && x.Contains("\"eventType\":\"puzzle_solved\"")) == 1, "Light solve duplicate.");
                    Check(requests.Count(x => x.Contains("\"puzzleType\":\"crystal_light\"") && x.Contains("\"eventType\":\"choice_made\"")) == 2, "Light choices wrong.");
                    foreach (string optional in new[] { "hidden_object", "pattern_puzzle" })
                        Check(requests.Any(x => x.Contains("\"region\":\"kristal_magara\"") && x.Contains("\"puzzleType\":\"" + optional + "\"") && x.Contains("\"eventType\":\"puzzle_solved\"")), "Missing optional event: " + optional);
                    Check(requests.Any(x => x.Contains("\"region\":\"kristal_magara\"") && x.Contains("\"eventType\":\"npc_helped\"")), "Missing NPC help event.");
                    Check(errors == 0, "Console errors: " + errors);
                    Write("PASS HTTP progression: area/reward once per region; crystal choice=2/solved=1; reload no duplicates; Console errors=0.");
                    Write("PASS NPC and two optional puzzles. Backend unavailable; in-memory HTTP receiver, no database test.");
                    RegionProgressController.ResetAdventure();
                    Check(progress.CollectedSeedCount == 0 && !Find<KristalMagaraLevelController>().LightPathCompleted, "Session reset failed.");
                    Write("PASS session reset. Android performance remains untested.");
                    Cleanup(); Debug.Log("Three-region Play verification PASSED."); EditorApplication.isPlaying = false; break;
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
