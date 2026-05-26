using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI Panels")]
    public GameObject startPanel;
    public GameObject winPanel;
    public GameObject losePanel;
    public GameObject techniqueUnlockPanel; // NEW: The technique popup panel

    [Header("UI First Selected Buttons")]
    public GameObject startButton;
    public GameObject winRestartButton;
    public GameObject loseRestartButton;
    public GameObject techniqueYesButton;   // NEW: Highlighted button for technique panel

    [Header("UI Text Elements")]
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI finalTimeText;

    [Header("Trophy System")]
    public Image trophyUIImage; // The HUD shrinking trophy
    public Image finalTrophyImage; // NEW: The final trophy shown on the Win Panel
    public Sprite goldSprite;
    public Sprite silverSprite;
    public Sprite bronzeSprite;
    public float goldTimeLimit = 30f;
    public float silverTimeLimit = 60f;

    [Header("Game State")]
    public bool isPlaying = false;
    private float gameTimer = 0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (startPanel != null) startPanel.SetActive(true);
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (techniqueUnlockPanel != null) techniqueUnlockPanel.SetActive(false); // Ensure panel starts closed

        // Null checks added here
        if (countdownText != null) countdownText.gameObject.SetActive(false);
        if (timerText != null) timerText.gameObject.SetActive(false);

        SelectUIObject(startButton);
    }

    void Update()
    {
        if (isPlaying)
        {
            gameTimer += Time.deltaTime;
            UpdateTimerDisplay(gameTimer, timerText);
            UpdateTrophyDisplay();
        }
    }

    public void StartGameSequence()
    {
        if (startPanel != null) startPanel.SetActive(false);
        EventSystem.current.SetSelectedGameObject(null);
        StartCoroutine(CountdownRoutine());
    }

    IEnumerator CountdownRoutine()
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);

            countdownText.text = "3";
            yield return new WaitForSeconds(1f);

            countdownText.text = "2";
            yield return new WaitForSeconds(1f);

            countdownText.text = "1";
            yield return new WaitForSeconds(1f);

            countdownText.text = "GO!";
            yield return new WaitForSeconds(1f);

            countdownText.gameObject.SetActive(false);
        }
        else
        {
            // If there's no countdown text, just wait a brief moment before starting
            yield return new WaitForSeconds(1f);
        }

        if (timerText != null) timerText.gameObject.SetActive(true);
        gameTimer = 0f;
        isPlaying = true;
    }

    public void WinGame()
    {
        if (!isPlaying) return;

        isPlaying = false;

        // Null checks added here
        if (timerText != null) timerText.gameObject.SetActive(false);
        if (trophyUIImage != null) trophyUIImage.gameObject.SetActive(false);

        UpdateTimerDisplay(gameTimer, finalTimeText);

        // Figure out which trophy they earned as a number
        int earnedTrophyValue = 1; // Default to Bronze (1)

        if (finalTrophyImage != null)
        {
            if (gameTimer <= goldTimeLimit)
            {
                finalTrophyImage.sprite = goldSprite;
                earnedTrophyValue = 3; // Gold (3)
            }
            else if (gameTimer <= silverTimeLimit)
            {
                finalTrophyImage.sprite = silverSprite;
                earnedTrophyValue = 2; // Silver (2)
            }
            else
            {
                finalTrophyImage.sprite = bronzeSprite;
                // Remains Bronze (1)
            }
        }

        // ==========================================
        // CHECK FIRST TIME WIN & SAVE DATA
        // ==========================================
        string currentLevelName = SceneManager.GetActiveScene().name;

        // Look up the previously saved trophy (defaults to 0 if they haven't played/won yet)
        int savedTrophyValue = PlayerPrefs.GetInt(currentLevelName + "_Trophy", 0);
        bool isFirstTimeWin = (savedTrophyValue == 0);

        // If the trophy they just got is better than their saved one, save it
        if (earnedTrophyValue > savedTrophyValue)
        {
            PlayerPrefs.SetInt(currentLevelName + "_Trophy", earnedTrophyValue);
            PlayerPrefs.Save();
        }

        // ==========================================
        // PANEL ROUTING LOGIC
        // ==========================================
        if (isFirstTimeWin && techniqueUnlockPanel != null)
        {
            // First time completing! Route to the Technique screen instead of the win panel
            techniqueUnlockPanel.SetActive(true);
            SelectUIObject(techniqueYesButton);
        }
        else
        {
            // Regular win sequence
            if (winPanel != null) winPanel.SetActive(true);
            SelectUIObject(winRestartButton);
        }
    }

    // ==========================================
    // NEW: TECHNIQUE PANEL BUTTON INTERFACES
    // ==========================================
    public void OnTechniqueNoPressed()
    {
        if (techniqueUnlockPanel != null) techniqueUnlockPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(true);
        SelectUIObject(winRestartButton);
    }

    public void OnTechniqueYesPressed()
    {
        // Drop a navigation flag so the main menu knows to skip straight to techniques
        PlayerPrefs.SetInt("AutoOpenTechniques", 1);
        PlayerPrefs.Save();

        ReturnToMenu();
    }

    public void LoseGame()
    {
        isPlaying = false;

        // Null checks added here
        if (timerText != null) timerText.gameObject.SetActive(false);
        if (trophyUIImage != null) trophyUIImage.gameObject.SetActive(false);

        if (losePanel != null) losePanel.SetActive(true);
        SelectUIObject(loseRestartButton);
    }

    public void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    private void UpdateTimerDisplay(float time, TextMeshProUGUI textElement)
    {
        if (textElement == null) return;

        int minutes = Mathf.FloorToInt(time / 60F);
        int seconds = Mathf.FloorToInt(time % 60F);

        textElement.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void SelectUIObject(GameObject uiObject)
    {
        if (uiObject == null) return;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(uiObject);
    }

    private void UpdateTrophyDisplay()
    {
        if (trophyUIImage == null) return;

        if (gameTimer <= goldTimeLimit)
        {
            trophyUIImage.sprite = goldSprite;
            trophyUIImage.fillAmount = 1f - (gameTimer / goldTimeLimit);
        }
        else if (gameTimer <= silverTimeLimit)
        {
            trophyUIImage.sprite = silverSprite;
            float silverDuration = silverTimeLimit - goldTimeLimit;
            float timeInSilver = gameTimer - goldTimeLimit;
            trophyUIImage.fillAmount = 1f - (timeInSilver / silverDuration);
        }
        else
        {
            trophyUIImage.sprite = bronzeSprite;
            trophyUIImage.fillAmount = 1f;
        }
    }
}