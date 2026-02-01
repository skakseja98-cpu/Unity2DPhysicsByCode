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

    [Header("Dash")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 0.5f;
    private bool isDashing;
    private float dashTimeLeft;
    private float lastDashTime = -10f;
    private Vector2 dashDir;

    [Header("Ground Detection")]
    public LayerMask groundLayer;
    public float rayLength = 0.1f;
    public float rayInset = 0.05f;
    [SerializeField] private bool isGroundDetected;

    private Rigidbody2D rb;
    private BoxCollider2D boxCol;
    
    private Vector2 velocity;
    private Vector2 inputVector;
    
    // [수정 1] gravity 변수의 역할을 '기본 중력'과 '현재 적용 중력'으로 분리
    private float baseGravity;       // 점프 높이 기반으로 계산된 원래 중력 (기준값)
    private float gravityMultiplier = 1f; // GravityManager에서 조절할 배율 (기본 1)
    private float jumpForce;
    private int facingDirection = 1;

    // [수정 2] 외부(GravityManager)에서 중력 배율을 조절하는 함수 추가
    public void SetGravityScale(float scale)
    {
        gravityMultiplier = scale;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCol = GetComponent<BoxCollider2D>();

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f; 
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // [수정 3] 초기 중력을 'baseGravity'에 저장
        baseGravity = -(2 * jumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        jumpForce = Mathf.Abs(baseGravity) * timeToJumpApex;
    }

    void Update()
    {
        // ... (입력, 대쉬 입력, 코요테 타임 로직은 기존과 동일) ...
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputY = Input.GetAxisRaw("Vertical");
        inputVector = new Vector2(inputX, inputY);

        if (inputX != 0) facingDirection = (int)Mathf.Sign(inputX);

        if (Input.GetButtonDown("Dash") && Time.time >= lastDashTime + dashCooldown)
            StartDash();

        if (isGroundDetected) coyoteTimeCounter = coyoteTime;
        else coyoteTimeCounter -= Time.deltaTime;

        if (Input.GetButtonDown("Jump") && coyoteTimeCounter > 0f)
        {
            isDashing = false;
            velocity = rb.linearVelocity;
            velocity.y = jumpForce; // 점프 힘은 중력 배율과 상관없이 일정하게 유지 (원하면 여기도 배율 곱하기 가능)
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
        Time.timeScale = 0f; 
        yield return new WaitForSecondsRealtime(duration); 
        Time.timeScale = 1f; 
    }

    void StartDash()
    {
        isDashing = true;
        dashTimeLeft = dashDuration;
        lastDashTime = Time.time;
        StartCoroutine(StopTime(0.07f));

        if (inputVector == Vector2.zero) dashDir = new Vector2(facingDirection, 0);
        else dashDir = inputVector.normalized;
    }

    void FixedUpdate()
    {
        if (isDashing)
        {
            if (dashTimeLeft > 0)
            {
                rb.linearVelocity = dashDir * dashSpeed;
                dashTimeLeft -= Time.fixedDeltaTime;
                return; 
            }
            else
            {
                isDashing = false;
                rb.linearVelocity = dashDir * maxSpeed;
            }
        }

        CheckGroundStatus();
        velocity = rb.linearVelocity;

        float targetSpeed = inputVector.x * maxSpeed;
        float accelRate;
        if (inputVector.x != 0)
            accelRate = (Mathf.Sign(inputVector.x) != Mathf.Sign(velocity.x) && Mathf.Abs(velocity.x) > 0.1f) ? turnSpeed : acceleration;
        else
            accelRate = deceleration;

        velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, accelRate * Time.fixedDeltaTime);

        // [수정 4] 최종 중력 계산 로직 변경
        // 기본 중력(baseGravity)에 현재 배율(gravityMultiplier)을 곱해서 적용
        float currentGravity = baseGravity * gravityMultiplier; 

        // 떨어질 때 가속 (중력 방향이 아래일 때와 위일 때를 모두 고려)
        // 중력이 아래(-), 속도가 아래(-) 이거나 / 중력이 위(+), 속도가 위(+) 일 때 가속
        bool isFalling = (currentGravity < 0 && velocity.y < 0) || (currentGravity > 0 && velocity.y > 0);
        
        if (isFalling) 
            currentGravity *= fallGravityMult;

        velocity.y += currentGravity * Time.fixedDeltaTime;
        
        // 최대 낙하 속도 제한 (역중력 상황 고려)
        if (currentGravity < 0)
             velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
        else if (currentGravity > 0)
             velocity.y = Mathf.Min(velocity.y, maxFallSpeed);

        rb.linearVelocity = velocity;
    }

    // ... (CheckGroundStatus, OnDrawGizmos 기존과 동일) ...
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
        // (기존 코드 생략)
        Bounds bounds = boxCol.bounds;
        float yOrigin = bounds.min.y + 0.05f;
        float checkDist = 0.05f + rayLength;
        float xLeft = bounds.min.x + rayInset;
        float xRight = bounds.max.x - rayInset;
        float xCenter = bounds.center.x;

        Gizmos.color = isGroundDetected ? Color.green : Color.red;
        Gizmos.DrawLine(new Vector2(xLeft, yOrigin), new Vector2(xLeft, yOrigin - checkDist));
        Gizmos.DrawLine(new Vector2(xCenter, yOrigin), new Vector2(xCenter, yOrigin - checkDist));
        Gizmos.DrawLine(new Vector2(xRight, yOrigin), new Vector2(xRight, yOrigin - checkDist));
        Gizmos.DrawRay(transform.position, Vector3.right * facingDirection * 1.5f);
    }
}