using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PuzzleTrigger : MonoBehaviour
{
    [Header("Bağlantılar")]
    [SerializeField] private PuzzlePopupUI puzzlePopup;
    [SerializeField] private PlayerController playerController;

    [Tooltip("Boş bırakılırsa bulmaca seçim menüsü açılır. hidden_object, memory_match veya pattern_puzzle kullanın.")]
    [SerializeField] private string preferredPuzzleType = "";

    private bool playerIsNearby;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Update()
    {
        if (playerController != null && !playerController.isActiveAndEnabled)
        {
            return;
        }

        if (playerIsNearby && Input.GetKeyDown(KeyCode.E))
        {
            if (puzzlePopup == null)
            {
                Debug.LogError("PuzzleTrigger: PuzzlePopupUI bağlantısı yapılmamış.");
                return;
            }

            if (string.IsNullOrEmpty(preferredPuzzleType))
            {
                puzzlePopup.OpenPopup();
            }
            else
            {
                puzzlePopup.OpenPuzzle(preferredPuzzleType);
            }
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
