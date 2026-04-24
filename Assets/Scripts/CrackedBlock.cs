using UnityEngine;
using System.Collections;

public class CrackedBlock : MonoBehaviour
{
    [Header("Settings")]
    public int stepsToBreak = 2; // NEW: Set to 1 for your new block, 2 for the old one

    [Header("Materials")]
    public Material crackMat2; // You can leave this empty for Cracked1

    private int stepCount = 0;
    private bool isDestroying = false;
    private Renderer blockRenderer;

    void Awake()
    {
        blockRenderer = GetComponent<Renderer>();
    }

    public void OnStepped()
    {
        if (isDestroying) return;

        stepCount++;

        // If we haven't reached the breaking point yet, apply the damaged material
        if (stepCount < stepsToBreak)
        {
            if (blockRenderer != null && crackMat2 != null)
            {
                blockRenderer.material = crackMat2;
            }
        }
        // If we hit the step limit, initiate destruction
        else if (stepCount >= stepsToBreak)
        {
            isDestroying = true;
            StartCoroutine(DestroyAfterDelay());
        }
    }

    IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(0.8f);
        Destroy(gameObject);
    }
}