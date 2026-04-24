using UnityEngine;
using System.Collections;

public class SpikeBlock : MonoBehaviour
{
    [Header("Visuals")]
    public GameObject spikeVisual; // Drag your child spike GameObject here

    private bool isTriggered = false;

    void Start()
    {
        // Make sure the spike is hidden when the game starts
        if (spikeVisual != null)
        {
            spikeVisual.SetActive(false);
        }
    }

    // We pass the PlayerController so we can check the player's position later
    public void OnStepped(PlayerController player)
    {
        if (isTriggered) return; // Prevent multiple overlapping triggers

        isTriggered = true;
        StartCoroutine(ActivateSpike(player));
    }

    IEnumerator ActivateSpike(PlayerController player)
    {
        // Wait for the trap delay
        yield return new WaitForSeconds(0.5f);

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

        // OPTIONAL: Wait 1 second and retract the spike so the trap can be triggered again
        yield return new WaitForSeconds(1.0f);

        if (spikeVisual != null)
        {
            spikeVisual.SetActive(false);
        }

        isTriggered = false; // Reset the trap
    }
}