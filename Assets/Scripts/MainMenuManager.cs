using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement; // NEW: Required to change scenes

public class MainMenuManager : MonoBehaviour
{
    [Header("Navigation")]
    public GameObject firstSelectedButton; // Drag your Tower1 Button here

    void Start()
    {
        // Focus the controller on the first button when the Main Menu loads
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(firstSelectedButton);
    }

    // NEW: The UI Buttons will call this function and pass in the name of the scene
    public void LoadLevel(string levelName)
    {
        SceneManager.LoadScene(levelName);
    }
}