using UnityEngine;
using UnityEngine.UI;

public class PuzzlePopupUI : MonoBehaviour
{
    [Header("Event Bilgileri")]
    [SerializeField] private string childId = "demo-child-001";
    [SerializeField] private string region = "isikli_vadi";
    [SerializeField] private string puzzleType = "hidden_object";

    [Header("Bağlantılar")]
    [SerializeField] private GameEventSender eventSender;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button successButton;
    [SerializeField] private Button failureButton;
    [SerializeField] private Button closeButton;

    [Header("Koruma")]
    [SerializeField] private float actionCooldown = 0.35f;

    private float nextAllowedActionTime;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        successButton.onClick.AddListener(CompleteSuccessfully);
        failureButton.onClick.AddListener(CompleteWithFailure);
        closeButton.onClick.AddListener(AbandonPuzzle);
        SetPopupVisible(false);
    }

    public void OpenPopup()
    {
        if (IsOpen || Time.unscaledTime < nextAllowedActionTime)
        {
            return;
        }

        if (eventSender == null || panelRoot == null)
        {
            Debug.LogError("PuzzlePopupUI bağlantıları eksik.");
            return;
        }

        SetButtonsInteractable(true);
        SetPopupVisible(true);
        SendPuzzleEvent("puzzle_started", "Unity üzerinden puzzle kağıdı tetiklendi");
    }

    private void CompleteSuccessfully()
    {
        SendResultAndClose(
            "puzzle_solved",
            "Popup üzerinden puzzle başarılı tamamlandı"
        );
    }

    private void CompleteWithFailure()
    {
        SendResultAndClose(
            "puzzle_failed",
            "Popup üzerinden puzzle başarısız oldu"
        );
    }

    private void AbandonPuzzle()
    {
        SendResultAndClose(
            "puzzle_abandoned",
            "Popup kapatıldı, puzzle yarıda bırakıldı"
        );
    }

    private void SendResultAndClose(string eventType, string value)
    {
        if (!IsOpen || Time.unscaledTime < nextAllowedActionTime)
        {
            return;
        }

        nextAllowedActionTime = Time.unscaledTime + actionCooldown;
        SetButtonsInteractable(false);
        SendPuzzleEvent(eventType, value);
        SetPopupVisible(false);
    }

    private void SendPuzzleEvent(string eventType, string value)
    {
        eventSender.SendEvent(childId, eventType, region, puzzleType, value);
    }

    private void SetPopupVisible(bool isVisible)
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(isVisible);
        }

        if (playerController != null)
        {
            playerController.enabled = !isVisible;
        }
    }

    private void SetButtonsInteractable(bool isInteractable)
    {
        successButton.interactable = isInteractable;
        failureButton.interactable = isInteractable;
        closeButton.interactable = isInteractable;
    }
}
