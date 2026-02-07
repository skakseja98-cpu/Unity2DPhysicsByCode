using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class Player_Movement : MonoBehaviour
{
    [Header("Horizontal")]
    public float maxSpeed = 10f;
    public float acceleration = 50f;
    public float deceleration = 50f;
    public float turnSpeed = 80f;

    [Header("Vertical")]
    public float jumpHeight = 4f;
    public float timeToJumpApex = 0.4f;
    public float maxFallSpeed = 20f;
    public float fallGravityMult = 1.5f;
    public float jumpCutMult = 0.5f;

    [Header("Dash")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 0.5f;

    [Header("Ground Detection")]
    public LayerMask groundLayer;
    public float rayLength = 0.1f;
    // BoxCast 관련 변수 제거하고 원래 쓰시던 변수만 남김
    public float rayInset = 0.05f; // [추가] 모서리에서 안쪽으로 들어오는 간격
    public float coyoteTime;
    
    // 상태 확인용 프로퍼티
    public bool IsGrounded { get; private set; }
    public bool IsDashing { get; private set; }
    public int FacingDirection { get; private set; } = 1;

    // 내부 변수
    private Rigidbody2D rb;
    private BoxCollider2D boxCol;
    private float baseGravity;
    private float gravityMultiplier = 1f; 
    private float jumpForce;
    private float coyoteTimeCounter;
    private float lastDashTime = -10f;
    private Vector2 dashDir;
    private float dashTimeLeft; // HandleDash용

    public void Initialize(Rigidbody2D _rb, BoxCollider2D _col)
    {
        rb = _rb;
        boxCol = _col;
        
        // 오리지널 물리 공식
        baseGravity = -(2 * jumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        jumpForce = Mathf.Abs(baseGravity) * timeToJumpApex;
    }

    public void SetGravityScale(float scale) => gravityMultiplier = scale;

    public void HandleGroundCheck()
    {
        Bounds bounds = boxCol.bounds;
        float yOrigin = bounds.min.y + 0.05f; 
        float checkDist = 0.05f + rayLength;

        // [수정] 3방향 레이캐스트 (좌측, 중앙, 우측)
        Vector2 originCenter = new Vector2(bounds.center.x, yOrigin);
        Vector2 originLeft = new Vector2(bounds.min.x + rayInset, yOrigin);
        Vector2 originRight = new Vector2(bounds.max.x - rayInset, yOrigin);

        RaycastHit2D hitC = Physics2D.Raycast(originCenter, Vector2.down, checkDist, groundLayer);
        RaycastHit2D hitL = Physics2D.Raycast(originLeft, Vector2.down, checkDist, groundLayer);
        RaycastHit2D hitR = Physics2D.Raycast(originRight, Vector2.down, checkDist, groundLayer);
        
        // 셋 중 하나라도 땅을 감지하면 접지로 판정
        IsGrounded = (hitC.collider != null || hitL.collider != null || hitR.collider != null);

        if (IsGrounded) coyoteTimeCounter = coyoteTime;
        else coyoteTimeCounter -= Time.deltaTime;
    }

    public void ProcessMove(Vector2 input, bool isJumpDown, bool isJumpUp, bool isDashDown)
    {
        if (IsDashing) return;

        if (input.x != 0) FacingDirection = (int)Mathf.Sign(input.x);

        // 점프 로직
        if (isJumpDown && coyoteTimeCounter > 0f)
        {
            IsDashing = false; // 원본 로직 반영
            Vector2 vel = rb.linearVelocity;
            vel.y = jumpForce;
            rb.linearVelocity = vel;
            coyoteTimeCounter = 0f;
        }

        // 점프 컷
        if (isJumpUp && rb.linearVelocity.y > 0)
        {
            Vector2 vel = rb.linearVelocity;
            vel.y *= jumpCutMult;
            rb.linearVelocity = vel;
        }

        // 대쉬 시작
        if (isDashDown && Time.time >= lastDashTime + dashCooldown)
        {
            StartCoroutine(PerformDash(input));
        }
    }

    public void ApplyPhysics(Vector2 input, bool isSwinging)
    {
        // [원상복구] FixedUpdate의 로직 흐름 그대로 복원
        if (IsDashing)
        {
            // HandleDash 로직 복원 (코루틴 대신 직접 제어하던 방식과 코루틴 방식을 적절히 연결)
            // PerformDash 코루틴이 속도를 제어하므로 여기선 유지
            return;
        }

        Vector2 velocity = rb.linearVelocity;

        if (isSwinging)
        {
            // 스윙 로직은 Controller/Grapple에서 힘을 가하고 있으므로 여기선 감속만
             // velocity.x *= 0.995f; // 원본에 있던 감속 (Grapple쪽에서 처리하거나 여기서 처리)
        }
        else
        {
            // [원상복구] MoveTowards 이동 로직
            float targetSpeed = input.x * maxSpeed;
            float currentAccel = acceleration; 
            
            if (input.x != 0)
            {
                if (Mathf.Sign(input.x) != Mathf.Sign(velocity.x) && Mathf.Abs(velocity.x) > 0.1f)
                    currentAccel = turnSpeed;
            }
            else
            {
                currentAccel = deceleration;
            }

            velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, currentAccel * Time.fixedDeltaTime);
        }

        // [원상복구] 중력 로직 (제가 넣었던 velocity.y = -1 같은 인위적 코드는 삭제)
        float currentGravity = baseGravity * gravityMultiplier; 
        bool isFalling = (currentGravity < 0 && velocity.y < 0) || (currentGravity > 0 && velocity.y > 0);
        if (isFalling) currentGravity *= fallGravityMult;
        
        velocity.y += currentGravity * Time.fixedDeltaTime;

        if (currentGravity < 0) velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
        else if (currentGravity > 0) velocity.y = Mathf.Min(velocity.y, maxFallSpeed);

        rb.linearVelocity = velocity;
    }

    IEnumerator PerformDash(Vector2 input)
    {
        IsDashing = true; 
        lastDashTime = Time.time;
        
        // StartDash() 로직
        Time.timeScale = 0f; 
        yield return new WaitForSecondsRealtime(0.07f); 
        Time.timeScale = 1f;

        if (input == Vector2.zero) dashDir = new Vector2(FacingDirection, 0); 
        else dashDir = input.normalized;

        // HandleDash() 로직 시뮬레이션
        float startTime = Time.time;
        while(Time.time < startTime + dashDuration)
        {
            rb.linearVelocity = dashDir * dashSpeed;
            yield return new WaitForFixedUpdate();
        }

        IsDashing = false; 
        rb.linearVelocity = dashDir * maxSpeed;
    }
    
    // [Gizmo] 원래 로직대로 직선(Ray)만 표시
    private void OnDrawGizmos()
    {
        if (boxCol == null) boxCol = GetComponent<BoxCollider2D>();
        
        Bounds bounds = boxCol.bounds;
        float yOrigin = bounds.min.y + 0.05f; 
        float checkDist = 0.05f + rayLength;

        Vector2 originCenter = new Vector2(bounds.center.x, yOrigin);
        Vector2 originLeft = new Vector2(bounds.min.x + rayInset, yOrigin);
        Vector2 originRight = new Vector2(bounds.max.x - rayInset, yOrigin);

        Gizmos.color = IsGrounded ? Color.green : Color.red;
        
        Gizmos.DrawLine(originCenter, originCenter + Vector2.down * checkDist);
        Gizmos.DrawLine(originLeft, originLeft + Vector2.down * checkDist);
        Gizmos.DrawLine(originRight, originRight + Vector2.down * checkDist);
    }
}