using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Follow Settings (Normal)")]
    public float followSmoothTimeX = 0.1f; 
    public float followSmoothTimeY = 0.5f; 

    [Header("Follow Settings (Zoom Mode)")]
    public float zoomFollowSmoothTime = 0.5f;

    [Header("Offset")]
    public Vector3 offset = new Vector3(0, 1, -10);

    [Header("Free Look / Zoom Settings (Left Ctrl)")]
    public float zoomOutSize = 10f; 
    public float zoomSpeed = 5f;

    [Header("Free Look Distances")]
    [Tooltip("위쪽으로 살펴볼 거리")]
    public float freeLookDistUp = 6f;
    [Tooltip("아래쪽으로 살펴볼 거리")]
    public float freeLookDistDown = 6f;
    [Tooltip("왼쪽으로 살펴볼 거리")]
    public float freeLookDistLeft = 6f;
    [Tooltip("오른쪽으로 살펴볼 거리")]
    public float freeLookDistRight = 6f;

    [Space(10)]
    public float freeLookSmoothTime = 0.3f; 

    // 내부 변수
    private float currentVelocityX;
    private float currentVelocityY;
    
    private float currentLookOffsetX; 
    private float currentLookOffsetY; 
    private float lookVelocityX;
    private float lookVelocityY;

    private Camera cam;
    private float defaultSize;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam != null) defaultSize = cam.orthographicSize;
    }

    void LateUpdate()
    {
        if (target == null) return;

        bool isZooming = Input.GetKey(KeyCode.LeftControl);
        HandleCameraMode(isZooming);

        float targetX = target.position.x + offset.x + currentLookOffsetX;
        float targetY = target.position.y + offset.y + currentLookOffsetY;
        float targetZ = target.position.z + offset.z;

        float currentSmoothX = isZooming ? zoomFollowSmoothTime : followSmoothTimeX;
        float currentSmoothY = isZooming ? zoomFollowSmoothTime : followSmoothTimeY;

        // 시간 정지 시에도 카메라 이동을 위해 unscaledDeltaTime 사용
        float newX = Mathf.SmoothDamp(transform.position.x, targetX, ref currentVelocityX, currentSmoothX, Mathf.Infinity, Time.unscaledDeltaTime);
        float newY = Mathf.SmoothDamp(transform.position.y, targetY, ref currentVelocityY, currentSmoothY, Mathf.Infinity, Time.unscaledDeltaTime);

        transform.position = new Vector3(newX, newY, targetZ);
    }

    void HandleCameraMode(bool isZooming)
    {
        if (isZooming)
        {
            if (cam != null)
            {
                cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, zoomOutSize, Time.unscaledDeltaTime * zoomSpeed);
            }

            float xInput = Input.GetAxisRaw("Horizontal");
            float yInput = Input.GetAxisRaw("Vertical");
            Vector2 inputDir = new Vector2(xInput, yInput).normalized;

            // [수정] 방향별로 다른 거리 적용
            float targetOffX = 0f;
            if (inputDir.x > 0) targetOffX = inputDir.x * freeLookDistRight;
            else if (inputDir.x < 0) targetOffX = inputDir.x * freeLookDistLeft; // inputDir.x가 음수이므로 결과도 음수(좌측 이동)

            float targetOffY = 0f;
            if (inputDir.y > 0) targetOffY = inputDir.y * freeLookDistUp;
            else if (inputDir.y < 0) targetOffY = inputDir.y * freeLookDistDown; // inputDir.y가 음수이므로 결과도 음수(하측 이동)

            // unscaledDeltaTime을 사용하여 시간 정지 중에도 부드럽게 이동
            currentLookOffsetX = Mathf.SmoothDamp(currentLookOffsetX, targetOffX, ref lookVelocityX, freeLookSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            currentLookOffsetY = Mathf.SmoothDamp(currentLookOffsetY, targetOffY, ref lookVelocityY, freeLookSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        }
        else
        {
            if (cam != null)
            {
                cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, defaultSize, Time.unscaledDeltaTime * zoomSpeed);
            }

            // 복귀
            currentLookOffsetX = Mathf.SmoothDamp(currentLookOffsetX, 0f, ref lookVelocityX, freeLookSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            currentLookOffsetY = Mathf.SmoothDamp(currentLookOffsetY, 0f, ref lookVelocityY, freeLookSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        }
    }
}