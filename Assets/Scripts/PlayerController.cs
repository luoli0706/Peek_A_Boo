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
    private Vector2 lookDelta;
    private bool crouchHeld;
    private bool jumpTriggered;

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = GetComponentInChildren<Camera>()?.transform;

        Debug.Log($"[PlayerCtrl] Start camera={cameraTransform?.name ?? "NULL"}");

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

    // Called by PlayerInput (SendMessages) — always caches input, Update() gates application
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        lookDelta = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed) jumpTriggered = true;
    }

    public void OnCrouch(InputValue value)
    {
        crouchHeld = value.isPressed;
    }

    void Update()
    {
        if (!inputEnabled) return;

        // Apply mouse look
        if (lookDelta.sqrMagnitude > 0.0001f)
        {
            Vector2 delta = lookDelta * mouseSensitivity;

            cameraPitch -= delta.y;
            cameraPitch = Mathf.Clamp(cameraPitch, -89f, 89f);

            if (cameraTransform != null)
                cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);

            transform.Rotate(Vector3.up, delta.x);
        }

        // Apply movement
        float speed = crouchHeld ? crouchSpeed : moveSpeed;
        Vector3 move = new Vector3(moveInput.x, 0f, moveInput.y);
        move = transform.TransformDirection(move) * speed * Time.deltaTime;
        transform.position += move;

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

    public void EnableInput()
    {
        inputEnabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Debug.Log("[PlayerCtrl] Input enabled");
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
