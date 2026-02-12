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
    
    [Header("Corner Correction (Smooth)")] // [신규 기능]
    [Tooltip("머리 위 장애물 감지 거리")]
    public float cornerCheckDist = 0.3f;
    [Tooltip("코너 보정 시 옆으로 밀어주는 속도 (부드러운 이동)")]
    public float cornerSlideSpeed = 5f;
    
    [Header("Double Jump")]
    public int maxJumps = 2; 
    public float doubleJumpMultiplier = 0.8f;

    [Header("Low Gravity Settings")]
    [Range(0f, 1f)] public float jumpForceScaling = 0.5f;
    [Range(0f, 1f)] public float horizontalControlScaling = 0.5f; 
    [Range(0f, 1f)] public float maxSpeedScaling = 0.5f;

    [Header("Zero Gravity (Space)")]
    public float zeroGravityMaxSpeed = 15f;
    public float zeroGravityAccel = 15f; 
    public float zeroGravityDrag = 0.5f;

    [Header("Zero Gravity Swing")]
    public float zeroGravitySwingMaxSpeed = 40f; 
    public float velocityDecayRate = 10f;        

    [Header("Ground Detection")]
    public LayerMask groundLayer;
    public float rayLength = 0.1f;
    public float rayInset = 0.05f;
    public float coyoteTime = 0.15f;
    
    public bool IsGrounded { get; private set; }
    public int FacingDirection { get; private set; } = 1;
    public int JumpsLeft { get; private set; } 
    public float CurrentGravityMultiplier => gravityMultiplier;

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
            JumpsLeft = maxJumps; 
        }
        else 
        {
            coyoteTimeCounter -= Time.deltaTime;
            if (coyoteTimeCounter < 0 && JumpsLeft == maxJumps)
            {
                JumpsLeft--;
            }
        }
    }

    public void PerformJump()
    {
        if (Mathf.Abs(gravityMultiplier) < 0.1f) return;

        if (JumpsLeft > 0)
        {
            float force = jumpForce;

            bool isFirstJump = IsGrounded || coyoteTimeCounter > 0;

            if (isFirstJump)
            {
                JumpsLeft = maxJumps - 1;
            }
            else
            {
                force *= doubleJumpMultiplier;
                JumpsLeft--;
            }

            float gScale = Mathf.Abs(gravityMultiplier);
            if (gScale < 1f && gScale > 0.01f)
            {
                force *= Mathf.Pow(gScale, jumpForceScaling);
            }

            Vector2 vel = rb.linearVelocity;
            vel.y = force;
            rb.linearVelocity = vel;

            coyoteTimeCounter = 0f; 
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
            // 스윙 로직 (생략)
        }
        else
        {
            float currentMaxSpeed = maxSpeed;
            float currentAccel = 0f;

            if (input.x != 0)
            {
                if (Mathf.Sign(input.x) != Mathf.Sign(velocity.x) && Mathf.Abs(velocity.x) > 0.1f)
                    currentAccel = turnSpeed;
                else
                    currentAccel = acceleration;
            }
            else
            {
                currentAccel = deceleration;
            }

            float gScale = Mathf.Abs(gravityMultiplier);
            if (gScale < 1f && gScale > 0.01f)
            {
                currentAccel *= Mathf.Pow(gScale, horizontalControlScaling);
                currentMaxSpeed *= Mathf.Pow(gScale, maxSpeedScaling);
            }

            float targetSpeed = input.x * currentMaxSpeed;
            velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, currentAccel * Time.fixedDeltaTime);
        }
        
        // [신규 기능] 코너 보정 로직 적용 (상승 중일 때만)
        if (!IsGrounded && velocity.y > 0 && !isSwinging)
        {
            ApplyCornerCorrection(ref velocity);
        }

        float currentGravity = baseGravity * gravityMultiplier; 
        bool isFalling = (currentGravity < 0 && velocity.y < 0) || (currentGravity > 0 && velocity.y > 0);
        if (isFalling) currentGravity *= fallGravityMult;
        
        velocity.y += currentGravity * Time.fixedDeltaTime;

        if (currentGravity < 0) velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
        else if (currentGravity > 0) velocity.y = Mathf.Min(velocity.y, maxFallSpeed);

        rb.linearVelocity = velocity;
    }

    // -----------------------------------------------------------------------
    // [신규 기능] 코너 보정 (Corner Correction) - 부드러운 슬라이딩 방식
    // -----------------------------------------------------------------------
    private void ApplyCornerCorrection(ref Vector2 velocity)
    {
        Bounds bounds = boxCol.bounds;
        
        // 머리 위 감지 거리 (속도에 비례하되 최소값 보장)
        float checkDistance = Mathf.Max(cornerCheckDist, velocity.y * Time.fixedDeltaTime);
        
        // 왼쪽 끝과 오른쪽 끝에서 위로 레이 발사
        // rayInset을 약간 주어 완벽한 끝보다는 살짝 안쪽을 검사 (오작동 방지)
        Vector2 leftOrigin = new Vector2(bounds.min.x + rayInset, bounds.max.y);
        Vector2 rightOrigin = new Vector2(bounds.max.x - rayInset, bounds.max.y);

        bool hitLeft = Physics2D.Raycast(leftOrigin, Vector2.up, checkDistance, groundLayer);
        bool hitRight = Physics2D.Raycast(rightOrigin, Vector2.up, checkDistance, groundLayer);

        // 한쪽만 닿았을 때 (모서리 상황)
        if (hitLeft && !hitRight)
        {
            // 왼쪽이 막힘 -> 오른쪽으로 밀어줌
            // 1. 위치를 직접 조금씩 이동 (부드럽게)
            float moveAmount = cornerSlideSpeed * Time.fixedDeltaTime;
            rb.position += new Vector2(moveAmount, 0);
            
            // 2. 속도 보정 (선택 사항: 벽쪽으로 미는 입력이 있어도 상쇄시킬 수 있음)
            // if (velocity.x < 0) velocity.x = 0; 
        }
        else if (!hitLeft && hitRight)
        {
            // 오른쪽이 막힘 -> 왼쪽으로 밀어줌
            float moveAmount = cornerSlideSpeed * Time.fixedDeltaTime;
            rb.position -= new Vector2(moveAmount, 0);
            
            // if (velocity.x > 0) velocity.x = 0;
        }
        
        // 둘 다 닿았다면 완벽한 천장이므로 보정하지 않음 (그냥 부딪힘)
    }

    private void ApplyZeroGravityMovement(Vector2 input, bool isSwinging)
    {
        if (input != Vector2.zero)
        {
            rb.AddForce(input * zeroGravityAccel, ForceMode2D.Force);
        }

        float currentSpeed = rb.linearVelocity.magnitude;

        if (isSwinging)
        {
            if (currentSpeed > zeroGravitySwingMaxSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * zeroGravitySwingMaxSpeed;
            }
        }
        else
        {
            if (currentSpeed > zeroGravityMaxSpeed)
            {
                float newSpeed = Mathf.MoveTowards(currentSpeed, zeroGravityMaxSpeed, velocityDecayRate * Time.fixedDeltaTime);
                rb.linearVelocity = rb.linearVelocity.normalized * newSpeed;
            }
            else
            {
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
        
        // Ground Check Gizmos
        Gizmos.color = IsGrounded ? Color.green : Color.red;
        float yOrigin = bounds.min.y + 0.05f; 
        float checkDist = 0.05f + rayLength;
        Vector2 originCenter = new Vector2(bounds.center.x, yOrigin);
        Vector2 originLeft = new Vector2(bounds.min.x + rayInset, yOrigin);
        Vector2 originRight = new Vector2(bounds.max.x - rayInset, yOrigin);
        Gizmos.DrawLine(originCenter, originCenter + Vector2.down * checkDist);
        Gizmos.DrawLine(originLeft, originLeft + Vector2.down * checkDist);
        Gizmos.DrawLine(originRight, originRight + Vector2.down * checkDist);

        // Corner Correction Gizmos
        Gizmos.color = Color.yellow;
        Vector2 headLeft = new Vector2(bounds.min.x + rayInset, bounds.max.y);
        Vector2 headRight = new Vector2(bounds.max.x - rayInset, bounds.max.y);
        Gizmos.DrawLine(headLeft, headLeft + Vector2.up * cornerCheckDist);
        Gizmos.DrawLine(headRight, headRight + Vector2.up * cornerCheckDist);
    }
}