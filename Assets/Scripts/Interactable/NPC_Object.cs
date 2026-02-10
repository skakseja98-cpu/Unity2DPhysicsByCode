using UnityEngine;

public class NpcObject : MonoBehaviour, IInteractable
{
    // ... (기존 변수들 유지) ...
    public Sprite outlineSprite;
    private Sprite defaultSprite;
    private SpriteRenderer sr;
    
    [Header("--- 대화 내용 ---")]
    [TextArea] public string[] sentences;
    public float exitDistance = 3.0f;

    // 🔴 여기가 핵심! 지저분한 변수들을 하나로 묶었습니다.
    [Header("--- 목소리 스타일 설정 ---")]
    public DialogueStyle voiceStyle; 

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

    public void OnInteract()
    {
        // 매니저에게 내 스타일 꾸러미(voiceStyle)를 통째로 넘깁니다.
        InteractionUIManager.Instance.StartDialog(transform.position, sentences, voiceStyle);
    }
    
    public float GetExitDistance() => exitDistance;
}

// 📦 설정 꾸러미 (인스펙터에서 깔끔하게 보임)
[System.Serializable]
public class DialogueStyle
{
    [Header("속도 & 리듬")]
    [Tooltip("글자 나오는 속도 (작을수록 빠름)")]
    [Range(0.01f, 0.2f)] 
    public float typingSpeed = 0.05f;

    [Tooltip("쉼표, 마침표에서 잠깐 멈출까요?")]
    public bool pauseOnPunctuation = true;

    [Header("사운드 설정")]
    [Tooltip("목소리 파일 (없으면 기본음)")]
    public AudioClip uniqueVoiceClip;

    [Tooltip("기본 피치 (높을수록 얇은 소리)")]
    [Range(0.5f, 3.0f)] 
    public float pitch = 1.0f;

    [Tooltip("피치가 얼마나 떨릴까요? (0이면 로봇 소리, 0.2면 자연스러움)")]
    [Range(0.0f, 0.5f)] 
    public float pitchVariance = 0.1f;

    [Tooltip("소리 재생 빈도 (1: 매 글자마다, 3: 3글자마다)")]
    [Range(1, 5)] 
    public int soundFrequency = 2;
    
    [Tooltip("목소리 크기")]
    [Range(0.1f, 2.0f)] 
    public float volume = 1.0f;
}