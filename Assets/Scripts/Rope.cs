using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Rope : MonoBehaviour
{
    [Header("Visual Settings")]
    public int segmentCountPerMeter = 3; 
    public float baseGravity = -30f; // [변경] 원래 gravity였던 것을 baseGravity로 이름 변경
    public int constraintIterations = 10;
    
    private LineRenderer lineRenderer;
    private List<RopeNode> nodes = new List<RopeNode>();
    private Transform targetTransform;
    private Vector2 startPos;
    private float segmentLength;
    
    // [추가] 현재 적용 중인 중력값
    private float currentGravity; 

    public class RopeNode
    {
        public Vector2 position;
        public Vector2 oldPosition;
        public RopeNode(Vector2 pos) { position = pos; oldPosition = pos; }
    }

    // [수정] 파라미터에 float gravityScale 추가됨
    public void InitializeRope(Vector2 anchor, Transform player, float maxLen, float gravityScale)
    {
        lineRenderer = GetComponent<LineRenderer>();
        startPos = anchor;
        targetTransform = player;
        
        // [추가] 초기 중력 설정
        currentGravity = baseGravity * gravityScale;

        nodes.Clear();
        
        int totalSegments = Mathf.CeilToInt(maxLen * segmentCountPerMeter);
        if (totalSegments < 2) totalSegments = 2;

        segmentLength = maxLen / (totalSegments - 1);

        for (int i = 0; i < totalSegments; i++)
        {
            Vector2 pos = Vector2.Lerp(startPos, targetTransform.position, (float)i / (totalSegments - 1));
            nodes.Add(new RopeNode(pos));
        }
        
        lineRenderer.positionCount = totalSegments;
    }

    // [추가] 게임 도중(우주 진입 등) 중력이 바뀔 때 호출
    public void SetGravityScale(float scale)
    {
        currentGravity = baseGravity * scale;
    }

    public void UpdateRopeLength(float newTotalLength)
    {
        if (nodes.Count < 2) return;
        segmentLength = newTotalLength / (nodes.Count - 1);
    }

    public void UpdateEndPosition(Vector2 playerPos)
    {
        if(nodes.Count > 0)
        {
            nodes[nodes.Count - 1].position = playerPos;
        }
    }

    void FixedUpdate()
    {
        if (nodes.Count == 0 || targetTransform == null) return;

        SimulateNodes(Time.fixedDeltaTime);

        for (int i = 0; i < constraintIterations; i++)
        {
            ApplyConstraints();
        }

        DrawRope();
    }

    private void SimulateNodes(float dt)
    {
        // [수정] gravity 대신 currentGravity 사용
        Vector2 gravityVec = new Vector2(0, currentGravity * dt * dt);

        for (int i = 0; i < nodes.Count; i++)
        {
            if (i == 0 || i == nodes.Count - 1) continue;

            RopeNode node = nodes[i];
            Vector2 velocity = node.position - node.oldPosition;
            node.oldPosition = node.position;
            
            node.position += velocity * 0.99f + gravityVec;
        }
    }

    private void ApplyConstraints()
    {
        nodes[0].position = startPos;
        if (targetTransform != null) nodes[nodes.Count - 1].position = targetTransform.position;

        for (int i = 0; i < nodes.Count - 1; i++)
        {
            RopeNode nodeA = nodes[i];
            RopeNode nodeB = nodes[i + 1];

            Vector2 distVec = nodeB.position - nodeA.position;
            float dist = distVec.magnitude;

            if (dist > segmentLength)
            {
                float error = dist - segmentLength;
                Vector2 dir = distVec.normalized;
                Vector2 correction = dir * error * 0.5f;
                
                if (i != 0) nodeA.position += correction;
                if (i + 1 != nodes.Count - 1) nodeB.position -= correction;
            }
        }
    }

    private void DrawRope()
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            lineRenderer.SetPosition(i, nodes[i].position);
        }
    }
}