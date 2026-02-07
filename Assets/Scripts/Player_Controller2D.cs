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

    [Header("Anchor & Rope System")]
    public GameObject ropePrefab;
    public float anchorRayDistance = 10f; 
    public float maxRopeLength = 15f;     
    public float minRopeLength = 1.0f;    // 평소 유지하는 최소 거리
    public float swingAcceleration = 40f; 

    [Header("Retraction (G Key)")]
    public float pullInitSpeed = 5f;
    public float pullMaxSpeed = 25f;
    public float pullAccelDuration = 1.0f;
    public float pullMemoryTime = 0.5f;

    [Header("Release Boost")]
    public float releaseVelocityMult = 1.2f; // 놓을 때 현재 속도를 몇 배로 증폭할지
    public float releaseUpwardForce = 5f;    // 놓을 때 위쪽으로 가해주는 보너스 힘

    // --- 내부 변수 ---
    private Rigidbody2D rb;
    private BoxCollider2D boxCol;
    private Vector2 velocity;
    private Vector2 inputVector;
    
    private float baseGravity;
    private float gravityMultiplier = 1f;
    private float jumpForce;
    private int facingDirection = 1;

    // 앵커 관련
    private Rope2D currentRope;
    private Vector2 anchorPos;
    private bool hasAnchor;
    private float currentMaxLen; 
    
    // 당기기 가속 관련
    private float pullTimer = 0f;
    private float lastPullTime = -10f;

    // 고스트 모드 관련
    private bool isGhostMode = false;
    private int playerLayer;
    private int groundLayerIndex;

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

        baseGravity = -(2 * jumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        jumpForce = Mathf.Abs(baseGravity) * timeToJumpApex;

        playerLayer = gameObject.layer;
        
        // Ground Layer Index 추출
        groundLayerIndex = 0;
        int layerVal = groundLayer.value;
        while(layerVal > 1) {
            layerVal >>= 1;
            groundLayerIndex++;
        }
    }

    void Update()
    {
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputY = Input.GetAxisRaw("Vertical");
        inputVector = new Vector2(inputX, inputY);

        if (inputX != 0) facingDirection = (int)Mathf.Sign(inputX);

        if (Input.GetKeyDown(KeyCode.F)) ToggleAnchor();

        if (Input.GetButtonDown("Dash") && Time.time >= lastDashTime + dashCooldown)
            StartDash();

        if (isGroundDetected) coyoteTimeCounter = coyoteTime;
        else coyoteTimeCounter -= Time.deltaTime;

        if (Input.GetButtonDown("Jump") && coyoteTimeCounter > 0f)
        {
            isDashing = false;
            velocity = rb.linearVelocity;
            velocity.y = jumpForce;
            rb.linearVelocity = velocity;
            coyoteTimeCounter = 0f;
            
            if(hasAnchor && currentMaxLen < maxRopeLength) currentMaxLen += 1f;
        }

        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0)
        {
            velocity = rb.linearVelocity;
            velocity.y *= jumpCutMult;
            rb.linearVelocity = velocity;
        }

        if (hasAnchor && currentRope != null)
        {
            currentRope.UpdateEndPosition(transform.position);
        }
    }

    void FixedUpdate()
    {
        if (isDashing)
        {
            HandleDash();
            return;
        }

        CheckGroundStatus();
        velocity = rb.linearVelocity;

        // 1. 고스트 모드 관리
        ManageGhostMode();

        // 2. 이동 로직 분기
        bool isTaut = hasAnchor && Vector2.Distance(transform.position, anchorPos) >= currentMaxLen - 0.2f;
        bool isSwinging = hasAnchor && !isGroundDetected && isTaut;

        if (isSwinging)
        {
            // [스윙 모드] AddForce 사용
            if (inputVector.x != 0)
            {
                rb.AddForce(new Vector2(inputVector.x * swingAcceleration, 0), ForceMode2D.Force);
            }
            velocity.x *= 0.995f; 
        }
        else
        {
            // [일반 모드] MoveTowards 사용 (빠릿한 조작감)
            float targetSpeed = inputVector.x * maxSpeed;
            float currentAccel = acceleration; 
            
            if (inputVector.x != 0)
            {
                if (Mathf.Sign(inputVector.x) != Mathf.Sign(velocity.x) && Mathf.Abs(velocity.x) > 0.1f)
                    currentAccel = turnSpeed;
            }
            else
            {
                currentAccel = deceleration;
            }

            velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, currentAccel * Time.fixedDeltaTime);
        }

        // 3. 중력
        float currentGravity = baseGravity * gravityMultiplier; 
        bool isFalling = (currentGravity < 0 && velocity.y < 0) || (currentGravity > 0 && velocity.y > 0);
        if (isFalling) currentGravity *= fallGravityMult;
        velocity.y += currentGravity * Time.fixedDeltaTime;

        if (currentGravity < 0) velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
        else if (currentGravity > 0) velocity.y = Mathf.Min(velocity.y, maxFallSpeed);

        rb.linearVelocity = velocity;

        // 4. 로프 물리
        if (hasAnchor) ApplyRopePhysics();
    }

    void ToggleAnchor()
    {
        if (hasAnchor)
        {
            // [수정] 속도 체크(magnitude)를 제거하여, 공중이라면 언제든(가만히 있어도)
            // 줄을 끊을 때 살짝 튀어 오르는 '반동'을 주어 손맛을 살림.
            // 단, 땅에 서 있을 때(!isGroundDetected)는 낙사 방지를 위해 발동 안 함.
            if (!isGroundDetected)
            {
                // 1. 현재 속도 증폭 (움직이고 있었다면 더 빠르게)
                Vector2 boostVel = rb.linearVelocity * releaseVelocityMult;
                
                // 2. 위쪽 방향 보너스 (가만히 있었다면 제자리 톡튀, 움직였다면 롱점프)
                boostVel += Vector2.up * releaseUpwardForce;

                rb.linearVelocity = boostVel;
            }

            // 앵커 회수
            if(currentRope != null) Destroy(currentRope.gameObject);
            currentRope = null;
            hasAnchor = false;
        }
        else
        {
            // ... (설치 로직은 그대로) ...
            Vector2[] directions = { Vector2.down, Vector2.up, Vector2.right, Vector2.left, new Vector2(1, -1), new Vector2(-1, -1) };
            RaycastHit2D closestHit = new RaycastHit2D();
            float minDst = float.MaxValue;
            bool found = false;

            foreach (var dir in directions)
            {
                RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, anchorRayDistance, groundLayer);
                if (hit.collider != null && hit.distance < minDst)
                {
                    minDst = hit.distance;
                    closestHit = hit;
                    found = true;
                }
            }

            if (found)
            {
                anchorPos = closestHit.point;
                GameObject ropeObj = Instantiate(ropePrefab, anchorPos, Quaternion.identity);
                currentRope = ropeObj.GetComponent<Rope2D>();
                
                currentMaxLen = maxRopeLength; 
                currentRope.InitializeRope(anchorPos, transform, maxRopeLength); 
                hasAnchor = true;
            }
        }
    }

    // [핵심 수정] 당기기 로직 변경
    void ApplyRopePhysics()
    {
        // [수정 1] 목표 지점 계산: 앵커 자체가 아니라, '앵커 위로 서 있을 위치'를 목표로 함
        // BoxCollider의 절반 높이만큼 위로 보정
        float standOffset = boxCol.bounds.extents.y; 
        Vector2 targetPos = anchorPos + Vector2.up * standOffset;

        // 당기기 계산을 위한 벡터 (플레이어 -> 목표 지점)
        Vector2 toTarget = targetPos - rb.position;
        float distToTarget = toTarget.magnitude;

        // 로프 길이 계산용 벡터 (플레이어 -> 실제 앵커) - 물리 제한은 여전히 앵커 기준
        Vector2 toAnchor = anchorPos - rb.position;
        float distToAnchor = toAnchor.magnitude;

        // --- G키 당기기 ---
        if (Input.GetKey(KeyCode.G))
        {
            // [수정 2] 목표 지점(땅 위)까지의 거리가 0.1f보다 크면 계속 당김
            if (distToTarget > 0.1f) 
            {
                if (Time.time - lastPullTime > pullMemoryTime) pullTimer = 0f;
                pullTimer += Time.fixedDeltaTime;
                lastPullTime = Time.time;

                float t = Mathf.Clamp01(pullTimer / pullAccelDuration);
                float currentPullSpeed = Mathf.Lerp(pullInitSpeed, pullMaxSpeed, t);

                // 목표 지점(땅 위)을 향해 당김
                Vector2 pullVel = toTarget.normalized * currentPullSpeed;
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, pullVel, 0.1f);
                
                // 줄 길이 업데이트 (실제 앵커와의 거리 기준)
                currentMaxLen = distToAnchor;
                
                if(currentRope != null)
                {
                    currentRope.UpdateRopeLength(currentMaxLen);
                }
            }
        }
        
        // --- 거리 제한 (추락 방지) ---
        // 추락 방지는 여전히 '실제 앵커'를 기준으로 해야 함 (줄은 앵커에 매달려 있으니까)
        if (distToAnchor > currentMaxLen)
        {
            Vector2 tetherDir = toAnchor.normalized;
            Vector2 constrainedPos = anchorPos - tetherDir * currentMaxLen;
            rb.position = Vector2.Lerp(rb.position, constrainedPos, 0.5f);

            float velDot = Vector2.Dot(rb.linearVelocity, tetherDir);
            if (velDot < 0)
            {
                Vector2 dampingForce = tetherDir * (-velDot); 
                rb.linearVelocity += dampingForce;
            }
        }
    }

    void ManageGhostMode()
    {
        bool gKeyHeld = Input.GetKey(KeyCode.G);
        bool isInsideWall = Physics2D.OverlapBox(boxCol.bounds.center, boxCol.bounds.size * 0.9f, 0f, groundLayer);

        bool shouldBeGhost = false;

        if (gKeyHeld)
        {
            if (isGroundDetected) shouldBeGhost = false;
            else shouldBeGhost = true;
        }
        else
        {
            // 키를 뗐을 때: 벽 속이면 탈출할 때까지 유령 유지
            // 앵커 위로 올라와서 벽(땅) 밖으로 나오면 자동으로 false가 되어 충돌이 켜짐 -> 착지 성공!
            if (isInsideWall) shouldBeGhost = true;
            else shouldBeGhost = false;
        }
        
        if (isGhostMode != shouldBeGhost)
        {
            SetGhostMode(shouldBeGhost);
        }
    }

    void SetGhostMode(bool active)
    {
        isGhostMode = active;
        Physics2D.IgnoreLayerCollision(playerLayer, groundLayerIndex, active);
    }

    void HandleDash()
    {
        if (dashTimeLeft > 0) { rb.linearVelocity = dashDir * dashSpeed; dashTimeLeft -= Time.fixedDeltaTime; }
        else { isDashing = false; rb.linearVelocity = dashDir * maxSpeed; }
    }
    
    IEnumerator StopTime(float duration) { Time.timeScale = 0f; yield return new WaitForSecondsRealtime(duration); Time.timeScale = 1f; }

    void StartDash()
    {
        isDashing = true; dashTimeLeft = dashDuration; lastDashTime = Time.time;
        StartCoroutine(StopTime(0.07f));
        if (inputVector == Vector2.zero) dashDir = new Vector2(facingDirection, 0); else dashDir = inputVector.normalized;
    }

    private void CheckGroundStatus()
    {
        Bounds bounds = boxCol.bounds;
        float yOrigin = bounds.min.y + 0.05f; float checkDist = 0.05f + rayLength;
        RaycastHit2D hitC = Physics2D.Raycast(new Vector2(bounds.center.x, yOrigin), Vector2.down, checkDist, groundLayer);
        isGroundDetected = (hitC.collider != null);
    }
}