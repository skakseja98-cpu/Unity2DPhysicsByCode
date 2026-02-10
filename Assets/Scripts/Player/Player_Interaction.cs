using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public float detectRange = 2f;
    public LayerMask interactLayer;

    private IInteractable currentInteractable;

    void Update()
    {
        // 1. 플레이어 주변 감지 (기존 로직)
        DetectObject();

        // 2. F키 입력 처리 (수정됨)
        if (Input.GetKeyDown(KeyCode.F))
        {
            // 만약 대화창이 이미 열려있다면? -> 대화 넘기기/스킵 (NPC 상호작용 아님)
            if (InteractionUIManager.Instance.IsDialogOpen())
            {
                InteractionUIManager.Instance.AdvanceDialog();
            }
            // 서류가 열려있다면? -> 닫기
            else if (InteractionUIManager.Instance.IsDocumentOpen())
            {
                InteractionUIManager.Instance.CloseDocument();
            }
            // 아무것도 안 열려있고, 감지된 물체가 있다면? -> 상호작용 시작
            else if (currentInteractable != null)
            {
                currentInteractable.OnInteract();
            }
        }
        
        // 3. 거리 체크 (멀어지면 대화 종료)
        CheckDistanceForExit();
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