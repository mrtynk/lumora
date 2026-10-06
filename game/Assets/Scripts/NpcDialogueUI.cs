using UnityEngine;
using UnityEngine.UI;

public class NpcDialogueUI : MonoBehaviour
{
    private const string PuzzleType = "npc_dialogue";

    [Header("Event Bilgileri")]
    [SerializeField] private string childId = "demo-child-001";
    [SerializeField] private string region = "isikli_vadi";

    [Header("Bağlantılar")]
    [SerializeField] private GameEventSender eventSender;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private DemoFlowController demoFlowController;
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Button helpButton;
    [SerializeField] private Button laterButton;
    [SerializeField] private Button closeButton;

    [Header("Koruma")]
    [SerializeField] private float actionCooldown = 0.35f;

    private float nextAllowedActionTime;
    private bool isHandlingChoice;

    public bool IsOpen => popupRoot != null && popupRoot.activeSelf;

    private void Awake()
    {
        helpButton.onClick.AddListener(ChooseHelp);
        laterButton.onClick.AddListener(ChooseLater);
        closeButton.onClick.AddListener(CloseWithoutChoice);
        SetPopupVisible(false);
    }

    public void OpenDialogue()
    {
        if (IsOpen || Time.unscaledTime < nextAllowedActionTime)
        {
            return;
        }

        if (!HasRequiredReferences())
        {
            Debug.LogError("NpcDialogueUI bağlantıları eksik.");
            return;
        }

        isHandlingChoice = false;
        SetButtonsInteractable(true);
        SetPopupVisible(true);
        demoFlowController?.NotifyNpcDialogueOpened();
    }

    private void ChooseHelp()
    {
        HandleChoice(true);
    }

    private void ChooseLater()
    {
        HandleChoice(false);
    }

    private void HandleChoice(bool willHelp)
    {
        if (!IsOpen || isHandlingChoice)
        {
            return;
        }

        isHandlingChoice = true;
        SetButtonsInteractable(false);

        SendEvent("dialogue_selected", "NPC diyaloğunda seçim yapıldı");
        SendEvent("choice_made", "Oyuncu NPC diyaloğunda karar verdi");

        if (willHelp)
        {
            SendEvent("npc_helped", "Oyuncu NPC'ye yardım etmeyi seçti");
        }

        demoFlowController?.NotifyNpcChoice(willHelp);
        ClosePopup();
    }

    private void CloseWithoutChoice()
    {
        if (!IsOpen || isHandlingChoice)
        {
            return;
        }

        isHandlingChoice = true;
        SetButtonsInteractable(false);
        ClosePopup();
    }

    private void SendEvent(string eventType, string value)
    {
        eventSender.SendEvent(childId, eventType, region, PuzzleType, value);
    }

    private void ClosePopup()
    {
        nextAllowedActionTime = Time.unscaledTime + actionCooldown;
        SetPopupVisible(false);
    }

    private void SetPopupVisible(bool isVisible)
    {
        popupRoot.SetActive(isVisible);

        if (playerController != null)
        {
            playerController.enabled = !isVisible;
        }
    }

    private void SetButtonsInteractable(bool isInteractable)
    {
        helpButton.interactable = isInteractable;
        laterButton.interactable = isInteractable;
        closeButton.interactable = isInteractable;
    }

    private bool HasRequiredReferences()
    {
        return eventSender != null &&
               popupRoot != null &&
               helpButton != null &&
               laterButton != null &&
               closeButton != null;
    }
}
