using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
public class Anchor : MonoBehaviour
{
    [Header("Visual Feedback")]
    [Tooltip("가까이 갔을 때 교체될 외곽선 하이라이트 이미지입니다.")]
    public Sprite highlightSprite; 
    private Sprite defaultSprite;  
    
    [Tooltip("앵커 자식으로 있는 파티클 시스템 객체입니다. 앵커에 줄이 연결되면 꺼집니다.")]
    public GameObject highlightParticle; 
    
    [Header("Attachment Settings")]
    [Tooltip("앵커의 '축(연결 지점)'으로 사용할 Transform입니다. 비어있으면 아래 오프셋을 사용합니다.")]
    public Transform anchorAxis; 

    [Tooltip("앵커 중심점으로부터 줄이 실제로 연결될 위치의 오프셋(상대 좌표)입니다.")]
    public Vector2 ropeOffset = Vector2.zero;

    [Header("Rope Settings")]
    [Tooltip("이 앵커에 연결될 때 적용할 줄의 길이입니다.")]
    public float ropeLength = 10f; 

    private SpriteRenderer sr;

    public Vector2 AttachPoint
    {
        get
        {
            if (anchorAxis != null) return anchorAxis.position;
            return (Vector2)transform.position + ropeOffset;
        }
    }

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) defaultSprite = sr.sprite; // 시작할 때 원본 이미지 저장
        
        GetComponent<CircleCollider2D>().isTrigger = true;
    }

    public void Select()
    {
        // 범위에 들어오면 하이라이트 이미지로 교체
        if (sr != null && highlightSprite != null) 
        {
            sr.sprite = highlightSprite;
        }
    }

    public void Deselect()
    {
        // 범위를 벗어나거나 연결되었을 때 원본 이미지로 복구
        if (sr != null && defaultSprite != null) 
        {
            sr.sprite = defaultSprite;
        }
    }

    // [신규] 줄 연결 상태에 따라 파티클을 켜고 끄는 함수
    public void SetConnected(bool isConnected)
    {
        if (highlightParticle != null)
        {
            highlightParticle.SetActive(!isConnected);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 finalPos = AttachPoint;
        Gizmos.DrawSphere(finalPos, 0.15f);
        Gizmos.DrawLine(transform.position, finalPos);
    }
}