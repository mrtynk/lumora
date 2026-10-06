using UnityEngine;
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

    [Header("İlerleme UI")]
    [SerializeField] private Text activeRegionText;
    [SerializeField] private Text seedCountText;
    [SerializeField] private Text portalStatusText;
    [SerializeField] private Text objectiveText;
    [SerializeField] private Text notificationText;

    private bool[] exploredRegions;
    private bool[] collectedSeeds;
    private int activeRegionIndex;
    private int collectedSeedCount;

    private void Start()
    {
        if (!HasRequiredReferences())
        {
            Debug.LogError("RegionProgressController bağlantıları eksik.");
            enabled = false;
            return;
        }

        exploredRegions = new bool[RegionNames.Length];
        collectedSeeds = new bool[RegionNames.Length];
        activeRegionIndex = 0;
        collectedSeedCount = 0;

        for (int i = 0; i < portals.Length; i++)
        {
            portals[i].SetUnlocked(false);
        }

        notificationText.text =
            "Bu bölgedeki ışık tohumunu bul ve portalı aç.";
        MarkRegionExplored(activeRegionIndex);
        UpdateProgressUi();
    }

    public bool TryCollectLightSeed(int regionIndex)
    {
        if (!IsValidRegion(regionIndex) ||
            regionIndex != activeRegionIndex ||
            collectedSeeds[regionIndex])
        {
            return false;
        }

        collectedSeeds[regionIndex] = true;
        collectedSeedCount++;
        portals[regionIndex].SetUnlocked(true);

        eventSender.SendEvent(
            ChildId,
            "reward_collected",
            RegionIds[regionIndex],
            ProgressType,
            "Işık tohumu toplandı: " + RegionNames[regionIndex]
        );

        notificationText.text = regionIndex == RegionNames.Length - 1
            ? "Tüm ışık tohumları bulundu! Lumora'nın ışığı geri dönüyor."
            : "Portal açıldı. Sonraki bölgeye geçebilirsin.";
        UpdateProgressUi();
        return true;
    }

    public void TryUsePortal(int regionIndex)
    {
        if (!IsValidRegion(regionIndex) || regionIndex != activeRegionIndex)
        {
            return;
        }

        if (!collectedSeeds[regionIndex])
        {
            notificationText.text =
                "Portal kapalı. Önce bu bölgedeki ışık tohumunu bul.";
            UpdateProgressUi();
            return;
        }

        if (regionIndex == RegionNames.Length - 1)
        {
            notificationText.text =
                "Lumora yeniden aydınlandı! Dört ışık tohumunu da buldun.";
            UpdateProgressUi();
            return;
        }

        int nextRegionIndex = regionIndex + 1;
        MovePlayerToRegion(nextRegionIndex);
        activeRegionIndex = nextRegionIndex;
        notificationText.text =
            RegionNames[nextRegionIndex] + " bölgesine ulaştın.";
        MarkRegionExplored(nextRegionIndex);
        UpdateProgressUi();
    }

    private void MovePlayerToRegion(int regionIndex)
    {
        Vector3 destination = regionSpawnPoints[regionIndex].position;
        Vector3 regionOffset =
            regionSpawnPoints[regionIndex].position -
            regionSpawnPoints[activeRegionIndex].position;
        CharacterController characterController =
            player.GetComponent<CharacterController>();

        if (characterController != null)
        {
            characterController.enabled = false;
        }

        player.position = destination;

        if (characterController != null)
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
        eventSender.SendEvent(
            ChildId,
            "area_explored",
            RegionIds[regionIndex],
            ProgressType,
            "Bölge keşfedildi: " + RegionNames[regionIndex]
        );
    }

    private void UpdateProgressUi()
    {
        activeRegionText.text =
            "Aktif Bölge: " + RegionNames[activeRegionIndex];
        seedCountText.text =
            "Işık Tohumu: " + collectedSeedCount + " / " + RegionNames.Length;

        if (!collectedSeeds[activeRegionIndex])
        {
            portalStatusText.text = "Portal: Kilitli";
            objectiveText.text =
                "Hedef: Bu bölgedeki ışık tohumunu bul.";
            return;
        }

        if (activeRegionIndex == RegionNames.Length - 1)
        {
            portalStatusText.text = "Portal: Final Hedefi Açık";
            objectiveText.text =
                "Hedef: Lumora'nın ışığını geri getir.";
            return;
        }

        portalStatusText.text = "Portal: Açık";
        objectiveText.text =
            "Hedef: Açık portaldan sonraki bölgeye geç.";
    }

    private bool HasRequiredReferences()
    {
        int regionCount = RegionNames.Length;
        return eventSender != null &&
               player != null &&
               mainCamera != null &&
               regionSpawnPoints != null &&
               regionSpawnPoints.Length == regionCount &&
               lightSeeds != null &&
               lightSeeds.Length == regionCount &&
               portals != null &&
               portals.Length == regionCount &&
               regionOneGuide != null &&
               activeRegionText != null &&
               seedCountText != null &&
               portalStatusText != null &&
               objectiveText != null &&
               notificationText != null;
    }

    private static bool IsValidRegion(int regionIndex)
    {
        return regionIndex >= 0 && regionIndex < RegionNames.Length;
    }
}
