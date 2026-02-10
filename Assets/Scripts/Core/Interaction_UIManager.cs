using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class InteractionUIManager : MonoBehaviour
{
    public static InteractionUIManager Instance;

    // ... (기존 UI 변수들 유지) ...
    [Header("--- 서류 UI ---")]
    public GameObject docPanel;
    public Image docImageSlot;

    [Header("--- 대화 UI ---")]
    public GameObject dialogBoxPrefab;
    private GameObject currentDialogBox;
    private TextMeshProUGUI dialogText;

    [Header("--- 기본 사운드 ---")]
    public AudioSource audioSource;
    public AudioClip defaultTypingClip;

    // 🔴 현재 스타일을 저장할 변수 (DialogueStyle 타입)
    private DialogueStyle currentStyle;

    private Queue<string> sentences = new Queue<string>();
    private string currentSentence;
    private bool isTyping = false;
    private Coroutine typingCoroutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        if(docPanel != null) docPanel.SetActive(false);
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    // 🔴 매개변수가 DialogueStyle 하나로 깔끔해졌습니다!
    public void StartDialog(Vector3 position, string[] lines, DialogueStyle style)
    {
        CloseDialog();
        
        // 스타일 저장 (만약 null이면 기본값 생성)
        currentStyle = style ?? new DialogueStyle(); 

        // 오디오 클립 미리 세팅
        AudioClip clipToPlay = currentStyle.uniqueVoiceClip != null ? currentStyle.uniqueVoiceClip : defaultTypingClip;
        audioSource.clip = clipToPlay; // PlayOneShot 대신 미리 세팅해도 됨 (여기선 유연하게 유지)

        sentences.Clear();
        foreach (string line in lines) sentences.Enqueue(line);

        currentDialogBox = Instantiate(dialogBoxPrefab, position + new Vector3(0, 1.5f, 0), Quaternion.identity);
        dialogText = currentDialogBox.GetComponentInChildren<TextMeshProUGUI>();

        NextSentence();
    }

    public void AdvanceDialog()
    {
        if (currentDialogBox == null) return;

        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            dialogText.text = currentSentence;
            isTyping = false;
        }
        else
        {
            NextSentence();
        }
    }

    void NextSentence()
    {
        if (sentences.Count == 0)
        {
            CloseDialog();
            return;
        }
        currentSentence = sentences.Dequeue();
        if(typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypewriterEffect(currentSentence));
    }

    public void CloseDialog()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        
        if (currentDialogBox != null)
        {
            Destroy(currentDialogBox);
            currentDialogBox = null;
        }
        isTyping = false;
    }

    public bool IsDialogOpen() => currentDialogBox != null;

    // 🔴 타자기 효과 (업그레이드 버전)
    IEnumerator TypewriterEffect(string fullText)
    {
        isTyping = true;
        dialogText.text = ""; 
        int charCount = 0;

        foreach (char letter in fullText.ToCharArray())
        {
            dialogText.text += letter;
            charCount++;

            // 1. 소리 재생 (빈도 설정 적용)
            // 공백 아니고, 설정된 빈도(Frequency)마다 재생
            if (letter != ' ' && charCount % currentStyle.soundFrequency == 0)
            {
                PlayTypingSound();
            }

            // 2. 기본 대기 (타이핑 속도)
            yield return new WaitForSeconds(currentStyle.typingSpeed);

            // 3. 구두점 일시정지 (Punctuation Pause)
            // 쉼표나 마침표 뒤에서는 조금 더 쉬어서 '읽는 맛'을 줌
            if (currentStyle.pauseOnPunctuation)
            {
                if (letter == ',' || letter == '.' || letter == '?' || letter == '!')
                {
                    // 기본 속도의 5배만큼 더 쉼
                    yield return new WaitForSeconds(currentStyle.typingSpeed * 5.0f);
                }
            }
        }
        isTyping = false;
    }

    // 🔴 소리 재생 (랜덤 피치 + 볼륨 적용)
    void PlayTypingSound()
    {
        if (audioSource == null) return;

        AudioClip clipToPlay = currentStyle.uniqueVoiceClip != null ? currentStyle.uniqueVoiceClip : defaultTypingClip;
        
        if (clipToPlay != null)
        {
            // 랜덤 피치: 기준 피치에서 ±Variance 만큼 흔들림
            float randomPitch = currentStyle.pitch + Random.Range(-currentStyle.pitchVariance, currentStyle.pitchVariance);
            
            audioSource.pitch = randomPitch;
            audioSource.PlayOneShot(clipToPlay, currentStyle.volume);
        }
    }

    // ... (서류 관련 코드는 그대로 유지) ...
    public void ShowDocument(Sprite docSprite)
    {
        docImageSlot.sprite = docSprite;
        docImageSlot.preserveAspect = true;
        docPanel.SetActive(true);
    }
    public void CloseDocument() => docPanel.SetActive(false);
    public bool IsDocumentOpen() => docPanel.activeSelf;
}