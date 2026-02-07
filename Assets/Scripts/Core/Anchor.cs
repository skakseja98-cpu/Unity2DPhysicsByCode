using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
public class Anchor : MonoBehaviour
{
    [Header("Visual Feedback")]
    public Color highlightColor = Color.blue; // 선택됐을 때 색상
    private Color defaultColor;
    
    [Header("Attachment Settings")]
    [Tooltip("앵커 중심점으로부터 줄이 실제로 연결될 위치의 오프셋(상대 좌표)입니다.")]
    public Vector2 ropeOffset = Vector2.zero; // [신규 기능] 오프셋 설정

    private SpriteRenderer sr;

    // 외부에서 실제 월드 좌표 상의 연결 지점을 가져오는 프로퍼티
    public Vector2 AttachPoint => (Vector2)transform.position + ropeOffset;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        defaultColor = sr.color;
        
        // 앵커는 트리거로 설정하여 물리 충돌 방지
        GetComponent<CircleCollider2D>().isTrigger = true;
    }

    public void Select()
    {
        if (sr != null) sr.color = highlightColor;
    }

    public void Deselect()
    {
        if (sr != null) sr.color = defaultColor;
    }

    // 에디터에서 앵커를 선택했을 때 연결 지점을 시각적으로 보여줌
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 finalPos = AttachPoint;
        // 연결 지점에 작은 빨간 공 표시
        Gizmos.DrawSphere(finalPos, 0.15f);
        // 중심점에서 연결 지점까지 선 표시
        Gizmos.DrawLine(transform.position, finalPos);
    }
}