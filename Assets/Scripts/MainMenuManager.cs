using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Menu Groups (Canvases/Panels)")]
    public GameObject mainMenuGroup;
    public GameObject playGamesGroup;
    public GameObject trainingGroup;
    public GameObject toolsGroup;
    public GameObject techniquesGroup;
    public GameObject firstTimeGroup;

    [Header("Default Selected Buttons (For Controller/Keyboard)")]
    public GameObject mainMenuFirstBTN;     // e.g., PlayGameBTN
    public GameObject playGamesFirstBTN;    // e.g., Level1BTN
    public GameObject trainingFirstBTN;     // e.g., PracticeStageBTN
    public GameObject toolsFirstBTN;        // e.g., UnlockAllBTN
    public GameObject techniquesFirstBTN;   // e.g., ReturnBTN (if empty)
    public GameObject firstTimeFirstBTN;    // Your "Yes" button

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

        // 2. Setup the initial menu state
        if (PlayerPrefs.GetInt("HasSeenTutorialPrompt", 0) == 1)
        {
            // Player has been here before. Skip the prompt and open Main Menu.
            firstTimeGroup.SetActive(false);
            OpenMainMenu();
        }
        else
        {
            // Brand new player! Show the prompt.
            HideAllMenus(); // Keep the background clean
            firstTimeGroup.SetActive(true);
            SetControllerFocus(firstTimeFirstBTN);
        }
    }

    // ==========================================
    // MODULAR MENU NAVIGATION
    // ==========================================

    // Use these public methods to hook up your UI Button OnClick() events in the Inspector

    public void OpenMainMenu()
    {
        SwitchToMenu(mainMenuGroup, mainMenuFirstBTN);
    }

    public void OpenPlayGames()
    {
        SwitchToMenu(playGamesGroup, playGamesFirstBTN);
    }

    public void OpenTraining()
    {
        SwitchToMenu(trainingGroup, trainingFirstBTN);
    }

    public void OpenTools()
    {
        SwitchToMenu(toolsGroup, toolsFirstBTN);
    }

    public void OpenTechniques()
    {
        SwitchToMenu(techniquesGroup, techniquesFirstBTN);
    }

    // ==========================================
    // CORE SYSTEM HELPERS
    // ==========================================

    // Automatically shuts off all screens, turns on the target screen, and snaps controller focus.
    private void SwitchToMenu(GameObject menuToOpen, GameObject buttonToFocus)
    {
        HideAllMenus();

        if (menuToOpen != null)
        {
            menuToOpen.SetActive(true);
        }

        SetControllerFocus(buttonToFocus);
    }

    // Ensures we never have two menus overlapping by accident
    private void HideAllMenus()
    {
        if (mainMenuGroup) mainMenuGroup.SetActive(false);
        if (playGamesGroup) playGamesGroup.SetActive(false);
        if (trainingGroup) trainingGroup.SetActive(false);
        if (toolsGroup) toolsGroup.SetActive(false);
        if (techniquesGroup) techniquesGroup.SetActive(false);
    }

    private void SetControllerFocus(GameObject targetButton)
    {
        if (targetButton != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(targetButton);
        }
    }

    // ==========================================
    // FIRST TIME PROMPT LOGIC
    // ==========================================

    public void PlayTutorial(string tutorialSceneName)
    {
        MarkTutorialPromptAsSeen();
        SceneManager.LoadScene(tutorialSceneName);
    }

    public void HideFirstTimePanel()
    {
        firstTimeGroup.SetActive(false);
        MarkTutorialPromptAsSeen();
        OpenMainMenu(); // Cleanly transitions to the main layout
    }

    private void MarkTutorialPromptAsSeen()
    {
        PlayerPrefs.SetInt("HasSeenTutorialPrompt", 1);
        PlayerPrefs.Save();
    }

    // ==========================================
    // GAMEPLAY & TOOL LOGIC
    // ==========================================

    public void LoadScene(string levelName)
    {
        SceneManager.LoadScene(levelName);
    }

    public void ResetSaveData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Placeholder for your Unlock All functionality
    public void UnlockAll()
    {
        Debug.Log("Unlock All clicked! Add your unlock logic here.");
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
}