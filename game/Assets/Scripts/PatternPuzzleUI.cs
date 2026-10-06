using UnityEngine;
using UnityEngine.UI;

public class PatternPuzzleUI : MonoBehaviour
{
    private const string PuzzleType = "pattern_puzzle";

    [Header("Bağlantılar")]
    [SerializeField] private PuzzlePopupUI puzzlePopup;
    [SerializeField] private Button redButton;
    [SerializeField] private Button blueButton;
    [SerializeField] private Button yellowButton;
    [SerializeField] private Text statusText;

    [Header("Ayarlar")]
    [SerializeField] private int failedAttemptThreshold = 3;

    private int wrongAttemptCount;
    private bool isCompleted;
    private bool failureReported;

    private void Awake()
    {
        redButton.onClick.AddListener(SelectWrongColor);
        blueButton.onClick.AddListener(SelectCorrectColor);
        yellowButton.onClick.AddListener(SelectWrongColor);
    }

    public void BeginPuzzle()
    {
        wrongAttemptCount = 0;
        isCompleted = false;
        failureReported = false;
        statusText.text = "Doğru rengi seç.";
        SetColorButtonsInteractable(true);
    }

    private void SelectCorrectColor()
    {
        if (!CanInteract())
        {
            return;
        }

        isCompleted = true;
        statusText.text = "Doğru! Örüntü tamamlandı.";
        SetColorButtonsInteractable(false);

        puzzlePopup.SendProgressEvent(
            PuzzleType,
            "choice_made",
            "Pattern Puzzle doğru seçim yapıldı"
        );
        puzzlePopup.CompletePuzzle(
            PuzzleType,
            "Pattern Puzzle tamamlandı"
        );
    }

    private void SelectWrongColor()
    {
        if (!CanInteract())
        {
            return;
        }

        wrongAttemptCount++;
        statusText.text = "Bu renk değil. Tekrar dene.";
        puzzlePopup.SendProgressEvent(
            PuzzleType,
            "wrong_click",
            "Pattern Puzzle yanlış seçim yapıldı"
        );

        if (!failureReported && wrongAttemptCount >= failedAttemptThreshold)
        {
            failureReported = true;
            puzzlePopup.SendProgressEvent(
                PuzzleType,
                "puzzle_failed",
                "Pattern Puzzle başarısız oldu"
            );
        }
    }

    private bool CanInteract()
    {
        return !isCompleted &&
               puzzlePopup != null &&
               puzzlePopup.IsPuzzleActive(PuzzleType);
    }

    private void SetColorButtonsInteractable(bool isInteractable)
    {
        redButton.interactable = isInteractable;
        blueButton.interactable = isInteractable;
        yellowButton.interactable = isInteractable;
    }
}
