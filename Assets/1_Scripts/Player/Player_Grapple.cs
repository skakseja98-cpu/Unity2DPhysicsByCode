using UnityEngine;

public class Player_Grapple : MonoBehaviour
{
    [Header("Grapple Settings")]
    public GameObject ropePrefab;
    
    [Header("Anchor Detection")]
    public LayerMask anchorLayer;      
    public float detectionRadius = 10f; 
    
    [Header("Swinging Physics")]
    public float swingAcceleration = 40f;

    [Header("Auto Retraction")] // [수정] 쿨타임 변수 삭제 및 자동 해제 거리 추가
    public float pullInitSpeed = 5f;
    public float pullMaxSpeed = 25f;
    public float pullAccelDuration = 1.0f;
    [Tooltip("앵커와 이 거리만큼 가까워지면 자동으로 줄이 풀립니다.")]
    public float autoReleaseDistance = 1.5f;
    
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

    private Anchor currentTargetAnchor; 
    
    private bool isGhostMode;
    private int playerLayer;
    private int groundLayerIndex;
    private Anchor connectedAnchor;

    // [신규] 자동 당기기 상태 변수
    private bool isAutoRetracting = false; 

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

    // [신규] G키를 눌렀을 때 외부에서 호출
    public void StartAutoRetract()
    {
        if (HasAnchor && !isAutoRetracting)
        {
            isAutoRetracting = true;
            pullTimer = 0f;
        }
    }

    // [수정] bool 매개변수 제거
    public void ApplyPhysics(Vector2 inputDir)
    {
        // 당기는 중일 때 고스트 모드 유지
        ManageGhostMode(isAutoRetracting);

        if (!HasAnchor) 
        {
            IsTaut = false;
            return;
        }

        if (connectedAnchor != null)
        {
            anchorPos = connectedAnchor.AttachPoint; 
            if (CurrentRope != null) CurrentRope.UpdateStartPos(anchorPos);
        }

        float dist = Vector2.Distance(transform.position, anchorPos);
        
        IsTaut = dist >= currentMaxLen - 0.2f;

        if (!movement.IsGrounded && IsTaut && inputDir.x != 0)
        {
            rb.AddForce(new Vector2(inputDir.x * swingAcceleration, 0), ForceMode2D.Force);
        }

        ApplyRetraction(); // [수정]
        ApplyDistanceConstraint();
        
        if (CurrentRope != null) CurrentRope.UpdateEndPosition(transform.position);
    }

    private void ManageGhostMode(bool isRetracting)
    {
        if (isRetracting)
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
        connectedAnchor = target; 
        anchorPos = target.AttachPoint;

        if(connectedAnchor != null) connectedAnchor.SetConnected(true);

        GameObject ropeObj = Instantiate(ropePrefab, anchorPos, Quaternion.identity);
        CurrentRope = ropeObj.GetComponent<Rope>();
        
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
        
        if (connectedAnchor != null) connectedAnchor.SetConnected(false); 
        connectedAnchor = null; 

        // [추가] 당기기 상태 강제 초기화
        isAutoRetracting = false;
        pullTimer = 0f;
        
        FindClosestAnchor();
    }

    // [수정] 목표 거리에 도달하면 자동으로 줄 해제
    private void ApplyRetraction()
    {
        if (isAutoRetracting)
        {
            Vector2 toAnchor = anchorPos - rb.position;
            float dist = toAnchor.magnitude;

            // 해제 거리에 도달하면 자동 해제 후 함수 종료
            if (dist <= autoReleaseDistance)
            {
                TryReleaseAnchor();
                return;
            }

            pullTimer += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(pullTimer / pullAccelDuration);
            float speed = Mathf.Lerp(pullInitSpeed, pullMaxSpeed, t);
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, toAnchor.normalized * speed, 0.1f);
            
            currentMaxLen = dist;
            if (CurrentRope != null) CurrentRope.UpdateRopeLength(currentMaxLen);
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