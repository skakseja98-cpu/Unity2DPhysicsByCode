using UnityEngine;
using System.Collections;

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

    [Header("Coyote Time")]
    public float coyoteTime = 0.2f;
    private float coyoteTimeCounter;

    [Header("Dash (New!)")]
    public float dashSpeed = 20f;      // 대쉬 속도
    public float dashDuration = 0.2f;  // 대쉬 지속 시간
    public float dashCooldown = 0.5f;  // 대쉬 쿨타임
    private bool isDashing;            // 현재 대쉬 중인가?
    private float dashTimeLeft;        // 대쉬 남은 시간
    private float lastDashTime = -10f; // 마지막 대쉬 시점
    private Vector2 dashDir;           // 대쉬 방향 저장용

    [Header("Ground Detection")]
    public LayerMask groundLayer;
    public float rayLength = 0.1f;
    public float rayInset = 0.05f;
    [SerializeField] private bool isGroundDetected;

    private Rigidbody2D rb;
    private BoxCollider2D boxCol;
    
    private Vector2 velocity;
    private Vector2 inputVector; // X, Y 입력 통합
    private float gravity;
    private float jumpForce;
    private int facingDirection = 1; // 1: 오른쪽, -1: 왼쪽

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
        // 1. 입력 받기 (X, Y 모두 받음)
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputY = Input.GetAxisRaw("Vertical"); // W, S 키
        inputVector = new Vector2(inputX, inputY);

        // 2. 바라보는 방향 갱신 (입력이 있을 때만 바꿈)
        if (inputX != 0)
        {
            facingDirection = (int)Mathf.Sign(inputX);
            // 필요하다면 여기서 스프라이트 좌우 반전(Flip) 코드 추가
            // transform.localScale = new Vector3(facingDirection, 1, 1);
        }

        // 3. 대쉬 입력 처리
        // 쿨타임 지났는지 확인
        if (Input.GetButtonDown("Dash") && Time.time >= lastDashTime + dashCooldown)
        {
            StartDash();
        }

        // --- 대쉬 중이면 아래 로직(점프, 코요테) 무시하거나, 대쉬 캔슬 점프를 구현할 수도 있음 ---
        // 여기서는 심플하게 대쉬 중에도 점프 로직은 돌리되, 물리 적용에서 우선순위를 둡니다.

        // 4. 코요테 타임 & 점프
        if (isGroundDetected) coyoteTimeCounter = coyoteTime;
        else coyoteTimeCounter -= Time.deltaTime;

        if (Input.GetButtonDown("Jump") && coyoteTimeCounter > 0f)
        {
            // 대쉬 중에 점프하면? -> 대쉬 캔슬 점프 (선택 사항)
            isDashing = false; // 점프하는 순간 대쉬 종료

            velocity = rb.linearVelocity;
            velocity.y = jumpForce;
            rb.linearVelocity = velocity;
            coyoteTimeCounter = 0f;
        }

        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0)
        {
            velocity = rb.linearVelocity;
            velocity.y *= jumpCutMult;
            rb.linearVelocity = velocity;
        }
    }

    IEnumerator StopTime(float duration)
{
    // 1. 시간 멈춤
    Time.timeScale = 0f; 
    
    // 2. 현실 시간(Realtime) 기준으로 0.1초 대기
    // (그냥 WaitForSeconds를 쓰면 게임 시간이 멈춰서 영원히 안 깨어남)
    yield return new WaitForSecondsRealtime(duration); 
    
    // 3. 시간 복구
    Time.timeScale = 1f; 
}

    void StartDash()
    {
        isDashing = true;
        dashTimeLeft = dashDuration;
        lastDashTime = Time.time;

        StartCoroutine(StopTime(0.07f)); // 시간정지

        // 대쉬 방향 계산
        // 입력이 없으면(0,0) -> 바라보는 방향으로
        if (inputVector == Vector2.zero)
        {
            dashDir = new Vector2(facingDirection, 0);
        }
        else
        {
            // 입력이 있으면 정규화(Normalize)해서 대각선도 속도 일정하게
            dashDir = inputVector.normalized;
        }
    }

    void FixedUpdate()
    {
        // 0. 대쉬 처리 (최우선 순위)
        if (isDashing)
        {
            // 대쉬 지속 시간 체크
            if (dashTimeLeft > 0)
            {
                // 중력 무시, 가속 무시. 오직 대쉬 방향으로 꽂음
                rb.linearVelocity = dashDir * dashSpeed;
                dashTimeLeft -= Time.fixedDeltaTime;
                
                // 대쉬 중에는 바닥 체크나 다른 로직 수행 안 함 (바로 리턴)
                return; 
            }
            else
            {
                // 대쉬 끝
                isDashing = false;
                // 대쉬 끝나면 속도를 약간 줄여줄지, 그대로 관성 유지할지는 선택 (여기선 관성 유지)
                rb.linearVelocity = dashDir * maxSpeed; // 대쉬 끝났으니 최대 달리기 속도로 맞춰줌 (자연스러운 감속)
            }
        }


        // --- 아래는 일반 이동(Run & Gravity) 로직 ---

        CheckGroundStatus();
        velocity = rb.linearVelocity;

        // 수평 이동
        float targetSpeed = inputVector.x * maxSpeed;
        float accelRate;
        if (inputVector.x != 0)
            accelRate = (Mathf.Sign(inputVector.x) != Mathf.Sign(velocity.x) && Mathf.Abs(velocity.x) > 0.1f) ? turnSpeed : acceleration;
        else
            accelRate = deceleration;

        velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, accelRate * Time.fixedDeltaTime);

        // 수직 이동 (중력)
        float currentGravity = gravity;
        if (velocity.y < 0) currentGravity *= fallGravityMult;

        velocity.y += currentGravity * Time.fixedDeltaTime;
        velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);

        rb.linearVelocity = velocity;
    }

    private void CheckGroundStatus()
    {
        Bounds bounds = boxCol.bounds;
        float yOrigin = bounds.min.y + 0.05f; 
        float checkDist = 0.05f + rayLength;
        float xLeft = bounds.min.x + rayInset;
        float xRight = bounds.max.x - rayInset;
        float xCenter = bounds.center.x;

        RaycastHit2D hitL = Physics2D.Raycast(new Vector2(xLeft, yOrigin), Vector2.down, checkDist, groundLayer);
        RaycastHit2D hitC = Physics2D.Raycast(new Vector2(xCenter, yOrigin), Vector2.down, checkDist, groundLayer);
        RaycastHit2D hitR = Physics2D.Raycast(new Vector2(xRight, yOrigin), Vector2.down, checkDist, groundLayer);


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
        Gizmos.DrawRay(transform.position, Vector3.right * facingDirection * 1.5f);
    }
}