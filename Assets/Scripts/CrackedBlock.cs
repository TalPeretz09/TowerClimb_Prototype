using UnityEngine;
using System.Collections;

public class CrackedBlock : MonoBehaviour
{
    [Header("Materials")]
    public Material crackMat2;

    private int stepCount = 0;
    private bool isDestroying = false;
    private Renderer blockRenderer;

    void Awake()
    {
        blockRenderer = GetComponent<Renderer>();
    }

    // Called by the PlayerController when the player steps onto this block
    public void OnStepped()
    {
        if (isDestroying) return;

        stepCount++;

        if (stepCount == 1)
        {
            // First step: Change material
            if (blockRenderer != null && crackMat2 != null)
            {
                blockRenderer.material = crackMat2;
            }
        }
        else if (stepCount >= 2)
        {
            // Second step: Initiate destruction
            isDestroying = true;
            StartCoroutine(DestroyAfterDelay());
        }
    }

    IEnumerator DestroyAfterDelay()
    {
        // Wait for 0.8 seconds as requested, then destroy the block
        yield return new WaitForSeconds(0.8f);
        Destroy(gameObject);
    }
}