using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class TechniqueVideoManager : MonoBehaviour
{
    [Header("Video Components")]
    public VideoPlayer videoPlayer;

    [Header("Play/Pause UI")]
    public Image playPauseIcon; // The child image component on your button
    public Sprite playSprite;
    public Sprite pauseSprite;

    // Optional: Hide the raw image if no video is loaded so it's not a black square
    public RawImage videoDisplay;

    void OnEnable()
    {
        // Whenever this menu opens, make sure everything is reset and clean
        ResetVideoPlayer();
    }

    // ==========================================
    // 1. LOAD & PLAY NEW VIDEO
    // ==========================================

    // Hook this up to each of your 7 Technique Buttons!
    public void LoadAndPlayVideo(VideoClip newClip)
    {
        if (newClip == null) return;

        // Turn on the screen if you had it hidden
        if (videoDisplay != null) videoDisplay.enabled = true;

        // Load the new clip and force it to play
        videoPlayer.clip = newClip;
        videoPlayer.Play();

        // Update the UI icon to show the Pause symbol
        playPauseIcon.sprite = pauseSprite;
    }

    // ==========================================
    // 2. PLAY / PAUSE TOGGLE
    // ==========================================

    // Hook this up to your Play/Pause Button
    public void TogglePlayPause()
    {
        // Don't do anything if they haven't selected a technique yet
        if (videoPlayer.clip == null) return;

        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
            playPauseIcon.sprite = playSprite; // Swap to Triangle
        }
        else
        {
            videoPlayer.Play();
            playPauseIcon.sprite = pauseSprite; // Swap to Bars
        }
    }

    // ==========================================
    // 3. CLEANUP
    // ==========================================

    // Hook this up to your Return Button so the video stops when leaving the menu
    public void ResetVideoPlayer()
    {
        videoPlayer.Stop();
        videoPlayer.clip = null;
        playPauseIcon.sprite = playSprite;

        // Hide the screen so it looks clean before a technique is picked
        if (videoDisplay != null) videoDisplay.enabled = false;
    }
}