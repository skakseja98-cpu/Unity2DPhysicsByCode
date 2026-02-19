using UnityEngine;
using System.Collections;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class Player_Movement : MonoBehaviour
{
    // ... (기존 변수들 그대로 유지) ...
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
    
    [Header("Corner Correction (Smooth)")]
    public float cornerCheckDist = 0.3f;
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

    [Header("Climbing Settings")]
    public float climbSpeed = 5f;
    public LayerMask climbableLayer; 
    public float climbJumpCooldownTime = 0.2f;

    public bool IsGrounded { get; private set; }
    public bool IsClimbing { get; private set; }
    public bool CanClimb { get; private set; }
    public int FacingDirection { get; private set; } = 1;
    public int JumpsLeft { get; private set; } 
    public float CurrentClimbCooldown { get; private set; }
    public float CurrentGravityMultiplier => gravityMultiplier;

    private Rigidbody2D rb;
    private BoxCollider2D boxCol;
    private float baseGravity;
    private float gravityMultiplier = 1f; 
    private float jumpForce;
    private float coyoteTimeCounter;

    // [추가] 현재 붙잡고 있는 벽의 물리엔진(속도 확인용)
    private Rigidbody2D currentWallRB;
    private Rigidbody2D currentGroundRB;

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
        // ... (기존 코드 유지) ...
        Bounds bounds = boxCol.bounds;
        float yOrigin = bounds.min.y + 0.05f; 
        float checkDist = 0.05f + rayLength;

        Vector2 originCenter = new Vector2(bounds.center.x, yOrigin);
        Vector2 originLeft = new Vector2(bounds.min.x + rayInset, yOrigin);
        Vector2 originRight = new Vector2(bounds.max.x - rayInset, yOrigin);

        RaycastHit2D hitC = Physics2D.Raycast(originCenter, Vector2.down, checkDist, groundLayer);
        RaycastHit2D hitL = Physics2D.Raycast(originLeft, Vector2.down, checkDist, groundLayer);
        RaycastHit2D hitR = Physics2D.Raycast(originRight, Vector2.down, checkDist, groundLayer);

        RaycastHit2D validHit = hitC.collider != null ? hitC : (hitL.collider != null ? hitL : hitR);
        
        bool wasGrounded = IsGrounded;
        IsGrounded = (hitC.collider != null || hitL.collider != null || hitR.collider != null);

        if (IsGrounded) 
        {
            coyoteTimeCounter = coyoteTime;
            JumpsLeft = maxJumps;

            currentGroundRB = validHit.collider.GetComponent<Rigidbody2D>();
        }
        else 
        {
            coyoteTimeCounter -= Time.deltaTime;
            if (coyoteTimeCounter < 0 && JumpsLeft == maxJumps)
            {
                JumpsLeft--;
            }
            currentGroundRB = null;
        }
    }

    void Update()
    {
        if (CurrentClimbCooldown > 0)
        {
            CurrentClimbCooldown -= Time.deltaTime;
        }
    }

    public void PerformJump()
    {
        if (IsClimbing)
        {
            SetClimbing(false);
            CurrentClimbCooldown = climbJumpCooldownTime;
            // 벽에서 점프할 때 벽의 속도도 같이 더해줌 (관성 유지)
            if (currentWallRB != null) rb.linearVelocity += currentWallRB.linearVelocity;
        }

        // ... (기존 점프 로직 유지) ...
        if (Mathf.Abs(gravityMultiplier) < 0.1f) return;

        if (JumpsLeft > 0)
        {
            float force = jumpForce;
            bool isFirstJump = IsGrounded || coyoteTimeCounter > 0;

            if (isFirstJump) JumpsLeft = maxJumps - 1;
            else
            {
                force *= doubleJumpMultiplier;
                JumpsLeft--;
            }

            float gScale = Mathf.Abs(gravityMultiplier);
            if (gScale < 1f && gScale > 0.01f) force *= Mathf.Pow(gScale, jumpForceScaling);

            Vector2 vel = rb.linearVelocity;
            vel.y = force;

            if (IsGrounded && currentGroundRB != null)
            {
                vel.y += currentGroundRB.linearVelocity.y;
            }
            
            rb.linearVelocity = vel;
            coyoteTimeCounter = 0f; 
        }
    }

    public void CutJump()
    {
        // ... (기존 코드 유지) ...
        if (rb.linearVelocity.y > 0)
        {
            Vector2 vel = rb.linearVelocity;
            vel.y *= jumpCutMult;
            rb.linearVelocity = vel;
        }
    }

    public void ApplyPhysics(Vector2 input, bool isSwinging)
    {
        if (IsClimbing)
        {
            ApplyClimbingPhysics(input);
            return; 
        }

        // ... (기존 이동 로직 유지) ...
        if (input.x != 0) FacingDirection = (int)Mathf.Sign(input.x);

        if (Mathf.Abs(gravityMultiplier) < 0.1f)
        {
            ApplyZeroGravityMovement(input, isSwinging); 
            return;
        }

        Vector2 velocity = rb.linearVelocity;

        Vector2 platformVelocity = Vector2.zero;
        if (IsGrounded && currentGroundRB != null && !isSwinging)
        {
            platformVelocity = currentGroundRB.linearVelocity;
            velocity -= platformVelocity; // 연산을 위해 발판 속도를 잠시 뺌
        }

        if (isSwinging)
        {
            // 스윙 로직
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

        if (!isSwinging)
        {
            velocity += platformVelocity;
        }

        rb.linearVelocity = velocity;
    }

    // [수정] 벽타기 물리 적용 함수
    private void ApplyClimbingPhysics(Vector2 input)
    {
        // 1. 내 의지대로 움직이는 속도
        Vector2 myVelocity = input * climbSpeed;

        // 2. 벽이 움직이는 속도 (벽이 RB를 가지고 있다면)
        Vector2 wallVelocity = Vector2.zero;
        if (currentWallRB != null)
        {
            wallVelocity = currentWallRB.linearVelocity;
        }

        // 3. 최종 속도 = 내 이동 + 벽 이동
        rb.linearVelocity = myVelocity + wallVelocity;

        // 바닥에 닿았는데 아래로 내려가려 하면 -> 벽타기 해제
        if (IsGrounded && input.y < 0)
        {
            SetClimbing(false);
        }
    }

    // [수정] 충돌 시 벽의 정보(Rigidbody)를 가져옴
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & climbableLayer) != 0)
        {
            CanClimb = true;
            // 벽이 움직이는 물체라면 Rigidbody2D가 있을 것임
            currentWallRB = collision.GetComponent<Rigidbody2D>();
        }
    }
    
    // [수정] 떨어지거나 나갈 때 정보 초기화
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & climbableLayer) != 0)
        {
            CanClimb = false;
            
            // 나가려는 벽이 내가 잡고 있던 벽이라면 초기화
            if(collision.GetComponent<Rigidbody2D>() == currentWallRB)
            {
                currentWallRB = null;
            }

            if (IsClimbing)
            {
                SetClimbing(false);
            }
        }
    }

    // ... (나머지 Corner Correction, Zero Gravity 로직 등 기존 코드 유지) ...
    private void ApplyCornerCorrection(ref Vector2 velocity)
    {
        Bounds bounds = boxCol.bounds;
        float checkDistance = Mathf.Max(cornerCheckDist, velocity.y * Time.fixedDeltaTime);
        Vector2 leftOrigin = new Vector2(bounds.min.x + rayInset, bounds.max.y);
        Vector2 rightOrigin = new Vector2(bounds.max.x - rayInset, bounds.max.y);

        bool hitLeft = Physics2D.Raycast(leftOrigin, Vector2.up, checkDistance, groundLayer);
        bool hitRight = Physics2D.Raycast(rightOrigin, Vector2.up, checkDistance, groundLayer);

        if (hitLeft && !hitRight)
        {
            float moveAmount = cornerSlideSpeed * Time.fixedDeltaTime;
            rb.position += new Vector2(moveAmount, 0);
        }
        else if (!hitLeft && hitRight)
        {
            float moveAmount = cornerSlideSpeed * Time.fixedDeltaTime;
            rb.position -= new Vector2(moveAmount, 0);
        }
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

    public void SetClimbing(bool active)
    {
        if (IsClimbing == active) return;

        IsClimbing = active;

        if (active)
        {
            rb.linearVelocity = Vector2.zero;
            JumpsLeft = maxJumps;
        }
        else
        {
            // [중요] 벽에서 손을 놓을 때, 벽 정보를 즉시 잊어버리지 않도록 주의
            // (점프 관성을 위해 PerformJump에서 처리함)
            // 여기서는 상태만 변경
        }
    }
    
    // ... (Gizmo 등 나머지 유지) ...
     private void OnDrawGizmos()
    {
        if (boxCol == null) boxCol = GetComponent<BoxCollider2D>();
        Bounds bounds = boxCol.bounds;
        Gizmos.color = IsGrounded ? Color.green : Color.red;
        float yOrigin = bounds.min.y + 0.05f; 
        float checkDist = 0.05f + rayLength;
        Vector2 originCenter = new Vector2(bounds.center.x, yOrigin);
        Vector2 originLeft = new Vector2(bounds.min.x + rayInset, yOrigin);
        Vector2 originRight = new Vector2(bounds.max.x - rayInset, yOrigin);
        Gizmos.DrawLine(originCenter, originCenter + Vector2.down * checkDist);
        Gizmos.DrawLine(originLeft, originLeft + Vector2.down * checkDist);
        Gizmos.DrawLine(originRight, originRight + Vector2.down * checkDist);

        Gizmos.color = Color.yellow;
        Vector2 headLeft = new Vector2(bounds.min.x + rayInset, bounds.max.y);
        Vector2 headRight = new Vector2(bounds.max.x - rayInset, bounds.max.y);
        Gizmos.DrawLine(headLeft, headLeft + Vector2.up * cornerCheckDist);
        Gizmos.DrawLine(headRight, headRight + Vector2.up * cornerCheckDist);
    }
}