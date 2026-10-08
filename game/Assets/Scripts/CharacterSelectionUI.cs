using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectionUI : MonoBehaviour
{
    [Header("Bağlantılar")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private GameObject selectionRoot;
    [SerializeField] private MainMenuUI mainMenuUI;
    [SerializeField] private IntroVideoUI introVideoUI;
    [SerializeField] private Button maleButton;
    [SerializeField] private Button femaleButton;
    [SerializeField] private Button backButton;

    private void Awake()
    {
        maleButton.onClick.AddListener(SelectMaleCharacter);
        femaleButton.onClick.AddListener(SelectFemaleCharacter);
        backButton.onClick.AddListener(ReturnToMainMenu);
    }

    private void OnDestroy()
    {
        if (maleButton != null)
        {
            maleButton.onClick.RemoveListener(SelectMaleCharacter);
        }

        if (femaleButton != null)
        {
            femaleButton.onClick.RemoveListener(SelectFemaleCharacter);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(ReturnToMainMenu);
        }
    }

    public void ShowSelection()
    {
        if (!HasRequiredReferences())
        {
            Debug.LogError("CharacterSelectionUI bağlantıları eksik.");
            return;
        }

        gameObject.SetActive(true);
        selectionRoot.SetActive(true);
        SetPlayerControl(false);
    }

    private void SelectMaleCharacter()
    {
        CompleteSelection(SelectedCharacterState.Male);
    }

    private void SelectFemaleCharacter()
    {
        CompleteSelection(SelectedCharacterState.Female);
    }

    private void CompleteSelection(string characterId)
    {
        SelectedCharacterState.SelectCharacter(characterId);
        selectionRoot.SetActive(false);
        gameObject.SetActive(false);
        introVideoUI.PlaySelectedCharacterIntro();
    }

    private void ReturnToMainMenu()
    {
        selectionRoot.SetActive(false);
        gameObject.SetActive(false);
        mainMenuUI.ShowMainMenu();
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
               selectionRoot != null &&
               mainMenuUI != null &&
               introVideoUI != null &&
               maleButton != null &&
               femaleButton != null &&
               backButton != null;
    }
}
