using UnityEngine;

public class Player_Controller : MonoBehaviour
{
    private Player_Input _input;
    private Player_Movement _movement;
    private Player_Grapple _grapple;
    private Rigidbody2D _rb;
    private BoxCollider2D _col;
    private Player_Interaction _interaction;

    public void SetGravityScale(float scale)
    {
        if (_movement != null) _movement.SetGravityScale(scale);
        if (_grapple != null) _grapple.SetGravityScale(scale);
    }

    public bool IsGrounded => _movement.IsGrounded;
    public float HorizontalSpeed => Mathf.Abs(_rb.linearVelocity.x);
    public Rigidbody2D Rb => _rb;
    public bool IsZeroGravity => Mathf.Abs(_movement.CurrentGravityMultiplier) < 0.01f;
    public bool IsClimbing => _movement.IsClimbing;

    public static Player_Controller Instance;

    void Awake()
    {
        if (Instance == null) 
            Instance = this;
        else 
            Destroy(gameObject);

        _input = GetComponent<Player_Input>();
        _movement = GetComponent<Player_Movement>();
        _grapple = GetComponent<Player_Grapple>();
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<BoxCollider2D>();
        _interaction = GetComponent<Player_Interaction>();

        _movement.Initialize(_rb, _col);
        _grapple.Initialize(_rb, _col, _movement);
    }

    void Update()
    {
        if (_movement != null) _movement.HandleGroundCheck();

        if (_movement.CanClimb && 
            _input.MoveVector.y > 0.1f && 
            !_movement.IsClimbing && 
            _movement.CurrentClimbCooldown <= 0)
        {
            _movement.SetClimbing(true);
        }

        // -------------------------------------------------------
        // [수정] 키 역할 분리
        // -------------------------------------------------------

        // 1. [E] NPC / 문 상호작용 (대화 스킵 포함)
        if (_input.IsNpcInteractDown)
        {
            if (_interaction != null) _interaction.HandleNpcInteraction();
        }

        // 3. [F] 앵커 발사 (기존 유지, 이제 다른 기능과 겹치지 않음)
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

        // ... (점프 로직 유지) ...
        if (_input.IsJumpDown) _movement.PerformJump();
        if (_input.IsJumpUp) _movement.CutJump();
    }

    void FixedUpdate()
    {
        _grapple.ApplyPhysics(_input.IsRetractHeld, _input.MoveVector);

        // 스윙 상태: 앵커가 있고 + 땅이 아니고 + 줄이 팽팽할 때
        bool isSwinging = _grapple.HasAnchor && !_movement.IsGrounded && _grapple.IsTaut;
        
        _movement.ApplyPhysics(_input.MoveVector, isSwinging);
    }
}