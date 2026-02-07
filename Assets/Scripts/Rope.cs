using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Rope2D : MonoBehaviour
{
    [Header("Visual Settings")]
    public int segmentCountPerMeter = 3; // 1미터당 마디 개수 (높을수록 부드러움)
    public float gravity = -30f;
    public int constraintIterations = 10;
    
    private LineRenderer lineRenderer;
    private List<RopeNode> nodes = new List<RopeNode>();
    private Transform targetTransform;
    private Vector2 startPos;
    private float segmentLength;

    public class RopeNode
    {
        public Vector2 position;
        public Vector2 oldPosition;
        public RopeNode(Vector2 pos) { position = pos; oldPosition = pos; }
    }

    public void InitializeRope(Vector2 anchor, Transform player, float maxLen)
    {
        lineRenderer = GetComponent<LineRenderer>();
        startPos = anchor;
        targetTransform = player;

        nodes.Clear();
        
        // 초기 생성 시 넉넉하게 마디를 생성 (maxLen 기준)
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

    // [핵심] 플레이어 컨트롤러에서 호출하여 비주얼 로프 길이를 강제로 줄임
    public void UpdateRopeLength(float newTotalLength)
    {
        if (nodes.Count < 2) return;
        
        // 마디 개수는 유지하되, 마디 간격(segmentLength)을 줄여서 전체 길이를 맞춤
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
        Vector2 gravityVec = new Vector2(0, gravity * dt * dt);

        for (int i = 0; i < nodes.Count; i++)
        {
            if (i == 0 || i == nodes.Count - 1) continue;

            RopeNode node = nodes[i];
            Vector2 velocity = node.position - node.oldPosition;
            node.oldPosition = node.position;
            
            // 공기 저항 0.99f
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

            // 줄이 segmentLength보다 길어질 때만 줄임 (Slack 유지)
            // G키를 눌러 segmentLength가 작아지면, 여기서 자동으로 딸려 올라감
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