using UnityEngine;

public class Player_Input : MonoBehaviour
{
    [Header("Controls")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode dashKey = KeyCode.LeftShift; // 혹은 Mouse1
    public KeyCode grappleKey = KeyCode.F;
    public KeyCode retractKey = KeyCode.G;

    // 다른 스크립트에서 가져다 쓸 변수들
    public Vector2 MoveVector { get; private set; }
    public bool IsJumpDown { get; private set; }
    public bool IsJumpUp { get; private set; }
    public bool IsDashDown { get; private set; }
    public bool IsGrappleDown { get; private set; }
    public bool IsRetractHeld { get; private set; }

    void Update()
    {
        // 이동 입력 (WASD / 화살표)
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        MoveVector = new Vector2(x, y);

        // 버튼 입력 감지
        IsJumpDown = Input.GetKeyDown(jumpKey);
        IsJumpUp = Input.GetKeyUp(jumpKey);
        
        // 대쉬 (ButtonName "Dash" 대신 키코드로 통일하거나 프로젝트 세팅에 맞게 수정)
        IsDashDown = Input.GetKeyDown(dashKey) || Input.GetButtonDown("Dash"); 
        
        IsGrappleDown = Input.GetKeyDown(grappleKey);
        IsRetractHeld = Input.GetKey(retractKey);
    }
}