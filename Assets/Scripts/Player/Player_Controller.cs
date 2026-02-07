using UnityEngine;

public class Player_Controller : MonoBehaviour
{
    private Player_Input _input;
    private Player_Movement _movement;
    private Player_Grapple _grapple;
    private Rigidbody2D _rb;
    private BoxCollider2D _col;

    public void SetGravityScale(float scale)
    {
        if (_movement != null) _movement.SetGravityScale(scale);
        if (_grapple != null) _grapple.SetGravityScale(scale);
    }

    public bool IsGrounded => _movement.IsGrounded;
    public float HorizontalSpeed => Mathf.Abs(_rb.linearVelocity.x);
    public Rigidbody2D Rb => _rb;
    public bool IsZeroGravity => Mathf.Abs(_movement.CurrentGravityMultiplier) < 0.01f;

    void Awake()
    {
        _input = GetComponent<Player_Input>();
        _movement = GetComponent<Player_Movement>();
        _grapple = GetComponent<Player_Grapple>();
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<BoxCollider2D>();

        _movement.Initialize(_rb, _col);
        _grapple.Initialize(_rb, _col, _movement);
    }

    void Update()
    {
        // 1. 접지 체크 (매 프레임)
        _movement.HandleGroundCheck();

        // [수정] F키: 앵커 토글 (설치 되어있으면 해제, 없으면 발사)
        if (_input.IsGrappleDown)
        {
            if (_grapple.HasAnchor)
            {
                _grapple.TryReleaseAnchor();
            }
            else
            {
                _grapple.TryFireAnchor();
            }
        }

        // [수정] Space키: 앵커 여부와 상관없이 점프 수행 (독립적)
        // 이제 줄에 매달려 있어도 스페이스바를 누르면 줄을 끊지 않고 
        // 2단 점프 힘을 받아 위로 솟구칩니다. (줄을 끊으려면 F를 눌러야 함)
        if (_input.IsJumpDown)
        {
            _movement.PerformJump();
        }

        if (_input.IsJumpUp)
        {
            _movement.CutJump();
        }
    }

    void FixedUpdate()
    {
        _grapple.ApplyPhysics(_input.IsRetractHeld, _input.MoveVector);

        // 스윙 상태: 앵커가 있고 + 땅이 아니고 + 줄이 팽팽할 때
        bool isSwinging = _grapple.HasAnchor && !_movement.IsGrounded && _grapple.IsTaut;
        
        _movement.ApplyPhysics(_input.MoveVector, isSwinging);
    }
}