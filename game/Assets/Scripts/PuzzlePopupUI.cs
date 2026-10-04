using System.Collections;
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
    [SerializeField] private HiddenObjectPuzzleUI hiddenObjectPuzzle;
    [SerializeField] private Button closeButton;

    [Header("Koruma")]
    [SerializeField] private float actionCooldown = 0.35f;

    private float nextAllowedActionTime;
    private bool isEnding;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    private void Awake()
    {
        closeButton.onClick.AddListener(AbandonPuzzle);
        SetPopupVisible(false);
    }

    public void OpenPopup()
    {
        if (IsOpen || Time.unscaledTime < nextAllowedActionTime)
        {
            return;
        }

        if (eventSender == null || panelRoot == null || hiddenObjectPuzzle == null)
        {
            Debug.LogError("PuzzlePopupUI bağlantıları eksik.");
            return;
        }

        isEnding = false;
        closeButton.interactable = true;
        hiddenObjectPuzzle.BeginPuzzle();
        SetPopupVisible(true);
        SendPuzzleEvent("puzzle_started", "Unity üzerinden puzzle kağıdı tetiklendi");
    }

    public void SendProgressEvent(string eventType, string value)
    {
        if (IsOpen && !isEnding)
        {
            SendPuzzleEvent(eventType, value);
        }
    }

    public void CompletePuzzle()
    {
        if (!IsOpen || isEnding)
        {
            return;
        }

        isEnding = true;
        closeButton.interactable = false;
        SendPuzzleEvent(
            "puzzle_solved",
            "Hidden Object bulmacasında Işık Tohumu bulundu"
        );
        StartCoroutine(CloseCompletedPuzzle());
    }

    private void AbandonPuzzle()
    {
        if (!IsOpen || isEnding || Time.unscaledTime < nextAllowedActionTime)
        {
            return;
        }

        isEnding = true;
        nextAllowedActionTime = Time.unscaledTime + actionCooldown;
        closeButton.interactable = false;
        SendPuzzleEvent(
            "puzzle_abandoned",
            "Popup kapatıldı, puzzle yarıda bırakıldı"
        );
        SetPopupVisible(false);
    }

    private IEnumerator CloseCompletedPuzzle()
    {
        yield return new WaitForSecondsRealtime(actionCooldown);
        nextAllowedActionTime = Time.unscaledTime + actionCooldown;
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

}
