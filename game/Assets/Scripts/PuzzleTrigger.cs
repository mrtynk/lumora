using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PuzzleTrigger : MonoBehaviour
{
    [Header("Bağlantılar")]
    [SerializeField] private PuzzlePopupUI puzzlePopup;

    private bool playerIsNearby;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Update()
    {
        if (playerIsNearby && Input.GetKeyDown(KeyCode.E))
        {
            if (puzzlePopup == null)
            {
                Debug.LogError("PuzzleTrigger: PuzzlePopupUI bağlantısı yapılmamış.");
                return;
            }

            puzzlePopup.OpenPopup();
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
