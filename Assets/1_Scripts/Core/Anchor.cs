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
    public Vector2 ropeOffset = Vector2.zero;

    [Header("Rope Settings")]
    [Tooltip("이 앵커에 연결될 때 적용할 줄의 길이입니다.")]
    public float ropeLength = 10f; // [신규 기능] 완전 개별화된 줄 길이

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

    void OnDrawGizmosSelected()
    {
        // 연결 지점 표시
        Gizmos.color = Color.red;
        Vector3 finalPos = AttachPoint;
        Gizmos.DrawSphere(finalPos, 0.15f);
        Gizmos.DrawLine(transform.position, finalPos);

        // (시각화 요청 없음: 줄 길이 원 그리기 제거됨)
    }
}