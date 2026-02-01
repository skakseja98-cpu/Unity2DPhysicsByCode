using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]

public class Player_Controller2D : MonoBehaviour
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
    
    [Header("Feel")]
    public float fallGravityMult = 1.5f;
    public float jumpCutMult = 0.5f;

    [Header("Coyote Time (New!)")]
    public float coyoteTime = 0.2f;    // 떨어져도 점프 가능한 유예 시간 (초)
    private float coyoteTimeCounter;   // 실제 타이머

    [Header("Ground Detection (Logic)")]
    public LayerMask groundLayer;
    public float rayLength = 0.1f;
    public float rayInset = 0.05f;

    // 땅 감지 여부 (Raycast 결과)
    [SerializeField]
    private bool isGroundDetected; // 변수명 명확화를 위해 변경 (기존 canJump)

    private Rigidbody2D rb;
    private BoxCollider2D boxCol;
    
    private Vector2 velocity;
    private float moveInput;
    private float gravity;
    private float jumpForce;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCol = GetComponent<BoxCollider2D>();

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f; 
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        gravity = -(2 * jumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        jumpForce = Mathf.Abs(gravity) * timeToJumpApex;
    }

    void Update()
    {
        // 1. 입력 받기
        moveInput = Input.GetAxisRaw("Horizontal");

        // 2. 코요테 타임 계산 (핵심 로직)
        if (isGroundDetected)
        {
            // 땅에 있으면 타이머를 계속 0.2초로 리필
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            // 땅에서 떨어지면 시간 깎기 시작
            coyoteTimeCounter -= Time.deltaTime;
        }

        // 3. 점프 시도
        // 조건: 점프 키를 눌렀고 + 코요테 타임이 남아있다면
        if (Input.GetButtonDown("Jump"))
        {
            if (coyoteTimeCounter > 0f)
            {
                velocity = rb.linearVelocity;
                velocity.y = jumpForce; // 강제로 상승 속도 주입
                rb.linearVelocity = velocity;

                // [중요] 점프했으면 유예 시간 즉시 삭제 (더블 점프 방지)
                coyoteTimeCounter = 0f; 
            }
        }

        // 4. 점프 컷 (숏 점프)
        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0)
        {
            velocity = rb.linearVelocity;
            velocity.y *= jumpCutMult;
            rb.linearVelocity = velocity;
        }
    }

    void FixedUpdate()
    {
        // 1. 바닥 감지 (Raycast) -> 결과는 isGroundDetected에 저장
        CheckGroundStatus();

        // 2. 속도 가져오기
        velocity = rb.linearVelocity;

        // 3. 수평 이동
        float targetSpeed = moveInput * maxSpeed;
        float accelRate;

        if (moveInput != 0)
            accelRate = (Mathf.Sign(moveInput) != Mathf.Sign(velocity.x) && Mathf.Abs(velocity.x) > 0.1f) ? turnSpeed : acceleration;
        else
            accelRate = deceleration;

        velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, accelRate * Time.fixedDeltaTime);

        // 4. 수직 이동 (중력)
        float currentGravity = gravity;
        if (velocity.y < 0) currentGravity *= fallGravityMult;

        velocity.y += currentGravity * Time.fixedDeltaTime;
        velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);

        // 5. 최종 적용
        rb.linearVelocity = velocity;
    }

    private void CheckGroundStatus()
    {
        Bounds bounds = boxCol.bounds;
        float yOrigin = bounds.min.y + 0.05f; 
        float xLeft = bounds.min.x + rayInset;
        float xRight = bounds.max.x - rayInset;
        float xCenter = bounds.center.x;
        float checkDist = 0.05f + rayLength;

        RaycastHit2D hitL = Physics2D.Raycast(new Vector2(xLeft, yOrigin), Vector2.down, checkDist, groundLayer);
        RaycastHit2D hitC = Physics2D.Raycast(new Vector2(xCenter, yOrigin), Vector2.down, checkDist, groundLayer);
        RaycastHit2D hitR = Physics2D.Raycast(new Vector2(xRight, yOrigin), Vector2.down, checkDist, groundLayer);

        // 땅 감지 여부 갱신
        isGroundDetected = (hitL.collider != null || hitC.collider != null || hitR.collider != null);
    }

    private void OnDrawGizmos()
    {
        if (boxCol == null) return;

        Bounds bounds = boxCol.bounds;
        float yOrigin = bounds.min.y + 0.05f;
        float checkDist = 0.05f + rayLength;
        float xLeft = bounds.min.x + rayInset;
        float xRight = bounds.max.x - rayInset;
        float xCenter = bounds.center.x;

        // 땅 감지되면 초록, 아니면 빨강
        Gizmos.color = isGroundDetected ? Color.green : Color.red;

        Gizmos.DrawLine(new Vector2(xLeft, yOrigin), new Vector2(xLeft, yOrigin - checkDist));
        Gizmos.DrawLine(new Vector2(xCenter, yOrigin), new Vector2(xCenter, yOrigin - checkDist));
        Gizmos.DrawLine(new Vector2(xRight, yOrigin), new Vector2(xRight, yOrigin - checkDist));
    }
}