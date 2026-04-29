using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // NEW: Required for UI Images

public class MainMenuManager : MonoBehaviour
{
    [Header("Navigation")]
    public GameObject firstSelectedButton;

    [Header("Level Trophy UI Images")]
    public Image tower1TrophyImage; // The Image component next to Tower 1 button
    public Image tower2TrophyImage; // The Image component next to Tower 2 button
    public Image tower3TrophyImage; // The Image component next to Tower 3 button

    [Header("Menu Trophy Sprites")]
    public Sprite emptyTrophySprite;
    public Sprite bronzeMenuSprite;
    public Sprite silverMenuSprite;
    public Sprite goldMenuSprite;

    void Start()
    {
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstSelectedButton);

        // NEW: Update all three level trophies as soon as the menu loads!
        // We pass in the exact names of your scenes so it looks up the right save data.
        LoadAndDisplayTrophy("Tower1", tower1TrophyImage);
        LoadAndDisplayTrophy("Tower2", tower2TrophyImage);
        LoadAndDisplayTrophy("Tower3", tower3TrophyImage);
    }

    public void LoadLevel(string levelName)
    {
        SceneManager.LoadScene(levelName);
    }

    // NEW: Helper function to read the save data and swap the image
    private void LoadAndDisplayTrophy(string levelName, Image trophyImageToUpdate)
    {
        if (trophyImageToUpdate == null) return;

        // Retrieve the saved integer for this level. If it doesn't exist yet, it returns 0.
        int savedTrophy = PlayerPrefs.GetInt(levelName + "_Trophy", 0);

        // Swap the sprite based on the number
        switch (savedTrophy)
        {
            case 3:
                trophyImageToUpdate.sprite = goldMenuSprite;
                break;
            case 2:
                trophyImageToUpdate.sprite = silverMenuSprite;
                break;
            case 1:
                trophyImageToUpdate.sprite = bronzeMenuSprite;
                break;
            case 0:
            default:
                trophyImageToUpdate.sprite = emptyTrophySprite;
                break;
        }
    }

    // Wipes all save data and refreshes the menu
    public void ResetSaveData()
    {
        // 1. Delete all saved PlayerPrefs
        PlayerPrefs.DeleteAll();

        // 2. Force Unity to save that wipe to the hard drive instantly
        PlayerPrefs.Save();

        // 3. Reload the Main Menu scene so the trophies visually disappear
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}