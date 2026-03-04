using UnityEngine;
using System.Collections;

public class Player_Visuals : MonoBehaviour
{
    [Header("Hit Flash Settings")]
    public Color flashColor = Color.red;
    
    [Tooltip("깜빡임 효과가 유지되는 총 시간 (실제 시간 기준)")]
    public float totalFlashDuration = 1.0f;
    
    [Tooltip("해당 시간 동안 몇 번 깜빡일 것인가")]
    public int flashCount = 5;

    private SpriteRenderer sr;
    private Color originalColor;
    private Coroutine flashCoroutine;

    void Start()
    {
        // 플레이어 본체 또는 자식에 있는 SpriteRenderer를 찾습니다.
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) originalColor = sr.color;
    }

    public void TriggerFlash()
    {
        if (sr == null) return;
        
        // 이미 깜빡이고 있다면 초기화 후 다시 시작 (연속 피격 대비)
        if (flashCoroutine != null) 
        {
            StopCoroutine(flashCoroutine);
            sr.color = originalColor; 
        }
        
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        // 1회 깜빡임(빨강색 전환 -> 원래색 복구)에 걸리는 시간 계산
        float interval = totalFlashDuration / (flashCount * 2);

        for (int i = 0; i < flashCount; i++)
        {
            sr.color = flashColor;
            yield return new WaitForSecondsRealtime(interval);

            sr.color = originalColor;
            yield return new WaitForSecondsRealtime(interval);
        }

        flashCoroutine = null;
    }
}