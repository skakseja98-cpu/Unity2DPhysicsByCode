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
    public float retractionCooldown = 0.5f;
    
    [Header("Release Boost")]
    public float releaseVelocityMult = 1.2f;
    public float releaseUpwardForce = 5f;

    public bool HasAnchor { get; private set; }
    public Rope CurrentRope { get; private set; }
    public bool IsTaut { get; private set; }

    private Rigidbody2D rb;
    private BoxCollider2D boxCol;
    private Player_Movement movement;

    private Vector2 anchorPos;
    private float currentMaxLen;
    private float pullTimer;

    private float currentGravityScale = 1f;
    private float nextRetractTime = 0f;

    private bool isGhostMode;
    private int playerLayer;
    private int groundLayerIndex;

    public void Initialize(Rigidbody2D _rb, BoxCollider2D _col, Player_Movement _move)
    {
        rb = _rb;
        boxCol = _col;
        movement = _move;

        playerLayer = gameObject.layer;
        // LayerMask 비트 연산으로 Index 찾기 (안전한 방식)
        int layerVal = groundLayer.value;
        int index = 0;
        while(layerVal > 1) { layerVal >>= 1; index++; }
        groundLayerIndex = index;
    }

    public void SetGravityScale(float scale)
    {
        currentGravityScale = scale;
        if (CurrentRope != null) CurrentRope.SetGravityScale(scale);
    }

    // [수정] 외부(Controller)에서 호출하는 방식으로 변경 (HandleInput 삭제)
    public void TryFireAnchor()
    {
        if (HasAnchor) return; // 이미 있으면 발사 안 함 (F는 설치만)
        FireAnchor();
    }

    public void TryReleaseAnchor()
    {
        if (HasAnchor) ReleaseAnchor();
    }

    public void ApplyPhysics(bool isRetractHeld, Vector2 inputDir)
    {
        ManageGhostMode(isRetractHeld);

        if (!HasAnchor) 
        {
            IsTaut = false;
            return;
        }

        float dist = Vector2.Distance(transform.position, anchorPos);
        IsTaut = dist >= currentMaxLen - 0.2f;

        if (!movement.IsGrounded && IsTaut && inputDir.x != 0)
        {
            rb.AddForce(new Vector2(inputDir.x * swingAcceleration, 0), ForceMode2D.Force);
        }

        ApplyRetraction(isRetractHeld);
        ApplyDistanceConstraint();
        
        if (CurrentRope != null) CurrentRope.UpdateEndPosition(transform.position);
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
            CurrentRope.InitializeRope(anchorPos, transform, maxRopeLength, currentGravityScale);
            HasAnchor = true;
        }
    }

    private void ReleaseAnchor()
    {
        // [수정] 무중력 상태(중력 절대값이 0.1 미만)가 아닐 때만 반동을 적용합니다.
        // 무중력일 때는 관성 그대로 날아가야 하므로 이 로직을 건너뜁니다.
        if (!movement.IsGrounded && Mathf.Abs(currentGravityScale) > 0.1f)
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
        if (isHeld && Time.time >= nextRetractTime)
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
            if (pullTimer > 0f) nextRetractTime = Time.time + retractionCooldown;
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
            if (velDot < 0) rb.linearVelocity += tetherDir * (-velDot);
        }
    }

    private void ManageGhostMode(bool isRetractHeld)
    {
        if (movement.IsGrounded)
        {
            if (isGhostMode) SetGhostMode(false);
            return;
        }

        bool isInsideWall = Physics2D.OverlapBox(boxCol.bounds.center, boxCol.bounds.size * 0.7f, 0f, groundLayer);
        bool shouldBeGhost = false;

        if (isRetractHeld) shouldBeGhost = true;
        else if (isInsideWall) shouldBeGhost = true;

        if (isGhostMode != shouldBeGhost) SetGhostMode(shouldBeGhost);
    }

    private void SetGhostMode(bool active)
    {
        isGhostMode = active;
        Physics2D.IgnoreLayerCollision(playerLayer, groundLayerIndex, active);
    }
}