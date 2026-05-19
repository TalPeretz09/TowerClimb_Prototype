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
    public float cameraFollowSpeed = 5f;

    // VISUALS
    [Header("Visuals")]
    public GameObject armsObject;
    public GameObject dustParticlePrefab;

    private Vector3 originalArmsPos;
    private Quaternion originalArmsRot;

    void Awake()
    {
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
        gridPosition = Vector3Int.RoundToInt(transform.position);
        transform.position = gridPosition;

        if (cameraPivot != null)
        {
            Vector3 startPos = cameraPivot.position;
            startPos.y = transform.position.y;
            cameraPivot.position = startPos;
        }

        if (armsObject != null)
        {
            originalArmsPos = armsObject.transform.localPosition;
            originalArmsRot = armsObject.transform.localRotation;
            armsObject.SetActive(false);
        }
    }

    void Update()
    {
        // --- COUNTDOWN / PAUSE STATE ---
        if (GameManager.Instance != null && !GameManager.Instance.isPlaying)
        {
            moveHoldTime = 0f;

            // NEW: Safety check to keep arms completely disabled during countdown
            if (armsObject != null && armsObject.activeSelf)
            {
                armsObject.SetActive(false);
            }
            return;
        }

        // --- GAME PLAYING STATE ---
        // NEW: Automatically syncs the arms if the player was holding the button when the countdown ended
        if (armsObject != null && !isHanging && armsObject.activeSelf != interactHeld)
        {
            UpdateArmsState();
        }

        CheckGravity();
        HandleMovement();
        CheckFloorInteractions();
    }

    // =========================
    // ARMS STATE MANAGER
    // =========================
    void UpdateArmsState()
    {
        if (armsObject == null) return;

        // NEW: Prevent arms from being enabled manually if the game hasn't started
        if (GameManager.Instance != null && !GameManager.Instance.isPlaying)
        {
            armsObject.SetActive(false);
            armsObject.transform.localPosition = originalArmsPos;
            armsObject.transform.localRotation = originalArmsRot;
            return;
        }

        if (isHanging)
        {
            armsObject.SetActive(true);
            armsObject.transform.localPosition = new Vector3(originalArmsPos.x, 0.5f, originalArmsPos.z);
            armsObject.transform.localRotation = Quaternion.Euler(-60f, originalArmsRot.eulerAngles.y, originalArmsRot.eulerAngles.z);
        }
        else
        {
            armsObject.SetActive(interactHeld);
            armsObject.transform.localPosition = originalArmsPos;
            armsObject.transform.localRotation = originalArmsRot;
        }
    }

    // =========================
    // EFFECTS
    // =========================
    void SpawnDust(Vector3 position)
    {
        if (dustParticlePrefab != null)
        {
            // Spawn the dust at the block's position, shifted slightly down towards the floor
            Instantiate(dustParticlePrefab, position + (Vector3.down * 0.4f), Quaternion.Euler(-90, 0, 0));
        }
    }

    // =========================
    // GRAVITY
    // =========================
    void CheckGravity()
    {
        if (isHanging) return;

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
                lastStandingPos = new Vector3Int(9999, 9999, 9999);
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
    }

    // =========================
    // FLOOR INTERACTIONS
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
            }

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
    // HANGING MOVEMENT
    // =========================
    void TryHangMovement(Vector3Int dir)
    {
        // 1. CLIMB UP
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
        // 2. DROP DOWN
        else if (dir == -currentFacing)
        {
            isHanging = false;
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
        // 3. SHIMMY
        else
        {
            float dot = Vector3.Dot((Vector3)dir, (Vector3)currentFacing);

            if (Mathf.Abs(dot) < 0.1f)
            {
                Vector3Int targetPos = gridPosition + dir;
                Vector3Int targetHeadPos = targetPos + Vector3Int.up;
                Vector3Int targetGrabBlock = targetPos + currentFacing;

                if (!HasBlock(targetPos) && !HasBlock(targetHeadPos) && HasBlock(targetGrabBlock))
                {
                    gridPosition = targetPos;
                    transform.position = gridPosition;
                }
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
    // BASIC MOVEMENT
    // =========================
    void TryMove(Vector3Int dir)
    {
        Vector3Int target = gridPosition + dir;

        if (HasBlock(target)) return;

        if (CanStand(target))
        {
            gridPosition = target;
            transform.position = gridPosition;
        }
        else
        {
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
                if (hits[0].CompareTag("Immovable")) return;
                blocksToMove.Add(hits[0].transform);
            }
            checkPos += dir;
        }

        if (!HasBlock(checkPos))
        {
            for (int i = blocksToMove.Count - 1; i >= 0; i--)
            {
                // NEW: Spawn dust at the OLD position before moving the block
                SpawnDust(blocksToMove[i].position);
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
        Collider[] targetHits = Physics.OverlapBox(
            blockPos,
            Vector3.one * 0.4f,
            Quaternion.identity,
            LayerMask.GetMask("Block")
        );

        if (targetHits.Length > 0 && targetHits[0].CompareTag("Immovable")) return;

        Vector3Int behind = gridPosition - currentFacing;

        if (!HasBlock(behind))
        {
            // NEW: Spawn dust at the block's OLD position before pulling it
            SpawnDust(blockPos);

            MoveBlock(blockPos, gridPosition);

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

        // NEW: Only allow the camera to rotate horizontally if the game is actually active
        if (GameManager.Instance != null && GameManager.Instance.isPlaying)
        {
            float rotation = lookInput.x * cameraSpeed * Time.deltaTime;
            cameraPivot.Rotate(Vector3.up, rotation);
        }

        // Smooth Omnidirectional Tracking remains active so the camera behaves normally on spawn/snap
        Vector3 targetPos = transform.position;
        cameraPivot.position = Vector3.Lerp(cameraPivot.position, targetPos, cameraFollowSpeed * Time.deltaTime);
    }
}