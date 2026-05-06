using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float lookSensitivity = 0.5f;
    public float jumpForce = 5f;
    public float gravity = -15f;

    [Header("Camera")]
    public Camera playerCamera;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool isLocked = true;
    private bool crouchHeld;
    private bool jumpTriggered;
    private float xRotation;
    private float verticalVelocity;
    private float groundY;

    void Start()
    {
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
            playerCamera = Camera.main;
        if (playerCamera == null)
            Debug.LogError("[PlayerCtrl] No camera found! Add Camera as child or tag as MainCamera.");

        isLocked = true;
        groundY = transform.position.y;

        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += OnGameStateChanged;

        Debug.Log($"[PlayerCtrl] Start camera={playerCamera?.name ?? "NULL"}, groundY={groundY}");
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
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                break;
            case GameState.GameOver:
                isLocked = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
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
        // Horizontal movement
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        transform.position += move * moveSpeed * Time.deltaTime;

        // Vertical jump / gravity
        if (jumpTriggered && Mathf.Abs(transform.position.y - groundY) < 0.1f)
        {
            verticalVelocity = jumpForce;
        }

        verticalVelocity += gravity * Time.deltaTime;
        transform.position += Vector3.up * verticalVelocity * Time.deltaTime;

        // Ground clamp
        if (transform.position.y < groundY)
        {
            transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
            verticalVelocity = 0f;
        }
    }

    public void ApplyServerPosition(Vector3 position, float yaw)
    {
        transform.position = position;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }
}
