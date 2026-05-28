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

    // ==========================================
    // MENU HIERARCHY GROUPS
    // ==========================================
    [Header("Menu Groups (Canvases/Panels)")]
    public GameObject mainMenuGroup;
    public GameObject playGamesGroup;
    public GameObject trainingGroup;
    public GameObject toolsGroup;
    public GameObject techniquesGroup;
    public GameObject firstTimeGroup;

    // ==========================================
    // UI NAVIGATION FOCUS TARGETS
    // ==========================================
    [Header("Default Selected Buttons")]
    public GameObject mainMenuFirstBTN;
    public GameObject playGamesFirstBTN;
    public GameObject trainingFirstBTN;
    public GameObject toolsFirstBTN;
    public GameObject techniquesFirstBTN;
    public GameObject firstTimeFirstBTN;

    // ==========================================
    // PROGRESSION & DATABASES
    // ==========================================
    [Header("Level Progression System")]
    public LevelButtonData[] levels;

    [Header("Technique System Link")]
    [Tooltip("Drag the GameObject with your TechniqueUnlockManager here")]
    public TechniqueUnlockManager techniqueUnlockManager;

    [Header("Menu Trophy Sprites")]
    public Sprite emptyTrophySprite;
    public Sprite bronzeMenuSprite;
    public Sprite silverMenuSprite;
    public Sprite goldMenuSprite;

    void Start()
    {
        // Evaluate level states based on disk data before rendering the screen.
        UpdateLevelProgression();

        // Router logic to automatically direct players to the correct deep-linked interface on startup.
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
        // Global administrative toggle that overrides standard progression rules for sandboxing.
        bool overrideUnlockAll = PlayerPrefs.GetInt("UnlockAllOverride", 0) == 1;

        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] == null) continue;

            bool isUnlocked = false;

            // Level 1 is accessible by default. Subsequent levels require a valid trophy on the prior stage.
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

            // Sync structural UI visibility to match calculated access permissions.
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
    // MODULAR MENU NAVIGATION INTERFACES
    // ==========================================
    public void OpenMainMenu() => SwitchToMenu(mainMenuGroup, mainMenuFirstBTN);
    public void OpenPlayGames() => SwitchToMenu(playGamesGroup, playGamesFirstBTN);
    public void OpenTraining() => SwitchToMenu(trainingGroup, trainingFirstBTN);
    public void OpenTools() => SwitchToMenu(toolsGroup, toolsFirstBTN);

    public void OpenTechniques()
    {
        SwitchToMenu(techniquesGroup, techniquesFirstBTN);

        // Force a UI canvas validation refresh to account for newly registered unlock states.
        if (techniqueUnlockManager != null)
        {
            techniqueUnlockManager.RefreshUI();
        }
    }

    // ==========================================
    // CORE SYSTEM SUBSYSTEM HELPERS
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
            // Fully purge the event selection context before assigning a new target.
            // This prevents gamepad state desynchronization or visual ghost-highlight highlights.
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(targetButton);
        }
    }

    // ==========================================
    // FIRST TIME PROMPT INTERACTION
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
    // LEVEL LOAD & ADMINISTRATIVE TOOLS
    // ==========================================
    public void LoadLevel(string levelName)
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

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    private bool IsLevelUnlocked(string levelName)
    {
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
        // Flush all persistent profile parameters from internal storage and reload runtime context.
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void UnlockAll()
    {
        // 1. Force state flags to mock unlock states without polluting high scores with unearned trophies.
        PlayerPrefs.SetInt("UnlockAllOverride", 1);

        // 2. Map structural database iterations directly into static subsystem dependencies.
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

        // Hard reset the interface hierarchy layout to cascade changes through standard rendering loops.
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void LoadAndDisplayTrophy(string levelName, Image trophyImageToUpdate)
    {
        if (trophyImageToUpdate == null) return;

        int savedTrophy = PlayerPrefs.GetInt(levelName + "_Trophy", 0);

        // Map discrete grading boundaries cleanly to configured editor sprites.
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