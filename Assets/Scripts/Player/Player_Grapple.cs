using UnityEngine;

public class Player_Grapple : MonoBehaviour
{
    [Header("Grapple Settings")]
    public GameObject ropePrefab;
    
    [Header("Anchor Detection")]
    public LayerMask anchorLayer;      
    public float detectionRadius = 10f; // 앵커 감지 범위
    
    // [삭제됨] public float maxRopeLength = 15f; -> 이제 Anchor의 설정을 따릅니다.
    
    [Header("Swinging Physics")]
    public float swingAcceleration = 40f;

    [Header("Retraction")]
    public float pullInitSpeed = 5f;
    public float pullMaxSpeed = 25f;
    public float pullAccelDuration = 1.0f;
    public float retractionCooldown = 0.5f;
    
    [Header("Release Boost")]
    public float releaseVelocityMult = 1.2f;
    public float releaseUpwardForce = 5f;

    // 상태 프로퍼티
    public bool HasAnchor { get; private set; }
    public Rope CurrentRope { get; private set; }
    public bool IsTaut { get; private set; }
    
    // 내부 변수
    private Rigidbody2D rb;
    private BoxCollider2D boxCol;
    private Player_Movement movement;

    private Vector2 anchorPos;
    private float currentMaxLen;
    private float pullTimer;

    private float currentGravityScale = 1f;
    private float nextRetractTime = 0f;

    private Anchor currentTargetAnchor; 
    
    private bool isGhostMode;
    private int playerLayer;
    private int groundLayerIndex;

    public void Initialize(Rigidbody2D _rb, BoxCollider2D _col, Player_Movement _move)
    {
        rb = _rb;
        boxCol = _col;
        movement = _move;

        playerLayer = gameObject.layer;
        
        int layerVal = movement.groundLayer.value;
        int index = 0;
        while(layerVal > 1) { layerVal >>= 1; index++; }
        groundLayerIndex = index;
    }

    public void SetGravityScale(float scale)
    {
        currentGravityScale = scale;
        if (CurrentRope != null) CurrentRope.SetGravityScale(scale);
    }

    void Update()
    {
        if (!HasAnchor)
        {
            FindClosestAnchor();
        }
        else
        {
            if (currentTargetAnchor != null)
            {
                currentTargetAnchor.Deselect();
                currentTargetAnchor = null;
            }
        }
    }

    public void TryFireAnchor()
    {
        if (HasAnchor) return; 

        if (currentTargetAnchor != null)
        {
            ConnectToAnchor(currentTargetAnchor);
        }
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

    private void ManageGhostMode(bool isRetractHeld)
    {
        if (isRetractHeld)
        {
            if (!isGhostMode) SetGhostMode(true);
            return;
        }

        if (isGhostMode) 
        {
            SetGhostMode(false);
        }
    }

    private void SetGhostMode(bool active)
    {
        isGhostMode = active;
        Physics2D.IgnoreLayerCollision(playerLayer, groundLayerIndex, active);
    }

    private void FindClosestAnchor()
    {
        Collider2D[] anchors = Physics2D.OverlapCircleAll(transform.position, detectionRadius, anchorLayer);
        
        float minDst = float.MaxValue;
        Anchor newClosest = null;

        foreach (var col in anchors)
        {
            float dst = Vector2.Distance(transform.position, col.transform.position);
            
            if (dst < minDst)
            {
                Vector2 dir = (col.transform.position - transform.position).normalized;
                float distToAnchor = Vector2.Distance(transform.position, col.transform.position);
                
                if (!Physics2D.Raycast(transform.position, dir, distToAnchor, movement.groundLayer))
                {
                    Anchor anchorScript = col.GetComponent<Anchor>();
                    if (anchorScript != null)
                    {
                        minDst = dst;
                        newClosest = anchorScript;
                    }
                }
            }
        }

        if (newClosest != currentTargetAnchor)
        {
            if (currentTargetAnchor != null) currentTargetAnchor.Deselect();
            if (newClosest != null) newClosest.Select();
            
            currentTargetAnchor = newClosest;
        }
    }

    private void ConnectToAnchor(Anchor target)
    {
        anchorPos = target.AttachPoint;

        GameObject ropeObj = Instantiate(ropePrefab, anchorPos, Quaternion.identity);
        CurrentRope = ropeObj.GetComponent<Rope>();
        
        // [수정] 앵커의 개별 길이 설정을 가져옵니다.
        currentMaxLen = target.ropeLength; 

        CurrentRope.InitializeRope(anchorPos, transform, currentMaxLen, currentGravityScale);
        HasAnchor = true;
    }

    private void ReleaseAnchor()
    {
        if (!movement.IsGrounded && Mathf.Abs(currentGravityScale) > 0.1f)
        {
            Vector2 boostVel = rb.linearVelocity * releaseVelocityMult;
            boostVel += Vector2.up * releaseUpwardForce;
            rb.linearVelocity = boostVel;
        }

        if (CurrentRope != null) Destroy(CurrentRope.gameObject);
        CurrentRope = null;
        HasAnchor = false;
        
        FindClosestAnchor();
    }

    private void ApplyRetraction(bool isHeld)
    {
        if (isHeld && Time.time >= nextRetractTime)
        {
            Vector2 toAnchor = anchorPos - rb.position;
            
            if (toAnchor.magnitude > 0.3f)
            {
                pullTimer += Time.fixedDeltaTime;
                float t = Mathf.Clamp01(pullTimer / pullAccelDuration);
                float speed = Mathf.Lerp(pullInitSpeed, pullMaxSpeed, t);
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, toAnchor.normalized * speed, 0.1f);
                
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
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}