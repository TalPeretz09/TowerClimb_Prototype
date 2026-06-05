using UnityEngine;

public class MobilePerformanceManager : MonoBehaviour
{
    private static MobilePerformanceManager instance;

    [Header("Frame Rate Settings")]
    [Tooltip("Target frame rate for mobile. 60 is standard, 30 can save battery.")]
    [SerializeField] private int targetFPS = 60;

    void Awake()
    {
        // 1. Safeguard: If an instance already exists in the game, destroy this new one.
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // 2. Set this object as the permanent instance and protect it from being deleted.
        instance = this;
        DontDestroyOnLoad(gameObject);

        // 3. Optimize the frame rate for mobile performance
        OptimizeFrameRate();
    }

    private void OptimizeFrameRate()
    {
        // On mobile, vSync must be disabled (0) for custom targetFrameRates to work properly.
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFPS;

        Debug.Log($"Performance Manager Initialized. Target FPS set to: {targetFPS}");
    }
}