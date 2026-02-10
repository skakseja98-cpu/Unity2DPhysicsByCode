using UnityEngine;

public class NpcObject : MonoBehaviour, IInteractable
{
    // ... (기존 아웃라인 관련 변수들 생략) ...
    public Sprite outlineSprite;
    private Sprite defaultSprite;
    private SpriteRenderer sr;

    [Header("--- 대화 내용 ---")]
    [TextArea]
    public string[] sentences;
    public float exitDistance = 3.0f;

    [Header("--- 성격 설정 (New!) ---")]
    [Tooltip("글자 나오는 속도 (작을수록 빠름)\n0.02: 화남/급함\n0.05: 보통\n0.1: 졸림/느긋함")]
    [Range(0.01f, 0.2f)]
    public float typingSpeed = 0.05f; // 기본값 보통

    [Tooltip("목소리 톤 (높을수록 얇은 소리)\n0.6: 거인/괴물\n1.0: 평범\n1.5: 요정/아이")]
    [Range(0.5f, 2.0f)]
    public float voicePitch = 1.0f; // 기본값 1.0

    [Tooltip("전용 목소리 파일 (비워두면 기본 소리 사용)")]
    public AudioClip uniqueVoiceClip; 

    // ... (Start, OnFocus, OnDefocus는 그대로 둠) ...

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        defaultSprite = sr.sprite;
    }

    public void OnFocus()
    {
        if (outlineSprite != null) sr.sprite = outlineSprite;
        else sr.color = Color.green; 
    }

    public void OnDefocus()
    {
        if (outlineSprite != null) sr.sprite = defaultSprite;
        else sr.color = Color.white;
        InteractionUIManager.Instance.CloseDialog(); 
    }

    // 🔴 중요: 매니저에게 내 성격 정보를 같이 넘깁니다!
    public void OnInteract()
    {
        InteractionUIManager.Instance.StartDialog(
            transform.position, 
            sentences, 
            typingSpeed,    // 내 속도
            voicePitch,     // 내 톤
            uniqueVoiceClip // 내 목소리 파일
        );
    }
    
    public float GetExitDistance() => exitDistance;
}