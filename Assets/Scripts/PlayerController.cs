using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    // =========================
    // INPUT STATE
    // =========================
    PlayerInputActions input;
    Vector2 moveInput;
    Vector2 lookInput;
    bool interactHeld;

    // =========================
    // GRID & SPATIAL STATE
    // =========================
    public Vector3Int gridPosition;
    Vector3Int currentFacing = Vector3Int.forward;

    // =========================
    // CHARACTER STATE
    // =========================
    public bool isHanging = false;

    // Prevents immediate re-registration of movement input upon transitioning out of a hang state.
    private bool dropInputLock = false;

    // Caches the coordinates of the last occupied floor tile to ensure step-based traps only trigger once per entry.
    Vector3Int lastStandingPos = new Vector3Int(9999, 9999, 9999);

    // =========================
    // TIMING PARAMETERS
    // =========================
    float moveHoldTime = 0f;
    float holdThreshold = 0.1f;

    // =========================
    // CAMERA REFERENCES
    // =========================
    [Header("Camera")]
    public Transform cameraPivot;
    public float cameraSpeed = 120f;
    public float cameraFollowSpeed = 5f;

    // =========================
    // VISUAL REFERENCES
    // =========================
    [Header("Visuals")]
    public GameObject armsObject;
    public GameObject dustParticlePrefab;

    // Caches the default transform properties of the arm visuals to properly restore them after contextual animations.
    private Vector3 originalArmsPos;
    private Quaternion originalArmsRot;

    void Awake()
    {
        // Initialize the Input System and subscribe to standard context events.
        input = new PlayerInputActions();

        input.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        input.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        input.Player.Look.canceled += ctx => lookInput = Vector2.zero;

        input.Player.Interact.performed += ctx =>
        {
            interactHeld = true;
            UpdateArmsState();
        };

        input.Player.Interact.canceled += ctx =>
        {
            interactHeld = false;
            UpdateArmsState();
        };
    }

    void OnEnable() => input.Enable();
    void OnDisable() => input.Disable();

    void Start()
    {
        // Snap the player to an absolute integer-based grid position upon spawning.
        gridPosition = Vector3Int.RoundToInt(transform.position);
        transform.position = gridPosition;

        // Align the camera pivot to match the player's initial vertical level.
        if (cameraPivot != null)
        {
            Vector3 startPos = cameraPivot.position;
            startPos.y = transform.position.y;
            cameraPivot.position = startPos;
        }

        // Cache initial visual states and hide conditional visuals by default.
        if (armsObject != null)
        {
            originalArmsPos = armsObject.transform.localPosition;
            originalArmsRot = armsObject.transform.localRotation;
            armsObject.SetActive(false);
        }
    }

    void Update()
    {
        // Halt core logic and hide interaction visuals if the game is globally paused or in a countdown phase.
        if (GameManager.Instance != null && !GameManager.Instance.isPlaying)
        {
            moveHoldTime = 0f;

            if (armsObject != null && armsObject.activeSelf)
            {
                armsObject.SetActive(false);
            }
            return;
        }

        // Dynamically update contextual visuals if the player's physical state changes.
        if (armsObject != null && !isHanging && armsObject.activeSelf != interactHeld)
        {
            UpdateArmsState();
        }

        CheckGravity();
        HandleMovement();
        CheckFloorInteractions();
    }

    // =========================
    // VISUAL STATE MANAGER
    // =========================
    void UpdateArmsState()
    {
        if (armsObject == null) return;

        // Force disable visual interactions if game execution is halted.
        if (GameManager.Instance != null && !GameManager.Instance.isPlaying)
        {
            armsObject.SetActive(false);
            armsObject.transform.localPosition = originalArmsPos;
            armsObject.transform.localRotation = originalArmsRot;
            return;
        }

        // Apply a specific positional offset and rotation when the player is suspended from a ledge.
        if (isHanging)
        {
            armsObject.SetActive(true);
            armsObject.transform.localPosition = new Vector3(originalArmsPos.x, 0.5f, originalArmsPos.z);
            armsObject.transform.localRotation = Quaternion.Euler(-60f, originalArmsRot.eulerAngles.y, originalArmsRot.eulerAngles.z);
        }
        else
        {
            // Revert to standard interaction display based on input polling.
            armsObject.SetActive(interactHeld);
            armsObject.transform.localPosition = originalArmsPos;
            armsObject.transform.localRotation = originalArmsRot;
        }
    }

    // =========================
    // ENVIRONMENTAL EFFECTS
    // =========================
    void SpawnDust(Vector3 position)
    {
        // Instantiates feedback geometry slightly below the block's origin to align with the floor.
        if (dustParticlePrefab != null)
        {
            Instantiate(dustParticlePrefab, position + (Vector3.down * 0.4f), Quaternion.Euler(-90, 0, 0));
        }
    }

    // =========================
    // GRAVITY RESOLUTION
    // =========================
    void CheckGravity()
    {
        if (isHanging) return;

        // Cast downwards to identify supporting geometry. If unsupported, force the player down to the nearest valid floor.
        if (!CanStand(gridPosition))
        {
            Vector3Int searchPos = gridPosition;
            bool foundFloor = false;

            for (int i = 0; i < 200; i++)
            {
                if (CanStand(searchPos))
                {
                    foundFloor = true;
                    break;
                }
                searchPos += Vector3Int.down;
            }

            if (foundFloor)
            {
                gridPosition = searchPos;
                transform.position = gridPosition;

                // Reset the interaction cache to ensure traps register the new vertical arrival.
                lastStandingPos = new Vector3Int(9999, 9999, 9999);
            }
            else
            {
                // Trigger loss state if the downward cast exceeds expected world bounds (falling into the void).
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.LoseGame();
                }
                Destroy(gameObject);
            }
        }
    }

    // =========================
    // FLOOR TRIGGERS
    // =========================
    void CheckFloorInteractions()
    {
        if (isHanging)
        {
            lastStandingPos = new Vector3Int(9999, 9999, 9999);
            return;
        }

        Vector3Int feetPos = gridPosition + Vector3Int.down;
        bool hasMovedToNewBlock = (feetPos != lastStandingPos);

        Collider[] hits = Physics.OverlapBox(
            feetPos,
            Vector3.one * 0.4f,
            Quaternion.identity,
            LayerMask.GetMask("Block")
        );

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Victory"))
            {
                GameManager.Instance.WinGame();

                // Hardcoded technique unlocks triggered upon specific level completion events.
                if (SceneManager.GetActiveScene().name == "Tower2")
                {
                    TechniqueUnlockManager.UnlockTechnique("Tech4");
                    TechniqueUnlockManager.UnlockTechnique("Tech5");
                }
                else if (SceneManager.GetActiveScene().name == "Tower4")
                {
                    TechniqueUnlockManager.UnlockTechnique("Tech6");
                    TechniqueUnlockManager.UnlockTechnique("Tech7");
                }
            }

            // Only execute hazard logic if the player has fully transitioned onto a new grid tile.
            if (hasMovedToNewBlock)
            {
                if (hit.CompareTag("Cracked1") || hit.CompareTag("Cracked2"))
                {
                    CrackedBlock cracked = hit.GetComponent<CrackedBlock>();
                    if (cracked != null)
                    {
                        cracked.OnStepped();
                    }
                }
                else if (hit.CompareTag("Spike"))
                {
                    SpikeBlock spike = hit.GetComponent<SpikeBlock>();
                    if (spike != null)
                    {
                        spike.OnStepped(this);
                    }
                }
            }
        }

        if (hasMovedToNewBlock)
        {
            lastStandingPos = feetPos;
        }
    }

    void LateUpdate()
    {
        // Defer camera movement to LateUpdate to ensure it interpolates smoothly after all positional physics have been processed.
        HandleCamera();
    }

    // =========================
    // INPUT DIRECTION MAPPING
    // =========================
    Vector3Int GetGridDirection(Vector2 input)
    {
        // Enforce an analog deadzone to prevent drift.
        if (input.magnitude < 0.5f) return Vector3Int.zero;

        // Fallback to absolute cardinal directions if the camera pivot is missing.
        if (cameraPivot == null)
        {
            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
                return input.x > 0 ? Vector3Int.right : Vector3Int.left;
            else
                return input.y > 0 ? Vector3Int.forward : Vector3Int.back;
        }

        // Map relative 2D input to global 3D space relative to the current camera viewing angle.
        Vector3 camForward = cameraPivot.forward;
        Vector3 camRight = cameraPivot.right;

        camForward.y = 0;
        camRight.y = 0;

        camForward.Normalize();
        camRight.Normalize();

        Vector3 desiredWorldDir = (camRight * input.x) + (camForward * input.y);

        // Snap the resulting vector to the nearest absolute cardinal axis.
        if (Mathf.Abs(desiredWorldDir.x) > Mathf.Abs(desiredWorldDir.z))
        {
            return desiredWorldDir.x > 0 ? Vector3Int.right : Vector3Int.left;
        }
        else
        {
            return desiredWorldDir.z > 0 ? Vector3Int.forward : Vector3Int.back;
        }
    }

    // =========================
    // MOVEMENT ROUTER
    // =========================
    void HandleMovement()
    {
        Vector3Int dir = GetGridDirection(moveInput);

        // Reset execution locks and timers when the user fully releases the movement axis.
        if (dir == Vector3Int.zero)
        {
            moveHoldTime = 0f;
            dropInputLock = false;
            return;
        }

        // Halt positional logic if a strict drop interaction recently occurred and input hasn't been reset.
        if (dropInputLock) return;

        moveHoldTime += Time.deltaTime;

        if (isHanging)
        {
            if (moveHoldTime >= holdThreshold)
            {
                TryHangMovement(dir);
                moveHoldTime = 0f;
            }
            return;
        }

        Vector3Int front = gridPosition + currentFacing;
        bool isGrabbing = interactHeld && HasBlock(front);

        // Distinguish between a quick directional tap (turning in place) and a deliberate hold (spatial movement).
        if (moveHoldTime < holdThreshold)
        {
            if (!isGrabbing && currentFacing != dir)
            {
                currentFacing = dir;
                RotatePlayer(dir);
            }
        }
        else
        {
            TryMoveOrPush(dir);
            moveHoldTime = 0f;
        }
    }

    // =========================
    // HANGING LOGIC
    // =========================
    void TryHangMovement(Vector3Int dir)
    {
        // 1. Climb up the ledge if attempting to move forward into the block.
        if (dir == currentFacing)
        {
            Vector3Int standPos = gridPosition + currentFacing + Vector3Int.up;

            if (!HasBlock(standPos))
            {
                isHanging = false;
                UpdateArmsState();

                gridPosition = standPos;
                transform.position = gridPosition;
                RotatePlayer(currentFacing);
            }
        }
        // 2. Detach from the ledge and initiate a downward drop.
        else if (dir == -currentFacing)
        {
            isHanging = false;
            dropInputLock = true;
            UpdateArmsState();

            Vector3Int searchPos = gridPosition;
            bool foundFloor = false;

            for (int i = 0; i < 200; i++)
            {
                if (CanStand(searchPos))
                {
                    foundFloor = true;
                    break;
                }
                searchPos += Vector3Int.down;
            }

            if (foundFloor)
            {
                gridPosition = searchPos;
                transform.position = gridPosition;
                RotatePlayer(currentFacing);
            }
            else
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.LoseGame();
                }
                Destroy(gameObject);
            }
        }
        // 3. Shimmy laterally along the ledge plane.
        else
        {
            float dot = Vector3.Dot((Vector3)dir, (Vector3)currentFacing);

            if (Mathf.Abs(dot) < 0.1f)
            {
                Vector3Int targetPos = gridPosition + dir;
                Vector3Int targetHeadPos = targetPos + Vector3Int.up;
                Vector3Int targetGrabBlock = targetPos + currentFacing;

                // Move laterally if the adjacent space is clear but retains a block to hold.
                if (!HasBlock(targetPos) && !HasBlock(targetHeadPos) && HasBlock(targetGrabBlock))
                {
                    gridPosition = targetPos;
                    transform.position = gridPosition;
                }
                // Wrap around outer corners dynamically.
                else if (!HasBlock(targetPos) && !HasBlock(targetHeadPos) && !HasBlock(targetGrabBlock))
                {
                    Vector3Int diagonalPos = targetPos + currentFacing;
                    Vector3Int diagonalHeadPos = diagonalPos + Vector3Int.up;

                    if (!HasBlock(diagonalPos) && !HasBlock(diagonalHeadPos))
                    {
                        gridPosition = diagonalPos;
                        currentFacing = -dir;
                        transform.position = gridPosition;
                        RotatePlayer(currentFacing);
                    }
                }
                // Handle inward corners.
                else if (HasBlock(targetPos))
                {
                    currentFacing = dir;
                    RotatePlayer(currentFacing);
                }
            }
        }
    }

    // =========================
    // INTERACTION DECISION
    // =========================
    void TryMoveOrPush(Vector3Int dir)
    {
        Vector3Int front = gridPosition + currentFacing;

        // Resolve push/pull block manipulation before resolving standard physical movement.
        if (interactHeld && HasBlock(front))
        {
            float dot = Vector3.Dot((Vector3)dir, (Vector3)currentFacing);

            if (dot > 0)
            {
                PushBlock(front, currentFacing);
                return;
            }
            else if (dot < 0)
            {
                PullBlock(front, -currentFacing);
                return;
            }
        }

        if (dir == currentFacing && CanClimb(dir))
        {
            TryClimb(dir);
            return;
        }

        if (dir == currentFacing && CanClimbDown(dir))
        {
            TryClimbDown(dir);
            return;
        }

        TryMove(dir);
    }

    // =========================
    // STANDARD TRAVERSAL
    // =========================
    void TryMove(Vector3Int dir)
    {
        Vector3Int target = gridPosition + dir;

        // Reject movement if obstructed by geometry at head/body level.
        if (HasBlock(target)) return;

        // Commit standard linear displacement if geometry exists to stand on.
        if (CanStand(target))
        {
            gridPosition = target;
            transform.position = gridPosition;
        }
        else
        {
            // If traversing into an empty spatial column, transition into a ledge hang.
            Vector3Int targetDown = target + Vector3Int.down;

            if (!HasBlock(target) && !HasBlock(targetDown))
            {
                isHanging = true;
                UpdateArmsState();

                gridPosition = targetDown;
                currentFacing = -dir;
                transform.position = gridPosition;
                RotatePlayer(currentFacing);
            }
        }
    }

    // =========================
    // UPWARD CLIMBING
    // =========================
    bool CanClimb(Vector3Int dir)
    {
        Vector3Int front = gridPosition + dir;
        Vector3Int aboveFront = front + Vector3Int.up;
        Vector3Int abovePlayer = gridPosition + Vector3Int.up;

        // Requires a solid block in front, and unobstructed vertical clearance for both origin and destination.
        return HasBlock(front) && !HasBlock(aboveFront) && !HasBlock(abovePlayer);
    }

    void TryClimb(Vector3Int dir)
    {
        Vector3Int front = gridPosition + dir;
        Vector3Int aboveFront = front + Vector3Int.up;
        gridPosition = aboveFront;
        transform.position = gridPosition;
    }

    // =========================
    // DOWNWARD CLIMBING
    // =========================
    bool CanClimbDown(Vector3Int dir)
    {
        Vector3Int front = gridPosition + dir;
        Vector3Int frontDown = front + Vector3Int.down;

        // Requires adjacent empty space cascading down into a solid block.
        return !HasBlock(front) && !HasBlock(frontDown) && CanStand(frontDown);
    }

    void TryClimbDown(Vector3Int dir)
    {
        Vector3Int frontDown = gridPosition + dir + Vector3Int.down;
        gridPosition = frontDown;
        transform.position = gridPosition;
    }

    // =========================
    // KINEMATIC PUSHING
    // =========================
    void PushBlock(Vector3Int startBlockPos, Vector3Int dir)
    {
        List<Transform> blocksToMove = new List<Transform>();
        Vector3Int checkPos = startBlockPos;

        // Iteratively scan outward along the requested vector to calculate potential multi-block push chains.
        while (HasBlock(checkPos))
        {
            Collider[] hits = Physics.OverlapBox(
                checkPos,
                Vector3.one * 0.4f,
                Quaternion.identity,
                LayerMask.GetMask("Block")
            );

            if (hits.Length > 0)
            {
                if (hits[0].CompareTag("Immovable")) return; // Halt entire operation if an anchor block is detected.
                blocksToMove.Add(hits[0].transform);
            }
            checkPos += dir;
        }

        // Commit positional translation for all queued blocks if the final adjacent tile is void.
        if (!HasBlock(checkPos))
        {
            for (int i = blocksToMove.Count - 1; i >= 0; i--)
            {
                SpawnDust(blocksToMove[i].position);
                blocksToMove[i].position += (Vector3)dir;
            }
            Physics.SyncTransforms();
        }
    }

    // =========================
    // KINEMATIC PULLING
    // =========================
    void PullBlock(Vector3Int blockPos, Vector3Int dir)
    {
        Collider[] targetHits = Physics.OverlapBox(
            blockPos,
            Vector3.one * 0.4f,
            Quaternion.identity,
            LayerMask.GetMask("Block")
        );

        if (targetHits.Length > 0 && targetHits[0].CompareTag("Immovable")) return;

        Vector3Int behind = gridPosition - currentFacing;

        // Verify the coordinate behind the player is empty to accommodate the backward displacement.
        if (!HasBlock(behind))
        {
            SpawnDust(blockPos);
            MoveBlock(blockPos, gridPosition);

            // Back-step seamlessly if geometry exists; otherwise, gracefully fail into a ledge hang.
            if (CanStand(behind))
            {
                gridPosition = behind;
                transform.position = gridPosition;
            }
            else
            {
                isHanging = true;
                UpdateArmsState();

                gridPosition = behind + Vector3Int.down;
                transform.position = gridPosition;
                RotatePlayer(currentFacing);
            }
            Physics.SyncTransforms();
        }
    }

    // =========================
    // GRID QUERIES
    // =========================
    bool HasBlock(Vector3Int pos)
    {
        return Physics.CheckBox(
            pos,
            Vector3.one * 0.4f,
            Quaternion.identity,
            LayerMask.GetMask("Block")
        );
    }

    bool CanStand(Vector3Int pos)
    {
        return HasBlock(pos + Vector3Int.down);
    }

    // =========================
    // KINEMATIC TRANSLATION
    // =========================
    void MoveBlock(Vector3Int from, Vector3Int to)
    {
        Collider[] hits = Physics.OverlapBox(
            from,
            Vector3.one * 0.4f,
            Quaternion.identity,
            LayerMask.GetMask("Block")
        );

        if (hits.Length > 0)
        {
            hits[0].transform.position = to;
            Physics.SyncTransforms();
        }
    }

    // =========================
    // ROTATION HANDLER
    // =========================
    void RotatePlayer(Vector3Int dir)
    {
        Vector3 forward = new Vector3(dir.x, 0, dir.z);

        // Apply a distinct pitch offset to physically align the player model against the wall when hanging.
        if (isHanging)
        {
            transform.rotation = Quaternion.LookRotation(forward) * Quaternion.Euler(15f, 0, 0);
        }
        else
        {
            transform.rotation = Quaternion.LookRotation(forward);
        }
    }

    // =========================
    // CAMERA CONTROLLER
    // =========================
    void HandleCamera()
    {
        if (cameraPivot == null) return;

        // Rotate the primary pivot via manual right-stick input.
        if (GameManager.Instance != null && GameManager.Instance.isPlaying)
        {
            float rotation = lookInput.x * cameraSpeed * Time.deltaTime;
            cameraPivot.Rotate(Vector3.up, rotation);
        }

        // Linearly interpolate the pivot position to fluidly follow player displacement without rigid locking.
        Vector3 targetPos = transform.position;
        cameraPivot.position = Vector3.Lerp(cameraPivot.position, targetPos, cameraFollowSpeed * Time.deltaTime);
    }
}