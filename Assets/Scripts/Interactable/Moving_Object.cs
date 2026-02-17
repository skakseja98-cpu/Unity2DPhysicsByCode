using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovingObject : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("이동할 상대적인 거리와 방향")]
    public Vector2 moveOffset = new Vector2(0, 5f);

    [Tooltip("평균 이동 속도 (값이 클수록 빠름)")]
    public float speed = 3f;

    [Tooltip("끝 지점에 도달했을 때 대기 시간 (초)")]
    public float waitTime = 1f;

    [Tooltip("게임 시작 시 랜덤한 위치에서 시작할지 여부")]
    public bool randomizeStartTime = false;

    [Header("Gizmos")]
    public Color gizmoColor = Color.green;

    // 내부 변수
    private Rigidbody2D rb;
    private Vector2 startPos;
    private Vector2 endPos;

    private Vector2 currentStart; // 현재 출발지
    private Vector2 currentEnd;   // 현재 목적지

    private float moveDuration;   // 이동에 걸리는 총 시간
    private float currentTime;    // 현재 이동 시간
    private float waitTimer;      // 대기 타이머
    private bool isWaiting;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // 물리 충돌을 위해 Kinematic 설정
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // 시작/끝 지점 계산
        startPos = rb.position;
        endPos = startPos + moveOffset;

        // 초기 목표 설정
        currentStart = startPos;
        currentEnd = endPos;

        // 거리와 속도를 이용해 이동 시간 계산 (Time = Distance / Speed)
        float distance = Vector2.Distance(startPos, endPos);
        if (speed <= 0) speed = 1f; // 0 나누기 방지
        moveDuration = distance / speed;

        if (randomizeStartTime)
        {
            // 중간 시간부터 시작하도록 설정
            currentTime = Random.Range(0f, moveDuration);
        }
    }

    void FixedUpdate()
    {
        // 1. 대기 상태 처리
        if (isWaiting)
        {
            waitTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = Vector2.zero; // 대기 중엔 속도 0 (중요: 플레이어가 미끄러지지 않음)

            if (waitTimer <= 0)
            {
                isWaiting = false;
                
                // 목표 지점 스왑 (출발지 <-> 목적지 뒤집기)
                Vector2 temp = currentStart;
                currentStart = currentEnd;
                currentEnd = temp;
                
                currentTime = 0f;
            }
            return;
        }

        // 2. 시간 진행 및 진행률(0~1) 계산
        currentTime += Time.fixedDeltaTime;
        float ratio = Mathf.Clamp01(currentTime / moveDuration);

        // 3. 부드러운 곡선 적용 (Ease-In-Out) [핵심]
        // SmoothStep은 0에서 천천히 시작해서, 중간에 빨라졌다가, 1에서 천천히 멈춤
        float smoothRatio = Mathf.SmoothStep(0f, 1f, ratio);

        // 4. 위치 계산 (Lerp)
        Vector2 newPos = Vector2.Lerp(currentStart, currentEnd, smoothRatio);

        // 5. 속도 수동 계산 [매우 중요]
        // Rigidbody를 그냥 MovePosition으로만 옮기면 내부 속도가 0이 되어
        // 플레이어가 벽을 탔을 때 관성을 못 받습니다. 직접 속도를 계산해 넣어줍니다.
        Vector2 velocity = (newPos - rb.position) / Time.fixedDeltaTime;
        rb.linearVelocity = velocity;

        // 6. 실제 이동 적용
        rb.MovePosition(newPos);

        // 7. 도착 판정
        if (ratio >= 1f)
        {
            rb.position = currentEnd; // 위치 보정
            rb.linearVelocity = Vector2.zero; // 도착했으니 정지
            
            isWaiting = true;
            waitTimer = waitTime;
        }
    }

    void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawLine(startPos, endPos);
            Gizmos.DrawWireSphere(startPos, 0.2f);
            Gizmos.DrawWireSphere(endPos, 0.2f);
        }
        else
        {
            Gizmos.color = gizmoColor;
            Vector2 from = transform.position;
            Vector2 to = from + moveOffset;
            Gizmos.DrawLine(from, to);
            Gizmos.DrawWireSphere(to, 0.2f);
        }
    }
}