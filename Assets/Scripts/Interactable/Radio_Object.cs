using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))] // 범위 확인용 트리거
public class Radio_Object : MonoBehaviour
{
    [Header("Radio Settings")]
    [Tooltip("플레이어가 이 거리 안으로 들어오면 소리가 들리기 시작합니다.")]
    public float soundDistance = 10f;

    [Header("Audio Sources")]
    [Tooltip("방송 내용 (목소리/음악)")]
    public AudioSource voiceSource;
    
    [Tooltip("지지직거리는 잡음 (노이즈)")]
    public AudioSource noiseSource;

    [Header("Volume Control (Manual)")]
    [Range(0f, 1f)] public float voiceVolume = 1.0f; // 님이 직접 조절
    [Range(0f, 1f)] public float noiseVolume = 0.5f; // 님이 직접 조절

    private Transform playerTransform;

    void Start()
    {
        // 1. 플레이어 찾기
        playerTransform = Player_Controller.Instance.transform;

        // 2. 오디오 소스 설정 (3D 사운드)
        SetupAudioSource(voiceSource);
        SetupAudioSource(noiseSource);

        // 3. 재생 시작 (Loop)
        if (voiceSource.clip != null) voiceSource.Play();
        if (noiseSource.clip != null) noiseSource.Play();
    }

    void SetupAudioSource(AudioSource source)
    {
        if (source == null) return;
        
        source.loop = true;
        source.playOnAwake = true;
        source.spatialBlend = 1.0f; // 1.0이면 완전 3D 사운드 (거리에 따라 작아짐)
        source.minDistance = 1f;    // 가장 크게 들리는 최소 거리
        source.maxDistance = soundDistance; // 소리가 들리는 최대 거리
        source.rolloffMode = AudioRolloffMode.Linear; // 거리에 따라 일정하게 줄어듦
    }

    void Update()
    {
        if (playerTransform == null) return;

        // 플레이어와의 거리 계산 대신, AudioSource의 내장 기능(Rolloff)을 믿고
        // 여기서는 '최대 볼륨'만 조절해줍니다.
        // (거리에 따른 소리 크기 변화는 유니티 엔진이 알아서 해줍니다)

        if (voiceSource != null) voiceSource.volume = voiceVolume;
        if (noiseSource != null) noiseSource.volume = noiseVolume;
    }

    // 에디터에서 범위 보여주기
    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, soundDistance);
    }
}