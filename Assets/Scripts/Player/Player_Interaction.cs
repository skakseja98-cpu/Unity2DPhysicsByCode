using UnityEngine;

public class Player_Interaction : MonoBehaviour
{
    public static Player_Interaction Instance;
    public float detectRange = 2f;
    public LayerMask interactLayer;

    private IInteractable currentInteractable;

    void Awake()
    {
        if (Instance == null) 
            Instance = this;
        else 
            Destroy(gameObject);
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

        // 3. 감지된 대상(NPC)이 있으면 대화 시작
        if (currentInteractable != null)
        {
            currentInteractable.OnInteract();
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