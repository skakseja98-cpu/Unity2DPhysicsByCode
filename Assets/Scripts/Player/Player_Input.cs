using UnityEngine;

public class Player_Input : MonoBehaviour
{
    [Header("Controls")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode grappleKey = KeyCode.F;
    public KeyCode retractKey = KeyCode.G;

    // 변수들
    public Vector2 MoveVector { get; private set; }
    public bool IsJumpDown { get; private set; }
    public bool IsJumpUp { get; private set; }
    public bool IsGrappleDown { get; private set; }
    public bool IsRetractHeld { get; private set; }

    void Update()
    {
        // 이동 입력
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        MoveVector = new Vector2(x, y);

        // 점프/더블점프
        IsJumpDown = Input.GetKeyDown(jumpKey);
        IsJumpUp = Input.GetKeyUp(jumpKey);
        
        // 앵커 발사/해제(F)
        IsGrappleDown = Input.GetKeyDown(grappleKey);
        
        // 줄 당기기 (G)
        IsRetractHeld = Input.GetKey(retractKey);
    }
}