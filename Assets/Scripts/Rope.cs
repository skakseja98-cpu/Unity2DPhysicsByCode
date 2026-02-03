using System.Collections.Generic;
using UnityEngine;

public class VerletRope2D : MonoBehaviour
{
    // ... (기존 설정 변수들은 동일함) ...
    [Header("로프 설정")]
    public Transform startPoint; 
    public int segmentCount = 15; 
    public float ropeLength = 5.0f; 
    public float gravity = -9.81f; 
    
    [Header("시뮬레이션 정밀도")]
    [Range(1, 50)]
    public int constraintIterations = 20; 
    // [신규] 로프의 빳빳함을 조절하는 변수 추가
    [Range(0f, 2f)]
    public float bendingStiffness = 1.0f; // 0 = 부드러움(잘 접힘), 2 = 강철 막대기처럼 빳빳함

    // [신규] 충돌 설정
    [Header("충돌 설정")]
    public LayerMask groundMask; // 인스펙터에서 'Ground' 레이어를 선택하세요.
    public float ropeRadius = 0.1f; // 로프의 두께 (지형에 파고드는 깊이 방지용)
    public float friction = 0.5f;   // 바닥에 닿았을 때 미끄러짐 방지용 마찰력 (0~1)

    [Header("물리 저항 설정")]
    [Range(0.8f, 1.0f)]
    public float damping = 0.95f; // 공기 저항 (1 = 저항 없음, 0.8 = 매우 무거움)

    

    private LineRenderer lineRenderer;
    private EdgeCollider2D edgeCollider; // 콜라이더 변수 추가
    private List<RopeNode> nodes = new List<RopeNode>();
    private float segmentLength;

    public class RopeNode
    {
        public Vector2 position;
        public Vector2 oldPosition;
        public bool isLocked;

        public RopeNode(Vector2 pos)
        {
            position = pos;
            oldPosition = pos;
            isLocked = false;
        }
    }

    

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        edgeCollider = GetComponent<EdgeCollider2D>(); // 초기화
        edgeCollider.isTrigger = true; // 플레이어가 통과하며 감지해야 하므로 Trigger 체크

        lineRenderer.positionCount = segmentCount;
        segmentLength = ropeLength / (segmentCount - 1);

        Vector2 startPos = startPoint != null ? (Vector2)startPoint.position : (Vector2)transform.position;
        for (int i = 0; i < segmentCount; i++)
        {
            nodes.Add(new RopeNode(startPos + Vector2.down * (segmentLength * i)));
        }

