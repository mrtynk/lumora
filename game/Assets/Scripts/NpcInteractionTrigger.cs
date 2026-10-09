using UnityEngine;

[RequireComponent(typeof(Collider))]
public class NpcInteractionTrigger : MonoBehaviour
{
    [SerializeField] private NpcDialogueUI dialogueUI;
    [SerializeField] private PlayerController playerController;

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

        if (!playerIsNearby || !Input.GetKeyDown(KeyCode.E))
        {
            return;
        }

        if (dialogueUI == null)
        {
            Debug.LogError("NpcInteractionTrigger: NpcDialogueUI bağlantısı yapılmamış.");
            return;
        }

        dialogueUI.OpenDialogue();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsNearby = true;
            Debug.Log("Orman Dostu ile konuşmak için E tuşuna bas.");
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
