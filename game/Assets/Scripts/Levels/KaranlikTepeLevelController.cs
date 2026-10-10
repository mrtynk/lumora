using UnityEngine;
using UnityEngine.UI;

public class KaranlikTepeLevelController : MonoBehaviour
{
    public const string MechanicKey = "karanlik_tepe/light_beacon";
    [SerializeField] private RegionProgressController progress;
    [SerializeField] private GameEventSender eventSender;
    [SerializeField] private PlayerController player;
    [SerializeField] private LevelFollowCamera followCamera;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private LightBeacon[] beacons;
    [SerializeField] private GameObject summitGate;
    [SerializeField] private LightSeedCollectible lightSeed;
    [SerializeField] private Text instruction;
    public bool LightPathCompleted => progress.IsMechanicCompleted(MechanicKey);

    private void Start()
    {
        Respawn();
        progress.BeginExploration();
        player.enabled = progress.HasStarted;
        foreach (LightBeacon beacon in beacons) beacon.SetLit(progress.IsMechanicCompleted(beacon.StateKey));
        RefreshAccess();
    }

    private void Update()
    {
        if (!progress.HasStarted) return;
        Vector3 p = player.transform.position;
        if (p.y < -8 || Mathf.Abs(p.x) > 64 || Mathf.Abs(p.z) > 64) Respawn();
        if (!player.isActiveAndEnabled || LightPathCompleted) return;
        if (Input.GetKeyDown(KeyCode.E))
            foreach (LightBeacon beacon in beacons)
                if (TryActivate(beacon)) break;
    }

    public bool TryActivate(LightBeacon beacon)
    {
        if (!progress.HasStarted || !player.isActiveAndEnabled || beacon == null ||
            System.Array.IndexOf(beacons, beacon) < 0 ||
            Vector3.Distance(player.transform.position, beacon.transform.position) > 4 ||
            !progress.TryCompleteMechanic(beacon.StateKey)) return false;

        beacon.SetLit(true);
        eventSender.SendEvent("demo-child-001", "choice_made", "karanlik_tepe", "light_beacon", "Işık feneri aktive edildi");
        bool allLit = true;
        foreach (LightBeacon item in beacons) allLit &= item.IsLit;
        if (allLit && progress.TryCompleteMechanic(MechanicKey))
            eventSender.SendEvent("demo-child-001", "puzzle_solved", "karanlik_tepe", "light_beacon", "Karanlık Tepe ışık yolu tamamlandı");
        RefreshAccess();
        return true;
    }

    private void RefreshAccess()
    {
        summitGate.SetActive(!LightPathCompleted);
        lightSeed.gameObject.SetActive(progress.HasStarted && LightPathCompleted && !progress.IsSeedCollected(3));
        int count = 0;
        foreach (LightBeacon beacon in beacons) if (beacon.IsLit) count++;
        instruction.text = LightPathCompleted ? "Zirve yolu aydınlandı! Son tohumu bul ve Işık Ağacı'na dön."
            : "Fenerler: " + count + " / 3 · Yaklaş ve E ile ışığı uyandır.";
        RenderSettings.ambientLight = Color.Lerp(new Color(.42f,.43f,.59f), new Color(.68f,.60f,.66f), count / 3f);
    }

    private void Respawn()
    {
        var controller = player.GetComponent<CharacterController>();
        bool wasEnabled = controller.enabled;
        controller.enabled = false;
        player.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        controller.enabled = wasEnabled;
        followCamera.SnapToTarget();
    }
}
