using UnityEngine;

[DisallowMultipleComponent]
public class LevelFollowCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 10f, -14f);
    [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1.2f, 3f);
    [SerializeField, Min(0.01f)] private float smoothTime = 0.18f;
    private Vector3 velocity;

    private void Start() => SnapToTarget();

    private void LateUpdate()
    {
        if (target == null) return;
        transform.position = Vector3.SmoothDamp(transform.position,
            target.position + offset, ref velocity, smoothTime);
        transform.LookAt(target.position + lookOffset);
    }

    public void SnapToTarget()
    {
        if (target == null) return;
        velocity = Vector3.zero;
        transform.position = target.position + offset;
        transform.LookAt(target.position + lookOffset);
    }
}
