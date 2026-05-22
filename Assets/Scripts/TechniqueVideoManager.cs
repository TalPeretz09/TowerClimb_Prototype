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
        // Tell Unity to run our "OnVideoFinished" function the exact moment any video ends
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoFinished;
        }
    }

    private void OnDestroy()
    {
        // Clean up the event listener when the script is destroyed to avoid memory leaks
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }

    void OnEnable()
    {
        ResetVideoPlayer();
    }

    // ==========================================
    // 1. LOAD VIDEO (WITHOUT AUTOPLAY)
    // ==========================================

    // Kept the method name the same so you don't have to re-link your buttons!
    public void LoadAndPlayVideo(VideoClip newClip)
    {
        if (newClip == null) return;

        if (videoDisplay != null) videoDisplay.enabled = true;

        // Assign the clip
        videoPlayer.clip = newClip;

        // Pause immediately so it cues up the first frame but doesn't play
        videoPlayer.Pause();
        videoPlayer.frame = 0;

        // Keep the icon as the Play triangle since the video is waiting
        playPauseIcon.sprite = playSprite;
    }

    // ==========================================
    // 2. PLAY / PAUSE TOGGLE
    // ==========================================

    public void TogglePlayPause()
    {
        if (videoPlayer.clip == null) return;

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

    // This runs automatically via the event we hooked up in Awake()
    private void OnVideoFinished(VideoPlayer source)
    {
        source.Pause();             // Stop playback
        source.frame = 0;           // Rewind completely back to the start
        playPauseIcon.sprite = playSprite; // Swap icon back to the Play triangle
    }

    // ==========================================
    // 4. CLEANUP ON LEAVING MENU
    // ==========================================

    public void ResetVideoPlayer()
    {
        videoPlayer.Stop();
        videoPlayer.clip = null;
        playPauseIcon.sprite = playSprite;

        if (videoDisplay != null) videoDisplay.enabled = false;
    }
}