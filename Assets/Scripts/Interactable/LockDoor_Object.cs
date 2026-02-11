using UnityEngine;

public class LockDoor_Object : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    public string requiredKeyName = "Key_Card_Lv1"; // 필요한 아이템 이름
    public Sprite openSprite; // 문 열린 이미지
    private bool isOpen = false;

    [Header("Feedback")]
    public string lockedMessage = "문이 잠겨있다. [보안 카드]가 필요해 보인다.";
    public string openMessage = "문이 열렸다.";
    
    // 목소리 스타일 (문이 덜컹거리는 소리 등을 넣을 수도 있음)
    public DialogueStyle interactionStyle; 

    private SpriteRenderer sr;
    private BoxCollider2D col;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<BoxCollider2D>();
    }

    public void OnFocus()
    {
        if (!isOpen) sr.color = Color.red; // 잠겨있으면 빨간색
    }

    public void OnDefocus()
    {
        sr.color = Color.white;
    }

    public void OnInteract()
    {
        if (isOpen) return;

        // 1. 플레이어가 들고 있는 아이템 가져오기
        Pickupable_Object item = Player_Interaction.Instance.GetHeldItem();

        // 2. 아이템이 있고 + 이름이 맞는지 확인
        if (item != null && item.name == requiredKeyName)
        {
            UnlockDoor();
        }
        else
        {
            // 잠김 메시지 출력
            string[] lines = { lockedMessage };
            InteractionUIManager.Instance.StartDialog(transform.position, lines, interactionStyle);
        }
    }

    void UnlockDoor()
    {
        isOpen = true;
        
        // 열쇠 소모 (삭제)
        Player_Interaction.Instance.DestroyHeldItem();

        // 문 열림 처리 (이미지 변경 + 콜라이더 끄기 등)
        if (openSprite != null) sr.sprite = openSprite;
        if (col != null) col.enabled = false; // 지나갈 수 있게

        // 성공 메시지
        string[] lines = { openMessage };
        InteractionUIManager.Instance.StartDialog(transform.position, lines, interactionStyle);
    }
}