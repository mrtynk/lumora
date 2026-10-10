using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RegionProgressController : MonoBehaviour
{
    private const string ChildId = "demo-child-001";
    private const string ProgressType = "region_progress";

    private static readonly string[] RegionIds =
    {
        "isikli_vadi",
        "sisli_orman",
        "kristal_magara",
        "karanlik_tepe"
    };

    private static readonly string[] RegionNames =
    {
        "Işıklı Vadi",
        "Sisli Orman",
        "Kristal Mağara",
        "Karanlık Tepe"
    };

    [Header("Oyun Bağlantıları")]
    [SerializeField] private GameEventSender eventSender;
    [SerializeField] private Transform player;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform[] regionSpawnPoints;
    [SerializeField] private LightSeedCollectible[] lightSeeds;
    [SerializeField] private PortalTrigger[] portals;
    [SerializeField] private GameObject regionOneGuide;

    [Header("Sahne Akışı")]
    [SerializeField] private bool startAutomatically = true;
    [SerializeField] private bool useSceneTransitions;
    [SerializeField, Range(0, 3)] private int sceneRegionIndex;

    [Header("İlerleme UI")]
    [SerializeField] private Text activeRegionText;
    [SerializeField] private Text seedCountText;
    [SerializeField] private Text portalStatusText;
    [SerializeField] private Text objectiveText;
    [SerializeField] private Text notificationText;

    private bool[] exploredRegions;
    private bool[] collectedSeeds;
    private int activeRegionIndex;
    private bool hasStarted;
    private bool sceneTransitionInProgress;

    // Only production scenes share this Play-session state. The technical
    // four-platform prototype continues using its own instance arrays.
    private static readonly bool[] sessionSeeds = new bool[4];
    private static readonly bool[] sessionExplored = new bool[4];
    private static readonly System.Collections.Generic.HashSet<string> sessionMechanics =
        new System.Collections.Generic.HashSet<string>();

    public bool IsMechanicCompleted(string key) => sessionMechanics.Contains(key);

    public bool TryCompleteMechanic(string key)
    {
        return hasStarted && useSceneTransitions && !string.IsNullOrWhiteSpace(key) && sessionMechanics.Add(key);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void ResetAdventure()
    {
        System.Array.Clear(sessionSeeds, 0, sessionSeeds.Length);
        System.Array.Clear(sessionExplored, 0, sessionExplored.Length);
        sessionMechanics.Clear();
    }

    public int CollectedSeedCount
    {
        get
        {
            EnsureInitialized();
            int count = 0;
            foreach (bool collected in collectedSeeds) if (collected) count++;
            return count;
        }
    }

    public bool HasStarted => hasStarted;
    public int ActiveRegionIndex
    {
        get
        {
            EnsureInitialized();
            return activeRegionIndex;
        }
    }

    private void Awake()
    {
        EnsureInitialized();
    }

    private void Start()
    {
        EnsureInitialized();
        RefreshPortals();
        UpdateProgressUi();
        if (startAutomatically)
        {
            BeginExploration();
        }
    }

    private void EnsureInitialized()
    {
        if (collectedSeeds != null)
        {
            return;
        }
        exploredRegions = useSceneTransitions ? sessionExplored : new bool[RegionNames.Length];
        collectedSeeds = useSceneTransitions ? sessionSeeds : new bool[RegionNames.Length];
        activeRegionIndex = useSceneTransitions
            ? Mathf.Clamp(sceneRegionIndex, 0, RegionNames.Length - 1)
            : 0;
    }

    // Production levels call this after the story panel, while the prototype
    // keeps its existing automatic Start behaviour.
    public void BeginExploration()
    {
        EnsureInitialized();
        if (hasStarted)
        {
            return;
        }
        if (!HasRequiredReferences())
        {
            Debug.LogWarning("RegionProgressController bağlantıları eksik; bölge henüz başlatılmadı.", this);
            return;
        }

        if (useSceneTransitions && activeRegionIndex > 0 && !collectedSeeds[activeRegionIndex - 1])
        {
            Notify("Önce " + RegionNames[activeRegionIndex - 1] + " bölgesindeki ışık tohumunu bul.");
            UpdateProgressUi();
            return;
        }

        hasStarted = true;
        for (int i = 0; i < lightSeeds.Length; i++)
            lightSeeds[i].gameObject.SetActive(!collectedSeeds[useSceneTransitions ? sceneRegionIndex : i]);
        RefreshPortals();
        Notify("Bu bölgedeki ışık tohumunu bul ve portalı aç.");
        MarkRegionExplored(activeRegionIndex);
        UpdateProgressUi();
    }

    public bool IsSeedCollected(int regionIndex)
    {
        EnsureInitialized();
        return IsValidRegion(regionIndex) && collectedSeeds[regionIndex];
    }

    public bool TryCollectLightSeed(int regionIndex)
    {
        EnsureInitialized();
        if (!hasStarted || !IsValidRegion(regionIndex) ||
            regionIndex != activeRegionIndex ||
            collectedSeeds[regionIndex])
        {
            return false;
        }

        collectedSeeds[regionIndex] = true;
        RefreshPortals();

        SendProgressEvent(
            "reward_collected", regionIndex,
            "Işık tohumu toplandı: " + RegionNames[regionIndex]);

        Notify(regionIndex == RegionNames.Length - 1
            ? "Tüm ışık tohumları bulundu! Lumora'nın ışığı geri dönüyor."
            : "Portal açıldı. Sonraki bölgeye geçebilirsin.");
        UpdateProgressUi();
        return true;
    }

    private void SendProgressEvent(string eventType, int regionIndex, string value)
    {
        if (eventSender == null || !eventSender.isActiveAndEnabled)
        {
            Debug.LogWarning("Bölge eventi gönderilemedi: GameEventSender aktif değil.", this);
            return;
        }
        eventSender.SendEvent(
            ChildId,
            eventType,
            RegionIds[regionIndex],
            ProgressType,
            value
        );
    }

    public void TryUsePortal(int regionIndex, string destinationSceneName = null)
    {
        EnsureInitialized();
        if (!hasStarted || sceneTransitionInProgress ||
            !IsValidRegion(regionIndex) || regionIndex != activeRegionIndex)
        {
            return;
        }

        if (!collectedSeeds[regionIndex])
        {
            Notify("Portal kapalı. Önce bu bölgedeki ışık tohumunu bul.");
            UpdateProgressUi();
            return;
        }

        if (regionIndex == RegionNames.Length - 1)
        {
            if (useSceneTransitions && CollectedSeedCount == RegionNames.Length)
            {
                OpenNextScene(RegionNames.Length, destinationSceneName);
                return;
            }
            Notify("Lumora yeniden aydınlandı! Dört ışık tohumunu da buldun.");
            UpdateProgressUi();
            return;
        }

        int nextRegionIndex = regionIndex + 1;
        if (useSceneTransitions)
        {
            OpenNextScene(nextRegionIndex, destinationSceneName);
            return;
        }
        if (!CanMoveToRegion(nextRegionIndex))
        {
            Notify(RegionNames[nextRegionIndex] + " yakında.");
            return;
        }
        MovePlayerToRegion(nextRegionIndex);
        activeRegionIndex = nextRegionIndex;
        Notify(RegionNames[nextRegionIndex] + " bölgesine ulaştın.");
        MarkRegionExplored(nextRegionIndex);
        UpdateProgressUi();
    }

    private void OpenNextScene(int nextRegionIndex, string destinationSceneName)
    {
        string destinationLabel = nextRegionIndex < RegionNames.Length ? RegionNames[nextRegionIndex] : "Lumora Finali";
        // An unfinished region must not be marked explored or loaded as a
        // placeholder. A future scene can be wired on the same PortalTrigger.
        if (string.IsNullOrWhiteSpace(destinationSceneName) ||
            !RegionSceneLoader.CanLoad(destinationSceneName) ||
            SceneManager.GetActiveScene().name == destinationSceneName)
        {
            Notify(destinationLabel + " yakında.");
            return;
        }

        sceneTransitionInProgress = true;
        Notify(destinationLabel + " yükleniyor...");
        try
        {
            // Single unloads the current scene and its managers together.
            // The destination's controller owns its own area_explored event.
            AsyncOperation load = RegionSceneLoader.Load(destinationSceneName);
            if (load == null)
            {
                sceneTransitionInProgress = false;
                Notify(destinationLabel + " yakında.");
            }
        }
        catch (System.Exception exception)
        {
            sceneTransitionInProgress = false;
            Notify(destinationLabel + " yakında.");
            Debug.LogWarning("Portal sahnesi açılamadı: " + exception.Message, this);
        }
    }

    private bool CanMoveToRegion(int regionIndex)
    {
        return player != null && regionSpawnPoints != null &&
               activeRegionIndex < regionSpawnPoints.Length &&
               regionIndex < regionSpawnPoints.Length &&
               regionSpawnPoints[activeRegionIndex] != null &&
               regionSpawnPoints[regionIndex] != null;
    }

    private void MovePlayerToRegion(int regionIndex)
    {
        Vector3 destination = regionSpawnPoints[regionIndex].position;
        Vector3 regionOffset =
            regionSpawnPoints[regionIndex].position -
            regionSpawnPoints[activeRegionIndex].position;
        CharacterController characterController =
            player.GetComponent<CharacterController>();

        bool controllerWasEnabled = characterController != null && characterController.enabled;
        if (controllerWasEnabled)
        {
            characterController.enabled = false;
        }

        player.position = destination;

        if (controllerWasEnabled)
        {
            characterController.enabled = true;
        }

        if (mainCamera != null)
        {
            mainCamera.transform.position += regionOffset;
        }

        if (regionIndex > 0 && regionOneGuide != null)
        {
            regionOneGuide.SetActive(false);
        }
    }

    private void MarkRegionExplored(int regionIndex)
    {
        if (exploredRegions[regionIndex])
        {
            return;
        }

        exploredRegions[regionIndex] = true;
        SendProgressEvent("area_explored", regionIndex,
            "Bölge keşfedildi: " + RegionNames[regionIndex]);
    }

    private void UpdateProgressUi()
    {
        SetText(activeRegionText, "Aktif Bölge: " + RegionNames[activeRegionIndex]);
        SetText(seedCountText, "Işık Tohumu: " + CollectedSeedCount + " / " + RegionNames.Length);

        if (!collectedSeeds[activeRegionIndex])
        {
            SetText(portalStatusText, "Portal: Kilitli");
            SetText(objectiveText, useSceneTransitions && activeRegionIndex == RegionNames.Length - 1
                ? "Hedef: Son Işık Tohumu'nu bul." : "Hedef: Bu bölgedeki ışık tohumunu bul.");
            return;
        }

        if (activeRegionIndex == RegionNames.Length - 1)
        {
            SetText(portalStatusText, "Portal: Final Hedefi Açık");
            SetText(objectiveText, useSceneTransitions ? "Hedef: Işık Ağacı'na geri dön." : "Hedef: Lumora'nın ışığını geri getir.");
            return;
        }

        SetText(portalStatusText, "Portal: Açık");
        SetText(objectiveText, "Hedef: Açık portaldan sonraki bölgeye geç.");
    }

    private void RefreshPortals()
    {
        if (portals == null)
        {
            return;
        }
        for (int i = 0; i < portals.Length; i++)
        {
            int regionIndex = useSceneTransitions ? sceneRegionIndex : i;
            if (portals[i] != null && IsValidRegion(regionIndex))
            {
                portals[i].SetUnlocked(collectedSeeds[regionIndex]);
            }
        }
    }

    private void Notify(string message)
    {
        SetText(notificationText, message);
    }

    private static void SetText(Text label, string message)
    {
        if (label != null)
        {
            label.text = message;
        }
    }

    private bool HasRequiredReferences()
    {
        int regionCount = useSceneTransitions ? 1 : RegionNames.Length;
        if (eventSender == null || player == null ||
            regionSpawnPoints == null || regionSpawnPoints.Length != regionCount ||
            lightSeeds == null || lightSeeds.Length != regionCount ||
            portals == null || portals.Length != regionCount)
        {
            return false;
        }
        for (int i = 0; i < regionCount; i++)
        {
            if (regionSpawnPoints[i] == null || lightSeeds[i] == null || portals[i] == null)
            {
                return false;
            }
        }
        return true;
    }

    private static bool IsValidRegion(int regionIndex)
    {
        return regionIndex >= 0 && regionIndex < RegionNames.Length;
    }
}
