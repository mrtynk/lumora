using UnityEngine;

// Passive visual only. Session state and input belong to the level controller.
public class LightBeacon : MonoBehaviour
{
    [SerializeField] private string beaconId;
    [SerializeField] private Renderer lantern;
    [SerializeField] private Material unlitMaterial;
    [SerializeField] private Material litMaterial;
    [SerializeField] private GameObject illuminatedRoute;
    public string StateKey => "karanlik_tepe/beacon/" + beaconId;
    public bool IsLit { get; private set; }

    public void SetLit(bool lit)
    {
        IsLit = lit;
        lantern.sharedMaterial = lit ? litMaterial : unlitMaterial;
        illuminatedRoute.SetActive(lit);
    }
}
