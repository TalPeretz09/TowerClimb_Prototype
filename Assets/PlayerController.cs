using UnityEngine;
using UnityEngine.InputSystem;

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
    // INPUT ? GRID DIRECTION
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

        // TAP ? rotate only
        if (moveHoldTime < holdThreshold)
        {
            if (currentFacing != dir)
            {
                currentFacing = dir;
                RotatePlayer(dir);
            }
        }
        // HOLD ? move / push / pull
        else
        {
            TryMoveOrPush(dir);
            moveHoldTime = 0f;
        }
    }

    // =========================
    // MOVE OR PUSH/PULL
    // =========================
    void TryMoveOrPush(Vector3Int dir)
    {
        Vector3Int front = gridPosition + currentFacing;

        // PUSH / PULL
        if (interactHeld && HasBlock(front))
        {
            if (dir == currentFacing)
            {
                PushBlock(front, dir);
                return;
            }
            else if (dir == -currentFacing)
            {
                PullBlock(front, dir);
                return;
            }
        }

        //  CLIMB (NEW)
        if (dir == currentFacing && CanClimb(dir))
        {
            TryClimb(dir);
            return;
        }

        //  NORMAL MOVE
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
    // PUSH
    // =========================
    void PushBlock(Vector3Int blockPos, Vector3Int dir)
    {
        Vector3Int target = blockPos + dir;

        if (!HasBlock(target))
        {
            MoveBlock(blockPos, target);
            //DO NOT move player
        }
    }

    // =========================
    // PULL
    // =========================
    void PullBlock(Vector3Int blockPos, Vector3Int dir)
    {
        Vector3Int behind = gridPosition + dir;

        if (!HasBlock(behind) && CanStand(behind))
        {
            MoveBlock(blockPos, gridPosition);

            gridPosition = behind;
            transform.position = gridPosition;
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
        }
    }

    // =========================
    // ROTATION
    // =========================
    void RotatePlayer(Vector3Int dir)
    {
        Vector3 forward = new Vector3(dir.x, 0, dir.z);
        transform.forward = forward;
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