using UnityEngine;
using UnityEngine.UI;

public class HiddenObjectPuzzleUI : MonoBehaviour
{
    [Header("Bağlantılar")]
    [SerializeField] private PuzzlePopupUI puzzlePopup;
    [SerializeField] private Button lightSeedButton;
    [SerializeField] private Button shinyStoneButton;
    [SerializeField] private Button goldenLeafButton;
    [SerializeField] private Button hintButton;
    [SerializeField] private Text feedbackText;

    private bool isCompleted;
    private bool hintUsed;

    private void Awake()
    {
        lightSeedButton.onClick.AddListener(SelectLightSeed);
        shinyStoneButton.onClick.AddListener(
            () => SelectWrongObject("Parlak Taş")
        );
        goldenLeafButton.onClick.AddListener(
            () => SelectWrongObject("Altın Yaprak")
        );
        hintButton.onClick.AddListener(UseHint);
    }

    public void BeginPuzzle()
    {
        isCompleted = false;
        hintUsed = false;
        feedbackText.text = "Işık Tohumu'nu bul.";
        SetObjectButtonsInteractable(true);
        hintButton.interactable = true;
    }

    private void SelectLightSeed()
    {
        if (!CanInteract())
        {
            return;
        }

        isCompleted = true;
        feedbackText.text = "Işık Tohumu bulundu!";
        SetObjectButtonsInteractable(false);
        hintButton.interactable = false;
        puzzlePopup.CompletePuzzle();
    }

    private void SelectWrongObject(string objectName)
    {
        if (!CanInteract())
        {
            return;
        }

        feedbackText.text = "Bu nesne değil. Tekrar dene.";
        puzzlePopup.SendProgressEvent(
            "wrong_click",
            "Yanlış nesne seçildi: " + objectName
        );
    }

    private void UseHint()
    {
        if (!CanInteract() || hintUsed)
        {
            return;
        }

        hintUsed = true;
        hintButton.interactable = false;
        feedbackText.text = "İpucu: Işık saçan tohumu seç.";
        puzzlePopup.SendProgressEvent(
            "hint_requested",
            "Hidden Object bulmacasında ipucu kullanıldı"
        );
    }

    private bool CanInteract()
    {
        return !isCompleted && puzzlePopup != null && puzzlePopup.IsOpen;
    }

    private void SetObjectButtonsInteractable(bool isInteractable)
    {
        lightSeedButton.interactable = isInteractable;
        shinyStoneButton.interactable = isInteractable;
        goldenLeafButton.interactable = isInteractable;
    }
}
