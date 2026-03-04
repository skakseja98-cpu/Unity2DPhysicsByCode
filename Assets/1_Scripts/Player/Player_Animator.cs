using UnityEngine;

public class Player_Animator : MonoBehaviour
{
    private Animator anim;
    private Player_Controller controller; 
    private Player_Input playerInput; 
    
    void Start()
    {
        anim = GetComponent<Animator>();
        controller = GetComponentInParent<Player_Controller>();
        playerInput = GetComponentInParent<Player_Input>();
    }

    void Update()
    {
        if (controller == null || anim == null || playerInput == null) return;

        // 1. [수정됨] 실제 이동 속도 대신 '키보드 입력값'을 기준으로 애니메이션 재생
        // (가만히 있으면 0, 좌/우 키를 누르면 0보다 큰 값이 들어감)
        anim.SetFloat("Speed", Mathf.Abs(playerInput.MoveVector.x));
        
        anim.SetBool("isGrounded", controller.IsGrounded);
        
        // 2. 벽타기 파라미터 갱신
        bool isClimbing = controller.IsClimbing;
        anim.SetBool("isClimbing", isClimbing);

        // 3. 벽타기 애니메이션 속도 제어
        if (isClimbing)
        {
            if (playerInput.MoveVector.magnitude > 0.1f)
            {
                anim.speed = 1f; 
            }
            else
            {
                anim.speed = 0f; 
            }
        }
        else
        {
            anim.speed = 1f;
        }

        // 4. [수정됨] 캐릭터 방향 전환도 '키보드 입력'을 기준으로 변경
        // (이걸 안 고치면 왼쪽으로 가는 발판을 탔을 때 플레이어가 강제로 왼쪽을 보게 됩니다)
        if (!isClimbing)
        {
            if (playerInput.MoveVector.x > 0.1f)
                transform.localScale = new Vector3(1, 1, 1);
            else if (playerInput.MoveVector.x < -0.1f)
                transform.localScale = new Vector3(-1, 1, 1);
        }
    }
}