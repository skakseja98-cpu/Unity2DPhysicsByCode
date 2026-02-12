using UnityEngine;

public class Push_Obstacle : MonoBehaviour
{
    [Header("Push Settings")]
    [Tooltip("플레이어가 날아갈 방향 (X, Y)")]
    public Vector2 pushDirection = Vector2.up; // 기본값: 위쪽

    [Tooltip("밀쳐내는 힘 (클수록 멀리 날아갑니다)")]
    public float pushForce = 20f;

    [Tooltip("체크하면 닿는 순간 플레이어의 기존 속도를 0으로 만들고 튕겨냅니다. (더 확실하게 튕겨나감)")]
    public bool resetVelocity = true;

    // [추가] 인스펙터에서 조절 가능한 경직(스턴) 시간
    [Header("Stun Settings")]
    [Tooltip("충돌 후 플레이어가 조작할 수 없는 시간 (초)")]
    public float stunDuration = 0.5f;

    // 플레이어와 충돌했을 때 (Trigger가 아니라 Collider 충돌)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 1. 플레이어인지 확인
        Player_Controller player = collision.gameObject.GetComponent<Player_Controller>();

        if (player != null)
        {
            Rigidbody2D rb = player.Rb; // Player_Controller의 Rigidbody 가져오기

            if (rb != null)
            {
                // 2. 기존 속도 초기화 (선택 사항)
                // 이걸 안 하면 플레이어가 달려오던 관성과 합쳐져서 예측 불가능하게 튈 수 있음
                if (resetVelocity)
                {
                    rb.linearVelocity = Vector2.zero; 
                }

                // 3. 힘 가하기 (Impulse = 순간적인 타격)
                rb.AddForce(pushDirection.normalized * pushForce, ForceMode2D.Impulse);
                
                // [추가] 플레이어의 Input 스크립트를 찾아 입력을 잠급니다.
                Player_Input playerInput = player.GetComponent<Player_Input>();
                if (playerInput != null)
                {
                    playerInput.DisableInput(stunDuration);
                }
            }
        }
    }

    // 에디터에서 튕겨낼 방향을 화살표로 보여줌
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        // 중심에서 설정한 방향으로 선을 그음
        Vector3 direction = (Vector3)pushDirection.normalized;
        Gizmos.DrawRay(transform.position, direction * 2f);
        
        // 화살표 머리
        Gizmos.DrawSphere(transform.position + direction * 2f, 0.2f);
    }
}