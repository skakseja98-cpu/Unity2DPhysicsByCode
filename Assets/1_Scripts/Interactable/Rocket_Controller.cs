using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class Rocket_Controller : MonoBehaviour
{
    [Header("1. Destination Setting")]
    [Tooltip("로켓이 도달할 목표 Y 위치 (절대 좌표)")]
    public float targetHeight = 100f;

    [Tooltip("도착까지 걸리는 시간 (초)")]
    public float flightDuration = 10f;

    [Header("2. Movement Curve (핵심)")]
    [Tooltip("시간에 따른 이동 비율 그래프. (X축: 0~1 시간, Y축: 0~1 진행률)\n가로축 0에서 시작해 1로 끝나는 'Ease In' 형태를 추천합니다.")]
    public AnimationCurve motionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("3. Pre-Launch (Ignition)")]
    [Tooltip("발사 전 덜덜거리는 시간")]
    public float ignitionDuration = 2.0f;
    [Tooltip("덜덜거리는 강도")]
    public float shakeMagnitude = 0.1f;

    [Header("4. FX References")]
    public ParticleSystem engineEffect; // 불꽃 파티클
    public AudioSource audioSource;     // 로켓 소리
    public AudioClip ignitionSound;     // 점화 소리
    public AudioClip flyingSound;       // 비행 소리

    public CameraFollow mainCam;

    // 상태 변수
    private bool isLaunched = false;
    private Rigidbody2D rb;
    private Vector2 startPos;
    private Vector2 targetPos;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // 물리 설정 강제 (움직이는 발판과 동일)
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        startPos = rb.position;
        // X축은 유지하고 Y축만 목표 높이로 설정
        targetPos = new Vector2(startPos.x, targetHeight);
    }

    // 외부에서(버튼, 트리거 등) 이 함수를 호출하면 발사 시작
    public void Launch()
    {
        if (isLaunched) return;
        StartCoroutine(LaunchSequence());
    }

    private IEnumerator LaunchSequence()
    {
        isLaunched = true;

        if (mainCam != null) mainCam.SetLockMode(true);

        // --- 1단계: 점화 (진동) ---
        if (engineEffect != null) engineEffect.Play();
        if (audioSource != null && ignitionSound != null)
        {
            audioSource.clip = ignitionSound;
            audioSource.Play();
        }

        float shakeTimer = 0f;
        while (shakeTimer < ignitionDuration)
        {
            shakeTimer += Time.deltaTime;
            // 원래 위치 근처에서 랜덤하게 떨림
            float xOffset = Random.Range(-1f, 1f) * shakeMagnitude;
            float yOffset = Random.Range(-1f, 1f) * shakeMagnitude;
            rb.MovePosition(startPos + new Vector2(xOffset, yOffset));
            yield return null;
        }

        // 진동 끝, 위치 복구
        rb.MovePosition(startPos);

        // --- 2단계: 비행 (커브 이동) ---
        if (audioSource != null && flyingSound != null)
        {
            audioSource.loop = true;
            audioSource.clip = flyingSound;
            audioSource.Play();
        }

        float flightTimer = 0f;

        while (flightTimer < flightDuration)
        {
            flightTimer += Time.fixedDeltaTime; // 물리 이동이므로 FixedDeltaTime 권장

            // 0 ~ 1 사이의 진행률 (시간 기준)
            float t = Mathf.Clamp01(flightTimer / flightDuration);

            // 커브에서 현재 진행률에 해당하는 Y값(이동 비율)을 가져옴
            float curveValue = motionCurve.Evaluate(t);

            // 현재 프레임의 목표 위치 계산 (Lerp)
            Vector2 nextPosition = Vector2.Lerp(startPos, targetPos, curveValue);

            // [중요] 위치를 옮기면서 속도(Velocity)도 계산해서 넣어줌
            // 플레이어가 매달렸을 때 관성을 받기 위함
            Vector2 velocity = (nextPosition - rb.position) / Time.fixedDeltaTime;
            rb.linearVelocity = velocity;

            // 실제 이동
            rb.MovePosition(nextPosition);

            yield return new WaitForFixedUpdate(); // FixedUpdate 주기에 맞춤
        }

        // --- 3단계: 도착 ---
        rb.position = targetPos;
        rb.linearVelocity = Vector2.zero;
        
        if (engineEffect != null) engineEffect.Stop();
        if (audioSource != null) audioSource.Stop();

        if (mainCam != null) mainCam.SetLockMode(false);

        Debug.Log("로켓 도착!");
    }

    public void ResetRocket()
    {
        // 1. 실행 중인 발사 코루틴 강제 종료
        StopAllCoroutines();

        // 2. 상태 변수 초기화
        isLaunched = false;
        
        // 3. 물리 속도 및 위치 초기화
        rb.linearVelocity = Vector2.zero;
        rb.MovePosition(startPos); // 시작 위치로 강제 이동
        rb.position = startPos;    // 이중 확인

        // 4. 이펙트 및 사운드 끄기
        if (engineEffect != null) engineEffect.Stop();
        if (audioSource != null) audioSource.Stop();

        // 5. 카메라 락 해제 (플레이어가 떨어졌으므로 카메라는 플레이어를 다시 따라가야 함)
        if (mainCam != null) mainCam.SetLockMode(false);

        Debug.Log("로켓이 초기화되었습니다.");
    }
    
    // 디버그용: 에디터에서 목표 지점 선 그리기
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector2 end = new Vector2(transform.position.x, targetHeight);
        Gizmos.DrawLine(transform.position, end);
        Gizmos.DrawSphere(end, 0.5f);
    }
}