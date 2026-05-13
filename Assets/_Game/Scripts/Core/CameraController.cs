using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("Pan")]
    public float panSpeed = 0.02f;
    public Vector2 xBounds = new Vector2(-5f, 25f);
    public Vector2 zBounds = new Vector2(-5f, 25f);

    [Header("Zoom")]
    public float zoomSpeed = 0.5f;
    public float pinchZoomSpeed = 0.05f;
    public float minZoom = 4f;
    public float maxZoom = 15f;

    [Header("Touch")]
    public float dragThreshold = 12f; // pixels before pan starts

    public bool IsPanning { get; private set; }

    private InputAction dragAction;
    private InputAction pointerPositionAction;
    private InputAction scrollAction;

    private bool isDragging;
    private Vector2 lastPointerPosition;
    private Vector2 dragStartPosition;
    private bool thresholdMet;
    private Camera cam;

    void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
        EnhancedTouchSupport.Enable();

        dragAction = new InputAction("Drag", InputActionType.Button);
        dragAction.AddBinding("<Mouse>/rightButton");

        pointerPositionAction = new InputAction("PointerPosition", InputActionType.Value,
            binding: "<Mouse>/position");

        scrollAction = new InputAction("Scroll", InputActionType.Value,
            binding: "<Mouse>/scroll/y");

        dragAction.Enable();
        pointerPositionAction.Enable();
        scrollAction.Enable();
    }

    void OnDestroy()
    {
        dragAction.Disable();
        pointerPositionAction.Disable();
        scrollAction.Disable();
        EnhancedTouchSupport.Disable();
    }

    void Update()
    {
        int touchCount = Touch.activeTouches.Count;

        if (touchCount >= 2)
        {
            HandlePinch();
            IsPanning = true;
        }
        else if (touchCount == 1)
        {
            HandleTouchPan(Touch.activeTouches[0]);
        }
        else
        {
            HandleMousePan();
            HandleScrollZoom();
        }
    }

    void HandleTouchPan(Touch touch)
    {
        if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
        {
            isDragging = true;
            thresholdMet = false;
            dragStartPosition = touch.screenPosition;
            lastPointerPosition = touch.screenPosition;
            IsPanning = false;
        }

        if (!isDragging) return;

        if (!thresholdMet)
        {
            if (Vector2.Distance(touch.screenPosition, dragStartPosition) > dragThreshold)
                thresholdMet = true;
            else
                return;
        }

        IsPanning = true;
        Vector2 delta = touch.delta;
        ApplyPanDelta(delta);

        if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended ||
            touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
        {
            isDragging = false;
            IsPanning = false;
        }
    }

    void HandlePinch()
    {
        Touch t0 = Touch.activeTouches[0];
        Touch t1 = Touch.activeTouches[1];

        // Pinch zoom
        Vector2 t0Prev = t0.screenPosition - t0.delta;
        Vector2 t1Prev = t1.screenPosition - t1.delta;
        float prevDist = Vector2.Distance(t0Prev, t1Prev);
        float currDist = Vector2.Distance(t0.screenPosition, t1.screenPosition);
        float pinchDelta = prevDist - currDist;
        cam.orthographicSize = Mathf.Clamp(
            cam.orthographicSize + pinchDelta * pinchZoomSpeed,
            minZoom, maxZoom);

        // Two-finger pan
        Vector2 centerDelta = (t0.delta + t1.delta) * 0.5f;
        ApplyPanDelta(centerDelta);
    }

    void HandleMousePan()
    {
        Vector2 currentPos = pointerPositionAction.ReadValue<Vector2>();

        if (dragAction.WasPressedThisFrame())
        {
            isDragging = true;
            thresholdMet = false;
            dragStartPosition = currentPos;
            lastPointerPosition = currentPos;
            IsPanning = false;
        }

        if (dragAction.WasReleasedThisFrame())
        {
            isDragging = false;
            IsPanning = false;
        }

        if (!isDragging) return;

        if (!thresholdMet)
        {
            if (Vector2.Distance(currentPos, dragStartPosition) > dragThreshold)
                thresholdMet = true;
            else
            {
                lastPointerPosition = currentPos;
                return;
            }
        }

        IsPanning = true;
        Vector2 delta = currentPos - lastPointerPosition;
        lastPointerPosition = currentPos;
        ApplyPanDelta(delta);
    }

    void HandleScrollZoom()
    {
        float scroll = scrollAction.ReadValue<float>();
        if (scroll == 0f) return;
        cam.orthographicSize = Mathf.Clamp(
            cam.orthographicSize - scroll * zoomSpeed,
            minZoom, maxZoom);
    }

    void ApplyPanDelta(Vector2 delta)
    {
        Vector3 move = new Vector3(-delta.x, 0f, -delta.y) * panSpeed * (cam.orthographicSize / 8f);
        Vector3 newPos = transform.position + move;
        newPos.x = Mathf.Clamp(newPos.x, xBounds.x, xBounds.y);
        newPos.z = Mathf.Clamp(newPos.z, zBounds.x, zBounds.y);
        transform.position = newPos;
    }

    /// <summary>Smoothly pan the camera so <paramref name="worldPos"/> is centred on screen.</summary>
    public void FocusOn(Vector3 worldPos, float duration = 0.35f)
        => StartCoroutine(AnimateFocus(worldPos, duration));

    System.Collections.IEnumerator AnimateFocus(Vector3 target, float duration)
    {
        Vector3 start = transform.position;
        Vector3 end   = new Vector3(
            Mathf.Clamp(target.x, xBounds.x, xBounds.y),
            transform.position.y,
            Mathf.Clamp(target.z, zBounds.x, zBounds.y));

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / duration), 3f); // ease-out cubic
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }
        transform.position = end;
    }
}
