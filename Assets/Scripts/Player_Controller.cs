using UnityEngine;

public class Player_Controller : MonoBehaviour
{
    // 컴포넌트 참조
    private Player_Input _input;
    private Player_Movement _movement;
    private Player_Grapple _grapple;
    private Rigidbody2D _rb;
    private BoxCollider2D _col;

    public void SetGravityScale(float scale)
    {
        // 이동 관련 중력 변경
        if (_movement != null) _movement.SetGravityScale(scale);
        
        // [추가] 로프 관련 중력 변경
        if (_grapple != null) _grapple.SetGravityScale(scale);
    }

    // 외부에서 상태 조회 (Animator 등)
    public bool IsGrounded => _movement.IsGrounded;
    public float HorizontalSpeed => Mathf.Abs(_rb.linearVelocity.x);
    public Rigidbody2D Rb => _rb;

    void Awake()
    {
        // 같은 오브젝트에 있는 컴포넌트들 가져오기
        _input = GetComponent<Player_Input>();
        _movement = GetComponent<Player_Movement>();
        _grapple = GetComponent<Player_Grapple>();
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<BoxCollider2D>();

        // 초기화 의존성 주입
        _movement.Initialize(_rb, _col);
        _grapple.Initialize(_rb, _col, _movement);
    }

    void Update()
    {
        // 1. 접지 체크 (매 프레임)
        _movement.HandleGroundCheck();

        // 2. 입력에 따른 로직 분기 처리
        // 앵커 로직
        _grapple.HandleInput(_input.IsGrappleDown, _input.IsRetractHeld, _input.MoveVector);
        
        // 이동/점프/대쉬 로직
        _movement.ProcessMove(_input.MoveVector, _input.IsJumpDown, _input.IsJumpUp, _input.IsDashDown);
    }

    void FixedUpdate()
    {
        // 물리 업데이트 순서 중요
        
        // 1. 앵커 물리 (줄 당기기, 매달리기 등)
        _grapple.ApplyPhysics(_input.IsRetractHeld, _input.MoveVector);

        // 2. 이동 물리 (걷기, 중력, 대쉬) - 앵커 상태에 따라 거동이 달라질 수 있음
        // 앵커에 매달려있고(HasAnchor) 땅에 안 닿았으면(Not Grounded) -> 스윙 모드
        bool isSwinging = _grapple.HasAnchor && !_movement.IsGrounded;
        
        _movement.ApplyPhysics(_input.MoveVector, isSwinging);
    }
}