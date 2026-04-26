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

    [Header("UI First Selected Buttons")]
    public GameObject startButton;
    public GameObject winRestartButton;
    public GameObject loseRestartButton;

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
        startPanel.SetActive(true);
        winPanel.SetActive(false);
        losePanel.SetActive(false);
        countdownText.gameObject.SetActive(false);
        timerText.gameObject.SetActive(false);

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
        startPanel.SetActive(false);
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
        trophyUIImage.gameObject.SetActive(false);
        winPanel.SetActive(true);
        


        UpdateTimerDisplay(gameTimer, finalTimeText);

        // ==========================================
        // NEW: ASSIGN THE FINAL TROPHY ON THE WIN SCREEN
        // ==========================================
        if (finalTrophyImage != null)
        {
            if (gameTimer <= goldTimeLimit)
            {
                finalTrophyImage.sprite = goldSprite;
            }
            else if (gameTimer <= silverTimeLimit)
            {
                finalTrophyImage.sprite = silverSprite;
            }
            else
            {
                finalTrophyImage.sprite = bronzeSprite;
            }
        }

        SelectUIObject(winRestartButton);
    }

    public void LoseGame()
    {
        isPlaying = false;
        losePanel.SetActive(true);
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

    private void SelectUIObject(GameObject uiObject)
    {
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