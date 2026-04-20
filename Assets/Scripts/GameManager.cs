using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI Panels")]
    public GameObject startPanel;
    public GameObject winPanel;

    [Header("UI Text Elements")]
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI finalTimeText;

    [Header("Game State")]
    public bool isPlaying = false;
    private float gameTimer = 0f;

    void Awake()
    {
        // Simple Singleton setup so the PlayerController can find this easily
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Setup initial UI state
        startPanel.SetActive(true);
        winPanel.SetActive(false);
        countdownText.gameObject.SetActive(false);
        timerText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (isPlaying)
        {
            gameTimer += Time.deltaTime;
            UpdateTimerDisplay(gameTimer, timerText);
        }
    }

    // Called by the UI "Start Button"
    public void StartGameSequence()
    {
        startPanel.SetActive(false);
        StartCoroutine(CountdownRoutine());
    }

    IEnumerator CountdownRoutine()
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

        // Start the game!
        timerText.gameObject.SetActive(true);
        gameTimer = 0f;
        isPlaying = true;
    }

    public void WinGame()
    {
        if (!isPlaying) return; // Prevent triggering multiple times

        isPlaying = false;
        timerText.gameObject.SetActive(false);
        winPanel.SetActive(true);

        UpdateTimerDisplay(gameTimer, finalTimeText);
    }

    // Called by the UI "Restart Button"
    public void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Helper function to format the time nicely (MM:SS)
    private void UpdateTimerDisplay(float time, TextMeshProUGUI textElement)
    {
        int minutes = Mathf.FloorToInt(time / 60F);
        int seconds = Mathf.FloorToInt(time % 60F); // Using modulo (%) is a cleaner way to get remaining seconds

        textElement.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}