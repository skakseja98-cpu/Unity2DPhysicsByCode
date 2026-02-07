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
    public int maxJumps = 2; 
    public float doubleJumpMultiplier = 0.8f;

    [Header("Low Gravity Settings")]
    [Tooltip("중력이 낮을 때 점프 힘을 얼마나 줄일지 (0=안 줄임, 1=중력비례 완전 감소)")]
    [Range(0f, 1f)]
    public float jumpForceScaling = 0.5f;

    [Tooltip("중력이 낮을 때 가속도(미끄러짐)를 얼마나 줄일지 (0=안 미끄러짐, 1=얼음판)")]
    [Range(0f, 1f)]
    public float horizontalControlScaling = 0.5f; 

    [Tooltip("중력이 낮을 때 최대 속도를 얼마나 줄일지 (0=속도 유지, 1=중력비례 느려짐)")]
    [Range(0f, 1f)]
    public float maxSpeedScaling = 0.5f; // [신규 기능]

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

            // 점프 힘 보정 (Scaling 적용)
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
            // 스윙 로직
        }
        else
        {
            // 현재 설정된 기본값 가져오기
            float currentMaxSpeed = maxSpeed;
            float currentAccel = 0f;

            // 가속도 결정
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

            // [핵심] 저중력 보정 로직 (가속도 & 최대속도)
            float gScale = Mathf.Abs(gravityMultiplier);
            if (gScale < 1f && gScale > 0.01f)
            {
                // 1. 미끄러짐(관성) 적용
                currentAccel *= Mathf.Pow(gScale, horizontalControlScaling);
                
                // 2. 최대 속도 감소 적용 [신규]
                currentMaxSpeed *= Mathf.Pow(gScale, maxSpeedScaling);
            }

            // 최종 목표 속도 계산 (보정된 MaxSpeed 사용)
            float targetSpeed = input.x * currentMaxSpeed;
            
            // 물리 적용
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