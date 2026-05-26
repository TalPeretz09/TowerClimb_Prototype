using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    // A custom data structure to pair each level button with its child components
    [System.Serializable]
    public class LevelButtonData
    {
        public string levelName;          // Must match the exact Unity Scene name (e.g., "Tower1")
        public Button buttonComponent;    // The Button component itself to turn clicking on/off
        public GameObject levelText;      // Child object: The level number/name text
        public GameObject trophyObject;   // Child object: The trophy Image object
        public GameObject lockImage;      // Child object: The lock icon Image object
    }

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

    [Header("Level Progression System")]
    [Tooltip("Add your level buttons here in consecutive order (Level 1, Level 2, Level 3...)")]
    public LevelButtonData[] levels;

    [Header("Menu Trophy Sprites")]
    public Sprite emptyTrophySprite;
    public Sprite bronzeMenuSprite;
    public Sprite silverMenuSprite;
    public Sprite goldMenuSprite;

    void Start()
    {
        // 1. Process progression and update child objects dynamically
        UpdateLevelProgression();

        // 2. Setup the initial menu state or handle incoming redirects
        if (PlayerPrefs.GetInt("AutoOpenTechniques", 0) == 1)
        {
            PlayerPrefs.SetInt("AutoOpenTechniques", 0);
            PlayerPrefs.Save();

            if (firstTimeGroup != null) firstTimeGroup.SetActive(false);
            OpenTechniques();
        }
        else if (PlayerPrefs.GetInt("HasSeenTutorialPrompt", 0) == 1)
        {
            if (firstTimeGroup != null) firstTimeGroup.SetActive(false);
            OpenMainMenu();
        }
        else
        {
            HideAllMenus();
            if (firstTimeGroup != null) firstTimeGroup.SetActive(true);
            SetControllerFocus(firstTimeFirstBTN);
        }
    }

    // ==========================================
    // PROGRESSION LOCK/UNLOCK SYSTEM
    // ==========================================
    private void UpdateLevelProgression()
    {
        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] == null) continue;

            bool isUnlocked = false;

            if (i == 0)
            {
                // The very first level button in the list is always unlocked by default
                isUnlocked = true;
            }
            else
            {
                // Look at the PREVIOUS level in the array
                string previousLevelName = levels[i - 1].levelName;
                int previousLevelTrophy = PlayerPrefs.GetInt(previousLevelName + "_Trophy", 0);

                // If previous level has a trophy score higher than 0, it means it was beaten!
                isUnlocked = (previousLevelTrophy > 0);
            }

            // Apply the interactivity and child visibility states
            if (levels[i].buttonComponent != null) levels[i].buttonComponent.interactable = isUnlocked;
            if (levels[i].levelText != null) levels[i].levelText.SetActive(isUnlocked);
            if (levels[i].lockImage != null) levels[i].lockImage.SetActive(!isUnlocked);

            if (levels[i].trophyObject != null)
            {
                levels[i].trophyObject.SetActive(isUnlocked);

                // If the level is unlocked, safely load whatever trophy sprite belongs here
                if (isUnlocked)
                {
                    Image trophyImageComponent = levels[i].trophyObject.GetComponent<Image>();
                    if (trophyImageComponent != null)
                    {
                        LoadAndDisplayTrophy(levels[i].levelName, trophyImageComponent);
                    }
                }
            }
        }
    }

    // ==========================================
    // MODULAR MENU NAVIGATION
    // ==========================================

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

    private void SwitchToMenu(GameObject menuToOpen, GameObject buttonToFocus)
    {
        HideAllMenus();

        if (menuToOpen != null)
        {
            menuToOpen.SetActive(true);
        }

        SetControllerFocus(buttonToFocus);
    }

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
        if (firstTimeGroup != null) firstTimeGroup.SetActive(false);
        MarkTutorialPromptAsSeen();
        OpenMainMenu();
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
        // Only allow loading the scene if the level is actually unlocked
        // (Prevents bypassing via keyboard shortcuts/hacks if button is visually disabled)
        if (IsLevelUnlocked(levelName))
        {
            SceneManager.LoadScene(levelName);
        }
        else
        {
            Debug.LogWarning("Attempted to load locked level: " + levelName);
        }
    }

    private bool IsLevelUnlocked(string levelName)
    {
        // Find the index of this level in our system
        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i].levelName == levelName)
            {
                if (i == 0) return true; // Level 1 is always open

                string previousLevelName = levels[i - 1].levelName;
                return PlayerPrefs.GetInt(previousLevelName + "_Trophy", 0) > 0;
            }
        }
        return false;
    }

    public void ResetSaveData()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void UnlockAll()
    {
        // Give every level context a mock trophy entry so they all open up
        for (int i = 0; i < levels.Length; i++)
        {
            PlayerPrefs.SetInt(levels[i].levelName + "_Trophy", 1);
        }
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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