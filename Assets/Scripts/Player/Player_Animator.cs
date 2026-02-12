using UnityEngine;

public class Player_Animator : MonoBehaviour
{
    private Animator anim;
    private Player_Controller controller; 
    
    void Start()
    {
        anim = GetComponent<Animator>();
        controller = GetComponentInParent<Player_Controller>();
    }

    void Update()
    {
        if (controller == null || anim == null) return;

        // 1. 기존 파라미터 갱신
        anim.SetFloat("Speed", controller.HorizontalSpeed);
        anim.SetBool("isGrounded", controller.IsGrounded);
        
        // 2. [신규] 벽타기 파라미터 갱신
        bool isClimbing = controller.IsClimbing;
        anim.SetBool("isClimbing", isClimbing);

        // 3. [신규] 벽타기 애니메이션 속도 제어 (가만히 있으면 멈춤)
        if (isClimbing)
        {
            // 플레이어의 실제 속도(x, y 모두 포함)가 거의 0이면 -> 멈춤
            if (controller.Rb.linearVelocity.magnitude > 0.1f)
            {
                anim.speed = 1f; // 움직임: 재생
            }
            else
            {
                anim.speed = 0f; // 정지: 프레임 고정 (매달린 느낌)
            }
        }
        else
        {
            // 벽타기가 아닐 때는 항상 정상 속도로 재생
            anim.speed = 1f;
        }

        // 4. 방향 전환 (기존 코드 유지)
        if (!isClimbing)
        {
            if (controller.Rb.linearVelocity.x > 0.1f)
                transform.localScale = new Vector3(1, 1, 1);
            else if (controller.Rb.linearVelocity.x < -0.1f)
                transform.localScale = new Vector3(-1, 1, 1);
        }
    }
}