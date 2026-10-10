using UnityEngine;

// Passive node: only the room controller polls input. No per-crystal Update/physics.
public class CrystalLightNode : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Transform pointer;
    [SerializeField] private Renderer crystal;
    [SerializeField] private Material waitingMaterial;
    [SerializeField] private Material connectedMaterial;
    [SerializeField] private LineRenderer beam;
    [SerializeField, Range(0, 3)] private int initialDirection;
    [SerializeField, Range(0, 3)] private int correctDirection;
    private int direction;

    public bool IsAligned => direction == correctDirection;

    public void Initialize(bool solved)
    {
        direction = solved ? correctDirection : initialDirection;
        Refresh();
    }

    public void RotateDirection()
    {
        direction = (direction + 1) % 4;
        Refresh();
    }

    private void Refresh()
    {
        Vector3 origin = transform.position + Vector3.up * 1.8f;
        Vector3 forward = Quaternion.Euler(0, direction * 90, 0) * Vector3.forward;
        pointer.localRotation = Quaternion.Euler(0, direction * 90, 0);
        beam.SetPosition(0, origin);
        beam.SetPosition(1, IsAligned ? target.position + Vector3.up * 1.8f : origin + forward * 5);
        crystal.sharedMaterial = IsAligned ? connectedMaterial : waitingMaterial;
        beam.startColor = beam.endColor = IsAligned ? Color.cyan : new Color(1, .55f, .9f);
    }
}
