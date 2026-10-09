using UnityEngine;

public class LightSeedCollectible : MonoBehaviour
{
    [SerializeField] private RegionProgressController progressController;
    [SerializeField] private int regionIndex;

    private bool collected;

    private void OnTriggerEnter(Collider other)
    {
        if (collected || progressController == null || !other.CompareTag("Player"))
        {
            return;
        }

        if (progressController.TryCollectLightSeed(regionIndex))
        {
            collected = true;
            gameObject.SetActive(false);
        }
    }
}
