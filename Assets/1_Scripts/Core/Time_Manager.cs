using UnityEngine;
using System.Collections;

public class Time_Manager : MonoBehaviour
{
    public static Time_Manager Instance;

    private float defaultTimeScale = 1.0f;
    private float defaultFixedDeltaTime;
    private Coroutine hitStopCoroutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        defaultFixedDeltaTime = Time.fixedDeltaTime;
    }

    // 역경직을 발생시키는 함수 (duration: 실제 유지 시간, targetTimeScale: 느려질 배속)
    public void TriggerHitStop(float duration, float targetTimeScale = 0.05f)
    {
        if (hitStopCoroutine != null) StopCoroutine(hitStopCoroutine);
        hitStopCoroutine = StartCoroutine(HitStopRoutine(duration, targetTimeScale));
    }

    private IEnumerator HitStopRoutine(float duration, float targetTimeScale)
    {
        Time.timeScale = targetTimeScale;
        // 물리 엔진(FixedUpdate) 주기도 함께 조절해야 뚝뚝 끊기지 않고 부드럽게 느려집니다.
        Time.fixedDeltaTime = defaultFixedDeltaTime * targetTimeScale; 

        // 게임 시간이 멈춰있어도 실제 시간(Realtime) 기준으로 타이머를 잽니다.
        yield return new WaitForSecondsRealtime(duration);

        // 시간 원상 복구
        Time.timeScale = defaultTimeScale;
        Time.fixedDeltaTime = defaultFixedDeltaTime;
        hitStopCoroutine = null;
    }
}