using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("Bağlantılar")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private CharacterSelectionUI characterSelectionUI;
    [SerializeField] private Button startButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Text informationText;

    private void Awake()
    {
        startButton.onClick.AddListener(StartNewGame);
        continueButton.onClick.AddListener(ShowContinueInformation);
        settingsButton.onClick.AddListener(ShowSettingsInformation);
    }

    private void Start()
    {
        if (!HasRequiredReferences())
        {
            Debug.LogError("MainMenuUI bağlantıları eksik.");
            return;
        }

        ShowMainMenu();
    }

    private void OnDestroy()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(StartNewGame);
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(ShowContinueInformation);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(ShowSettingsInformation);
        }
    }

    public void ShowMainMenu()
    {
        menuRoot.SetActive(true);
        informationText.text = string.Empty;
        SetPlayerControl(false);
    }

    private void StartNewGame()
    {
        menuRoot.SetActive(false);
        characterSelectionUI.ShowSelection();
    }

    private void ShowContinueInformation()
    {
        informationText.text =
            "Devam Et özelliği sonraki geliştirmede kullanılacak.";
    }

    private void ShowSettingsInformation()
    {
        informationText.text =
            "Ayarlar ekranı sonraki geliştirmede eklenecek.";
    }

    private void SetPlayerControl(bool isEnabled)
    {
        if (playerController != null)
        {
            playerController.enabled = isEnabled;
        }
    }

    private bool HasRequiredReferences()
    {
        return playerController != null &&
               menuRoot != null &&
               characterSelectionUI != null &&
               startButton != null &&
               continueButton != null &&
               settingsButton != null &&
               informationText != null;
    }
}
