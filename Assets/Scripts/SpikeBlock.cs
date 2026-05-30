using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))] // Automatically ensures an AudioSource is attached
public class SpikeBlock : MonoBehaviour
{
    [Header("Visuals")]
    public GameObject spikeVisual;     // The visual representation of the hazard.

    [Header("Post-Activation")]
    public Renderer indicatorRenderer; // The renderer used to display the block's current safety state.
    public Material safeMaterial;      // The material applied once the trap has been safely disarmed.

    [Header("Audio Settings")]
    [Tooltip("Sound played exactly when the spike shoots out.")]
    public AudioClip spikeProtrudeSound;

    private bool hasTriggered = false;
    private AudioSource audioSource;

    void Start()
    {
        // Cache the audio source and prevent it from playing on load
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // Ensure the hazard visual is disabled upon initialization.
        if (spikeVisual != null)
        {
            spikeVisual.SetActive(false);
        }
    }

    public void OnStepped(PlayerController player)
    {
        // Prevent re-triggering if the trap has already been activated.
        if (hasTriggered) return;

        hasTriggered = true;
        StartCoroutine(ActivateSpike(player));
    }

    IEnumerator ActivateSpike(PlayerController player)
    {
        // Delay before the hazard becomes active and potentially lethal.
        yield return new WaitForSeconds(0.65f);

        // Play the spike protrusion sound effect right as it appears
        if (spikeProtrudeSound != null)
        {
            audioSource.PlayOneShot(spikeProtrudeSound);
        }

        // Enable the hazard visual to indicate activation.
        if (spikeVisual != null)
        {
            spikeVisual.SetActive(true);
        }

        // Calculate the lethal zone immediately above the block's current position.
        Vector3Int blockPos = Vector3Int.RoundToInt(transform.position);
        Vector3Int killZone = blockPos + Vector3Int.up;

        // Verify if the player is still present in the calculated kill zone.
        if (player != null && player.gridPosition == killZone)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoseGame();
            }

            Destroy(player.gameObject);
        }

        // Keep the hazard active briefly before retracting it.
        yield return new WaitForSeconds(1.0f);

        if (spikeVisual != null)
        {
            spikeVisual.SetActive(false);
        }

        // Update the block's indicator to visually signify it is now safe to traverse.
        if (indicatorRenderer != null && safeMaterial != null)
        {
            indicatorRenderer.material = safeMaterial;
        }

        // The state flag remains true to permanently disable the trap for the remainder of the session.
    }
}