        nodes[0].isLocked = true;
    }

    void FixedUpdate()
    {
        // 1. 기본 물리 이동
        SimulatePhysics(Time.fixedDeltaTime);
        
        // 2. 제약 조건 (길이 맞추기)을 여러 번 반복
        for (int i = 0; i < constraintIterations; i++) 
        {
            ApplyConstraints();
            
            // [핵심 해결책] 길이가 맞춰진 후, 벽에 파고들었다면 즉시 밖으로 밀어냅니다.
            ResolveCollisions(); 
        }

        UpdateRopeGraphics();
        UpdateRopeCollider(); 
    }

    private void ResolveCollisions()
    {
        foreach (var node in nodes)
        {
            if (node.isLocked) continue;

            // 노드 위치에 겹쳐있는 콜라이더가 있는지 검사
            Collider2D hit = Physics2D.OverlapCircle(node.position, ropeRadius, groundMask);

            if (hit != null)
            {
                // 파고든 콜라이더에서 가장 가까운 바깥쪽 표면 점을 찾음
                Vector2 closestPoint = hit.ClosestPoint(node.position);

                // 노드를 콜라이더 바깥쪽 표면으로 강제 이동시킴
                // (표면에서 ropeRadius만큼 조금 더 띄워주어 확실히 빠져나오게 함)
                Vector2 directionOut = (node.position - closestPoint).normalized;
                
                // 만약 노드가 콜라이더의 정중앙에 있어서 방향을 못 잡을 경우를 위한 예외 처리
                if (directionOut == Vector2.zero) directionOut = Vector2.up;

                node.position = closestPoint + directionOut * ropeRadius;
            }
        }
    }

    // ... (SimulatePhysics와 ApplyConstraints 함수는 기존과 동일) ...

    // 1. 점의 이동 + [신규] 바닥 충돌 처리
    private void SimulatePhysics(float dt)
    {
        Vector2 gravityVector = new Vector2(0f, gravity);

        foreach (var node in nodes)
        {
            if (node.isLocked) continue;

            // 1) 다음 프레임에 이동할 위치를 미리 계산
            Vector2 velocity = node.position - node.oldPosition;
            Vector2 nextPosition = node.position + (velocity * damping)+ gravityVector * (dt * dt);

            // 2) 현재 위치에서 다음 위치로 가는 길목에 'Ground'가 있는지 검사 (CircleCast)
            Vector2 direction = nextPosition - node.position;
            float distance = direction.magnitude;

            // 로프 두께(ropeRadius)만큼의 원을 쏴서 충돌 여부 확인
            RaycastHit2D hit = Physics2D.CircleCast(node.position, ropeRadius, direction.normalized, distance, groundMask);

            // 3) 만약 바닥에 닿았다면
            if (hit.collider != null)
            {
                // 위치를 바닥 표면(충돌점 + 법선벡터 * 두께)으로 강제 고정
                nextPosition = hit.point + hit.normal * ropeRadius;

                // 속도를 줄여서 바닥에서 통통 튀는 것과 미끄러지는 것을 방지 (마찰력 적용)
                node.oldPosition = Vector2.Lerp(nextPosition, node.position, friction); 
            }
            else
            {
                // 충돌하지 않았다면 정상적으로 현재 위치를 과거 위치로 저장
                node.oldPosition = node.position;
            }

            // 4) 최종 위치 적용
            node.position = nextPosition;
        }
    }

    private void ApplyConstraints()
    {
        if (startPoint != null) nodes[0].position = startPoint.position;

        for (int i = 0; i < segmentCount - 1; i++)
        {
            RopeNode nodeA = nodes[i];
            RopeNode nodeB = nodes[i + 1];

            Vector2 direction = nodeB.position - nodeA.position;
            float currentDistance = direction.magnitude;
            float error = currentDistance - segmentLength;

            if (currentDistance > 0.0001f)
            {
                Vector2 correction = (direction / currentDistance) * error * 0.5f;
                if (!nodeA.isLocked) nodeA.position += correction;
                if (!nodeB.isLocked) nodeB.position -= correction;
            }
        }


        if (bendingStiffness > 0f)
        {
            for (int i = 0; i < segmentCount - 2; i++) 
            {
                RopeNode nodeA = nodes[i];
                RopeNode nodeB = nodes[i + 2]; 

                Vector2 direction = nodeB.position - nodeA.position;
                float currentDistance = direction.magnitude;
                
                // bendingStiffness 값에 따라 최소 유지 거리가 동적으로 변함
                float minBendDistance = segmentLength * bendingStiffness; 

                if (currentDistance < minBendDistance && currentDistance > 0.0001f)
                {
                    float error = currentDistance - minBendDistance;
                    Vector2 correction = (direction / currentDistance) * error * 0.5f;

                    if (!nodeA.isLocked) nodeA.position += correction;
                    if (!nodeB.isLocked) nodeB.position -= correction;
                }
            }
        }
    }

    private void UpdateRopeGraphics()
    {
        for (int i = 0; i < segmentCount; i++)
        {
            lineRenderer.SetPosition(i, nodes[i].position);
        }
    }

    // [신규] 로프 점의 위치를 콜라이더의 점 위치와 동기화
    private void UpdateRopeCollider()
    {
        List<Vector2> colliderPoints = new List<Vector2>();

        for (int i = 0; i < segmentCount; i++)
        {
            // EdgeCollider2D는 로컬 좌표계를 사용하므로 월드 좌표를 로컬로 변환
            Vector2 localPos = transform.InverseTransformPoint(nodes[i].position);
            colliderPoints.Add(localPos);
        }

        edgeCollider.SetPoints(colliderPoints);
    }
    
    // 플레이어가 로프의 특정 위치(월드 좌표)와 가장 가까운 점을 찾을 때 사용하는 함수
    public Vector2 GetClosestPointOnRope(Vector2 playerPos)
    {
        Vector2 closestPoint = nodes[0].position;
        float minDistance = Vector2.Distance(playerPos, closestPoint);

        foreach (var node in nodes)
        {
            float dist = Vector2.Distance(playerPos, node.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestPoint = node.position;
            }
        }
        return closestPoint;
    }
}