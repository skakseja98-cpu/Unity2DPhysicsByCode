using UnityEngine;

public class Player_Animator : MonoBehaviour
{
    private Animator anim;
    private Player_Controller2D controller;
    private Rigidbody2D rb;

    void Start()
    {
        // 1. 같은 오브젝트에 있는 애니메이터를 가져옵니다.
        anim = GetComponent<Animator>();
        
        // 2. 부모 오브젝트(Player)에 있는 컨트롤러와 리지드바디를 가져옵니다.
        controller = GetComponentInParent<Player_Controller2D>();
        rb = GetComponentInParent<Rigidbody2D>();
    }

    void Update()
    {
        if (controller == null || rb == null || anim == null) return;

        // [애니메이션 파라미터 전달]
        // 리지드바디의 실제 수평 속도를 사용하여 Speed 파라미터를 조절합니다.
        float horizontalSpeed = Mathf.Abs(rb.linearVelocity.x);
        anim.SetFloat("Speed", horizontalSpeed);

        // 부모의 바닥 감지 상태를 가져와서 전달합니다.
        anim.SetBool("isGrounded", controller.isGroundDetected);

        // [좌우 반전 처리]
        // 리지드바디의 속도가 0.1보다 크면 오른쪽, -0.1보다 작으면 왼쪽을 보게 합니다.
        if (rb.linearVelocity.x > 0.1f)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (rb.linearVelocity.x < -0.1f)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
    }
}