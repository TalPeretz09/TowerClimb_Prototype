using UnityEngine;

public class FloatingItem : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Angular velocity applied to the object per second. Modifying the Y-axis is standard for a localized spin.")]
    public Vector3 rotationSpeed = new Vector3(0f, 45f, 0f);

    [Header("Hover Settings")]
    [Tooltip("Frequency of the vertical oscillation.")]
    public float hoverSpeed = 2f;
    [Tooltip("Maximum vertical displacement from the origin during oscillation.")]
    public float hoverAmplitude = 0.25f;

    // Caches the initial world position to establish a baseline for the vertical offset.
    private Vector3 startPosition;

    void Start()
    {
        // Store the initial transform position upon instantiation.
        startPosition = transform.position;
    }

    void Update()
    {
        // =========================
        // ROTATION
        // =========================
        // Apply continuous rotation in world space, scaled by delta time to ensure framerate independence.
        transform.Rotate(rotationSpeed * Time.deltaTime, Space.World);

        // =========================
        // HOVER OSCILLATION
        // =========================
        // Calculate the vertical offset using a sine wave function driven by the application's elapsed time.
        float newY = startPosition.y + (Mathf.Sin(Time.time * hoverSpeed) * hoverAmplitude);

        // Update the transform's position, applying the calculated vertical offset while preserving the current X and Z coordinates.
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}