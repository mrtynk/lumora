using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

// In-memory HTTP receiver exercises the real GameEventSender without polluting the database.
public static class IsikliVadiPlayVerification
{
    private const string Key = "Lumora.Valley.PlayVerification";
    private const string Report = "Assets/Editor/IsikliVadiPlayVerification.results.txt";
    private static TcpListener listener;
    private static readonly ConcurrentQueue<string> requests = new ConcurrentQueue<string>();
    private static int phase, errors;
    private static double deadline;
    private static long highestFrame;
    private static bool oldBackground;
    private static RegionProgressController progress;
    private static PlayerController player;
    private static VideoPlayer video;
    private static int waypoint, walkTicks;
    private static readonly Vector2[] WalkRoute = {
        new Vector2(-6, -26), new Vector2(2, -16), new Vector2(4, -13),
        new Vector2(4, -10), new Vector2(4, 0), new Vector2(4, 4),
        new Vector2(-4, 11), new Vector2(-15, 22) };

    [MenuItem("Tools/Lumora/Tests/Verify Işıklı Vadi Play Flow")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SceneManager.GetActiveScene().path != LumoraLevelSceneBuilder.ScenePath) return;
        IsikliVadiSceneValidation.ValidateScene(SceneManager.GetActiveScene());
        SessionState.SetBool(Key, true);
        EditorApplication.isPlaying = true;
    }

    [InitializeOnLoadMethod]
    private static void Resume()
    {
        EditorApplication.playModeStateChanged -= StateChanged;
        EditorApplication.playModeStateChanged += StateChanged;
        if (SessionState.GetBool(Key, false))
        {
            phase = errors = 0;
            deadline = EditorApplication.timeSinceStartup + 2;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }
    }
    private static void StateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode) Cleanup();
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false)) Resume();
    }
    private static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
    private static UnityEngine.Object Ref(UnityEngine.Object owner, string field) => new SerializedObject(owner).FindProperty(field).objectReferenceValue;
    private static void Click(UnityEngine.Object owner, string field) => ((Button)Ref(owner, field)).onClick.Invoke();
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Write(string line) => File.AppendAllText(Report, line + Environment.NewLine);
    private static void OnLog(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception) errors++; }
    private static void Frame(VideoPlayer source, long frame) => highestFrame = Math.Max(highestFrame, frame);
    private static void Next(int step, double delay = 1) { phase = step; deadline = EditorApplication.timeSinceStartup + delay; }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.timeSinceStartup < deadline) return;
        try
        {
            switch (phase)
            {
                case 0:
                    File.WriteAllText(Report, "Işıklı Vadi Play verification / Unity " + Application.unityVersion + "\n");
                    oldBackground = Application.runInBackground;
                    Application.runInBackground = true;
                    Application.logMessageReceived += OnLog;
                    listener = new TcpListener(IPAddress.Loopback, 0);
                    listener.Start();
                    while (requests.TryDequeue(out _)) { }
                    TcpListener server = listener;
                    _ = Task.Run(() => Receive(server));
                    var sender = new SerializedObject(Find<GameEventSender>());
                    sender.FindProperty("eventEndpoint").stringValue = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port + "/api/events";
                    sender.ApplyModifiedPropertiesWithoutUndo();
                    progress = Find<RegionProgressController>(); player = Find<PlayerController>(); video = Find<VideoPlayer>();
                    Check(!progress.HasStarted && !player.enabled, "Menu must defer region/movement.");
                    Check(!progress.TryCollectLightSeed(0), "Seed accepted before story.");
                    Click(Find<MainMenuUI>(), "startButton");
                    video.sendFrameReadyEvents = true; video.frameReady += Frame;
                    highestFrame = 0;
                    Click(Find<CharacterSelectionUI>(), "maleButton");
                    Check(video.clip != null && video.clip.name == "erkek_intro", "Male clip mapping incorrect.");
                    Next(1, 15); break;
                case 1:
                    Check(highestFrame > 30, "Male video frames did not advance.");
                    Check(Find<StoryIntroUI>().IsOpen && !progress.HasStarted, "Story timing incorrect.");
                    Write("PASS male video frames=" + highestFrame + "; no early exploration.");
                    Click(Find<StoryIntroUI>(), "startButton");
                    Check(progress.HasStarted && player.enabled, "Adventure failed to unlock.");
                    progress.BeginExploration();
                    progress.TryUsePortal(0, "SisliOrman");
                    Check(((Collider)Ref(Find<PortalTrigger>(), "blockingCollider")).enabled, "Portal unlocked before seed.");
                    player.enabled = false;
                    waypoint = walkTicks = 0;
                    Next(10, 0); break;
                case 10:
                    Vector3 p = player.transform.position;
                    Vector2 delta = WalkRoute[waypoint] - new Vector2(p.x, p.z);
                    if (delta.magnitude < 0.4f)
                    {
                        waypoint++;
                        if (waypoint == WalkRoute.Length) { player.enabled = true; Next(11); break; }
                    }
                    Vector3 movement = new Vector3(delta.x, 0, delta.y).normalized * 0.1f;
                    player.GetComponent<CharacterController>().Move(movement + Vector3.down * 0.15f);
                    Check(++walkTicks < 6000 && p.y > -5f, "Physical path blocked or fell through terrain.");
                    break;
                case 11:
                    Check(progress.IsSeedCollected(0), "Walking to grove did not trigger seed collection.");
                    Write("PASS CharacterController physically traversed meadow, ramps, bridge and grove; ticks=" + walkTicks);
                    Check(!progress.TryCollectLightSeed(0), "Duplicate seed accepted.");
                    Check(!((Collider)Ref(Find<PortalTrigger>(), "blockingCollider")).enabled, "Portal remained locked.");
                    progress.TryUsePortal(0, "SisliOrman");
                    Check(SceneManager.GetActiveScene().name == "IsikliVadi", "Missing scene fallback failed.");
                    Write("PASS story, single progression owner, optional puzzles, portal lock/unlock/fallback.");
                    Find<PuzzlePopupUI>().OpenPuzzle("hidden_object");
                    Click(Find<HiddenObjectPuzzleUI>(), "shinyStoneButton");
                    Click(Find<HiddenObjectPuzzleUI>(), "hintButton");
                    Click(Find<HiddenObjectPuzzleUI>(), "lightSeedButton");
                    Next(2, 2); break;
                case 2:
                    Check(!Find<PuzzlePopupUI>().IsOpen, "Hidden Object did not close.");
                    Find<PuzzlePopupUI>().OpenPuzzle("memory_match");
                    Check(Find<PuzzlePopupUI>().IsPuzzleActive("memory_match"), "Memory Match failed to open.");
                    Click(Find<PuzzlePopupUI>(), "closeMemoryMatchButton");
                    Next(3, 2); break;
                case 3:
                    Find<PuzzlePopupUI>().OpenPuzzle("pattern_puzzle");
                    Click(Find<PatternPuzzleUI>(), "redButton");
                    Click(Find<PatternPuzzleUI>(), "blueButton");
                    Next(4, 2); break;
                case 4:
                    Check(!Find<PuzzlePopupUI>().IsOpen, "Pattern puzzle did not close.");
                    Find<NpcDialogueUI>().OpenDialogue();
                    Click(Find<NpcDialogueUI>(), "helpButton");
                    Next(5, 2); break;
                case 5:
                    Check(requests.Count(x => x.Contains("\"eventType\":\"area_explored\"")) == 1, "Area event count incorrect.");
                    Check(requests.Count(x => x.Contains("\"eventType\":\"reward_collected\"")) == 1, "Reward event count incorrect.");
                    Check(requests.Any(x => x.Contains("\"eventType\":\"npc_helped\"")), "NPC event missing.");
                    Check(requests.Any(x => x.Contains("\"eventType\":\"hint_requested\"")), "Hint event missing.");
                    Check(requests.Any(x => x.Contains("\"eventType\":\"puzzle_abandoned\"")), "Abandon event missing.");
                    Write("PASS real HTTP payloads received locally: " + requests.Count + "; area=1 reward=1; NPC/puzzle events present.");
                    highestFrame = 0;
                    Find<CharacterSelectionUI>().ShowSelection();
                    Click(Find<CharacterSelectionUI>(), "femaleButton");
                    Next(6, 4); break;
                case 6:
                    Check(video.clip.name == "kiz_intro" && highestFrame > 10, "Female video did not advance.");
                    Click(Find<IntroVideoUI>(), "skipButton");
                    Check(Find<StoryIntroUI>().IsOpen, "Skip did not open story.");
                    Check(errors == 0, "Console errors: " + errors);
                    Write("PASS female frames=" + highestFrame + ", skip, Console errors=0.");
                    Write("PASS integration. Physical traversal and device performance require manual testing.");
                    Debug.Log("Işıklı Vadi Play flow test PASSED. Test eventleri yalnızca yerel bellek alıcısına gönderildi.");
                    Cleanup(); EditorApplication.isPlaying = false; break;
            }
        }
        catch (Exception exception)
        {
            Write("FAIL " + exception);
            Cleanup(); Debug.LogError(exception); EditorApplication.isPlaying = false;
        }
    }

    private static async Task Receive(TcpListener server)
    {
        try
        {
            while (true)
            {
                using (TcpClient client = await server.AcceptTcpClientAsync())
                using (NetworkStream stream = client.GetStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true))
                {
                    int length = 0; string line;
                    while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
                        if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase)) length = int.Parse(line.Substring(15).Trim());
                    // JSON uses UTF-8 Turkish strings: read until the complete object, not byte count as char count.
                    var body = new StringBuilder();
                    while (length > 0)
                    {
                        int c = reader.Read(); if (c < 0) break;
                        body.Append((char)c);
                        if (body[body.Length - 1] == '}') break;
                    }
                    requests.Enqueue(body.ToString());
                    byte[] response = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: 2\r\nConnection: close\r\n\r\n{}");
                    await stream.WriteAsync(response, 0, response.Length);
                }
            }
        }
        catch (ObjectDisposedException) { }
        catch (SocketException) { }
    }
    private static void Cleanup()
    {
        SessionState.SetBool(Key, false);
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;
        if (video != null) video.frameReady -= Frame;
        listener?.Stop(); listener = null;
        Application.runInBackground = oldBackground;
    }
}
