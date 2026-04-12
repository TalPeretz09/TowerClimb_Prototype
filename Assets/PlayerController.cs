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

    // TIMING
    float moveHoldTime = 0f;
    float holdThreshold = 0.1f;

    // CAMERA
    [Header("Camera")]
    public Transform cameraPivot;
    public float cameraSpeed = 120f;

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
    }

    void Update()
    {
        HandleMovement();
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
    void TryHangMovement(Vector3Int dir)
    {
        // 1. CLIMB UP (Release Hang)
        if (dir == currentFacing)
        {
            // FIX: The block we are holding is at our exact Y level.
            // To stand on it, we just check the space directly above it.
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

            // Check if we are dropping onto a solid floor
            if (CanStand(gridPosition))
            {
                // Just stand in place
            }
            else
            {
                // Otherwise fall down 1 block
                Vector3Int dropPos = gridPosition + Vector3Int.down;
                if (!HasBlock(dropPos))
                {
                    gridPosition = dropPos;
                    transform.position = gridPosition;
                }
            }
            RotatePlayer(currentFacing);
        }
        // 3. SHIMMY (Left / Right)
        else
        {
            float dot = Vector3.Dot((Vector3)dir, (Vector3)currentFacing);

            if (Mathf.Abs(dot) < 0.1f)
            {
                Vector3Int targetPos = gridPosition + dir;
                Vector3Int targetHeadPos = targetPos + Vector3Int.up;

                // FIX: The edge we want to grab is at the same Y-level as our body
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
                // OPTION 3: Inside Corner Grab (BONUS)
                else if (HasBlock(targetPos))
                {
                    // If the space to our side is blocked, we don't move position,
                    // we just turn 90 degrees to hold onto that new wall!
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
            // Tilt forward slightly towards the block
            transform.rotation = Quaternion.LookRotation(forward) * Quaternion.Euler(15f, 0, 0);
        }
        else
        {
            // Normal upright rotation
            transform.rotation = Quaternion.LookRotation(forward);
        }
    }

    // =========================
    // CAMERA
    // =========================
    void HandleCamera()
    {
        float rotation = lookInput.x * cameraSpeed * Time.deltaTime;
        cameraPivot.Rotate(Vector3.up, rotation);
    }
}