using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))] // Automatically ensures an AudioSource is attached to your GameManager
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI Panels")]
    public GameObject startPanel;
    public GameObject winPanel;
    public GameObject losePanel;
    public GameObject techniqueUnlockPanel;

    [Header("UI First Selected Buttons")]
    public GameObject startButton;
    public GameObject winRestartButton;
    public GameObject loseRestartButton;
    public GameObject techniqueYesButton;

    [Header("UI Text Elements")]
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI finalTimeText;

    [Header("Trophy System")]
    public Image trophyUIImage;
    public Image finalTrophyImage;
    public Sprite goldSprite;
    public Sprite silverSprite;
    public Sprite bronzeSprite;
    public float goldTimeLimit = 30f;
    public float silverTimeLimit = 60f;

    [Header("Audio Settings")]
    [Tooltip("Sound played when the player successfully beats the level.")]
    public AudioClip winSound;
    [Tooltip("Sound played when the player dies or fails the level.")]
    public AudioClip loseSound;

    [Header("Game State")]
    public bool isPlaying = false;
    private float gameTimer = 0f;

    private AudioSource audioSource; // Reference to the AudioSource component

    void Awake()
    {
        // Enforce the singleton lifecycle architecture pattern.
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Cache the audio source and prevent it from playing randomly on load
        audioSource = GetComponent<AudioSource>();
        if (audioSource != null) audioSource.playOnAwake = false;
    }

    void Start()
    {
        // Establish baseline initial interface visibility profiles.
        if (startPanel != null) startPanel.SetActive(true);
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (techniqueUnlockPanel != null) techniqueUnlockPanel.SetActive(false);

        if (countdownText != null) countdownText.gameObject.SetActive(false);
        if (timerText != null) timerText.gameObject.SetActive(false);

        SelectUIObject(startButton);
    }

    void Update()
    {
        // Track core gameplay duration metrics asynchronously when the play state is active.
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
        // Sequential state sequence mapping the pre-game countdown frames.
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

        // Play the Win Sound Effect
        if (winSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(winSound);
        }

        if (timerText != null) timerText.gameObject.SetActive(false);
        if (trophyUIImage != null) trophyUIImage.gameObject.SetActive(false);

        UpdateTimerDisplay(gameTimer, finalTimeText);

        // Grade runtime completion speeds into standard tier archetypes.
        int earnedTrophyValue = 1; // Default: Bronze

        if (finalTrophyImage != null)
        {
            if (gameTimer <= goldTimeLimit)
            {
                finalTrophyImage.sprite = goldSprite;
                earnedTrophyValue = 3; // Gold
            }
            else if (gameTimer <= silverTimeLimit)
            {
                finalTrophyImage.sprite = silverSprite;
                earnedTrophyValue = 2; // Silver
            }
            else
            {
                finalTrophyImage.sprite = bronzeSprite;
            }
        }

        // ==========================================
        // PERSISTENCE & DATA EXTRACTION LOGIC
        // ==========================================
        string currentLevelName = SceneManager.GetActiveScene().name;

        int savedTrophyValue = PlayerPrefs.GetInt(currentLevelName + "_Trophy", 0);
        bool isFirstTimeWin = (savedTrophyValue == 0);

        // Commit progression metrics if the current clear tier exceeds historic records.
        if (earnedTrophyValue > savedTrophyValue)
        {
            PlayerPrefs.SetInt(currentLevelName + "_Trophy", earnedTrophyValue);
            PlayerPrefs.Save();
        }

        // ==========================================
        // CANVAS COMPONENT ROUTING PIPELINE
        // ==========================================
        if (isFirstTimeWin && techniqueUnlockPanel != null)
        {
            // Direct new victors straight to the technique unlock dialogue interface.
            techniqueUnlockPanel.SetActive(true);
            SelectUIObject(techniqueYesButton);
        }
        else
        {
            if (winPanel != null) winPanel.SetActive(true);
            SelectUIObject(winRestartButton);
        }
    }

    // ==========================================
    // DEEP-LINK INTERACTION INTERFACES
    // ==========================================
    public void OnTechniqueNoPressed()
    {
        if (techniqueUnlockPanel != null) techniqueUnlockPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(true);
        SelectUIObject(winRestartButton);
    }

    public void OnTechniqueYesPressed()
    {
        // Cache a programmatic routing deep-link flag prior to system context switches.
        PlayerPrefs.SetInt("AutoOpenTechniques", 1);
        PlayerPrefs.Save();

        ReturnToMenu();
    }

    public void LoseGame()
    {
        isPlaying = false;

        // Play the Lose Sound Effect
        if (loseSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(loseSound);
        }

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

        // Isolate context selections fully to support navigation switching across input mechanics.
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(uiObject);
    }

    private void UpdateTrophyDisplay()
    {
        if (trophyUIImage == null) return;

        // Calculate and apply localized delta distributions to step down HUD radial layouts dynamically.
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