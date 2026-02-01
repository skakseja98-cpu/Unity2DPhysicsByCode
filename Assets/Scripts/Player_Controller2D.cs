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

    [Header("Ground Detection (Logic)")]
    public LayerMask groundLayer;
    public float rayLength = 0.1f;   // 발바닥에서 얼마나 더 길게 검사할지 (짧을수록 정교함)
    public float rayInset = 0.05f;   // 박스 모서리에서 살짝 안쪽으로 들어온 위치에서 쏠 것인가 (벽타기 방지)

    // 상태 변수 (이름 변경됨)
    [SerializeField] // 인스펙터에서 확인용 (수정X)
    private bool canJump; 

    private Rigidbody2D rb;
    private BoxCollider2D boxCol; // 발바닥 위치 계산용
    
    private Vector2 velocity;
    private float moveInput;
    private float gravity;
    private float jumpForce;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCol = GetComponent<BoxCollider2D>();

        // 물리 설정
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f; // 중력은 내가 계산함
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // 중력 계산
        gravity = -(2 * jumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        jumpForce = Mathf.Abs(gravity) * timeToJumpApex;
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        // 점프 시도: isGrounded가 아니라 canJump 변수를 확인
        if (Input.GetButtonDown("Jump") && canJump)
        {
            velocity = rb.linearVelocity; // 현재 속도 갱신
            velocity.y = jumpForce;
            rb.linearVelocity = velocity;
        }

        // 점프 컷
        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0)
        {
            velocity = rb.linearVelocity;
            velocity.y *= jumpCutMult;
            rb.linearVelocity = velocity;
        }
    }

    void FixedUpdate()
    {
        // 1. 바닥 감지 (점프 가능 여부만 판단, 멈추는 기능 없음!)
        CheckGroundStatus();

        // 2. 속도 가져오기
        velocity = rb.linearVelocity;

        // 3. 수평 이동 계산
        float targetSpeed = moveInput * maxSpeed;
        float accelRate;

        if (moveInput != 0)
            accelRate = (Mathf.Sign(moveInput) != Mathf.Sign(velocity.x) && Mathf.Abs(velocity.x) > 0.1f) ? turnSpeed : acceleration;
        else
            accelRate = deceleration;

        velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, accelRate * Time.fixedDeltaTime);


        // 4. 수직 이동 (중력 적용)
        // [수정됨] 땅이든 아니든 일단 중력을 계속 적용합니다.
        // 땅에 닿으면 BoxCollider가 알아서 더 이상 못 내려가게 막아줍니다.ㅇ
        
        float currentGravity = gravity;
        
        // 떨어질 때만 중력을 세게 받음
        if (velocity.y < 0) currentGravity *= fallGravityMult;

        velocity.y += currentGravity * Time.fixedDeltaTime;
        velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);

        // 5. 최종 적용
        rb.linearVelocity = velocity;
    }

    // 3방향 레이캐스트 로직
    private void CheckGroundStatus()
    {
        // 박스 콜라이더의 경계값 가져오기
        Bounds bounds = boxCol.bounds;

        // 레이저 쏠 시작 높이 (발바닥보다 아주 살짝 위)
        float yOrigin = bounds.min.y + 0.05f; 
        
        // 레이저 쏠 X 좌표 3개 (좌측, 중앙, 우측)
        // rayInset을 줘서 모서리 끝보다 살짝 안쪽에서 쏘게 함 (벽에 비비다 점프되는 버그 방지)
        float xLeft = bounds.min.x + rayInset;
        float xRight = bounds.max.x - rayInset;
        float xCenter = bounds.center.x;

        // 실제 검사할 거리 (시작점에서 발바닥까지 거리 + 추가 여유분)
        float checkDist = 0.05f + rayLength;

        // 3발 발사
        RaycastHit2D hitL = Physics2D.Raycast(new Vector2(xLeft, yOrigin), Vector2.down, checkDist, groundLayer);
        RaycastHit2D hitC = Physics2D.Raycast(new Vector2(xCenter, yOrigin), Vector2.down, checkDist, groundLayer);
        RaycastHit2D hitR = Physics2D.Raycast(new Vector2(xRight, yOrigin), Vector2.down, checkDist, groundLayer);

        // 셋 중 하나라도 바닥에 닿으면 점프 가능
        canJump = (hitL.collider != null || hitC.collider != null || hitR.collider != null);
    }

    // 에디터에서 레이저 눈으로 확인하기
    private void OnDrawGizmos()
    {
        if (boxCol == null) return;

        Bounds bounds = boxCol.bounds;
        float yOrigin = bounds.min.y + 0.05f;
        float checkDist = 0.05f + rayLength;
        float xLeft = bounds.min.x + rayInset;
        float xRight = bounds.max.x - rayInset;
        float xCenter = bounds.center.x;

        // canJump 상태에 따라 색깔 변경 (닿으면 초록, 아니면 빨강)
        Gizmos.color = canJump ? Color.green : Color.red;

        Gizmos.DrawLine(new Vector2(xLeft, yOrigin), new Vector2(xLeft, yOrigin - checkDist));
        Gizmos.DrawLine(new Vector2(xCenter, yOrigin), new Vector2(xCenter, yOrigin - checkDist));
        Gizmos.DrawLine(new Vector2(xRight, yOrigin), new Vector2(xRight, yOrigin - checkDist));
    }
}