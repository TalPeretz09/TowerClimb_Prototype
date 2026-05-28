using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class TechniqueVideoManager : MonoBehaviour
{
    [Header("Video Components")]
    public VideoPlayer videoPlayer;
    public RawImage videoDisplay;

    [Header("Play/Pause UI")]
    public Image playPauseIcon;
    public Sprite playSprite;
    public Sprite pauseSprite;

    private void Awake()
    {
        // Subscribe to the loopPointReached event to execute logic upon video completion.
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoFinished;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from the event listener to prevent memory leaks when the object is destroyed.
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }

    void OnEnable()
    {
        // Ensure the video player is reset to its default state whenever the component is enabled.
        ResetVideoPlayer();
    }

    // ==========================================
    // 1. LOAD VIDEO (WITHOUT AUTOPLAY)
    // ==========================================

    public void LoadAndPlayVideo(VideoClip newClip)
    {
        if (newClip == null) return;

        // Enable the RawImage component to display the video feed.
        if (videoDisplay != null) videoDisplay.enabled = true;

        videoPlayer.clip = newClip;

        // Pause playback immediately to queue the first frame without auto-playing.
        videoPlayer.Pause();
        videoPlayer.frame = 0;

        // Set the UI toggle state to display the play icon.
        playPauseIcon.sprite = playSprite;
    }

    // ==========================================
    // 2. PLAY / PAUSE TOGGLE
    // ==========================================

    public void TogglePlayPause()
    {
        if (videoPlayer.clip == null) return;

        // Toggle between playing and paused states, updating the UI icon accordingly.
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
            playPauseIcon.sprite = playSprite;
        }
        else
        {
            videoPlayer.Play();
            playPauseIcon.sprite = pauseSprite;
        }
    }

    // ==========================================
    // 3. AUTOMATIC RESET ON FINISH
    // ==========================================

    // Event handler triggered when the video reaches its end.
    private void OnVideoFinished(VideoPlayer source)
    {
        source.Pause();                    // Halt playback at the end of the clip.
        source.frame = 0;                  // Reset the timeline to the first frame.
        playPauseIcon.sprite = playSprite; // Revert the UI state to the play icon.
    }

    // ==========================================
    // 4. CLEANUP ON LEAVING MENU
    // ==========================================

    public void ResetVideoPlayer()
    {
        // Stop playback, clear the assigned clip, and reset UI elements to their default states.
        videoPlayer.Stop();
        videoPlayer.clip = null;
        playPauseIcon.sprite = playSprite;

        // Disable the display to ensure a blank state when the menu is reopened.
        if (videoDisplay != null) videoDisplay.enabled = false;
    }
}