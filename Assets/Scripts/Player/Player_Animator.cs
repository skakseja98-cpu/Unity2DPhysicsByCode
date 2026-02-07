using UnityEngine;

public class Player_Animator : MonoBehaviour
{
    private Animator anim;
    private Player_Controller controller; // 이름 변경
    
    void Start()
    {
        anim = GetComponent<Animator>();
        // 부모의 새로운 컨트롤러를 찾습니다
        controller = GetComponentInParent<Player_Controller>();
    }

    void Update()
    {
        if (controller == null || anim == null) return;

        // 중앙 컨트롤러를 통해 데이터 접근
        anim.SetFloat("Speed", controller.HorizontalSpeed);
        anim.SetBool("isGrounded", controller.IsGrounded);

        // 방향 전환 (리지드바디 직접 접근 혹은 컨트롤러 통해 접근)
        if (controller.Rb.linearVelocity.x > 0.1f)
            transform.localScale = new Vector3(1, 1, 1);
        else if (controller.Rb.linearVelocity.x < -0.1f)
            transform.localScale = new Vector3(-1, 1, 1);
    }
}