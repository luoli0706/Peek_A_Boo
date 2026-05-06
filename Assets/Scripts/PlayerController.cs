using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5.0f;
    public float crouchSpeed = 2.0f;
    public float mouseSensitivity = 0.3f;

    [Header("References")]
    public Transform cameraTransform;

    private float cameraPitch;
    private bool inputEnabled = false;
    private Vector2 moveInput;
    private bool crouchHeld;
    private bool jumpTriggered;

    private PlayControls controls;

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = GetComponentInChildren<Camera>()?.transform;

        Debug.Log($"[PlayerCtrl] Start camera={cameraTransform?.name ?? "NULL"}");

        controls = new PlayControls();
        controls.Player.Enable();

        // Button actions still use events (one-shot triggers)
        controls.Player.Crouch.performed += OnCrouch;
        controls.Player.Crouch.canceled += OnCrouch;
        controls.Player.Jump.performed += OnJump;
        controls.Player.Jump.canceled += OnJump;

        Debug.Log("[PlayerCtrl] Input actions enabled");

        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += OnGameStateChanged;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;
        controls?.Dispose();
    }

    void OnGameStateChanged(GameState state, ushort countdown)
    {
        Debug.Log($"[PlayerCtrl] State={state}");
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

    void Update()
    {
        if (!inputEnabled) return;

        // Poll Move and Look directly — events don't fire every frame for held keys / continuous mouse
        moveInput = controls.Player.Move.ReadValue<Vector2>();
        Vector2 lookDelta = controls.Player.Look.ReadValue<Vector2>();

        if (lookDelta.sqrMagnitude > 0.01f)
        {
            lookDelta *= mouseSensitivity;

            cameraPitch -= lookDelta.y;
            cameraPitch = Mathf.Clamp(cameraPitch, -89f, 89f);

            if (cameraTransform != null)
                cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);

            transform.Rotate(Vector3.up, lookDelta.x);
        }

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
            NetworkManager.Instance.Send(1, false, data);
        }

        jumpTriggered = false;
    }

    void OnCrouch(InputAction.CallbackContext ctx)
    {
        if (!inputEnabled) { crouchHeld = false; return; }
        crouchHeld = ctx.ReadValueAsButton();
    }

    void OnJump(InputAction.CallbackContext ctx)
    {
        if (!inputEnabled) { jumpTriggered = false; return; }
        if (ctx.performed) jumpTriggered = true;
    }

    public void EnableInput()
    {
        inputEnabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Debug.Log("[PlayerCtrl] Input enabled, cursor locked");
    }

    public void DisableInput()
    {
        inputEnabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ApplyServerPosition(Vector3 position, float yaw)
    {
        transform.position = position;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }
}
