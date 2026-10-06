using UnityEngine;
using UnityEngine.UI;

public class DemoFlowController : MonoBehaviour
{
    [Header("Bağlantılar")]
    [SerializeField] private Text instructionText;

    private bool puzzleCompleted;
    private bool optionalFlowCompleted;

    private void Start()
    {
        instructionText.text =
            "İstersen PuzzlePaper'daki mini görevleri deneyebilirsin.";
    }

    public void NotifyPuzzleInteractionStarted()
    {
        if (!optionalFlowCompleted && !puzzleCompleted)
        {
            instructionText.text = "Bir bulmaca seç ve tamamla.";
        }
    }

    public void NotifyPuzzleCompleted()
    {
        if (optionalFlowCompleted)
        {
            return;
        }

        puzzleCompleted = true;
        instructionText.text =
            "Opsiyonel: Orman Dostu NPC'ye git ve E'ye bas.";
    }

    public void NotifyNpcDialogueOpened()
    {
        if (optionalFlowCompleted)
        {
            return;
        }

        instructionText.text = puzzleCompleted
            ? "Yardım Et seçeneğini seç."
            : "Önce PuzzlePaper'daki bir bulmacayı tamamla.";
    }

    public void NotifyNpcChoice(bool helpedNpc)
    {
        if (optionalFlowCompleted)
        {
            return;
        }

        if (!puzzleCompleted)
        {
            instructionText.text = "Önce bir bulmacayı tamamla.";
            return;
        }

        if (!helpedNpc)
        {
            instructionText.text = "NPC ile tekrar konuş ve Yardım Et'i seç.";
            return;
        }

        optionalFlowCompleted = true;
        instructionText.text =
            "Opsiyonel mini görev tamamlandı. Ana hedef: Işık Tohumu'nu bul.";
    }
}
