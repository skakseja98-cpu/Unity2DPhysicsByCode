using UnityEngine;

public class Player_Input : MonoBehaviour
{
    [Header("Controls")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode grappleKey = KeyCode.F;
    public KeyCode retractKey = KeyCode.G;
    public KeyCode observeKey = KeyCode.LeftControl; // [추가] 관찰 키

    // 변수들
    public Vector2 MoveVector { get; private set; }
    public bool IsJumpDown { get; private set; }
    public bool IsJumpUp { get; private set; }
    public bool IsGrappleDown { get; private set; }
    public bool IsRetractHeld { get; private set; }

    void Update()
    {
        // [핵심 로직] 관찰 키(Ctrl)를 누르고 있다면? -> 시간 정지 & 입력 차단
        if (Input.GetKey(observeKey))
        {
            Time.timeScale = 0f; // [추가] 시간을 멈춤 (물리, 애니메이션 정지)

            // 입력 데이터 초기화
            MoveVector = Vector2.zero;
            IsJumpDown = false;
            IsJumpUp = false;
            IsGrappleDown = false;
            IsRetractHeld = false;
            
            return; // 여기서 업데이트 종료
        }
        else
        {
            // [추가] 평소에는 시간이 정상적으로 흐름
            // (Ctrl을 떼는 순간 1로 복구됨)
            if (Time.timeScale == 0f) Time.timeScale = 1f;
        }

        // --- 아래는 평소 조작 (TimeScale = 1일 때만 실행됨) ---

        // 이동 입력
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        MoveVector = new Vector2(x, y);

        IsJumpDown = Input.GetKeyDown(jumpKey);
        IsJumpUp = Input.GetKeyUp(jumpKey);
        IsGrappleDown = Input.GetKeyDown(grappleKey);
        IsRetractHeld = Input.GetKey(retractKey);
    }
}