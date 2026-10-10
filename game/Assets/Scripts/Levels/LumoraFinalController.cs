using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LumoraFinalController : MonoBehaviour
{
    [SerializeField] private RegionProgressController progress;
    [SerializeField] private PlayerController player;
    [SerializeField] private Renderer[] treeCanopy;
    [SerializeField] private GameObject[] symbolicSeeds;
    [SerializeField] private GameObject completionPanel;
    [SerializeField] private Text title;
    [SerializeField] private Text message;
    [SerializeField] private Button returnButton;
    private MaterialPropertyBlock colors;
    private bool returning;
    public bool SequenceCompleted { get; private set; }

    private void Awake()
    {
        returnButton.onClick.AddListener(ReturnToMainMenu);
        player.enabled = false;
        completionPanel.SetActive(false);
        foreach (GameObject seed in symbolicSeeds) seed.SetActive(false);
        colors = new MaterialPropertyBlock();
        SetLight(0);
    }

    private IEnumerator Start()
    {
        // Opening this scene directly is a safe preview, never a fake victory.
        if (progress == null || progress.CollectedSeedCount != 4)
        {
            title.text = "Macera seni bekliyor";
            message.text = "Işık Ağacı'nı uyandırmak için dört bölgenin ışık tohumlarını bul.";
            completionPanel.SetActive(true);
            yield break;
        }
        for (int i = 0; i < symbolicSeeds.Length; i++)
        {
            symbolicSeeds[i].SetActive(true);
            float elapsed = 0;
            while (elapsed < 1.25f)
            {
                elapsed += Time.unscaledDeltaTime;
                SetLight((i + Mathf.Clamp01(elapsed / 1.25f)) / symbolicSeeds.Length);
                yield return null;
            }
        }
        SetLight(1);
        SequenceCompleted = true;
        completionPanel.SetActive(true);
    }

    private void SetLight(float amount)
    {
        colors.SetColor("_Color", Color.Lerp(new Color(.25f,.32f,.35f), new Color(.67f,1,.52f), amount));
        colors.SetColor("_EmissionColor", new Color(.65f,.86f,.22f) * amount * .7f);
        foreach (Renderer part in treeCanopy) part.SetPropertyBlock(colors);
        RenderSettings.ambientLight = Color.Lerp(new Color(.35f,.38f,.5f), new Color(.8f,.84f,.69f), amount);
    }

    public void ReturnToMainMenu()
    {
        if (returning) return;
        if (!RegionSceneLoader.CanLoad("IsikliVadi"))
        {
            message.text = "Ana menü sahnesi bulunamadı. IsikliVadi sahnesini derlemeye ekleyin.";
            return;
        }
        returning = true;
        returnButton.interactable = false;
        RegionProgressController.ResetAdventure();
        if (RegionSceneLoader.Load("IsikliVadi") == null)
        {
            returning = false;
            returnButton.interactable = true;
        }
    }

    private void OnDestroy()
    {
        if (returnButton != null) returnButton.onClick.RemoveListener(ReturnToMainMenu);
    }
}
