using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Follow Settings")]
    public float followSmoothTimeX = 0.1f; 
    public float followSmoothTimeY = 0.5f; 

    [Header("Offset")]
    public Vector3 offset = new Vector3(0, 1, -10);

    // 내부 변수
    private float currentVelocityX;
    private float currentVelocityY;
    private bool isLocked = false;

    // 화면 흔들림 제어 변수
    private float shakeTimer = 0f;
    private float currentShakeMagnitude = 0f;

    void LateUpdate()
    {
        if (target == null) return;

        if (isLocked)
        {
            transform.position = new Vector3(transform.position.x, target.position.y + offset.y, target.position.z + offset.z);
            return;
        }

        float targetX = offset.x; 
        float targetY = target.position.y + offset.y;
        float targetZ = target.position.z + offset.z;

        float newX = Mathf.SmoothDamp(transform.position.x, targetX, ref currentVelocityX, followSmoothTimeX, Mathf.Infinity, Time.unscaledDeltaTime);
        float newY = Mathf.SmoothDamp(transform.position.y, targetY, ref currentVelocityY, followSmoothTimeY, Mathf.Infinity, Time.unscaledDeltaTime);

        Vector3 finalPos = new Vector3(newX, newY, targetZ);

        // 화면 흔들림(Shake) 적용
        if (shakeTimer > 0)
        {
            finalPos += (Vector3)Random.insideUnitCircle * currentShakeMagnitude;
            shakeTimer -= Time.unscaledDeltaTime;
        }

        transform.position = finalPos;
    }

    public void SetLockMode(bool active)
    {
        isLocked = active;
    }

    // 외부에서 흔들림을 호출할 때 사용하는 함수
    public void TriggerShake(float duration, float magnitude)
    {
        shakeTimer = duration;
        currentShakeMagnitude = magnitude;
    }
}