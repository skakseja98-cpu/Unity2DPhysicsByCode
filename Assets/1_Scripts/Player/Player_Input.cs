using UnityEngine;

public class Player_Input : MonoBehaviour
{
    [Header("Controls")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode grappleKey = KeyCode.F;
    public KeyCode npcInteractKey = KeyCode.E;
    public KeyCode observeKey = KeyCode.LeftControl;
    public KeyCode retractKey = KeyCode.G;

    [Header("Observation Settings")]
    [Tooltip("관찰 모드 사용 후 재사용 대기시간 (초)")]
    public float observeCooldown = 3.0f;

    public Vector2 MoveVector { get; private set; }
    public bool IsJumpDown { get; private set; }
    public bool IsJumpUp { get; private set; }
    public bool IsGrappleDown { get; private set; }
    
    // [수정] Held -> Down으로 변경
    public bool IsRetractDown { get; private set; } 
    public bool IsNpcInteractDown { get; private set; } 

    private float currentCooldownTimer = 0f;
    private float inputDisableTimer = 0f;
    private bool isObserving = false;

    public float CurrentCooldownRatio => Mathf.Clamp01(currentCooldownTimer / observeCooldown);

    void Update()
    {
        if (inputDisableTimer > 0)
        {
            inputDisableTimer -= Time.deltaTime;
            ResetInputs();
            return;
        }

        HandleObservationInput();

        if (isObserving) return;

        HandleNormalInput();
    }

    public void DisableInput(float duration)
    {
        inputDisableTimer = duration;
        ResetInputs();
    }

    void HandleObservationInput()
    {
        if (currentCooldownTimer > 0)
        {
            currentCooldownTimer -= Time.unscaledDeltaTime;
        }

        if (Input.GetKey(observeKey) && currentCooldownTimer <= 0)
        {
            if (!isObserving) StartObservation();
            ResetInputs();
        }
        else
        {
            if (isObserving) StopObservation();
        }
    }

    void StartObservation()
    {
        isObserving = true;
        Time.timeScale = 0f;
    }

    void StopObservation()
    {
        isObserving = false;
        Time.timeScale = 1f;
        currentCooldownTimer = observeCooldown; 
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
        
        // [수정] GetKey -> GetKeyDown으로 변경
        IsRetractDown = Input.GetKeyDown(retractKey); 
    }

    void ResetInputs()
    {
        MoveVector = Vector2.zero;
        IsJumpDown = false;
        IsJumpUp = false;
        IsGrappleDown = false;
        IsRetractDown = false; // [수정]
        IsNpcInteractDown = false;
    }

    void OnGUI()
    {
        if (currentCooldownTimer > 0)
        {
            GUI.color = Color.red;
            GUI.Label(new Rect(10, 10, 200, 20), $"Cooldown: {currentCooldownTimer:F1}s");
        }
        else
        {
            GUI.color = Color.green;
            GUI.Label(new Rect(10, 10, 200, 20), "Ready (Hold Ctrl)");
        }
    }
}