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
    private bool currentDialogSkippable = true;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        if(docPanel != null) docPanel.SetActive(false);
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void StartDialog(Vector3 position, string[] lines, DialogueStyle style, bool canSkip)
    {
        CloseDialog();

        currentDialogSkippable = canSkip;
        
        // 스타일 저장 (만약 null이면 기본값 생성)
        currentStyle = style ?? new DialogueStyle(); 

        // 오디오 클립 미리 세팅
        AudioClip clipToPlay = currentStyle.uniqueVoiceClip != null ? currentStyle.uniqueVoiceClip : defaultTypingClip;
        audioSource.clip = clipToPlay; // PlayOneShot 대신 미리 세팅해도 됨 (여기선 유연하게 유지)

        sentences.Clear();
        foreach (string line in lines) sentences.Enqueue(line);

        currentDialogBox = Instantiate(dialogBoxPrefab, position + new Vector3(0, 1.5f, 0), Quaternion.identity);
        dialogText = currentDialogBox.GetComponentInChildren<TextMeshProUGUI>();

        dialogText.color = currentStyle.textColor;

        dialogText.text = ""; 
        dialogText.maxVisibleCharacters = 0;

        NextSentence();
    }

    public void AdvanceDialog()
    {
        if (currentDialogBox == null) return;

        if (isTyping)
        {
            // 🔴 [핵심 로직] 스킵이 불가능한 대화라면, 입력 무시!
            if (!currentDialogSkippable) return;

            // 스킵 가능하다면 -> 글자 제한 풀어서 한 번에 보여주기 (지난번 수정 코드)
            StopCoroutine(typingCoroutine);
            dialogText.maxVisibleCharacters = int.MaxValue; 
            dialogText.ForceMeshUpdate(); 
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
        
        // 1. 텍스트를 먼저 다 집어넣습니다. (태그가 포함된 상태로)
        dialogText.text = fullText;
        
        // 2. 일단 하나도 안 보이게 숨깁니다.
        dialogText.maxVisibleCharacters = 0;

        // 3. TMP가 텍스트를 분석할 시간을 줍니다 (필수!)
        dialogText.ForceMeshUpdate(); 

        // 4. 실제로 보여줄 글자 수(태그 제외)를 가져옵니다.
        TMP_TextInfo textInfo = dialogText.textInfo;
        int totalVisibleChars = textInfo.characterCount; 

        // 5. 0개부터 전체 개수까지 늘려갑니다.
        for (int i = 1; i <= totalVisibleChars; i++)
        {
            dialogText.maxVisibleCharacters = i;

            // --- 사운드 재생 ---
            if (i % currentStyle.soundFrequency == 0)
            {
                PlayTypingSound();
            }

            // --- 구두점 일시정지 (Punctuation Pause) ---
            // 현재 출력된 마지막 글자가 무엇인지 알아야 함
            if (currentStyle.pauseOnPunctuation)
            {
                // textInfo.characterInfo[i-1]에 현재 글자 정보가 들어있음
                char lastChar = textInfo.characterInfo[i - 1].character;
                
                if (lastChar == ',' || lastChar == '.' || lastChar == '?' || lastChar == '!')
                {
                    yield return new WaitForSeconds(currentStyle.typingSpeed * 5.0f);
                }
            }

            // 기본 대기
            yield return new WaitForSeconds(currentStyle.typingSpeed);
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