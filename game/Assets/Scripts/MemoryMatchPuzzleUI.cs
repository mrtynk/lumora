using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MemoryMatchPuzzleUI : MonoBehaviour
{
    private const string PuzzleType = "memory_match";

    [Header("Bağlantılar")]
    [SerializeField] private PuzzlePopupUI puzzlePopup;
    [SerializeField] private Button[] cardButtons;
    [SerializeField] private Text statusText;

    [Header("Ayarlar")]
    [SerializeField] private float wrongPairVisibleTime = 0.65f;
    [SerializeField] private int failedAttemptThreshold = 3;

    private readonly string[] cardSymbols = { "Gunes", "Ay", "Gunes", "Ay" };
    private readonly bool[] matchedCards = new bool[4];
    private int firstCardIndex = -1;
    private int matchedCardCount;
    private int wrongAttemptCount;
    private bool isCheckingPair;
    private bool failureReported;

    private void Awake()
    {
        for (int i = 0; i < cardButtons.Length; i++)
        {
            int cardIndex = i;
            cardButtons[i].onClick.AddListener(() => SelectCard(cardIndex));
        }
    }

    public void BeginPuzzle()
    {
        StopAllCoroutines();
        ShuffleSymbols();

        firstCardIndex = -1;
        matchedCardCount = 0;
        wrongAttemptCount = 0;
        isCheckingPair = false;
        failureReported = false;
        statusText.text = "Aynı sembolleri eşleştir.";

        for (int i = 0; i < cardButtons.Length; i++)
        {
            matchedCards[i] = false;
            SetCardFace(i, false);
            cardButtons[i].interactable = true;
        }
    }

    private void SelectCard(int cardIndex)
    {
        if (!CanSelectCard(cardIndex))
        {
            return;
        }

        SetCardFace(cardIndex, true);
        cardButtons[cardIndex].interactable = false;

        if (firstCardIndex < 0)
        {
            firstCardIndex = cardIndex;
            return;
        }

        if (cardSymbols[firstCardIndex] == cardSymbols[cardIndex])
        {
            matchedCards[firstCardIndex] = true;
            matchedCards[cardIndex] = true;
            matchedCardCount += 2;
            firstCardIndex = -1;
            statusText.text = "Doğru eşleşme!";

            if (matchedCardCount == cardButtons.Length)
            {
                statusText.text = "Tüm eşleşmeler tamamlandı!";
                puzzlePopup.CompletePuzzle(
                    PuzzleType,
                    "Memory Match bulmacası tamamlandı"
                );
            }

            return;
        }

        isCheckingPair = true;
        statusText.text = "Eşleşmedi, tekrar dene.";
        puzzlePopup.SendProgressEvent(
            PuzzleType,
            "retry_attempt",
            "Memory Match yanlış eşleşme denemesi"
        );
        wrongAttemptCount++;

        if (!failureReported && wrongAttemptCount >= failedAttemptThreshold)
        {
            failureReported = true;
            puzzlePopup.SendProgressEvent(
                PuzzleType,
                "puzzle_failed",
                "Memory Match bulmacasında deneme sınırına ulaşıldı"
            );
        }

        StartCoroutine(HideWrongPair(firstCardIndex, cardIndex));
    }

    private IEnumerator HideWrongPair(int firstIndex, int secondIndex)
    {
        SetUnmatchedCardsInteractable(false);
        yield return new WaitForSecondsRealtime(wrongPairVisibleTime);

        SetCardFace(firstIndex, false);
        SetCardFace(secondIndex, false);
        firstCardIndex = -1;
        isCheckingPair = false;
        statusText.text = "Aynı sembolleri eşleştir.";
        SetUnmatchedCardsInteractable(true);
    }

    private bool CanSelectCard(int cardIndex)
    {
        return puzzlePopup != null &&
               puzzlePopup.IsPuzzleActive(PuzzleType) &&
               !isCheckingPair &&
               cardIndex >= 0 &&
               cardIndex < cardButtons.Length &&
               !matchedCards[cardIndex];
    }

    private void SetUnmatchedCardsInteractable(bool isInteractable)
    {
        for (int i = 0; i < cardButtons.Length; i++)
        {
            cardButtons[i].interactable = isInteractable && !matchedCards[i];
        }
    }

    private void SetCardFace(int cardIndex, bool isFaceUp)
    {
        Text cardText = cardButtons[cardIndex].GetComponentInChildren<Text>();
        cardText.text = isFaceUp ? cardSymbols[cardIndex] : "?";
    }

    private void ShuffleSymbols()
    {
        for (int i = cardSymbols.Length - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            string currentSymbol = cardSymbols[i];
            cardSymbols[i] = cardSymbols[randomIndex];
            cardSymbols[randomIndex] = currentSymbol;
        }
    }
}
