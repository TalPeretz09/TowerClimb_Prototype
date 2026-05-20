using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Navigation")]
    public GameObject firstSelectedButton; // (This is currently your Yes button)
    public GameObject firstTimePanel;

    // NEW: A slot to hold the main menu button we return to when closing the panel
    public GameObject mainMenuReturnButton;

    [Header("Level Trophy UI Images")]
    public Image tower1TrophyImage;
    public Image tower2TrophyImage;
    public Image tower3TrophyImage;

    [Header("Menu Trophy Sprites")]
    public Sprite emptyTrophySprite;
    public Sprite bronzeMenuSprite;
    public Sprite silverMenuSprite;
    public Sprite goldMenuSprite;

    void Start()
    {
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstSelectedButton);

        LoadAndDisplayTrophy("Tower1", tower1TrophyImage);
        LoadAndDisplayTrophy("Tower2", tower2TrophyImage);
        LoadAndDisplayTrophy("Tower3", tower3TrophyImage);
    }

    public void LoadLevel(string levelName)
    {
        SceneManager.LoadScene(levelName);
    }

    private void LoadAndDisplayTrophy(string levelName, Image trophyImageToUpdate)
    {
        if (trophyImageToUpdate == null) return;

        int savedTrophy = PlayerPrefs.GetInt(levelName + "_Trophy", 0);

        switch (savedTrophy)
        {
            case 3: trophyImageToUpdate.sprite = goldMenuSprite; break;
            case 2: trophyImageToUpdate.sprite = silverMenuSprite; break;
            case 1: trophyImageToUpdate.sprite = bronzeMenuSprite; break;
            case 0:
            default: trophyImageToUpdate.sprite = emptyTrophySprite; break;
        }
    }

    public void ResetSaveData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // UPDATED: Now safely returns focus to the main menu layout
    public void HideFirstTimePanel()
    {
        firstTimePanel.SetActive(false);

        // NEW: Force the EventSystem to look back at the main menu layout
        if (mainMenuReturnButton != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(mainMenuReturnButton);
        }
    }
}