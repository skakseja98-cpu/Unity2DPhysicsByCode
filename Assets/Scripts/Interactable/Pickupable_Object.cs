using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class Pickupable_Object : MonoBehaviour, IInteractable
{
    private Rigidbody2D rb;
    private Collider2D col;
    private Transform originalParent;

    [Header("Settings")]
    public bool isHeavy = false; // 무거운 물건은 들었을 때 이동 속도 저하 등을 구현할 때 사용

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    public void OnFocus()
    {
        // 아웃라인 쉐이더나 색상 변경 로직 (기존 NPC_Object 참고)
        GetComponent<SpriteRenderer>().color = Color.yellow; 
    }

    public void OnDefocus()
    {
        GetComponent<SpriteRenderer>().color = Color.white;
    }

    public void OnInteract()
    {
        // 상호작용(F) 키를 누르면 플레이어에게 "나를 주워줘"라고 요청
        Player_Interaction.Instance.PickUpItem(this);
    }

    // 플레이어가 물건을 들었을 때 호출
    public void OnPickedUp(Transform holder)
    {
        rb.simulated = false; // 물리 연산 끄기 (안 그러면 플레이어랑 충돌해서 날아감)
        col.enabled = false;  // 충돌체 끄기
        
        transform.SetParent(holder); // 플레이어 손의 자식으로 설정
        transform.localPosition = Vector3.zero; // 위치 초기화
        transform.localRotation = Quaternion.identity;
    }

    // 플레이어가 물건을 던지거나 놓았을 때 호출
    public void OnDropped(Vector2 velocity)
    {
        transform.SetParent(null); // 부모 해제
        
        rb.simulated = true;
        col.enabled = true;

        // 플레이어의 이동 속도 + 던지는 힘을 더해줌 (자연스러운 투척)
        rb.linearVelocity = velocity; 
    }
}