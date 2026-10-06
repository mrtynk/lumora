using UnityEngine;
using UnityEngine.UI;

public class StoryIntroUI : MonoBehaviour
{
    [Header("Bağlantılar")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private GameObject introRoot;
    [SerializeField] private Button startButton;
    [SerializeField] private bool showOnStart = true;

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
        if (showOnStart)
        {
            ShowIntro();
        }
        else
        {
            introRoot.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(BeginAdventure);
        }
    }

    public void ShowIntro()
    {
        if (!HasRequiredReferences())
        {
            Debug.LogError("StoryIntroUI bağlantıları eksik.");
            return;
        }

        introCompleted = false;
        startButton.interactable = true;
        SetIntroVisible(true);
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
