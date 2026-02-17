using UnityEngine;

public class Player_Animator : MonoBehaviour
{
    private Animator anim;
    private Player_Controller controller; 
    // [추가] 입력을 확인하기 위해 Player_Input 참조 추가
    private Player_Input playerInput; 
    
    void Start()
    {
        anim = GetComponent<Animator>();
        controller = GetComponentInParent<Player_Controller>();
        // 부모 오브젝트에서 Input 컴포넌트 가져오기
        playerInput = GetComponentInParent<Player_Input>();
    }

    void Update()
    {
        if (controller == null || anim == null) return;

        // 1. 기존 파라미터 갱신
        anim.SetFloat("Speed", controller.HorizontalSpeed);
        anim.SetBool("isGrounded", controller.IsGrounded);
        
        // 2. 벽타기 파라미터 갱신
        bool isClimbing = controller.IsClimbing;
        anim.SetBool("isClimbing", isClimbing);

        // 3. [수정] 벽타기 애니메이션 속도 제어
        if (isClimbing)
        {
            // [핵심 변경] 실제 속도(velocity)가 아니라 '입력값(MoveVector)'이 있을 때만 재생
            // 입력이 없으면(0이면) 벽이 움직여서 몸이 이동하더라도 애니메이션은 멈춤(매달린 상태)
            if (playerInput != null && playerInput.MoveVector.magnitude > 0.1f)
            {
                anim.speed = 1f; // 입력이 있으면 재생 (기어 올라감)
            }
            else
            {
                anim.speed = 0f; // 입력이 없으면 정지 (매달려 있음)
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