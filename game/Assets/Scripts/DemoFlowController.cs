using UnityEngine;
using UnityEngine.UI;

public class DemoFlowController : MonoBehaviour
{
    private const string ChildId = "demo-child-001";
    private const string Region = "isikli_vadi";
    private const string FlowType = "demo_flow";

    [Header("Bağlantılar")]
    [SerializeField] private GameEventSender eventSender;
    [SerializeField] private Text instructionText;
    [SerializeField] private GameObject rewardObject;

    private bool areaExploredSent;
    private bool puzzleCompleted;
    private bool rewardCollectedSent;

    private void Start()
    {
        rewardObject.SetActive(false);
        instructionText.text = "PuzzlePaper'a git ve E'ye bas.";
        SendAreaExploredOnce();
    }

    public void NotifyPuzzleInteractionStarted()
    {
        if (!rewardCollectedSent && !puzzleCompleted)
        {
            instructionText.text = "Bir bulmaca seç ve tamamla.";
        }
    }

    public void NotifyPuzzleCompleted()
    {
        if (rewardCollectedSent)
        {
            return;
        }

        puzzleCompleted = true;
        instructionText.text = "Orman Dostu NPC'ye git ve E'ye bas.";
    }

    public void NotifyNpcDialogueOpened()
    {
        if (rewardCollectedSent)
        {
            return;
        }

        instructionText.text = puzzleCompleted
            ? "Yardım Et seçeneğini seç."
            : "Önce PuzzlePaper'daki bir bulmacayı tamamla.";
    }

    public void NotifyNpcChoice(bool helpedNpc)
    {
        if (rewardCollectedSent)
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

        rewardCollectedSent = true;
        rewardObject.SetActive(true);
        instructionText.text = "Demo tamamlandı: Işık Tohumu kazanıldı.";
        eventSender.SendEvent(
            ChildId,
            "reward_collected",
            Region,
            FlowType,
            "Işık Tohumu ödülü kazanıldı"
        );
    }

    private void SendAreaExploredOnce()
    {
        if (areaExploredSent)
        {
            return;
        }

        areaExploredSent = true;
        eventSender.SendEvent(
            ChildId,
            "area_explored",
            Region,
            FlowType,
            "Işıklı Vadi demo alanı keşfedildi"
        );
    }
}
