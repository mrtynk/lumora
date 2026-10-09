using UnityEngine;

public class PortalTrigger : MonoBehaviour
{
    [SerializeField] private RegionProgressController progressController;
    [SerializeField] private int regionIndex;
    [SerializeField] private bool isFinalPortal;
    [SerializeField] private Renderer portalRenderer;
    [SerializeField] private Collider blockingCollider;
    [SerializeField] private Material lockedMaterial;
    [SerializeField] private Material openMaterial;
    [Tooltip("Gerçek bölge sahnesinin adı. Prototipte boş bırakılabilir.")]
    [SerializeField] private string destinationSceneName;

    public void SetUnlocked(bool isUnlocked)
    {
        Material material = isUnlocked ? openMaterial : lockedMaterial;
        if (portalRenderer != null && material != null)
        {
            portalRenderer.sharedMaterial = material;
        }

        if (blockingCollider != null)
        {
            blockingCollider.enabled = isFinalPortal || !isUnlocked;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (progressController != null && other.CompareTag("Player"))
        {
            progressController.TryUsePortal(regionIndex, destinationSceneName);
        }
    }
}
