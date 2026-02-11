using UnityEngine;

public class Player_Interaction : MonoBehaviour
{
    public static Player_Interaction Instance;
    public float detectRange = 2f;
    public LayerMask interactLayer;


    [Header("Hold System")]
    public Transform holdPoint; // 1단계에서 만든 HoldPoint 연결
    public float throwForce = 5f; // 던지는 힘

    private Pickupable_Object currentHeldItem;

    private IInteractable currentInteractable;

    void Awake()
    {
        // [핵심 해결] 이 코드가 빠져있으면 에러가 납니다!
        // "게임이 시작되면 내가(this) 바로 그 Instance다"라고 선언하는 것입니다.
        if (Instance == null) 
            Instance = this;
        else 
            Destroy(gameObject);

        // ... (기존 초기화 코드들 유지)
        if(holdPoint == null) Debug.LogWarning("HoldPoint가 연결되지 않았습니다!");
    }


    void Update()
    {
        DetectObject();
        CheckDistanceForExit();
    }

    public void HandleNpcInteraction()
    {
        // 1. 대화창 넘기기 (최우선)
        if (InteractionUIManager.Instance.IsDialogOpen())
        {
            InteractionUIManager.Instance.AdvanceDialog();
            return; 
        }
        
        // 2. 서류 닫기
        if (InteractionUIManager.Instance.IsDocumentOpen())
        {
            InteractionUIManager.Instance.CloseDocument();
            return;
        }

        // 3. 감지된 대상이 있고 + "아이템이 아닐 때" (NPC, 문 등)
        if (currentInteractable != null)
        {
            // 현재 감지된 게 '줍는 아이템'이면 E키로는 반응 안 함 (무시)
            if (currentInteractable is Pickupable_Object) return;

            // NPC나 문(Door)이라면 상호작용 실행
            currentInteractable.OnInteract();
        }
    }

    public void HandleItemAction()
    {
        // 1. 이미 들고 있으면 -> 떨구기
        if (currentHeldItem != null)
        {
            DropItem();
            return;
        }

        // 2. 안 들고 있고, 바닥에 아이템이 감지됐다면 -> 줍기
        if (currentInteractable != null && currentInteractable is Pickupable_Object)
        {
            currentInteractable.OnInteract(); // 줍기 실행
        }
    }

    public void PickUpItem(Pickupable_Object item)
    {
        if (currentHeldItem != null) DropItem(); 

        currentHeldItem = item;
        currentHeldItem.OnPickedUp(holdPoint);
        
        if (currentInteractable == (IInteractable)item)
        {
            currentInteractable = null;
        }
    }

    public void DropItem()
    {
        if (currentHeldItem == null) return;

        // [수정 요청 반영] 던지지 않고 제자리에 툭 떨구기
        // 속도를 0으로 만들어서 바로 아래로 떨어지게 함 (자연스럽게 중력 받음)
        currentHeldItem.OnDropped(Vector2.zero); 
        
        currentHeldItem = null;
    }

    public Pickupable_Object GetHeldItem() => currentHeldItem;
    
    public void DestroyHeldItem()
    {
        if (currentHeldItem != null)
        {
            Destroy(currentHeldItem.gameObject);
            currentHeldItem = null;
        }
    }

    void DetectObject()
    {
        // 대화 중일 때는 새로운 감지를 잠시 멈출지, 계속할지 결정.
        // 여기선 이동 가능하므로 계속 감지하되, Focus 효과만 갱신.
        Collider2D hit = Physics2D.OverlapCircle(transform.position, detectRange, interactLayer);

        if (hit != null)
        {
            IInteractable detected = hit.GetComponent<IInteractable>();
            if (detected != null)
            {
                if (currentInteractable != detected)
                {
                    if (currentInteractable != null) currentInteractable.OnDefocus();
                    currentInteractable = detected;
                    currentInteractable.OnFocus();
                }
                return;
            }
        }

        if (currentInteractable != null)
        {
            currentInteractable.OnDefocus();
            currentInteractable = null;
        }
    }
    
    // 멀어지면 대화 끄는 로직
    void CheckDistanceForExit()
    {
        // 현재 잡고 있는 대상이 있고, 그게 NPC라면
        if (currentInteractable != null && currentInteractable is NpcObject)
        {
            NpcObject npc = (NpcObject)currentInteractable;
            
            // 플레이어와 NPC 사이의 거리 계산
            float dist = Vector2.Distance(transform.position, npc.transform.position);
            
            // 설정된 종료 거리보다 멀어지면
            if (dist > npc.GetExitDistance())
            {
                // 강제로 Focus 해제 (이러면 OnDefocus에서 대화창도 닫힘)
                npc.OnDefocus();
                currentInteractable = null;
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}