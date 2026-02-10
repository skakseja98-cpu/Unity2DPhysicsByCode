using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class InteractionUIManager : MonoBehaviour
{
    public static InteractionUIManager Instance;

    // ... (기존 변수들 생략) ...
    [Header("--- 서류 UI ---")]
    public GameObject docPanel;
    public Image docImageSlot;

    [Header("--- 대화 UI ---")]
    public GameObject dialogBoxPrefab;
    private GameObject currentDialogBox;
    private TextMeshProUGUI dialogText;

    [Header("--- 기본 사운드 설정 ---")]
    public AudioSource audioSource;
    public AudioClip defaultTypingClip; // 공용 삑 소리 (이름 변경됨!)

    // 🔴 현재 말하고 있는 NPC의 정보 저장용 변수
    private float currentTypingSpeed = 0.05f;
    private float currentVoicePitch = 1.0f;
    private AudioClip currentVoiceClip;

    // ... (변수들 생략) ...
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

    // 🔴 중요: 파라미터가 늘어났습니다!
    public void StartDialog(Vector3 position, string[] lines, float speed, float pitch, AudioClip clip)
    {
        CloseDialog(); // 기존 대화 닫기

        // 1. NPC가 준 정보 받아적기
        currentTypingSpeed = speed;
        currentVoicePitch = pitch;
        
        // NPC 전용 소리가 있으면 그거 쓰고, 없으면(null) 기본 소리 쓰기
        if (clip != null) currentVoiceClip = clip;
        else currentVoiceClip = defaultTypingClip;

        // 2. 대화 준비
        sentences.Clear();
        foreach (string line in lines) sentences.Enqueue(line);

        currentDialogBox = Instantiate(dialogBoxPrefab, position + new Vector3(0, 1.5f, 0), Quaternion.identity);
        dialogText = currentDialogBox.GetComponentInChildren<TextMeshProUGUI>();

        NextSentence();
    }

    // ... (AdvanceDialog, NextSentence, CloseDialog 등은 건드릴 필요 없음) ...
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
        if (currentDialogBox != null)
        {
            Destroy(currentDialogBox);
            currentDialogBox = null;
        }
        isTyping = false;
    }

    public bool IsDialogOpen() => currentDialogBox != null;


    // 🔴 타자기 효과 수정
    IEnumerator TypewriterEffect(string fullText)
    {
        isTyping = true;
        dialogText.text = ""; 
        int charCount = 0;

        foreach (char letter in fullText.ToCharArray())
        {
            dialogText.text += letter;
            charCount++;

            // 공백 아니고, 2글자마다 소리 재생 (취향따라 1이나 3으로 변경 가능)
            if (letter != ' ' && charCount % 2 == 0)
            {
                PlayTypingSound();
            }

            // 🔴 여기가 핵심! NPC가 정한 속도만큼 기다림
            yield return new WaitForSeconds(currentTypingSpeed); 
        }
        isTyping = false;
    }

    // 🔴 소리 재생 함수 수정
    void PlayTypingSound()
    {
        if (audioSource != null && currentVoiceClip != null)
        {
            // NPC가 정한 피치에 약간의 랜덤성(±0.1)을 더해서 자연스럽게
            audioSource.pitch = Random.Range(currentVoicePitch - 0.1f, currentVoicePitch + 0.1f);
            audioSource.PlayOneShot(currentVoiceClip);
        }
    }

    // ... (서류 관련 함수들 유지) ...
    public void ShowDocument(Sprite docSprite)
    {
        docImageSlot.sprite = docSprite;
        docImageSlot.preserveAspect = true;
        docPanel.SetActive(true);
    }
    public void CloseDocument() => docPanel.SetActive(false);
    public bool IsDocumentOpen() => docPanel.activeSelf;
}