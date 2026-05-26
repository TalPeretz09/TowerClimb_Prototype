using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [System.Serializable]
    public class LevelButtonData
    {
        public string levelName;
        public Button buttonComponent;
        public GameObject levelText;
        public GameObject trophyObject;
        public GameObject lockImage;
    }

    [Header("Menu Groups (Canvases/Panels)")]
    public GameObject mainMenuGroup;
    public GameObject playGamesGroup;
    public GameObject trainingGroup;
    public GameObject toolsGroup;
    public GameObject techniquesGroup;
    public GameObject firstTimeGroup;

    [Header("Default Selected Buttons")]
    public GameObject mainMenuFirstBTN;
    public GameObject playGamesFirstBTN;
    public GameObject trainingFirstBTN;
    public GameObject toolsFirstBTN;
    public GameObject techniquesFirstBTN;
    public GameObject firstTimeFirstBTN;

    [Header("Level Progression System")]
    public LevelButtonData[] levels;

    [Header("Technique System Link")]
    [Tooltip("Drag the GameObject with your TechniqueUnlockManager here")]
    public TechniqueUnlockManager techniqueUnlockManager; // NEW: Reference to your existing manager

    [Header("Menu Trophy Sprites")]
    public Sprite emptyTrophySprite;
    public Sprite bronzeMenuSprite;
    public Sprite silverMenuSprite;
    public Sprite goldMenuSprite;

    void Start()
    {
        UpdateLevelProgression();

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
        // NEW: Check if the player has clicked "Unlock All"
        bool overrideUnlockAll = PlayerPrefs.GetInt("UnlockAllOverride", 0) == 1;

        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] == null) continue;

            bool isUnlocked = false;

            // If it's Level 1 OR the player used the Unlock All tool, it opens up
            if (i == 0 || overrideUnlockAll)
            {
                isUnlocked = true;
            }
            else
            {
                string previousLevelName = levels[i - 1].levelName;
                int previousLevelTrophy = PlayerPrefs.GetInt(previousLevelName + "_Trophy", 0);
                isUnlocked = (previousLevelTrophy > 0);
            }

            if (levels[i].buttonComponent != null) levels[i].buttonComponent.interactable = isUnlocked;
            if (levels[i].levelText != null) levels[i].levelText.SetActive(isUnlocked);
            if (levels[i].lockImage != null) levels[i].lockImage.SetActive(!isUnlocked);

            if (levels[i].trophyObject != null)
            {
                levels[i].trophyObject.SetActive(isUnlocked);

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

        // Optionally: Refresh techniques UI when opening the menu
        if (techniqueUnlockManager != null)
        {
            techniqueUnlockManager.RefreshUI();
        }
    }

    // ==========================================
    // CORE SYSTEM HELPERS
    // ==========================================

    private void SwitchToMenu(GameObject menuToOpen, GameObject buttonToFocus)
    {
        HideAllMenus();
        if (menuToOpen != null) menuToOpen.SetActive(true);
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
        // Check the master override first
        if (PlayerPrefs.GetInt("UnlockAllOverride", 0) == 1) return true;

        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i].levelName == levelName)
            {
                if (i == 0) return true;

                string previousLevelName = levels[i - 1].levelName;
                return PlayerPrefs.GetInt(previousLevelName + "_Trophy", 0) > 0;
            }
        }
        return false;
    }

    public void ResetSaveData()
    {
        PlayerPrefs.DeleteAll(); // This also clears the new UnlockAllOverride
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void UnlockAll()
    {
        // 1. Activate the master override for level buttons (keeps trophy scores at 0)
        PlayerPrefs.SetInt("UnlockAllOverride", 1);

        // 2. Unlock all techniques using your existing static method
        if (techniqueUnlockManager != null)
        {
            foreach (var tech in techniqueUnlockManager.techniques)
            {
                TechniqueUnlockManager.UnlockTechnique(tech.techniqueId);
            }
        }
        else
        {
            Debug.LogWarning("TechniqueUnlockManager is not assigned in MainMenuManager!");
        }

        PlayerPrefs.Save();

        // Reload scene to visually update all locks, tags, and buttons
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
            default: trophyImageToUpdate.sprite = emptyTrophySprite; break; // This now correctly fires for Unlock All!
        }
    }
}