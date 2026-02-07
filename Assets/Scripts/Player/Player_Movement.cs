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

    [Header("Vertical (Jump)")]
    public float jumpHeight = 4f;
    public float timeToJumpApex = 0.4f;
    public float maxFallSpeed = 20f;
    public float fallGravityMult = 1.5f;
    public float jumpCutMult = 0.5f;
    
    [Header("Double Jump")]
    public int maxJumps = 2; // [중요] 이 값이 2인지 꼭 확인하세요!
    public float doubleJumpMultiplier = 0.8f;
    public float lowGravityJumpBonus = 1.5f;

    [Header("Zero Gravity (Space)")]
    public float zeroGravityMaxSpeed = 15f;
    public float zeroGravityAccel = 15f; 
    public float zeroGravityDrag = 0.5f;

    [Header("Zero Gravity Swing")]
    public float zeroGravitySwingMaxSpeed = 40f; // [추가] 스윙 중 허용되는 최대 속도
    public float velocityDecayRate = 10f;        // [추가] 줄을 놓은 후 속도가 줄어드는 빠르기

    [Header("Ground Detection")]
    public LayerMask groundLayer;
    public float rayLength = 0.1f;
    public float rayInset = 0.05f;
    public float coyoteTime = 0.15f; // [수정] 0.1은 너무 짧을 수 있어서 조금 늘림
    
    // 상태 프로퍼티
    public bool IsGrounded { get; private set; }
    public int FacingDirection { get; private set; } = 1;
    public int JumpsLeft { get; private set; } 
    public float CurrentGravityMultiplier => gravityMultiplier;

    // 내부 변수
    private Rigidbody2D rb;
    private BoxCollider2D boxCol;
    private float baseGravity;
    private float gravityMultiplier = 1f; 
    private float jumpForce;
    private float coyoteTimeCounter;

    public void Initialize(Rigidbody2D _rb, BoxCollider2D _col)
    {
        rb = _rb;
        boxCol = _col;
        
        baseGravity = -(2 * jumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        jumpForce = Mathf.Abs(baseGravity) * timeToJumpApex;
    }

    public void SetGravityScale(float scale) => gravityMultiplier = scale;

    public void HandleGroundCheck()
    {
        Bounds bounds = boxCol.bounds;
        float yOrigin = bounds.min.y + 0.05f; 
        float checkDist = 0.05f + rayLength;

        Vector2 originCenter = new Vector2(bounds.center.x, yOrigin);
        Vector2 originLeft = new Vector2(bounds.min.x + rayInset, yOrigin);
        Vector2 originRight = new Vector2(bounds.max.x - rayInset, yOrigin);

        RaycastHit2D hitC = Physics2D.Raycast(originCenter, Vector2.down, checkDist, groundLayer);
        RaycastHit2D hitL = Physics2D.Raycast(originLeft, Vector2.down, checkDist, groundLayer);
        RaycastHit2D hitR = Physics2D.Raycast(originRight, Vector2.down, checkDist, groundLayer);
        
        bool wasGrounded = IsGrounded;
        IsGrounded = (hitC.collider != null || hitL.collider != null || hitR.collider != null);

        if (IsGrounded) 
        {
            coyoteTimeCounter = coyoteTime;
            JumpsLeft = maxJumps; // 땅에 있으면 점프 횟수 리필
        }
        else 
        {
            coyoteTimeCounter -= Time.deltaTime;

            // [추가] 코요테 타임이 지났는데 아직 점프 횟수가 꽉 차있다면?
            // -> 발 헛디뎠으니 1단 점프 기회 박탈 (공중 점프만 가능하게)
            if (coyoteTimeCounter < 0 && JumpsLeft == maxJumps)
            {
                JumpsLeft--;
            }
        }
    }

    public void PerformJump()
    {
        if (Mathf.Abs(gravityMultiplier) < 0.1f) return;

        // 점프 가능 횟수가 남아있을 때만
        if (JumpsLeft > 0)
        {
            float force = jumpForce;

            // 판정 로직: 땅에 있거나 코요테 타임 안쪽이면 = "1단 점프"
            bool isFirstJump = IsGrounded || coyoteTimeCounter > 0;

            if (isFirstJump)
            {
                // [1단 점프]
                // 중력이 약할 때 보너스
                if (gravityMultiplier < 0.9f) force *= lowGravityJumpBonus;
                
                // [핵심 수정] 1단 점프를 했으면, 남은 횟수는 무조건 (최대 - 1)로 고정
                // 이렇게 하면 코요테 타임 때 점프해도 더블 점프 기회가 확실히 보장됨
                JumpsLeft = maxJumps - 1;
            }
            else
            {
                // [더블 점프 (공중)]
                force *= doubleJumpMultiplier;
                JumpsLeft--; // 횟수 하나 차감
            }

            // 물리 적용
            Vector2 vel = rb.linearVelocity;
            vel.y = force;
            rb.linearVelocity = vel;

            coyoteTimeCounter = 0f; // 점프 했으니 코요테 타임 종료
        }
    }

    public void CutJump()
    {
        if (rb.linearVelocity.y > 0)
        {
            Vector2 vel = rb.linearVelocity;
            vel.y *= jumpCutMult;
            rb.linearVelocity = vel;
        }
    }

    public void ApplyPhysics(Vector2 input, bool isSwinging)
    {
        if (input.x != 0) FacingDirection = (int)Mathf.Sign(input.x);

        if (Mathf.Abs(gravityMultiplier) < 0.1f)
    {
        ApplyZeroGravityMovement(input, isSwinging); 
        return;
    }

        Vector2 velocity = rb.linearVelocity;

        if (isSwinging)
        {
        }
        else
        {
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

        float currentGravity = baseGravity * gravityMultiplier; 
        bool isFalling = (currentGravity < 0 && velocity.y < 0) || (currentGravity > 0 && velocity.y > 0);
        if (isFalling) currentGravity *= fallGravityMult;
        
        velocity.y += currentGravity * Time.fixedDeltaTime;

        if (currentGravity < 0) velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
        else if (currentGravity > 0) velocity.y = Mathf.Min(velocity.y, maxFallSpeed);

        rb.linearVelocity = velocity;
    }

    private void ApplyZeroGravityMovement(Vector2 input, bool isSwinging)
    {
        // 1. 가속 (옵션 A: 수동 입력) - 그네 타듯이 입력하면 가속됨
        if (input != Vector2.zero)
        {
            rb.AddForce(input * zeroGravityAccel, ForceMode2D.Force);
        }

        float currentSpeed = rb.linearVelocity.magnitude;

        if (isSwinging)
        {
            // [상태 1: 스윙 중]
            // 저항(Drag)을 없애 가속력을 보존하고, 속도 제한을 대폭 늘려줍니다.
            if (currentSpeed > zeroGravitySwingMaxSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * zeroGravitySwingMaxSpeed;
            }
        }
        else
        {
            // [상태 2: 줄을 놓음 (자유 유영)]
            // 핵심: "Soft Cap" - 속도가 빠를 때 강제로 깎지 않고 서서히 줄임
            
            if (currentSpeed > zeroGravityMaxSpeed)
            {
                // 현재 속도가 평소 제한보다 빠르면, 서서히 감속 (자연스러운 관성 이동)
                float newSpeed = Mathf.MoveTowards(currentSpeed, zeroGravityMaxSpeed, velocityDecayRate * Time.fixedDeltaTime);
                rb.linearVelocity = rb.linearVelocity.normalized * newSpeed;
            }
            else
            {
                // 평범한 속도라면 드래그(마찰) 적용 및 일반 제한
                rb.linearVelocity *= (1f - zeroGravityDrag * Time.fixedDeltaTime);

                if (currentSpeed > zeroGravityMaxSpeed)
                {
                    rb.linearVelocity = rb.linearVelocity.normalized * zeroGravityMaxSpeed;
                }
            }
        }
    }
    
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