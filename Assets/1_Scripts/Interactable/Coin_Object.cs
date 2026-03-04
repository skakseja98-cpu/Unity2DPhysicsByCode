using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Coin_Object : MonoBehaviour
{
    [Header("Effect Settings")]
    [Tooltip("코인 획득 시 제자리에서 생성될 이펙트(사운드+파티클) 프리팹")]
    public GameObject effectPrefab;

    private void Awake()
    {
        // 안전을 위해 콜라이더가 무조건 트리거로 작동하게 설정합니다.
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 충돌한 대상이 플레이어인지 확인합니다.
        Player_Controller player = collision.GetComponent<Player_Controller>();

        if (player != null)
        {
            // 1. 이펙트 생성 (이펙트 프리팹이 등록되어 있을 경우)
            if (effectPrefab != null)
            {
                Instantiate(effectPrefab, transform.position, Quaternion.identity);
            }

            // 2. 코인 오브젝트 즉시 파괴
            Destroy(gameObject);
        }
    }
}