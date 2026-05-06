using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5.0f;
    public float crouchSpeed = 2.0f;
    public float mouseSensitivity = 2.0f;

    [Header("References")]
    public Transform cameraTransform;

    private float cameraPitch;
    private bool inputEnabled = false;
    private Vector2 moveInput;
    private bool crouchHeld;
    private bool jumpTriggered;

    private float debugLogTimer;
    private int moveCallCount;
    private int lookCallCount;

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = GetComponentInChildren<Camera>()?.transform;

        Debug.Log($"[PlayerCtrl] Start — camera={cameraTransform?.name ?? "NULL"}, GameManager={GameManager.Instance != null}");
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += OnGameStateChanged;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;
    }

    void OnGameStateChanged(GameState state, ushort countdown)
    {
        Debug.Log($"[PlayerCtrl] OnGameStateChanged: {state}, countdown={countdown}");
        switch (state)
        {
            case GameState.WaitingForPlayers:
            case GameState.Preparing:
            case GameState.Hiding:
            case GameState.Seeking:
            case GameState.RoundEnd:
                EnableInput();
                break;
            case GameState.GameOver:
                DisableInput();
                break;
        }
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (moveCallCount < 5)
            Debug.Log($"[PlayerCtrl] OnMove called — phase={ctx.phase}, value={ctx.ReadValue<Vector2>()}, inputEnabled={inputEnabled}");
        moveCallCount++;

        if (!inputEnabled) { moveInput = Vector2.zero; return; }
        moveInput = ctx.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext ctx)
    {
        if (lookCallCount < 5)
            Debug.Log($"[PlayerCtrl] OnLook called — phase={ctx.phase}, value={ctx.ReadValue<Vector2>()}, inputEnabled={inputEnabled}");
        lookCallCount++;

        if (!inputEnabled) return;

        Vector2 delta = ctx.ReadValue<Vector2>() * mouseSensitivity * Time.deltaTime;

        cameraPitch -= delta.y;
        cameraPitch = Mathf.Clamp(cameraPitch, -89f, 89f);

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);

        transform.Rotate(Vector3.up, delta.x);
    }

    public void OnCrouch(InputAction.CallbackContext ctx)
    {
        Debug.Log($"[PlayerCtrl] OnCrouch called — phase={ctx.phase}, inputEnabled={inputEnabled}");
        if (!inputEnabled) { crouchHeld = false; return; }
        crouchHeld = ctx.ReadValueAsButton();
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        Debug.Log($"[PlayerCtrl] OnJump called — phase={ctx.phase}, inputEnabled={inputEnabled}");
        if (!inputEnabled) { jumpTriggered = false; return; }
        if (ctx.performed) jumpTriggered = true;
    }

    void Update()
    {
        debugLogTimer -= Time.deltaTime;
        if (debugLogTimer <= 0f)
        {
            debugLogTimer = 1f;
            Debug.Log($"[PlayerCtrl] Update — inputEnabled={inputEnabled}, moveInput={moveInput}, pos={transform.position}");
        }

        if (!inputEnabled) return;

        // Local movement
        float speed = crouchHeld ? crouchSpeed : moveSpeed;
        Vector3 move = new Vector3(moveInput.x, 0f, moveInput.y);
        move = transform.TransformDirection(move) * speed * Time.deltaTime;
        transform.position += move;

        // Send input to server every frame
        if (NetworkManager.Instance != null && NetworkManager.Instance.IsConnected)
        {
            float rotY = transform.eulerAngles.y;
            byte[] data = ClientProtocol.SerializePlayerInput(
                moveInput.x, moveInput.y, rotY, crouchHeld, jumpTriggered);
            NetworkManager.Instance.Send(1, false, data); // ch1 unreliable
        }

        jumpTriggered = false;
    }

    public void EnableInput()
    {
        inputEnabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void DisableInput()
    {
        inputEnabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Called when server sends authoritative position correction (Phase 3)
    public void ApplyServerPosition(Vector3 position, float yaw)
    {
        transform.position = position;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }
}
