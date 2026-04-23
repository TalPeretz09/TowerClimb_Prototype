using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    // INPUT
    PlayerInputActions input;
    Vector2 moveInput;
    Vector2 lookInput;
    bool interactHeld;

    // GRID
    public Vector3Int gridPosition;
    Vector3Int currentFacing = Vector3Int.forward;

    // STATE
    public bool isHanging = false;
    // Tracks the last floor block to prevent registering multiple steps per second
    Vector3Int lastStandingPos = new Vector3Int(9999, 9999, 9999);

    // TIMING
    float moveHoldTime = 0f;
    float holdThreshold = 0.1f;

    // CAMERA
    [Header("Camera")]
    public Transform cameraPivot;
    public float cameraSpeed = 120f;
    public float cameraFollowSpeed = 5f; // New: Controls how snappy or floaty the camera tracks upward

    void Awake()
    {
        input = new PlayerInputActions();

        input.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        input.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        input.Player.Look.canceled += ctx => lookInput = Vector2.zero;

        input.Player.Interact.performed += ctx => interactHeld = true;
        input.Player.Interact.canceled += ctx => interactHeld = false;
    }

    void OnEnable() => input.Enable();
    void OnDisable() => input.Disable();

    void Start()
    {
        gridPosition = Vector3Int.RoundToInt(transform.position);
        transform.position = gridPosition;

        // Snap the camera to the player's starting height immediately
        if (cameraPivot != null)
        {
            Vector3 startPos = cameraPivot.position;
            startPos.y = transform.position.y;
            cameraPivot.position = startPos;
        }
    }

    void Update()
    {
        // NEW: Only allow movement if the game is actually playing
        if (GameManager.Instance != null && !GameManager.Instance.isPlaying)
        {
            moveHoldTime = 0f; // Reset move hold so they don't buffer inputs
            return;
        }

        HandleMovement();
        CheckFloorInteractions();
    }

    // =========================
    // FLOOR INTERACTIONS
    // =========================
    void CheckFloorInteractions()
    {
        // If the player is hanging, they aren't standing on anything. 
        // We reset the tracker so climbing back up counts as a fresh "step".
        if (isHanging)
        {
            lastStandingPos = new Vector3Int(9999, 9999, 9999);
            return;
        }

        Vector3Int feetPos = gridPosition + Vector3Int.down;

        // This bool is only true on the exact frame the player arrives on a new coordinate
        bool hasMovedToNewBlock = (feetPos != lastStandingPos);

        Collider[] hits = Physics.OverlapBox(
            feetPos,
            Vector3.one * 0.4f,
            Quaternion.identity,
            LayerMask.GetMask("Block")
        );

        foreach (Collider hit in hits)
        {
            // Victory can trigger anytime we are touching it
            if (hit.CompareTag("Victory"))
            {
                GameManager.Instance.WinGame();
            }

            // Step mechanics only trigger ONCE when arriving on the block
            if (hasMovedToNewBlock)
            {
                if (hit.CompareTag("Cracked"))
                {
                    CrackedBlock cracked = hit.GetComponent<CrackedBlock>();
                    if (cracked != null)
                    {
                        cracked.OnStepped();
                    }
                }
            }
        }

        // Update the tracker position so we don't trigger steps while standing still/rotating
        if (hasMovedToNewBlock)
        {
            lastStandingPos = feetPos;
        }
    }

    void LateUpdate()
    {
        HandleCamera();
    }

    // =========================
    // INPUT -> GRID DIRECTION
    // =========================
    Vector3Int GetGridDirection(Vector2 input)
    {
        if (input.magnitude < 0.5f) return Vector3Int.zero;

        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            return input.x > 0 ? Vector3Int.right : Vector3Int.left;
        else
            return input.y > 0 ? Vector3Int.forward : Vector3Int.back;
    }

    // =========================
    // MAIN MOVEMENT LOGIC
    // =========================
    void HandleMovement()
    {
        Vector3Int dir = GetGridDirection(moveInput);

        if (dir == Vector3Int.zero)
        {
            moveHoldTime = 0f;
            return;
        }

        moveHoldTime += Time.deltaTime;

        // --- HANGING STATE ---
        if (isHanging)
        {
            if (moveHoldTime >= holdThreshold)
            {
                TryHangMovement(dir);
                moveHoldTime = 0f;
            }
            return;
        }

        // --- NORMAL STATE ---
        Vector3Int front = gridPosition + currentFacing;
        bool isGrabbing = interactHeld && HasBlock(front);

        // TAP -> rotate only
        if (moveHoldTime < holdThreshold)
        {
            if (!isGrabbing && currentFacing != dir)
            {
                currentFacing = dir;
                RotatePlayer(dir);
            }
        }
        // HOLD -> move / push / pull
        else
        {
            TryMoveOrPush(dir);
            moveHoldTime = 0f;
        }
    }

    // =========================
    // HANGING MOVEMENT
    // =========================
    // =========================
    // HANGING MOVEMENT
    // =========================
    void TryHangMovement(Vector3Int dir)
    {
        // 1. CLIMB UP (Release Hang)
        if (dir == currentFacing)
        {
            Vector3Int standPos = gridPosition + currentFacing + Vector3Int.up;

            if (!HasBlock(standPos))
            {
                isHanging = false;
                gridPosition = standPos;
                transform.position = gridPosition;
                RotatePlayer(currentFacing);
            }
        }
        // 2. DROP DOWN (Release Hang)
        else if (dir == -currentFacing)
        {
            isHanging = false;

            Vector3Int searchPos = gridPosition;
            bool foundFloor = false;

            // Search downward. The limit of 200 prevents infinite loops in case of a bug.
            for (int i = 0; i < 200; i++)
            {
                if (CanStand(searchPos))
                {
                    foundFloor = true;
                    break; // Floor found, stop searching!
                }
                searchPos += Vector3Int.down;
            }

            if (foundFloor)
            {
                // Snap player directly to the block we found below them
                gridPosition = searchPos;
                transform.position = gridPosition;
                RotatePlayer(currentFacing);
            }
            else
            {
                // No floor found below - the player has fallen into the abyss!
                if (GameManager.Instance != null)
                {
                    // Call the method to activate your new lose panel
                    GameManager.Instance.LoseGame();
                }

                // Destroy the player character
                Destroy(gameObject);
            }
        }
        // 3. SHIMMY (Left / Right)
        else
        {
            float dot = Vector3.Dot((Vector3)dir, (Vector3)currentFacing);

            if (Mathf.Abs(dot) < 0.1f)
            {
                Vector3Int targetPos = gridPosition + dir;
                Vector3Int targetHeadPos = targetPos + Vector3Int.up;
                Vector3Int targetGrabBlock = targetPos + currentFacing;

                // OPTION 1: Straight Shimmy
                if (!HasBlock(targetPos) && !HasBlock(targetHeadPos) && HasBlock(targetGrabBlock))
                {
                    gridPosition = targetPos;
                    transform.position = gridPosition;
                }
                // OPTION 2: Outside Corner Swing
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
                // OPTION 3: Inside Corner Grab
                else if (HasBlock(targetPos))
                {
                    currentFacing = dir;
                    RotatePlayer(currentFacing);
                }
            }
        }
    }

    // =========================
    // MOVE OR PUSH/PULL
    // =========================
    void TryMoveOrPush(Vector3Int dir)
    {
        Vector3Int front = gridPosition + currentFacing;

        if (interactHeld && HasBlock(front))
        {
            float dot = Vector3.Dot((Vector3)dir, (Vector3)currentFacing);

            // PUSH (same direction)
            if (dot > 0)
            {
                PushBlock(front, currentFacing);
                return;
            }
            // PULL (opposite direction)
            else if (dot < 0)
            {
                PullBlock(front, -currentFacing);
                return;
            }
        }

        // CLIMB UP
        if (dir == currentFacing && CanClimb(dir))
        {
            TryClimb(dir);
            return;
        }

        // CLIMB DOWN
        if (dir == currentFacing && CanClimbDown(dir))
        {
            TryClimbDown(dir);
            return;
        }

        // MOVE
        TryMove(dir);
    }

    // =========================
    // BASIC MOVEMENT
    // =========================
    void TryMove(Vector3Int dir)
    {
        Vector3Int target = gridPosition + dir;

        // FIX: Prevent moving into a space that is already occupied by a block!
        if (HasBlock(target))
        {
            return;
        }

        if (CanStand(target))
        {
            gridPosition = target;
            transform.position = gridPosition;
        }
        else
        {
            // ENTER HANG STATE: Walk off a 2-block drop ledge
            Vector3Int targetDown = target + Vector3Int.down;

            if (!HasBlock(target) && !HasBlock(targetDown))
            {
                isHanging = true;
                gridPosition = targetDown;
                currentFacing = -dir;
                transform.position = gridPosition;
                RotatePlayer(currentFacing);
            }
        }
    }

    // =========================
    // CLIMB UP
    // =========================
    bool CanClimb(Vector3Int dir)
    {
        Vector3Int front = gridPosition + dir;
        Vector3Int aboveFront = front + Vector3Int.up;

        return HasBlock(front) && !HasBlock(aboveFront);
    }

    void TryClimb(Vector3Int dir)
    {
        Vector3Int front = gridPosition + dir;
        Vector3Int aboveFront = front + Vector3Int.up;

        gridPosition = aboveFront;
        transform.position = gridPosition;
    }

    // =========================
    // CLIMB DOWN
    // =========================
    bool CanClimbDown(Vector3Int dir)
    {
        Vector3Int front = gridPosition + dir;
        Vector3Int frontDown = front + Vector3Int.down;

        return !HasBlock(front) && !HasBlock(frontDown) && CanStand(frontDown);
    }

    void TryClimbDown(Vector3Int dir)
    {
        Vector3Int frontDown = gridPosition + dir + Vector3Int.down;

        gridPosition = frontDown;
        transform.position = gridPosition;
    }

    // =========================
    // PUSH
    // =========================
    void PushBlock(Vector3Int startBlockPos, Vector3Int dir)
    {
        List<Transform> blocksToMove = new List<Transform>();
        Vector3Int checkPos = startBlockPos;

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
                // NEW: If any block in the row is Immovable, the whole push fails.
                if (hits[0].CompareTag("Immovable"))
                {
                    return;
                }

                blocksToMove.Add(hits[0].transform);
            }

            checkPos += dir;
        }

        if (!HasBlock(checkPos))
        {
            for (int i = blocksToMove.Count - 1; i >= 0; i--)
            {
                blocksToMove[i].position += (Vector3)dir;
            }
            Physics.SyncTransforms();
        }
    }

    // =========================
    // PULL
    // =========================
    void PullBlock(Vector3Int blockPos, Vector3Int dir)
    {
        // NEW: Check if the specific block we are trying to pull is Immovable
        Collider[] targetHits = Physics.OverlapBox(
            blockPos,
            Vector3.one * 0.4f,
            Quaternion.identity,
            LayerMask.GetMask("Block")
        );

        if (targetHits.Length > 0 && targetHits[0].CompareTag("Immovable"))
        {
            return; // Abort the pull completely
        }

        Vector3Int behind = gridPosition - currentFacing;

        if (!HasBlock(behind))
        {
            MoveBlock(blockPos, gridPosition);

            if (CanStand(behind))
            {
                gridPosition = behind;
                transform.position = gridPosition;
            }
            else
            {
                // ENTER HANG STATE: Pull block and step back into empty air
                isHanging = true;
                gridPosition = behind + Vector3Int.down;
                transform.position = gridPosition;
                RotatePlayer(currentFacing);
            }
            Physics.SyncTransforms();
        }
    }

    // =========================
    // BLOCK DETECTION
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
    // BLOCK MOVEMENT
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
    // ROTATION (With Hang Tilt)
    // =========================
    void RotatePlayer(Vector3Int dir)
    {
        Vector3 forward = new Vector3(dir.x, 0, dir.z);

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
    // CAMERA
    // =========================
    void HandleCamera()
    {
        if (cameraPivot == null) return;

        // 1. Pivot Rotation (Horizontal)
        float rotation = lookInput.x * cameraSpeed * Time.deltaTime;
        cameraPivot.Rotate(Vector3.up, rotation);

        // 2. Smooth Omnidirectional Tracking
        // NEW: Now targets the player's exact X, Y, and Z position
        Vector3 targetPos = transform.position;

        cameraPivot.position = Vector3.Lerp(cameraPivot.position, targetPos, cameraFollowSpeed * Time.deltaTime);
    }
}