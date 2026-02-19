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
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

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

        if (_input.IsNpcInteractDown)
        {
            if (_interaction != null) _interaction.HandleNpcInteraction();
        }

        if (_input.IsGrappleDown)
        {
            if (_grapple.HasAnchor) _grapple.TryReleaseAnchor();
            else _grapple.TryFireAnchor();
        }

        // [신규] 자동 당기기 실행
        if (_input.IsRetractDown)
        {
            _grapple.StartAutoRetract();
        }

        if (_input.IsJumpDown) _movement.PerformJump();
        if (_input.IsJumpUp) _movement.CutJump();
    }

    void FixedUpdate()
    {
        // [수정] IsRetractHeld 매개변수 제거
        _grapple.ApplyPhysics(_input.MoveVector);

        bool isSwinging = _grapple.HasAnchor && !_movement.IsGrounded && _grapple.IsTaut;
        _movement.ApplyPhysics(_input.MoveVector, isSwinging);
    }
}