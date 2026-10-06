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

    public void SetUnlocked(bool isUnlocked)
    {
        if (portalRenderer != null)
        {
            portalRenderer.sharedMaterial = isUnlocked
                ? openMaterial
                : lockedMaterial;
        }

        if (blockingCollider != null)
        {
            blockingCollider.enabled = isFinalPortal || !isUnlocked;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            progressController.TryUsePortal(regionIndex);
        }
    }
}
