using UnityEngine;

public class Player_Input : MonoBehaviour
{
    [Header("Controls")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode grappleKey = KeyCode.F;
    public KeyCode npcInteractKey = KeyCode.E;
    public KeyCode retractKey = KeyCode.G;

    public Vector2 MoveVector { get; private set; }
    public bool IsJumpDown { get; private set; }
    public bool IsJumpUp { get; private set; }
    public bool IsGrappleDown { get; private set; }
    public bool IsRetractDown { get; private set; } 
    public bool IsNpcInteractDown { get; private set; } 

    private float inputDisableTimer = 0f;

    void Update()
    {
        if (inputDisableTimer > 0)
        {
            // 역경직 중이어도 실제 시간 기준으로 기절 시간이 줄어들도록 unscaled 사용
            inputDisableTimer -= Time.unscaledDeltaTime; 
            ResetInputs();
            return;
        }

        HandleNormalInput();
    }

    public void DisableInput(float duration)
    {
        inputDisableTimer = duration;
        ResetInputs();
    }

    void HandleNormalInput()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        MoveVector = new Vector2(x, y);

        IsJumpDown = Input.GetKeyDown(jumpKey);
        IsJumpUp = Input.GetKeyUp(jumpKey);
        IsGrappleDown = Input.GetKeyDown(grappleKey);
        IsNpcInteractDown = Input.GetKeyDown(npcInteractKey);
        IsRetractDown = Input.GetKeyDown(retractKey); 
    }

    void ResetInputs()
    {
        MoveVector = Vector2.zero;
        IsJumpDown = false;
        IsJumpUp = false;
        IsGrappleDown = false;
        IsRetractDown = false; 
        IsNpcInteractDown = false;
    }
}