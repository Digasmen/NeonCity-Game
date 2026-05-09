using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Pan")]
    public float panSpeed = 0.02f;
    public Vector2 xBounds = new Vector2(-5f, 25f);
    public Vector2 zBounds = new Vector2(-5f, 25f);

    [Header("Zoom")]
    public float zoomSpeed = 0.5f;
    public float minZoom = 4f;
    public float maxZoom = 15f;

    private InputAction dragAction;
    private InputAction pointerPositionAction;
    private InputAction scrollAction;

    private bool isDragging;
    private Vector2 lastPointerPosition;
    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();

        // Right mouse button + single touch both trigger pan
        dragAction = new InputAction("Drag", InputActionType.Button);
        dragAction.AddBinding("<Mouse>/rightButton");
        dragAction.AddBinding("<Touchscreen>/primaryTouch/press");

        pointerPositionAction = new InputAction("PointerPosition", InputActionType.Value, binding: "<Mouse>/position");
        pointerPositionAction.AddBinding("<Touchscreen>/primaryTouch/position");

        scrollAction = new InputAction("Scroll", InputActionType.Value, binding: "<Mouse>/scroll/y");

        dragAction.Enable();
        pointerPositionAction.Enable();
        scrollAction.Enable();
    }

    void OnDestroy()
    {
        dragAction.Disable();
        pointerPositionAction.Disable();
        scrollAction.Disable();
    }

    void Update()
    {
        HandlePan();
        HandleZoom();
    }

    void HandlePan()
    {
        Vector2 currentPos = pointerPositionAction.ReadValue<Vector2>();

        if (dragAction.WasPressedThisFrame())
        {
            isDragging = true;
            lastPointerPosition = currentPos;
        }

        if (dragAction.WasReleasedThisFrame())
            isDragging = false;

        if (!isDragging) return;

        Vector2 delta = currentPos - lastPointerPosition;
        lastPointerPosition = currentPos;

        Vector3 move = new Vector3(-delta.x, 0f, -delta.y) * panSpeed * (cam.orthographicSize / 8f);
        Vector3 newPos = transform.position + move;
        newPos.x = Mathf.Clamp(newPos.x, xBounds.x, xBounds.y);
        newPos.z = Mathf.Clamp(newPos.z, zBounds.x, zBounds.y);
        transform.position = newPos;
    }

    void HandleZoom()
    {
        float scroll = scrollAction.ReadValue<float>();
        if (scroll == 0f) return;

        cam.orthographicSize = Mathf.Clamp(
            cam.orthographicSize - scroll * zoomSpeed,
            minZoom, maxZoom);
    }
}
