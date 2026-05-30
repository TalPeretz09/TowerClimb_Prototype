using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))] // Ensures an AudioSource is automatically added
public class CrackedBlock : MonoBehaviour
{
    [Header("Settings")]
    // The total number of discrete step interactions allowed before the block collapses.
    public int stepsToBreak = 2;

    [Header("Materials")]
    // Material applied to represent visual degradation when the block enters a damaged state.
    public Material crackMat2;

    [Header("Audio Settings")]
    [Tooltip("Sound played every time the block is stepped on.")]
    public AudioClip crackSound;
    [Tooltip("Sound played when the block finally crumbles.")]
    public AudioClip disintegrateSound;

    private int stepCount = 0;
    private bool isDestroying = false;
    private Renderer blockRenderer;
    private Collider blockCollider;
    private AudioSource audioSource;

    void Awake()
    {
        // Cache local components to avoid runtime lookup overhead.
        blockRenderer = GetComponent<Renderer>();
        blockCollider = GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();

        // Ensure the AudioSource doesn't play anything on startup
        audioSource.playOnAwake = false;
    }

    public void OnStepped()
    {
        // Guard against redundant execution if the destruction sequence is already active.
        if (isDestroying) return;

        // Play the cracking sound immediately upon interaction
        if (crackSound != null)
        {
            audioSource.PlayOneShot(crackSound);
        }

        stepCount++;

        // Apply progressive visual feedback if the degradation threshold has not been reached.
        if (stepCount < stepsToBreak)
        {
            if (blockRenderer != null && crackMat2 != null)
            {
                blockRenderer.material = crackMat2;
            }
        }
        // Initiate the collapse routine once the step limit is satisfied.
        else if (stepCount >= stepsToBreak)
        {
            isDestroying = true;
            StartCoroutine(DestroyAfterDelay());
        }
    }

    IEnumerator DestroyAfterDelay()
    {
        // Provide a brief temporal window for player traversal or reaction before object disposal.
        yield return new WaitForSeconds(0.8f);

        // Play the disintegration sound
        if (disintegrateSound != null)
        {
            audioSource.PlayOneShot(disintegrateSound);

            // Hide the block and disable collision so the player falls through
            if (blockRenderer != null) blockRenderer.enabled = false;
            if (blockCollider != null) blockCollider.enabled = false;

            // Delay the actual destruction of the GameObject until the audio clip finishes playing
            Destroy(gameObject, disintegrateSound.length);
        }
        else
        {
            // Fallback if no audio clip is assigned
            Destroy(gameObject);
        }
    }
}