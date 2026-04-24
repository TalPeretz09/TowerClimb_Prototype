using UnityEngine;
using System.Collections;

public class SpikeBlock : MonoBehaviour
{
    [Header("Visuals")]
    public GameObject spikeVisual; // The spike that kills you

    [Header("Post-Activation")]
    public Renderer indicatorRenderer; // The child object that will turn green
    public Material safeMaterial;      // Your green material

    private bool hasTriggered = false;

    void Start()
    {
        // Make sure the spike is hidden when the game starts
        if (spikeVisual != null)
        {
            spikeVisual.SetActive(false);
        }
    }

    public void OnStepped(PlayerController player)
    {
        // If the trap has EVER been triggered, ignore all future steps
        if (hasTriggered) return;

        hasTriggered = true;
        StartCoroutine(ActivateSpike(player));
    }

    IEnumerator ActivateSpike(PlayerController player)
    {
        // Wait for the trap delay
        yield return new WaitForSeconds(0.65f);

        // Turn on the spike visual
        if (spikeVisual != null)
        {
            spikeVisual.SetActive(true);
        }

        // Determine the space directly above this block
        Vector3Int blockPos = Vector3Int.RoundToInt(transform.position);
        Vector3Int killZone = blockPos + Vector3Int.up;

        // Check if the player still exists AND is still standing exactly in the kill zone
        if (player != null && player.gridPosition == killZone)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoseGame();
            }

            Destroy(player.gameObject);
        }

        // Wait 1 second before retracting the spike
        yield return new WaitForSeconds(1.0f);

        if (spikeVisual != null)
        {
            spikeVisual.SetActive(false);
        }

        // Change the material of your new child gameobject to green
        if (indicatorRenderer != null && safeMaterial != null)
        {
            indicatorRenderer.material = safeMaterial;
        }

        // Notice we deliberately DO NOT set hasTriggered to false down here anymore.
        // This permanently locks the block in its "safe" state!
    }
}