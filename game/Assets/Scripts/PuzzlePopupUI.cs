using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PuzzlePopupUI : MonoBehaviour
{
    private const string HiddenObjectType = "hidden_object";
    private const string MemoryMatchType = "memory_match";
    private const string PatternPuzzleType = "pattern_puzzle";

    [Header("Event Bilgileri")]
    [SerializeField] private string childId = "demo-child-001";
    [SerializeField] private string region = "isikli_vadi";

    [Header("Genel Bağlantılar")]
    [SerializeField] private GameEventSender eventSender;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private DemoFlowController demoFlowController;
    [SerializeField] private GameObject popupRoot;

    [Header("Panel Bağlantıları")]
    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private GameObject hiddenObjectPanel;
    [SerializeField] private GameObject memoryMatchPanel;
    [SerializeField] private GameObject patternPuzzlePanel;
    [SerializeField] private HiddenObjectPuzzleUI hiddenObjectPuzzle;
    [SerializeField] private MemoryMatchPuzzleUI memoryMatchPuzzle;
    [SerializeField] private PatternPuzzleUI patternPuzzle;

    [Header("Buton Bağlantıları")]
    [SerializeField] private Button startHiddenObjectButton;
    [SerializeField] private Button startMemoryMatchButton;
    [SerializeField] private Button startPatternPuzzleButton;
    [SerializeField] private Button closeSelectionButton;
    [SerializeField] private Button closeHiddenObjectButton;
    [SerializeField] private Button closeMemoryMatchButton;
    [SerializeField] private Button closePatternPuzzleButton;

    [Header("Koruma")]
    [SerializeField] private float actionCooldown = 0.35f;

    private string activePuzzleType;
    private float nextAllowedActionTime;
    private bool isEnding;

    public bool IsOpen => popupRoot != null && popupRoot.activeSelf;

    private void Awake()
    {
        startHiddenObjectButton.onClick.AddListener(StartHiddenObjectPuzzle);
        startMemoryMatchButton.onClick.AddListener(StartMemoryMatchPuzzle);
        startPatternPuzzleButton.onClick.AddListener(StartPatternPuzzle);
        closeSelectionButton.onClick.AddListener(CloseSelection);
        closeHiddenObjectButton.onClick.AddListener(AbandonActivePuzzle);
        closeMemoryMatchButton.onClick.AddListener(AbandonActivePuzzle);
        closePatternPuzzleButton.onClick.AddListener(AbandonActivePuzzle);
        SetPopupVisible(false);
    }

    public void OpenPopup()
    {
        if (IsOpen || Time.unscaledTime < nextAllowedActionTime)
        {
            return;
        }

        if (!HasRequiredReferences())
        {
            Debug.LogError("PuzzlePopupUI bağlantıları eksik.");
            return;
        }

        activePuzzleType = string.Empty;
        isEnding = false;
        ShowOnly(selectionPanel);
        SetPopupVisible(true);
        demoFlowController?.NotifyPuzzleInteractionStarted();
    }

    public bool IsPuzzleActive(string puzzleType)
    {
        return IsOpen && !isEnding && activePuzzleType == puzzleType;
    }

    public void SendProgressEvent(string puzzleType, string eventType, string value)
    {
        if (IsPuzzleActive(puzzleType))
        {
            SendPuzzleEvent(puzzleType, eventType, value);
        }
    }

    public void CompletePuzzle(string puzzleType, string value)
    {
        if (!IsPuzzleActive(puzzleType))
        {
            return;
        }

        isEnding = true;
        SetActiveCloseButtonInteractable(false);
        SendPuzzleEvent(puzzleType, "puzzle_solved", value);
        demoFlowController?.NotifyPuzzleCompleted();
        StartCoroutine(CloseCompletedPuzzle());
    }

    private void StartHiddenObjectPuzzle()
    {
        if (!CanStartPuzzle())
        {
            return;
        }

        activePuzzleType = HiddenObjectType;
        hiddenObjectPuzzle.BeginPuzzle();
        closeHiddenObjectButton.interactable = true;
        ShowOnly(hiddenObjectPanel);
        SendPuzzleEvent(
            HiddenObjectType,
            "puzzle_started",
            "Hidden Object bulmacası başlatıldı"
        );
    }

    private void StartMemoryMatchPuzzle()
    {
        if (!CanStartPuzzle())
        {
            return;
        }

        activePuzzleType = MemoryMatchType;
        memoryMatchPuzzle.BeginPuzzle();
        closeMemoryMatchButton.interactable = true;
        ShowOnly(memoryMatchPanel);
        SendPuzzleEvent(
            MemoryMatchType,
            "puzzle_started",
            "Memory Match bulmacası başlatıldı"
        );
    }

    private void StartPatternPuzzle()
    {
        if (!CanStartPuzzle())
        {
            return;
        }

        activePuzzleType = PatternPuzzleType;
        patternPuzzle.BeginPuzzle();
        closePatternPuzzleButton.interactable = true;
        ShowOnly(patternPuzzlePanel);
        SendPuzzleEvent(
            PatternPuzzleType,
            "puzzle_started",
            "Pattern Puzzle başlatıldı"
        );
    }

    private bool CanStartPuzzle()
    {
        return IsOpen && !isEnding && string.IsNullOrEmpty(activePuzzleType);
    }

    private void CloseSelection()
    {
        if (!IsOpen || !string.IsNullOrEmpty(activePuzzleType))
        {
            return;
        }

        nextAllowedActionTime = Time.unscaledTime + actionCooldown;
        SetPopupVisible(false);
    }

    private void AbandonActivePuzzle()
    {
        if (!IsOpen || isEnding || string.IsNullOrEmpty(activePuzzleType))
        {
            return;
        }

        isEnding = true;
        SetActiveCloseButtonInteractable(false);

        string value;
        if (activePuzzleType == MemoryMatchType)
        {
            value = "Memory Match bulmacası kapatıldı";
        }
        else if (activePuzzleType == PatternPuzzleType)
        {
            value = "Pattern Puzzle kapatıldı";
        }
        else
        {
            value = "Popup kapatıldı, puzzle yarıda bırakıldı";
        }

        SendPuzzleEvent(activePuzzleType, "puzzle_abandoned", value);
        nextAllowedActionTime = Time.unscaledTime + actionCooldown;
        SetPopupVisible(false);
    }

    private IEnumerator CloseCompletedPuzzle()
    {
        yield return new WaitForSecondsRealtime(actionCooldown);
        nextAllowedActionTime = Time.unscaledTime + actionCooldown;
        SetPopupVisible(false);
    }

    private void SendPuzzleEvent(string puzzleType, string eventType, string value)
    {
        eventSender.SendEvent(childId, eventType, region, puzzleType, value);
    }

    private void ShowOnly(GameObject panelToShow)
    {
        selectionPanel.SetActive(panelToShow == selectionPanel);
        hiddenObjectPanel.SetActive(panelToShow == hiddenObjectPanel);
        memoryMatchPanel.SetActive(panelToShow == memoryMatchPanel);
        patternPuzzlePanel.SetActive(panelToShow == patternPuzzlePanel);
    }

    private void SetPopupVisible(bool isVisible)
    {
        if (popupRoot != null)
        {
            popupRoot.SetActive(isVisible);
        }

        if (playerController != null)
        {
            playerController.enabled = !isVisible;
        }
    }

    private void SetActiveCloseButtonInteractable(bool isInteractable)
    {
        if (activePuzzleType == MemoryMatchType)
        {
            closeMemoryMatchButton.interactable = isInteractable;
        }
        else if (activePuzzleType == PatternPuzzleType)
        {
            closePatternPuzzleButton.interactable = isInteractable;
        }
        else if (activePuzzleType == HiddenObjectType)
        {
            closeHiddenObjectButton.interactable = isInteractable;
        }
    }

    private bool HasRequiredReferences()
    {
        return eventSender != null &&
               popupRoot != null &&
               selectionPanel != null &&
               hiddenObjectPanel != null &&
               memoryMatchPanel != null &&
               patternPuzzlePanel != null &&
               hiddenObjectPuzzle != null &&
               memoryMatchPuzzle != null &&
               patternPuzzle != null;
    }
}
