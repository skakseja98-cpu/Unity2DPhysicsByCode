using UnityEngine;

public class Push_Obstacle : MonoBehaviour
{
    [Header("Push Settings")]
    [Tooltip("플레이어가 날아갈 방향 (X, Y)")]
    public Vector2 pushDirection = Vector2.up; 

    [Tooltip("밀쳐내는 힘 (클수록 멀리 날아갑니다)")]
    public float pushForce = 20f;

    [Tooltip("체크하면 닿는 순간 플레이어의 기존 속도를 0으로 만들고 튕겨냅니다.")]
    public bool resetVelocity = true;

    [Header("Stun Settings")]
    [Tooltip("충돌 후 플레이어가 조작할 수 없는 시간 (초)")]
    public float stunDuration = 0.5f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. 플레이어인지 확인
        Player_Controller player = collision.gameObject.GetComponent<Player_Controller>();

        if (player != null)
        {
            // [추가된 부분] ------------------------------------------------
            // 플레이어가 벽타기 중이라면 강제로 떼어냅니다.
            // 이걸 안 하면 물리력이 적용되자마자 다시 벽에 붙어버려서 안 날아갑니다.
            Player_Movement movement = player.GetComponent<Player_Movement>();
            if (movement != null && movement.IsClimbing)
            {
                movement.SetClimbing(false);
            }
            // -------------------------------------------------------------

            Rigidbody2D rb = player.Rb;

            if (rb != null)
            {
                // 2. 기존 속도 초기화 (튕겨나가는 방향을 확실하게 하기 위해)
                if (resetVelocity)
                {
                    rb.linearVelocity = Vector2.zero; 
                }

                // 3. 힘 가하기
                rb.AddForce(pushDirection.normalized * pushForce, ForceMode2D.Impulse);
                
                // 4. 입력 잠금 (경직)
                Player_Input playerInput = player.GetComponent<Player_Input>();
                if (playerInput != null)
                {
                    playerInput.DisableInput(stunDuration);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 direction = (Vector3)pushDirection.normalized;
        Gizmos.DrawRay(transform.position, direction * 2f);
        Gizmos.DrawSphere(transform.position + direction * 2f, 0.2f);
    }
}