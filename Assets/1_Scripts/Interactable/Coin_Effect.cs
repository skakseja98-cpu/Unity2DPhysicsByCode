using UnityEngine;

public class Coin_Effect : MonoBehaviour
{
    [Tooltip("이펙트가 유지될 시간(초). 오디오 길이나 파티클 길이에 맞춰 여유롭게 설정하세요.")]
    public float destroyDelay = 2.0f;

    void Start()
    {
        // 생성된 지 destroyDelay 초 뒤에 자기 자신(이펙트 프리팹)을 파괴합니다.
        Destroy(gameObject, destroyDelay);
    }
}