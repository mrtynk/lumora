using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PuzzleTrigger : MonoBehaviour
{
    [Header("Event Bilgileri")]
    [SerializeField] private string childId = "demo-child-001";
    [SerializeField] private string eventType = "puzzle_started";
    [SerializeField] private string region = "isikli_vadi";
    [SerializeField] private string puzzleType = "hidden_object";
    [SerializeField] private string value = "Unity üzerinden puzzle kağıdı tetiklendi";

    [Header("Bağlantılar")]
    [SerializeField] private GameEventSender eventSender;

    private bool playerIsNearby;
    private bool eventSent;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Update()
    {
        if (playerIsNearby && !eventSent && Input.GetKeyDown(KeyCode.E))
        {
            if (eventSender == null)
            {
                Debug.LogError("PuzzleTrigger: GameEventSender bağlantısı yapılmamış.");
                return;
            }

            eventSender.SendEvent(childId, eventType, region, puzzleType, value);
            eventSent = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsNearby = true;
            Debug.Log("Puzzle kağıdına yaklaştın. Event göndermek için E tuşuna bas.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsNearby = false;
        }
    }
}
