using UnityEngine;

public class Player_Grapple : MonoBehaviour
{
    [Header("Grapple Settings")]
    public GameObject ropePrefab;
    public LayerMask groundLayer;
    public float anchorRayDistance = 10f;
    public float maxRopeLength = 15f;
    public float swingAcceleration = 40f;

    [Header("Retraction")]
    public float pullInitSpeed = 5f;
    public float pullMaxSpeed = 25f;
    public float pullAccelDuration = 1.0f;
    
    [Header("Release Boost")]
    public float releaseVelocityMult = 1.2f;
    public float releaseUpwardForce = 5f;

    public bool HasAnchor { get; private set; }
    public Rope CurrentRope { get; private set; }

    private Rigidbody2D rb;
    private BoxCollider2D boxCol;
    private Player_Movement movement;

    private Vector2 anchorPos;
    private float currentMaxLen;
    private float pullTimer;

    // [추가] 현재 중력 스케일 저장용 변수
    private float currentGravityScale = 1f;

    private bool isGhostMode;
    private int playerLayer;
    private int groundLayerIndex;

    public void Initialize(Rigidbody2D _rb, BoxCollider2D _col, Player_Movement _move)
    {
        rb = _rb;
        boxCol = _col;
        movement = _move;

        playerLayer = gameObject.layer;
        groundLayerIndex = (int)Mathf.Log(groundLayer.value, 2);
    }

    // [추가] 외부(Controller -> GravityManager)에서 호출할 함수
    public void SetGravityScale(float scale)
    {
        currentGravityScale = scale;
        if (CurrentRope != null)
        {
            CurrentRope.SetGravityScale(scale);
        }
    }

    public void HandleInput(bool isGrappleDown, bool isRetractHeld, Vector2 inputDir)
    {
        if (isGrappleDown)
        {
            if (HasAnchor) ReleaseAnchor();
            else FireAnchor();
        }

        if (HasAnchor && CurrentRope != null)
        {
            CurrentRope.UpdateEndPosition(transform.position);
        }
    }

    public void ApplyPhysics(bool isRetractHeld, Vector2 inputDir)
    {
        ManageGhostMode(isRetractHeld);

        if (!HasAnchor) return;

        bool isTaut = Vector2.Distance(transform.position, anchorPos) >= currentMaxLen - 0.2f;
        if (!movement.IsGrounded && isTaut && inputDir.x != 0)
        {
            rb.AddForce(new Vector2(inputDir.x * swingAcceleration, 0), ForceMode2D.Force);
        }

        ApplyRetraction(isRetractHeld);
        ApplyDistanceConstraint();
    }

    private void FireAnchor()
    {
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
            CurrentRope = ropeObj.GetComponent<Rope>();
            currentMaxLen = maxRopeLength;
            
            // [수정] 이제 Rope가 4번째 인자를 받을 준비가 되었으므로 에러가 사라짐
            CurrentRope.InitializeRope(anchorPos, transform, maxRopeLength, currentGravityScale);
            
            HasAnchor = true;
        }
    }

    private void ReleaseAnchor()
    {
        if (!movement.IsGrounded)
        {
            Vector2 boostVel = rb.linearVelocity * releaseVelocityMult;
            boostVel += Vector2.up * releaseUpwardForce;
            rb.linearVelocity = boostVel;
        }

        if (CurrentRope != null) Destroy(CurrentRope.gameObject);
        CurrentRope = null;
        HasAnchor = false;
    }

    private void ApplyRetraction(bool isHeld)
    {
        if (isHeld)
        {
            float standOffset = boxCol.bounds.extents.y;
            Vector2 targetPos = anchorPos + Vector2.up * standOffset;
            Vector2 toTarget = targetPos - rb.position;
            
            if (toTarget.magnitude > 0.1f)
            {
                pullTimer += Time.fixedDeltaTime;
                float t = Mathf.Clamp01(pullTimer / pullAccelDuration);
                float speed = Mathf.Lerp(pullInitSpeed, pullMaxSpeed, t);
                
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, toTarget.normalized * speed, 0.1f);
                
                currentMaxLen = Vector2.Distance(rb.position, anchorPos);
                if (CurrentRope != null) CurrentRope.UpdateRopeLength(currentMaxLen);
            }
        }
        else
        {
            pullTimer = 0f;
        }
    }

    private void ApplyDistanceConstraint()
    {
        Vector2 toAnchor = anchorPos - rb.position;
        float dist = toAnchor.magnitude;

        if (dist > currentMaxLen)
        {
            Vector2 tetherDir = toAnchor.normalized;
            rb.position = Vector2.Lerp(rb.position, anchorPos - tetherDir * currentMaxLen, 0.5f);

            float velDot = Vector2.Dot(rb.linearVelocity, tetherDir);
            if (velDot < 0)
            {
                rb.linearVelocity += tetherDir * (-velDot);
            }
        }
    }

    private void ManageGhostMode(bool isRetractHeld)
    {
        // [핵심 수정 1] 땅에 서 있다면(Grounded), 절대 고스트 모드가 될 수 없음.
        // 땅을 뚫고 떨어지는 현상(버그)을 완벽하게 방지합니다.
        if (movement.IsGrounded)
        {
            if (isGhostMode) SetGhostMode(false);
            return;
        }

        // [핵심 수정 2] 감지 박스 크기를 0.9f -> 0.7f로 줄임.
        // 가장자리에 살짝 닿은 걸로 "갇혔다"고 판단하지 않도록 판정을 더 엄격하게 함.
        bool isInsideWall = Physics2D.OverlapBox(boxCol.bounds.center, boxCol.bounds.size * 0.7f, 0f, groundLayer);
        bool shouldBeGhost = false;

        if (isRetractHeld)
        {
            // 당기는 중(G키)이고 공중이라면 고스트 모드 진입
            shouldBeGhost = true;
        }
        else if (isInsideWall)
        {
            // 키를 뗐는데 아직 벽 속이라면, 탈출할 때까지 고스트 유지
            shouldBeGhost = true;
        }

        if (isGhostMode != shouldBeGhost)
        {
            SetGhostMode(shouldBeGhost);
        }
    }

    // [헬퍼 함수] 중복 코드 제거를 위해 분리
    private void SetGhostMode(bool active)
    {
        isGhostMode = active;
        Physics2D.IgnoreLayerCollision(playerLayer, groundLayerIndex, active);
    }
}