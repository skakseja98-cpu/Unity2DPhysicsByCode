using UnityEngine;
using UnityEngine.Events;

public class Interaction_Event : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    public bool oneTimeOnly = false; // 한 번만 작동할지 여부

    [Header("Events")]
    public UnityEvent onInteract; // 인스펙터에서 연결할 이벤트

    private bool hasTriggered = false;

    // IInteractable 인터페이스 구현
    public void OnFocus() 
    {
        // 필요 시 외곽선 처리 (NPC_Object와 같이 쓰면 겹칠 수 있으니 비워도 됨)
    }

    public void OnDefocus() { }

    public void OnInteract()
    {
        if (oneTimeOnly && hasTriggered) return;

        // 여기에 연결된 기능 실행 (예: Rocket_Controller.ResetRocket)
        onInteract.Invoke();
        
        hasTriggered = true;
    }
}