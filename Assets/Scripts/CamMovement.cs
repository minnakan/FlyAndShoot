using UnityEngine;

public class CamMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float fastMoveSpeed = 20f;
    [SerializeField] private float mouseSensitivity = 3f;
    [SerializeField] private float smoothTime = 0.1f;

    private Vector3 currentVelocity;
    private float rotationX;
    private float rotationY;
    private bool cursorLocked = false;

    void Start()
    {
        // Start with cursor unlocked
        UnlockCursor();
    }

    void Update()
    {
        HandleInput();
        HandleMovement();
        HandleRotation();
    }

    void HandleInput()
    {
        // Toggle cursor lock with Right Mouse Button
        if (Input.GetMouseButtonDown(1))
        {
            if (cursorLocked)
                UnlockCursor();
            else
                LockCursor();
        }
    }

    void HandleMovement()
    {
        if (!cursorLocked) return;

        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? fastMoveSpeed : moveSpeed;

        Vector3 moveDirection = new Vector3(
            Input.GetAxisRaw("Horizontal"),
            Input.GetKey(KeyCode.Space) ? 1f : Input.GetKey(KeyCode.LeftControl) ? -1f : 0f,
            Input.GetAxisRaw("Vertical")
        ).normalized;

        // Transform direction to be relative to camera orientation
        Vector3 targetVelocity = transform.TransformDirection(moveDirection) * currentSpeed;

        // Smooth movement
        transform.position += Vector3.SmoothDamp(
            Vector3.zero,
            targetVelocity * Time.deltaTime,
            ref currentVelocity,
            smoothTime
        );
    }

    void HandleRotation()
    {
        if (!cursorLocked) return;

        // Get mouse input
        rotationX -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        rotationY += Input.GetAxis("Mouse X") * mouseSensitivity;

        // Clamp vertical rotation
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);

        // Apply rotation
        transform.rotation = Quaternion.Euler(rotationX, rotationY, 0);
    }

    void LockCursor()
    {
        cursorLocked = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void UnlockCursor()
    {
        cursorLocked = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}