using UnityEngine;

public class FloatingItem : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Speed and axis of rotation. Usually you just want Y to be above 0.")]
    public Vector3 rotationSpeed = new Vector3(0f, 45f, 0f);

    [Header("Hover Settings")]
    [Tooltip("How fast the item moves up and down.")]
    public float hoverSpeed = 2f;
    [Tooltip("How far up and down the item moves from its starting point.")]
    public float hoverAmplitude = 0.25f;

    // To keep track of where we placed it in the scene
    private Vector3 startPosition;

    void Start()
    {
        // Remember the starting position so we can hover relative to it
        startPosition = transform.position;
    }

    void Update()
    {
        // 1. ROTATE
        // Rotate continuously based on the rotationSpeed vector
        transform.Rotate(rotationSpeed * Time.deltaTime, Space.World);

        // 2. HOVER
        // Calculate a smooth up and down value using Mathf.Sin and Time.time
        float newY = startPosition.y + (Mathf.Sin(Time.time * hoverSpeed) * hoverAmplitude);

        // Apply the new Y position while keeping the current X and Z
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}