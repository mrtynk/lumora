using UnityEngine;

// Connects the existing opening flow to this level, without owning progression.
public class IsikliVadiLevelController : MonoBehaviour
{
    [SerializeField] private StoryIntroUI storyIntro;
    [SerializeField] private RegionProgressController progress;
    [SerializeField] private PlayerController player;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private GameObject interactionsRoot;
    [SerializeField] private Canvas progressCanvas;
    [SerializeField] private LevelFollowCamera followCamera;
    [SerializeField] private Transform seedVisual;

    private CharacterController characterController;
    private Vector3 seedRestPosition;
    private bool adventureStarted;

    public bool AdventureStarted => adventureStarted;

    private void Awake()
    {
        if (storyIntro == null || progress == null || player == null ||
            spawnPoint == null || interactionsRoot == null || progressCanvas == null)
        {
            Debug.LogError("Işıklı Vadi başlangıç bağlantıları eksik.", this);
            enabled = false;
            return;
        }

        characterController = player.GetComponent<CharacterController>();
        player.enabled = false;
        interactionsRoot.SetActive(false);
        progressCanvas.enabled = false;
        if (seedVisual != null) seedRestPosition = seedVisual.localPosition;
        storyIntro.AdventureStarted += BeginAdventure;
    }

    private void OnDestroy()
    {
        if (storyIntro != null) storyIntro.AdventureStarted -= BeginAdventure;
    }

    private void BeginAdventure()
    {
        if (adventureStarted) return;
        adventureStarted = true;
        ReturnToSpawn();
        progressCanvas.enabled = true;
        progress.BeginExploration();
        interactionsRoot.SetActive(true);
        player.enabled = true;
    }

    private void LateUpdate()
    {
        if (!adventureStarted) return;

        // One animation for the important collectible, no updates on static decor.
        if (seedVisual != null && seedVisual.gameObject.activeInHierarchy)
        {
            seedVisual.localPosition = seedRestPosition +
                Vector3.up * (Mathf.Sin(Time.time * 1.8f) * 0.18f);
            seedVisual.Rotate(0f, 35f * Time.deltaTime, 0f, Space.Self);
        }

        Vector3 position = player.transform.position;
        if (position.y < -8f || Mathf.Abs(position.x) > 55f || Mathf.Abs(position.z) > 55f)
            ReturnToSpawn();
    }

    private void ReturnToSpawn()
    {
        bool controllerWasEnabled = characterController != null && characterController.enabled;
        if (characterController != null) characterController.enabled = false;
        player.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        if (characterController != null) characterController.enabled = controllerWasEnabled;
        if (followCamera != null) followCamera.SnapToTarget();
    }
}
