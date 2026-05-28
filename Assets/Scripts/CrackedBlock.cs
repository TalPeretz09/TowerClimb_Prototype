using UnityEngine;
using System.Collections;

public class CrackedBlock : MonoBehaviour
{
    [Header("Settings")]
    // The total number of discrete step interactions allowed before the block collapses.
    public int stepsToBreak = 2;

    [Header("Materials")]
    // Material applied to represent visual degradation when the block enters a damaged state.
    public Material crackMat2;

    private int stepCount = 0;
    private bool isDestroying = false;
    private Renderer blockRenderer;

    void Awake()
    {
        // Cache the local renderer component to avoid runtime lookup overhead.
        blockRenderer = GetComponent<Renderer>();
    }

    public void OnStepped()
    {
        // Guard against redundant execution if the destruction sequence is already active.
        if (isDestroying) return;

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
        Destroy(gameObject);
    }
}