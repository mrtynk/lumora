using UnityEngine;

public static class SelectedCharacterState
{
    public const string Male = "male";
    public const string Female = "female";

    private const string PlayerPrefsKey = "lumora_selected_character";

    private static string selectedCharacter =
        PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);

    public static string SelectedCharacter => selectedCharacter;
    public static bool HasSelection =>
        selectedCharacter == Male || selectedCharacter == Female;

    public static void SelectCharacter(string characterId)
    {
        if (characterId != Male && characterId != Female)
        {
            Debug.LogError("Geçersiz Lumora karakter seçimi: " + characterId);
            return;
        }

        selectedCharacter = characterId;
        PlayerPrefs.SetString(PlayerPrefsKey, selectedCharacter);
        PlayerPrefs.Save();
        Debug.Log("Seçilen Lumora karakteri: " + selectedCharacter);
    }
}
