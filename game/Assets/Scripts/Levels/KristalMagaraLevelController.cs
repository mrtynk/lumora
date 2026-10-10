using UnityEngine;
using UnityEngine.UI;

public class KristalMagaraLevelController : MonoBehaviour
{
    private const string MechanicKey = "kristal_magara/crystal_light";
    [SerializeField] private RegionProgressController progress;
    [SerializeField] private GameEventSender eventSender;
    [SerializeField] private PlayerController player;
    [SerializeField] private LevelFollowCamera followCamera;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private CrystalLightNode[] nodes;
    [SerializeField] private GameObject stoneDoor;
    [SerializeField] private LightSeedCollectible lightSeed;
    [SerializeField] private Text instruction;
    private float nextAction;
    public bool LightPathCompleted => progress.IsMechanicCompleted(MechanicKey);

    private void Start()
    {
        Respawn();
        progress.BeginExploration();
        player.enabled = progress.HasStarted;
        foreach (CrystalLightNode node in nodes) node.Initialize(LightPathCompleted);
        RefreshAccess();
    }

    private void Update()
    {
        if (!progress.HasStarted) return;
        if (player.transform.position.y < -5 || Mathf.Abs(player.transform.position.x) > 58 || Mathf.Abs(player.transform.position.z) > 58)
            Respawn();
        if (LightPathCompleted || !player.isActiveAndEnabled) return;
        if (Input.GetKeyDown(KeyCode.E))
        {
            foreach (CrystalLightNode node in nodes)
                if (TryRotate(node)) break;
        }
    }

    // Shared by keyboard interaction and integration tests; cannot act remotely or during popups.
    public bool TryRotate(CrystalLightNode node)
    {
        if (!progress.HasStarted || !player.isActiveAndEnabled || LightPathCompleted ||
            Time.unscaledTime < nextAction || System.Array.IndexOf(nodes, node) < 0 ||
            Vector3.Distance(player.transform.position, node.transform.position) > 4) return false;
        nextAction = Time.unscaledTime + .4f;
        node.RotateDirection();
        eventSender.SendEvent("demo-child-001", "choice_made", "kristal_magara", "crystal_light", "Kristal yönü değiştirildi");
        bool allAligned = true;
        foreach (CrystalLightNode crystal in nodes) allAligned &= crystal.IsAligned;
        if (allAligned && progress.TryCompleteMechanic(MechanicKey))
        {
            eventSender.SendEvent("demo-child-001", "puzzle_solved", "kristal_magara", "crystal_light", "Kristal ışık yolu tamamlandı");
            RefreshAccess();
        }
        return true;
    }

    private void RefreshAccess()
    {
        stoneDoor.SetActive(!LightPathCompleted);
        lightSeed.gameObject.SetActive(progress.HasStarted && LightPathCompleted && !progress.IsSeedCollected(2));
        instruction.text = LightPathCompleted
            ? "Işık yolu tamamlandı! Açılan kapıdan geç ve ışık tohumunu bul."
            : "Parlayan iki kristale yaklaş ve E ile çevir. Işıkları işaretli hedeflere yönelt.";
    }

    private void Respawn()
    {
        var controller = player.GetComponent<CharacterController>();
        bool previous = controller.enabled;
        controller.enabled = false;
        player.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        controller.enabled = previous;
        followCamera.SnapToTarget();
    }
}
