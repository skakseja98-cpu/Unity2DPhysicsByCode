using UnityEngine;

public class Push_Obstacle : MonoBehaviour
{
    [Header("Push Settings")]
    public Vector2 pushDirection = Vector2.up; 
    public float pushForce = 20f;
    public bool resetVelocity = true;

    [Header("Stun & Feel Settings")]
    public float stunDuration = 0.5f;
    public float hitStopDuration = 0.08f; 
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 0.3f;

    [Header("Audio Settings")]
    [Tooltip("소리를 재생할 오디오 소스 (장애물에 부착)")]
    public AudioSource audioSource;
    [Tooltip("플레이어가 부딪혔을 때 날 피격음")]
    public AudioClip hitSound;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player_Controller player = collision.gameObject.GetComponent<Player_Controller>();

        if (player != null)
        {
            Player_Movement movement = player.GetComponent<Player_Movement>();
            if (movement != null && movement.IsClimbing)
            {
                movement.SetClimbing(false);
            }

            Rigidbody2D rb = player.Rb;

            if (rb != null)
            {
                if (resetVelocity) rb.linearVelocity = Vector2.zero; 

                // 1. 물리적 밀치기 적용
                rb.AddForce(pushDirection.normalized * pushForce, ForceMode2D.Impulse);
                
                // 2. 조작 잠금
                Player_Input playerInput = player.GetComponent<Player_Input>();
                if (playerInput != null) playerInput.DisableInput(stunDuration);

                // 3. 타격감: 역경직
                if (Time_Manager.Instance != null)
                    Time_Manager.Instance.TriggerHitStop(hitStopDuration, 0.05f);

                // 4. 타격감: 카메라 흔들림
                CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
                if (cam != null) cam.TriggerShake(shakeDuration, shakeMagnitude);

                // 5. 타격감: 플레이어 붉은색 깜빡임
                Player_Visuals visuals = player.GetComponent<Player_Visuals>();
                if (visuals != null) visuals.TriggerFlash();

                // 6. 타격감: 사운드 재생
                if (audioSource != null && hitSound != null)
                {
                    audioSource.PlayOneShot(hitSound);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 direction = (Vector3)pushDirection.normalized;
        Gizmos.DrawRay(transform.position, direction * 2f);
        Gizmos.DrawSphere(transform.position + direction * 2f, 0.2f);
    }
}