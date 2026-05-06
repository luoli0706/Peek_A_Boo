using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float lookSensitivity = 0.5f;

    [Header("Camera")]
    public Camera playerCamera;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool isLocked = true;
    private bool crouchHeld;
    private bool jumpTriggered;
    private float xRotation;

    void Start()
    {
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Start locked until GameManager enables us
        isLocked = true;

        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += OnGameStateChanged;

        Debug.Log($"[PlayerCtrl] Start camera={playerCamera?.name ?? "NULL"}");
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= OnGameStateChanged;
    }

    void OnGameStateChanged(GameState state, ushort countdown)
    {
        switch (state)
        {
            case GameState.WaitingForPlayers:
            case GameState.Preparing:
            case GameState.Hiding:
            case GameState.Seeking:
            case GameState.RoundEnd:
                isLocked = false;
                break;
            case GameState.GameOver:
                isLocked = true;
                break;
        }
    }

    // ── Called by PlayerInput (SendMessages) ──

    public void OnMove(InputValue value)
    {
        if (isLocked) return;
        moveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        if (isLocked) return;
        lookInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (isLocked) return;
        if (value.isPressed) jumpTriggered = true;
    }

    public void OnCrouch(InputValue value)
    {
        if (isLocked) return;
        crouchHeld = value.isPressed;
    }

    // ── Update ──

    void Update()
    {
        if (!isLocked)
        {
            HandleRotation();
            HandleMovement();
        }

        // Send input to server
        if (NetworkManager.Instance != null && NetworkManager.Instance.IsConnected)
        {
            float rotY = transform.eulerAngles.y;
            byte[] data = ClientProtocol.SerializePlayerInput(
                moveInput.x, moveInput.y, rotY, crouchHeld, jumpTriggered);
            NetworkManager.Instance.Send(1, false, data);
        }

        jumpTriggered = false;
    }

    void HandleRotation()
    {
        if (playerCamera == null) return;

        float mouseX = lookInput.x * lookSensitivity;
        float mouseY = lookInput.y * lookSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    void HandleMovement()
    {
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        transform.position += move * moveSpeed * Time.deltaTime;
    }

    public void ApplyServerPosition(Vector3 position, float yaw)
    {
        transform.position = position;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }
}
