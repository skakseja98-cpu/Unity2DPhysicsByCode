using UnityEngine;

public class Player_Input : MonoBehaviour
{
    [Header("Controls")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode grappleKey = KeyCode.F;       // 앵커 (기존 유지)
    public KeyCode npcInteractKey = KeyCode.E;   // [신규] 대화
    public KeyCode observeKey = KeyCode.LeftControl;
    public KeyCode retractKey = KeyCode.G;

    [Header("Observation Settings")]
    [Tooltip("관찰 모드 사용 후 재사용 대기시간 (초)")]
    public float observeCooldown = 3.0f; // 기본 3초

    // 변수들
    public Vector2 MoveVector { get; private set; }
    public bool IsJumpDown { get; private set; }
    public bool IsJumpUp { get; private set; }
    public bool IsGrappleDown { get; private set; }
    public bool IsRetractHeld { get; private set; }
    public bool IsNpcInteractDown { get; private set; } 

    // 내부 상태 변수
    private float currentCooldownTimer = 0f;
    private bool isObserving = false;

    // (외부에서 쿨타임 UI 등을 표시할 때 접근 가능)
    public float CurrentCooldownRatio => Mathf.Clamp01(currentCooldownTimer / observeCooldown);

    void Update()
    {
        HandleObservationInput();

        // 관찰 중이면(시간 정지) 아래 일반 조작 로직은 실행하지 않음
        if (isObserving) return;

        HandleNormalInput();
    }

    void HandleObservationInput()
    {
        // 1. 쿨타임 감소 (시간이 멈춰도 쿨타임은 줄어들어야 하므로 unscaledDeltaTime 사용)
        if (currentCooldownTimer > 0)
        {
            currentCooldownTimer -= Time.unscaledDeltaTime;
        }

        // 2. 키 입력 체크
        // 키를 누르고 있고 + 쿨타임이 끝났다면 -> 진입
        if (Input.GetKey(observeKey) && currentCooldownTimer <= 0)
        {
            if (!isObserving)
            {
                StartObservation();
            }
            
            // 입력 데이터 초기화 (버그 방지)
            ResetInputs();
        }
        else
        {
            // 키를 뗐거나 쿨타임 중이라면 -> 해제
            if (isObserving)
            {
                StopObservation();
            }
        }
    }

    void StartObservation()
    {
        isObserving = true;
        Time.timeScale = 0f; // 시간 정지
    }

    void StopObservation()
    {
        isObserving = false;
        Time.timeScale = 1f; // 시간 정상화
        
        // [핵심] 모드 해제 시 쿨타임 시작
        currentCooldownTimer = observeCooldown; 
    }

    void HandleNormalInput()
    {
        // 이동 입력
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        MoveVector = new Vector2(x, y);

        IsJumpDown = Input.GetKeyDown(jumpKey);
        IsJumpUp = Input.GetKeyUp(jumpKey);
        IsGrappleDown = Input.GetKeyDown(grappleKey);         // F
        IsNpcInteractDown = Input.GetKeyDown(npcInteractKey); // E
        IsRetractHeld = Input.GetKey(retractKey);
    }

    void ResetInputs()
    {
        MoveVector = Vector2.zero;
        IsJumpDown = false;
        IsJumpUp = false;
        IsGrappleDown = false;
        IsRetractHeld = false;
        IsNpcInteractDown = false;
    }

    // 디버그용 (화면 좌측 상단에 쿨타임 표시)
    void OnGUI()
    {
        if (currentCooldownTimer > 0)
        {
            GUI.color = Color.red;
            GUI.Label(new Rect(10, 10, 200, 20), $"Cooldown: {currentCooldownTimer:F1}s");
        }
        else
        {
            GUI.color = Color.green;
            GUI.Label(new Rect(10, 10, 200, 20), "Ready (Hold Ctrl)");
        }
    }
}