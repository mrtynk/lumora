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
public static class FullAdventurePlayVerification
{
    private const string Key = "Lumora.FullAdventureTest";
    private const string Report = "Assets/Editor/FullAdventurePlayVerification.results.txt";
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

    [MenuItem("Tools/Lumora/Tests/Verify Full Adventure")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!RegionSceneLoader.CanLoad("KaranlikTepe") || !RegionSceneLoader.CanLoad("LumoraFinal")) throw new Exception("Önce son bölge ve final sahnelerini oluşturun.");
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
            timeout = readyAt + 420;
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
                    File.WriteAllText(Report, "Full adventure integration / Unity " + Application.unityVersion + "\n");
                    foreach (string sceneName in new[]{"IsikliVadi","SisliOrman","KristalMagara","KaranlikTepe","LumoraFinal"})
                        Check(Application.CanStreamedLevelBeLoaded(sceneName), "Build scene missing: " + sceneName);
                    Check(SceneUtility.GetScenePathByBuildIndex(0)==LumoraLevelSceneBuilder.ScenePath,"Wrong startup scene in build.");
                    Write("PASS five enabled build scenes; IsikliVadi is startup scene.");
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
                    PrepareWalk(KristalMagaraSceneBuilder.Route.Skip(9).Take(2).ToArray());
                    phase = 8; break;
                case 8:
                    if (!Walk()) break;
                    if (progress.CollectedSeedCount < 3) break; // Allow the next physics trigger tick.
                    player.enabled = true;
                    Check(progress.CollectedSeedCount == 3 && progress.IsSeedCollected(2), "Third seed not collected.");
                    Check(((Text)new SerializedObject(progress).FindProperty("seedCountText").objectReferenceValue).text == "Işık Tohumu: 3 / 4", "HUD not 3/4.");
                    Check(!progress.TryCollectLightSeed(2), "Third seed duplicated.");
                    Check(!((Collider)new SerializedObject(Find<PortalTrigger>()).FindProperty("blockingCollider").objectReferenceValue).enabled, "Exit still blocked.");
                    Write("PASS seed 3/4 and crystal gate; first three regions retain progression.");
                    PrepareWalk(new[]{new Vector2(30,48)}); phase=9; break;
                case 9:
                    if(SceneManager.GetActiveScene().name=="KaranlikTepe")
                    { phase=10; readyAt=EditorApplication.timeSinceStartup+1; break; }
                    if(player!=null && player.gameObject.scene.name=="KristalMagara")Walk();
                    break;
                case 10:
                    progress=Find<RegionProgressController>();
                    KaranlikTepeSceneBuilder.Validate(SceneManager.GetActiveScene());
                    Check(progress.HasStarted && progress.ActiveRegionIndex==3 && progress.CollectedSeedCount==3,"Hill initial state not 3/4.");
                    Check(SelectedCharacterState.SelectedCharacter=="male","Character lost in hill.");
                    Check(Find<MainMenuUI>()==null && Find<IntroVideoUI>()==null && Find<StoryIntroUI>()==null,"Opening UI repeated in hill.");
                    Check(!Find<LightSeedCollectible>().gameObject.activeSelf,"Fourth seed accessible before beacons.");
                    progress.TryUsePortal(3,"LumoraFinal");
                    Check(SceneManager.GetActiveScene().name=="KaranlikTepe","Locked final portal allowed entry.");
                    PrepareWalk(KaranlikTepeSceneBuilder.Route.Skip(1).Take(5).ToArray());
                    phase=11; break;
                case 11:
                    if(!Walk())break;
                    player.enabled=true;
                    var firstBeacon=Beacon("1");
                    Check(Find<KaranlikTepeLevelController>().TryActivate(firstBeacon),"First beacon failed.");
                    Check(!Find<KaranlikTepeLevelController>().TryActivate(firstBeacon),"Duplicate beacon activation.");
                    Check(!Find<KaranlikTepeLevelController>().TryActivate(Beacon("3")),"Remote activation allowed.");
                    Check(!Find<KaranlikTepeLevelController>().LightPathCompleted,"Gate opened early.");
                    phase=12; readyAt=EditorApplication.timeSinceStartup+1;break;
                case 12:
                    RegionSceneLoader.Load("KaranlikTepe");
                    phase=13;readyAt=EditorApplication.timeSinceStartup+2;break;
                case 13:
                    Check(Beacon("1").IsLit && !Beacon("2").IsLit && !Beacon("3").IsLit,"Partial beacon state lost on reload.");
                    Check(!Find<LightSeedCollectible>().gameObject.activeSelf,"Partial reload opened seed.");
                    PrepareWalk(KaranlikTepeSceneBuilder.Route.Skip(1).Take(6).ToArray());
                    phase=14;break;
                case 14:
                    if(!Walk())break;
                    player.enabled=true;
                    Check(Find<KaranlikTepeLevelController>().TryActivate(Beacon("2")),"Second beacon failed.");
                    PrepareWalk(new[]{new Vector2(20,22)});phase=15;break;
                case 15:
                    if(!Walk())break;
                    player.enabled=true;
                    Check(Find<KaranlikTepeLevelController>().TryActivate(Beacon("3")),"Third beacon failed.");
                    Check(Find<KaranlikTepeLevelController>().LightPathCompleted && Find<LightSeedCollectible>().gameObject.activeSelf,"Summit not unlocked.");
                    Find<NpcDialogueUI>().OpenDialogue();Click(Find<NpcDialogueUI>(),"helpButton");
                    Find<PuzzlePopupUI>().OpenPuzzle("memory_match");
                    SolveMemory();
                    phase=16;readyAt=EditorApplication.timeSinceStartup+1;break;
                case 16:
                    Find<PuzzlePopupUI>().OpenPuzzle("pattern_puzzle");Click(Find<PatternPuzzleUI>(),"blueButton");
                    phase=17;readyAt=EditorApplication.timeSinceStartup+1;break;
                case 17:
                    Check(progress.CollectedSeedCount==3,"Optional content granted main seed.");
                    PrepareWalk(KaranlikTepeSceneBuilder.Route.Skip(8).Take(2).ToArray());phase=18;break;
                case 18:
                    if(!Walk())break;
                    if(progress.CollectedSeedCount<4)break;
                    Check(progress.CollectedSeedCount==4 && progress.IsSeedCollected(3),"Fourth seed not collected.");
                    Check(((Text)new SerializedObject(progress).FindProperty("seedCountText").objectReferenceValue).text=="Işık Tohumu: 4 / 4","HUD not 4/4.");
                    Check(!progress.TryCollectLightSeed(3),"Fourth reward duplicated.");
                    Check(!((Collider)new SerializedObject(Find<PortalTrigger>()).FindProperty("blockingCollider").objectReferenceValue).enabled,"Final portal blocked.");
                    Write("PASS hill actual ramp/rock route, 3 beacon actions, partial reload, seed 4/4, NPC and optional Memory/Pattern.");
                    phase=19;readyAt=EditorApplication.timeSinceStartup+1;break;
                case 19:
                    RegionSceneLoader.Load("KaranlikTepe");phase=20;readyAt=EditorApplication.timeSinceStartup+2;break;
                case 20:
                    Check(Find<KaranlikTepeLevelController>().LightPathCompleted && !Find<LightSeedCollectible>().gameObject.activeSelf,"Completed hill reload failed.");
                    // The first visit physically traversed the gate; replay the route to the real final trigger.
                    PrepareWalk(KaranlikTepeSceneBuilder.Route.Skip(1).ToArray());phase=21;break;
                case 21:
                    if(SceneManager.GetActiveScene().name=="LumoraFinal")
                    {phase=22;readyAt=EditorApplication.timeSinceStartup+20;break;}
                    if(player!=null && player.gameObject.scene.name=="KaranlikTepe")Walk();
                    break;
                case 22:
                    progress=Find<RegionProgressController>();
                    Check(progress.CollectedSeedCount==4 && Find<LumoraFinalController>().SequenceCompleted,"Final did not validate/animate four seeds.");
                    var finalState = new SerializedObject(Find<LumoraFinalController>());
                    var finalSeeds = finalState.FindProperty("symbolicSeeds");
                    for (int i=0;i<4;i++) Check(((GameObject)finalSeeds.GetArrayElementAtIndex(i).objectReferenceValue).activeSelf,"Final symbolic seed hidden.");
                    var crown = (Renderer)finalState.FindProperty("treeCanopy").GetArrayElementAtIndex(0).objectReferenceValue;
                    var colorBlock = new MaterialPropertyBlock(); crown.GetPropertyBlock(colorBlock);
                    Check(colorBlock.GetColor("_EmissionColor").g > .5f,"Tree did not brighten.");
                    Check(((Text)finalState.FindProperty("title").objectReferenceValue).text=="Lumora Yeniden Parlıyor!","Final title missing.");
                    Check(SelectedCharacterState.SelectedCharacter=="male","Character lost in finale.");
                    Check(Find<MainMenuUI>()==null && Find<IntroVideoUI>()==null && Find<StoryIntroUI>()==null,"Opening UI repeated in finale.");
                    foreach(Type type in new[]{typeof(PlayerController),typeof(Camera),typeof(AudioListener),typeof(UnityEngine.EventSystems.EventSystem),typeof(GameEventSender),typeof(RegionProgressController)})
                        Check(UnityEngine.Object.FindObjectsByType(type,FindObjectsInactive.Include,FindObjectsSortMode.None).Length==1,"Duplicate "+type.Name);
                    foreach(string region in new[]{"isikli_vadi","sisli_orman","kristal_magara","karanlik_tepe"})
                        foreach(string eventType in new[]{"area_explored","reward_collected"})
                            Check(EventCount(region,"region_progress",eventType)==1,"Progression event duplicate/missing: "+region+"/"+eventType);
                    Check(EventCount("kristal_magara","crystal_light","choice_made")==2 && EventCount("kristal_magara","crystal_light","puzzle_solved")==1,"Crystal event count.");
                    Check(EventCount("karanlik_tepe","light_beacon","choice_made")==3 && EventCount("karanlik_tepe","light_beacon","puzzle_solved")==1,"Beacon event count.");
                    Check(EventCount("karanlik_tepe","memory_match","puzzle_solved")==1 && EventCount("karanlik_tepe","pattern_puzzle","puzzle_solved")==1,"Optional events missing.");
                    Check(EventCount("karanlik_tepe","npc_dialogue","npc_helped")==1,"NPC event missing.");
                    Write("PASS final scene, 4 symbolic seeds, tree lighting, completed UI, retained character, unique components.");
                    Write("PASS real HTTP payloads: area/reward once in all 4 regions; crystal choices=2/solve=1; beacon choices=3/solve=1; reload no duplicates.");
                    Click(Find<LumoraFinalController>(),"returnButton");phase=23;readyAt=EditorApplication.timeSinceStartup+3;break;
                case 23:
                    Check(SceneManager.GetActiveScene().name=="IsikliVadi" && Find<MainMenuUI>()!=null,"Return to menu failed.");
                    progress=Find<RegionProgressController>();
                    Check(progress.CollectedSeedCount==0 && !progress.HasStarted,"Reset did not clear seeds/start.");
                    Check(!progress.IsMechanicCompleted("kristal_magara/crystal_light") && !progress.IsMechanicCompleted(KaranlikTepeLevelController.MechanicKey),"Mechanics not reset.");
                    Check(!progress.IsMechanicCompleted("karanlik_tepe/beacon/1"),"Individual beacon not reset.");
                    RegionSceneLoader.Load("LumoraFinal");phase=24;readyAt=EditorApplication.timeSinceStartup+2;break;
                case 24:
                    Check(!Find<LumoraFinalController>().SequenceCompleted,"Direct final falsely completed game.");
                    Check(((Text)new SerializedObject(Find<LumoraFinalController>()).FindProperty("title").objectReferenceValue).text=="Macera seni bekliyor","Direct final fallback missing.");
                    Click(Find<LumoraFinalController>(),"returnButton");phase=25;readyAt=EditorApplication.timeSinceStartup+2;break;
                case 25:
                    Click(Find<MainMenuUI>(),"startButton");
                    Check(Find<CharacterSelectionUI>().gameObject.activeSelf,"New game skipped character selection.");
                    Click(Find<CharacterSelectionUI>(),"femaleButton");phase=26;readyAt=EditorApplication.timeSinceStartup+3;break;
                case 26:
                    Click(Find<IntroVideoUI>(),"skipButton");Click(Find<StoryIntroUI>(),"startButton");
                    Check(Find<RegionProgressController>().CollectedSeedCount==0 && Find<RegionProgressController>().HasStarted,"New game did not start at 0/4.");
                    phase=27;readyAt=EditorApplication.timeSinceStartup+1;break;
                case 27:
                    Check(EventCount("isikli_vadi","region_progress","area_explored")==2,"New game did not reset exploration flag.");
                    Check(SelectedCharacterState.SelectedCharacter=="female","New character selection failed.");
                    Check(errors==0,"Console errors="+errors);
                    Write("PASS final return, session/beacon/crystal/exploration reset, fresh female selection, direct-final safe fallback. Console errors=0.");
                    Write("Local HTTP receiver used; PostgreSQL and Android device performance not verified.");
                    Cleanup();Debug.Log("Full adventure verification PASSED.");EditorApplication.isPlaying=false;break;
            }
        }
        catch (Exception error)
        {
            Write("FAIL " + error); Cleanup(); Debug.LogError(error); EditorApplication.isPlaying = false;
        }
    }

    private static LightBeacon Beacon(string id) => UnityEngine.Object.FindObjectsByType<LightBeacon>(FindObjectsSortMode.None).Single(b=>b.name=="LightBeacon_"+id);
    private static int EventCount(string region,string puzzle,string type) => requests.Count(x=>x.Contains("\"region\":\""+region+"\"") && x.Contains("\"puzzleType\":\""+puzzle+"\"") && x.Contains("\"eventType\":\""+type+"\""));
    private static void SolveMemory()
    {
        var memory=Find<MemoryMatchPuzzleUI>();
        var symbols=(string[])typeof(MemoryMatchPuzzleUI).GetField("cardSymbols",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(memory);
        var cards=new SerializedObject(memory).FindProperty("cardButtons");
        foreach(string symbol in symbols.Distinct())
            foreach(int i in Enumerable.Range(0,4).Where(i=>symbols[i]==symbol))
                ((Button)cards.GetArrayElementAtIndex(i).objectReferenceValue).onClick.Invoke();
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
