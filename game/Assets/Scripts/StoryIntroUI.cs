using UnityEngine;
using UnityEngine.UI;

public class StoryIntroUI : MonoBehaviour
{
    [Header("Bağlantılar")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private GameObject introRoot;
    [SerializeField] private Button startButton;

    private bool introCompleted;

    public bool IsOpen => introRoot != null && introRoot.activeSelf;

    private void Awake()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(BeginAdventure);
        }
    }

    private void Start()
    {
        if (!HasRequiredReferences())
        {
            Debug.LogError("StoryIntroUI bağlantıları eksik.");
            return;
        }

        introCompleted = false;
        SetIntroVisible(true);
    }

    private void OnDestroy()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(BeginAdventure);
        }
    }

    private void BeginAdventure()
    {
        if (introCompleted || !IsOpen)
        {
            return;
        }

        introCompleted = true;
        startButton.interactable = false;
        SetIntroVisible(false);
    }

    private void SetIntroVisible(bool isVisible)
    {
        introRoot.SetActive(isVisible);

        if (playerController != null)
        {
            playerController.enabled = !isVisible;
        }
    }

    private bool HasRequiredReferences()
    {
        return playerController != null &&
               introRoot != null &&
               startButton != null;
    }
}
