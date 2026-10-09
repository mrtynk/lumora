using UnityEngine;

// Scene-local setup only: progression and analytics belong to RegionProgressController.
public class SisliOrmanLevelController : MonoBehaviour
{
    [SerializeField] private RegionProgressController progress;
    [SerializeField] private PlayerController player;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private LevelFollowCamera followCamera;
    [SerializeField] private Transform seedVisual;
    private CharacterController characterController;
    private Vector3 seedOrigin;

    private void Start()
    {
        if (progress == null || player == null || spawnPoint == null || followCamera == null)
        {
            Debug.LogError("Sisli Orman sahne bağlantıları eksik.", this);
            enabled = false;
            return;
        }
        characterController = player.GetComponent<CharacterController>();
        if (seedVisual != null) seedOrigin = seedVisual.localPosition;
        Respawn();
        progress.BeginExploration();
        player.enabled = progress.HasStarted;
    }

    private void LateUpdate()
    {
        if (!progress.HasStarted) return;
        if (seedVisual != null && seedVisual.gameObject.activeInHierarchy)
        {
            seedVisual.localPosition = seedOrigin + Vector3.up * Mathf.Sin(Time.time * 1.8f) * 0.15f;
            seedVisual.Rotate(Vector3.up, 30f * Time.deltaTime);
        }
        Vector3 p = player.transform.position;
        if (p.y < -6 || Mathf.Abs(p.x) > 45 || Mathf.Abs(p.z) > 45) Respawn();
    }

    private void Respawn()
    {
        bool wasEnabled = characterController.enabled;
        characterController.enabled = false;
        player.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        characterController.enabled = wasEnabled;
        followCamera.SnapToTarget();
    }
}
