using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Follow Settings (Normal)")]
    [Tooltip("평소 좌우 이동 부드러움 (낮을수록 빠릿함)")]
    public float followSmoothTimeX = 0.1f; 
    [Tooltip("평소 상하 이동 부드러움")]
    public float followSmoothTimeY = 0.5f; 

    [Header("Follow Settings (Zoom Mode)")]
    [Tooltip("줌 모드일 때의 이동 부드러움 (멀미 방지를 위해 높게 설정 권장)")]
    public float zoomFollowSmoothTime = 0.5f; // [추가] 줌 상태일 때는 이 값을 사용

    [Header("Offset")]
    public Vector3 offset = new Vector3(0, 1, -10);

    [Header("Look Up/Down Settings (Normal)")]
    public float lookSmoothTime = 0.2f; 
    public float lookInputTime = 0.5f;  
    public float lookUpDistance = 4f;   
    public float lookDownDistance = 2f; 

    [Header("Free Look / Zoom Settings (Left Ctrl)")]
    public float zoomOutSize = 10f; 
    public float zoomSpeed = 5f;
    public float freeLookDistance = 6f; 
    public float freeLookSmoothTime = 0.3f; // [수정] 오프셋 변화도 조금 더 부드럽게 (0.2 -> 0.3)

    // 내부 변수
    private float currentVelocityX;
    private float currentVelocityY;
    
    private float currentLookOffsetX; 
    private float currentLookOffsetY; 
    private float lookVelocityX;
    private float lookVelocityY;

    private float lookTimer;
    
    private Camera cam;
    private float defaultSize;

    private Player_Controller playerController;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam != null) defaultSize = cam.orthographicSize;

        if (target != null)
        {
            playerController = target.GetComponent<Player_Controller>();
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. 모드에 따른 시야(Offset) 계산
        bool isZooming = Input.GetKey(KeyCode.LeftControl);
        HandleCameraMode(isZooming);

        // 2. 최종 목표 위치 계산
        float targetX = target.position.x + offset.x + currentLookOffsetX;
        float targetY = target.position.y + offset.y + currentLookOffsetY;
        float targetZ = target.position.z + offset.z;

        // [핵심 수정] 줌 상태면 더 느리고 부드러운 SmoothTime(0.5)을, 아니면 평소 값(0.1)을 사용
        float currentSmoothX = isZooming ? zoomFollowSmoothTime : followSmoothTimeX;
        float currentSmoothY = isZooming ? zoomFollowSmoothTime : followSmoothTimeY;

        // 3. 부드러운 이동 적용
        float newX = Mathf.SmoothDamp(transform.position.x, targetX, ref currentVelocityX, currentSmoothX);
        float newY = Mathf.SmoothDamp(transform.position.y, targetY, ref currentVelocityY, currentSmoothY);

        transform.position = new Vector3(newX, newY, targetZ);
    }

    void HandleCameraMode(bool isZooming)
    {
        // [모드 1] 줌아웃 & 자유 시점
        if (isZooming)
        {
            if (cam != null)
            {
                cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, zoomOutSize, Time.deltaTime * zoomSpeed);
            }

            float xInput = Input.GetAxisRaw("Horizontal");
            float yInput = Input.GetAxisRaw("Vertical");
            Vector2 inputDir = new Vector2(xInput, yInput).normalized;

            float targetOffX = inputDir.x * freeLookDistance;
            float targetOffY = inputDir.y * freeLookDistance;

            // 오프셋 변경도 부드럽게 (freeLookSmoothTime 사용)
            currentLookOffsetX = Mathf.SmoothDamp(currentLookOffsetX, targetOffX, ref lookVelocityX, freeLookSmoothTime);
            currentLookOffsetY = Mathf.SmoothDamp(currentLookOffsetY, targetOffY, ref lookVelocityY, freeLookSmoothTime);
            
            lookTimer = 0f;
        }
        else
        {
            // [모드 2] 일반 복귀
            if (cam != null)
            {
                cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, defaultSize, Time.deltaTime * zoomSpeed);
            }

            // X축 오프셋 제거
            currentLookOffsetX = Mathf.SmoothDamp(currentLookOffsetX, 0f, ref lookVelocityX, freeLookSmoothTime);

            // Y축 오프셋은 일반 로직 위임
            HandleLookUpDown();
        }
    }

    void HandleLookUpDown()
    {
        if (playerController != null && playerController.IsZeroGravity)
        {
            lookTimer = 0f;
            currentLookOffsetY = Mathf.SmoothDamp(currentLookOffsetY, 0f, ref lookVelocityY, lookSmoothTime);
            return; 
        }

        float yInput = Input.GetAxisRaw("Vertical");

        if (yInput != 0)
        {
            lookTimer += Time.deltaTime;
            if (lookTimer >= lookInputTime)
            {
                float targetOffset = (yInput > 0) ? lookUpDistance : -lookDownDistance;
                currentLookOffsetY = Mathf.SmoothDamp(currentLookOffsetY, targetOffset, ref lookVelocityY, lookSmoothTime);
            }
        }
        else
        {
            lookTimer = 0f;
            currentLookOffsetY = Mathf.SmoothDamp(currentLookOffsetY, 0f, ref lookVelocityY, lookSmoothTime);
        }
    }
}