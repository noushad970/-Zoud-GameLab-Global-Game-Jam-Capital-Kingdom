using UnityEngine;
using UnityEngine.EventSystems;

namespace EmpireBuilder.World
{
    /// <summary>
    /// Clash-of-Clans style camera controller with responsive touch/swipe panning and pinch-to-zoom.
    /// Clamped to:
    /// - X axis: -23 to 140
    /// - Z axis: -144 to 122
    /// - Y axis (Zoom in / Zoom out): 38 to 80
    /// </summary>
    public class IsometricCameraController : MonoBehaviour
    {
        [Header("Target Camera")]
        [SerializeField] private Camera targetCamera;

        [Header("Axis Limitations (User Specified)")]
        [SerializeField] private Vector2 xLimits = new Vector2(-23f, 140f);
        [SerializeField] private Vector2 zLimits = new Vector2(-144f, 122f);
        [SerializeField] private float minZoomY = 38f;
        [SerializeField] private float maxZoomY = 80f;

        [Header("Swipe & Drag Settings")]
        [SerializeField] private float dragPanSpeed = 0.08f;
        [SerializeField] private float keyboardSpeed = 35f;
        [SerializeField] private bool useInertia = true;
        [Range(0.80f, 0.98f)] [SerializeField] private float inertiaDamping = 0.90f;

        [Header("Touch Zoom (Pinch / Scroll)")]
        [SerializeField] private float pinchZoomSpeed = 0.08f;
        [SerializeField] private float mouseScrollSpeed = 6f;
        [SerializeField] private float zoomSmoothDamp = 12f;

        private Vector2 lastPointerPos;
        private bool isPointerDragging = false;
        private Vector3 currentVelocity = Vector3.zero;
        private Vector3 previousFramePos;
        private float targetZoomHeight;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>() ?? Camera.main;
            }

            targetZoomHeight = Mathf.Clamp(transform.position.y, minZoomY, maxZoomY);
            previousFramePos = transform.position;
        }

        private void Start()
        {
            // Initial position clamp
            ClampCameraPosition();
        }

        private void Update()
        {
            HandleTouchControls();
            HandleMouseControls();
            HandleKeyboardControls();
            ApplyInertiaMovement();
            ApplyZoomSmoothing();
            ClampCameraPosition();
        }

        #region Touch Controls (1-finger swipe, 2-finger pinch)
        private void HandleTouchControls()
        {
            if (Input.touchCount == 0) return;

            // 1-Finger Touch: Swiping / Panning (Left, Right, Up, Down)
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Began)
                {
                    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    {
                        isPointerDragging = false;
                        return;
                    }

                    lastPointerPos = touch.position;
                    isPointerDragging = true;
                    currentVelocity = Vector3.zero;
                }
                else if (isPointerDragging && (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary))
                {
                    Vector2 screenDelta = touch.deltaPosition;
                    MoveCameraByScreenDelta(screenDelta);
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    isPointerDragging = false;
                }
            }
            // 2-Finger Touch: Pinch to Zoom In / Zoom Out (Y-Axis)
            else if (Input.touchCount == 2)
            {
                isPointerDragging = false;
                currentVelocity = Vector3.zero;

                Touch t0 = Input.GetTouch(0);
                Touch t1 = Input.GetTouch(1);

                Vector2 t0Prev = t0.position - t0.deltaPosition;
                Vector2 t1Prev = t1.position - t1.deltaPosition;

                float prevDistance = Vector2.Distance(t0Prev, t1Prev);
                float currentDistance = Vector2.Distance(t0.position, t1.position);
                float pinchDelta = currentDistance - prevDistance;

                // Pinch in = zoom out (+Y), Pinch out = zoom in (-Y)
                targetZoomHeight -= pinchDelta * pinchZoomSpeed;
                targetZoomHeight = Mathf.Clamp(targetZoomHeight, minZoomY, maxZoomY);
            }
        }
        #endregion

        #region Mouse Controls (Desktop / Editor drag & wheel)
        private void HandleMouseControls()
        {
            if (Input.touchCount > 0) return;

            // Mouse Drag to Swipe Left, Right, Up, Down
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    isPointerDragging = false;
                    return;
                }

                lastPointerPos = Input.mousePosition;
                isPointerDragging = true;
                currentVelocity = Vector3.zero;
            }
            else if (isPointerDragging && (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2)))
            {
                Vector2 currentMouse = Input.mousePosition;
                Vector2 screenDelta = currentMouse - lastPointerPos;
                lastPointerPos = currentMouse;

                MoveCameraByScreenDelta(screenDelta);
            }
            else if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2))
            {
                isPointerDragging = false;
            }

            // Mouse Wheel Zoom In / Out
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                targetZoomHeight -= scroll * mouseScrollSpeed * 10f;
                targetZoomHeight = Mathf.Clamp(targetZoomHeight, minZoomY, maxZoomY);
            }
        }
        #endregion

        #region Movement & Inertia
        private void MoveCameraByScreenDelta(Vector2 deltaPixel)
        {
            if (targetCamera == null) return;

            // Calculate camera forward & right projected onto the horizontal XZ plane
            Vector3 camForward = targetCamera.transform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = targetCamera.transform.right;
            camRight.y = 0f;
            camRight.Normalize();

            // Scale factor relative to camera height (zoom distance) so panning feels natural at any zoom
            float heightScale = transform.position.y / 50f;
            float factor = dragPanSpeed * heightScale;

            // Swiping right moves view left; swiping up moves view forward (natural drag)
            Vector3 worldMove = (-deltaPixel.x * camRight - deltaPixel.y * camForward) * factor;

            transform.position += worldMove;

            // Record velocity for fling inertia on release
            Vector3 frameDelta = transform.position - previousFramePos;
            currentVelocity = frameDelta / Mathf.Max(Time.deltaTime, 0.001f);
            previousFramePos = transform.position;
        }

        private void HandleKeyboardControls()
        {
            if (isPointerDragging) return;

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
            {
                Vector3 camForward = targetCamera.transform.forward;
                camForward.y = 0f;
                camForward.Normalize();

                Vector3 camRight = targetCamera.transform.right;
                camRight.y = 0f;
                camRight.Normalize();

                Vector3 dir = (camForward * v + camRight * h).normalized;
                transform.position += dir * (keyboardSpeed * Time.deltaTime);
                currentVelocity = Vector3.zero;
            }
        }

        private void ApplyInertiaMovement()
        {
            if (isPointerDragging || !useInertia) return;

            if (currentVelocity.sqrMagnitude > 0.01f)
            {
                transform.position += currentVelocity * Time.deltaTime;
                currentVelocity *= Mathf.Pow(inertiaDamping, Time.deltaTime * 60f);
            }
            else
            {
                currentVelocity = Vector3.zero;
            }
        }

        private void ApplyZoomSmoothing()
        {
            if (Mathf.Abs(transform.position.y - targetZoomHeight) > 0.01f)
            {
                Vector3 pos = transform.position;
                pos.y = Mathf.Lerp(pos.y, targetZoomHeight, Time.deltaTime * zoomSmoothDamp);
                transform.position = pos;
            }
        }

        private void ClampCameraPosition()
        {
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, xLimits.x, xLimits.y);
            pos.y = Mathf.Clamp(pos.y, minZoomY, maxZoomY);
            pos.z = Mathf.Clamp(pos.z, zLimits.x, zLimits.y);
            transform.position = pos;
        }
        #endregion
    }
}
