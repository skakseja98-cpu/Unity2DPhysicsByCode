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
    public bool isGroundDetected;

    [Header("Climbing (Rope)")]
    public float climbSpeed = 5f; // 로프 오르내리는 속도
    private bool isClimbing;      // 현재 로프를 타고 있는지 여부
    private bool isNearRope;      // 로프와 겹쳐 있는지 여부
    private VerletRope2D currentRope; // 현재 닿아있는 로프
    private float currentClimbIndex;  // 현재 매달린 로프의 마디 위치 (소수점 포함)
    public float swingForce = 0.5f; // [추가] 로프를 좌우로 흔드는 힘

    [Header("Grappling Hook")]
    public GameObject ropePrefab;       // [필수] 프로젝트 창에 만든 로프 프리팹을 넣을 칸
    public float maxGrappleDistance = 15f; // 훅이 닿는 최대 사거리
    public LineRenderer aimLine; // [추가] 조준할 때 보여줄 궤적 선
    

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

        // [추가: 로프 등반] --------------------------------------------------
        // 1. 로프 근처에서 [위] 방향키를 누르면 매달리기 시작
        if (isNearRope && !isClimbing && inputY > 0.1f)
        {
            AttachToRope();
        }

        // 2. 매달린 상태에서의 조작
        if (isClimbing)
        {
            if (Input.GetButtonDown("Jump")) DetachFromRope();

            currentClimbIndex -= inputY * climbSpeed * Time.deltaTime; 
            currentClimbIndex = Mathf.Clamp(currentClimbIndex, 0, currentRope.segmentCount - 1);
            
            // [신규] 좌우(A/D) 키를 누르면 플레이어가 매달린 노드에 힘을 가함 (로프 흔들기)
            if (inputX != 0)
            {
                int currentNode = Mathf.RoundToInt(currentClimbIndex);
                currentRope.AddForceToNode(currentNode, new Vector2(inputX * swingForce * Time.fixedDeltaTime, 0));
            }

            coyoteTimeCounter = 0; 
            isDashing = false;
        }
        else if (isNearRope && inputY > 0.1f) // 매달려있지 않을 때만 매달리기 가능
        {
            AttachToRope();
        }

        HandleAimAndShoot();
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

        // [추가: 로프 등반] 매달려 있을 때는 일반 물리를 무시하고 로프 위치를 따라감
        if (isClimbing)
        {
            // 현재 소수점 인덱스를 기준으로 위쪽 마디(A)와 아래쪽 마디(B)를 찾음
            int nodeA = Mathf.FloorToInt(currentClimbIndex);
            int nodeB = Mathf.CeilToInt(currentClimbIndex);
            float t = currentClimbIndex - nodeA; // 두 마디 사이의 비율 (0 ~ 1)

            // 두 마디 사이의 위치를 부드럽게 보간(Lerp)
            Vector2 posA = currentRope.GetNodePosition(nodeA);
            Vector2 posB = currentRope.GetNodePosition(nodeB);
            rb.position = Vector2.Lerp(posA, posB, t);

            // 속도를 0으로 만들어 중력의 영향을 받지 않게 함
            rb.linearVelocity = Vector2.zero;
            return; // <--- 중요: 여기서 끊어야 아래의 일반 이동/중력 코드가 실행되지 않음
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

    // [추가: 로프 등반 함수들] --------------------------------------------------
    private void AttachToRope()
    {
        isClimbing = true;
        // 플레이어 위치에서 가장 가까운 로프 마디 번호를 찾아 매달림
        currentClimbIndex = currentRope.GetClosestNodeIndex(transform.position); 
    }

    private void DetachFromRope()
    {
        isClimbing = false;
        // 로프에서 떨어질 때 살짝 위로 튀어오르는 힘을 줌
        velocity = rb.linearVelocity;
        velocity.y = jumpForce * 0.5f; 
        rb.linearVelocity = velocity;
    }

    // 로프(EdgeCollider2D - Trigger)에 닿았을 때 감지
    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.TryGetComponent<VerletRope2D>(out VerletRope2D rope))
        {
            isNearRope = true;
            currentRope = rope;
        }
    }

    // 로프에서 멀어졌을 때
    private void OnTriggerExit2D(Collider2D col)
    {
        if (col.GetComponent<VerletRope2D>() == currentRope)
        {
            isNearRope = false;
            if (!isClimbing) currentRope = null; // 매달려있지 않을 때만 로프 정보 삭제
        }
    }

    private void HandleAimAndShoot()
    {
        // 1. [제한사항] 로프를 타고 있을 때는 조준/발사 불가능
        if (isClimbing)
        {
            if (aimLine != null) aimLine.enabled = false;
            return;
        }

        // 2. 마우스 우클릭을 "누르고 있는 동안" 조준 모드 활성화
        if (Input.GetMouseButton(1)) 
        {
            if (aimLine != null) aimLine.enabled = true; // 조준선 켜기

            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 direction = (mousePos - (Vector2)transform.position).normalized;

            // 벽 감지
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, maxGrappleDistance, groundLayer);
            
            // 벽에 닿았으면 닿은 곳까지, 안 닿았으면 최대 사거리까지 조준선 끝점 설정
            Vector2 endPoint = hit.collider != null ? hit.point : (Vector2)transform.position + direction * maxGrappleDistance;

            // 조준선(LineRenderer) 그리기 (초록색 등으로 설정 추천)
            if (aimLine != null)
            {
                aimLine.SetPosition(0, transform.position); // 시작점: 플레이어
                aimLine.SetPosition(1, endPoint);           // 끝점: 벽 또는 허공
            }

            // 3. 조준 중일 때 마우스 좌클릭 + 벽에 닿았을 때만 로프 발사
            if (Input.GetMouseButtonDown(0) && hit.collider != null)
            {
                ShootRope(hit.point);
            }
        }
        else // 우클릭을 떼면 조준 모드 해제
        {
            if (aimLine != null) aimLine.enabled = false; // 조준선 끄기
        }
    }

    // [수정] 조준선이 가리키는 정확한 좌표(hitPoint)로 로프를 생성
    private void ShootRope(Vector2 hitPoint)
    {
        if (currentRope != null) Destroy(currentRope.gameObject); 

        GameObject newRopeObj = Instantiate(ropePrefab, hitPoint, Quaternion.identity);
        VerletRope2D newRope = newRopeObj.GetComponent<VerletRope2D>();

        newRope.InitializeGrapple(hitPoint, transform.position);
    }
}