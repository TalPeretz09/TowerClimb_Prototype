using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems; // NEW: Required to talk to the UI EventSystem

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI Panels")]
    public GameObject startPanel;
    public GameObject winPanel;
    public GameObject losePanel;

    [Header("UI First Selected Buttons")] // NEW: Slots for your buttons
    public GameObject startButton;
    public GameObject winRestartButton;
    public GameObject loseRestartButton;

    [Header("UI Text Elements")]
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI finalTimeText;

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
        startPanel.SetActive(true);
        winPanel.SetActive(false);
        losePanel.SetActive(false);
        countdownText.gameObject.SetActive(false);
        timerText.gameObject.SetActive(false);

        // NEW: Tell the controller to focus on the Start Button immediately
        SelectUIObject(startButton);
    }

    void Update()
    {
        if (isPlaying)
        {
            gameTimer += Time.deltaTime;
            UpdateTimerDisplay(gameTimer, timerText);
        }
    }

    public void StartGameSequence()
    {
        startPanel.SetActive(false);

        // NEW: Clear the UI selection so the player can't accidentally press buttons while playing
        EventSystem.current.SetSelectedGameObject(null);

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

        timerText.gameObject.SetActive(true);
        gameTimer = 0f;
        isPlaying = true;
    }

    public void WinGame()
    {
        if (!isPlaying) return;

        isPlaying = false;
        timerText.gameObject.SetActive(false);
        winPanel.SetActive(true);

        UpdateTimerDisplay(gameTimer, finalTimeText);

        // NEW: Tell the controller to focus on the Restart button
        SelectUIObject(winRestartButton);
    }

    public void LoseGame()
    {
        isPlaying = false;
        losePanel.SetActive(true);

        // NEW: Tell the controller to focus on the Restart button
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
        int minutes = Mathf.FloorToInt(time / 60F);
        int seconds = Mathf.FloorToInt(time % 60F);

        textElement.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // NEW: Helper function to safely hand control to a specific UI element
    private void SelectUIObject(GameObject uiObject)
    {
        // Always clear the current selection first. This ensures the EventSystem 
        // registers the change, even if it thinks it was already selecting something.
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(uiObject);
    }
}