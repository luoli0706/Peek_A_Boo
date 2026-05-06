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

    private PlayControls controls;

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = GetComponentInChildren<Camera>()?.transform;

        Debug.Log($"[PlayerCtrl] Start — camera={cameraTransform?.name ?? "NULL"}, GameManager={GameManager.Instance != null}");

        // Bypass PlayerInput component — subscribe directly to generated actions
        controls = new PlayControls();
        controls.Player.Move.performed += OnMove;
        controls.Player.Move.canceled += OnMove;
        controls.Player.Look.performed += OnLook;
        controls.Player.Look.canceled += OnLook;
        controls.Player.Crouch.performed += OnCrouch;
        controls.Player.Crouch.canceled += OnCrouch;
        controls.Player.Jump.performed += OnJump;
        controls.Player.Jump.canceled += OnJump;
        controls.Player.Enable();
        Debug.Log("[PlayerCtrl] PlayControls action map enabled");

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

    void OnMove(InputAction.CallbackContext ctx)
    {
        // Event kept for debug; actual movement uses controls.Player.Move.ReadValue in Update()
        Debug.Log($"[PlayerCtrl] OnMove — phase={ctx.phase}, val={ctx.ReadValue<Vector2>()}");
    }

    void OnLook(InputAction.CallbackContext ctx)
    {
        Debug.Log($"[PlayerCtrl] OnLook — phase={ctx.phase}, val={ctx.ReadValue<Vector2>()}");
        if (!inputEnabled) return;

        Vector2 delta = ctx.ReadValue<Vector2>() * mouseSensitivity * 0.05f;

        cameraPitch -= delta.y;
        cameraPitch = Mathf.Clamp(cameraPitch, -89f, 89f);

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);

        transform.Rotate(Vector3.up, delta.x);
    }

    void OnCrouch(InputAction.CallbackContext ctx)
    {
        Debug.Log($"[PlayerCtrl] OnCrouch — phase={ctx.phase}");
        if (!inputEnabled) { crouchHeld = false; return; }
        crouchHeld = ctx.ReadValueAsButton();
    }

    void OnJump(InputAction.CallbackContext ctx)
    {
        Debug.Log($"[PlayerCtrl] OnJump — phase={ctx.phase}");
        if (!inputEnabled) { jumpTriggered = false; return; }
        if (ctx.performed) jumpTriggered = true;
    }

    void Update()
    {
        if (!inputEnabled) return;

        // Poll current input directly — events only fire on change, held keys need polling
        moveInput = controls.Player.Move.ReadValue<Vector2>();

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
