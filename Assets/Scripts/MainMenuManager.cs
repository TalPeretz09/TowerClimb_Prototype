using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Navigation")]
    public GameObject firstSelectedButton; // (Your "Yes" button on the tutorial panel)
    public GameObject firstTimePanel;
    public GameObject mainMenuReturnButton; // (Your primary main menu button, e.g., Tower 1)

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
        // 1. Load trophies right away
        LoadAndDisplayTrophy("Tower1", tower1TrophyImage);
        LoadAndDisplayTrophy("Tower2", tower2TrophyImage);
        LoadAndDisplayTrophy("Tower3", tower3TrophyImage);

        // 2. Check if the player has already answered the tutorial prompt before
        if (PlayerPrefs.GetInt("HasSeenTutorialPrompt", 0) == 1)
        {
            // They've seen it! Force hide the panel immediately
            firstTimePanel.SetActive(false);

            // Directly focus on the main menu button instead
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(mainMenuReturnButton);
        }
        else
        {
            // Brand new player! Make sure the panel is open
            firstTimePanel.SetActive(true);

            // Focus on the panel's "Yes" button
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(firstSelectedButton);
        }
    }

    // Call this if they click "Yes" to play the tutorial
    public void PlayTutorial(string tutorialSceneName)
    {
        MarkTutorialPromptAsSeen();
        SceneManager.LoadScene(tutorialSceneName);
    }

    // Call this if they click "No" to skip the tutorial (or attach it to your No button)
    public void HideFirstTimePanel()
    {
        firstTimePanel.SetActive(false);
        MarkTutorialPromptAsSeen();

        if (mainMenuReturnButton != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(mainMenuReturnButton);
        }
    }

    // Helper method to keep things clean and DRY (Don't Repeat Yourself)
    private void MarkTutorialPromptAsSeen()
    {
        PlayerPrefs.SetInt("HasSeenTutorialPrompt", 1);
        PlayerPrefs.Save();
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
}