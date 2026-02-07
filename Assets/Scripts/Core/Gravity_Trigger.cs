using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class GravityTrigger : MonoBehaviour
{
    [Header("Stage Settings (Method A)")]
    [Tooltip("이 구역의 '아래쪽'으로 나갔을 때 적용할 중력 단계 인덱스 (예: 지구=0)")]
    public int lowerStageIndex; 

    [Tooltip("이 구역의 '위쪽'으로 나갔을 때 적용할 중력 단계 인덱스 (예: 우주=1)")]
    public int upperStageIndex;

    [Header("Debug")]
    public bool showGizmos = true;
    public Color boxColor = new Color(0, 1, 1, 0.3f); // 하늘색

    private BoxCollider2D boxCol;

    private void Awake()
    {
        boxCol = GetComponent<BoxCollider2D>();
        boxCol.isTrigger = true; // 강제로 트리거 설정
    }

    // 핵심 로직: 트리거를 '나갈 때' 판단함
    private void OnTriggerExit2D(Collider2D collision)
    {
        // 플레이어인지 확인
        Player_Controller player = collision.GetComponent<Player_Controller>();

        if (player != null)
        {
            // 트리거의 중심 Y좌표 (Global Position)
            float triggerCenterY = transform.position.y + boxCol.offset.y;
            
            // 플레이어의 현재 Y좌표
            float playerY = player.transform.position.y;

            // 판단 로직
            if (playerY > triggerCenterY)
            {
                // 중심선보다 위로 나갔다 -> 위쪽 스테이지 중력 적용
                GravityManager.Instance.SetGravityStage(upperStageIndex);
                // Debug.Log($"[GravityTrigger] 위로 탈출! Stage {upperStageIndex} 적용");
            }
            else
            {
                // 중심선보다 아래로 나갔다 -> 아래쪽 스테이지 중력 적용
                GravityManager.Instance.SetGravityStage(lowerStageIndex);
                // Debug.Log($"[GravityTrigger] 아래로 탈출! Stage {lowerStageIndex} 적용");
            }
        }
    }

    // 에디터에서 구역을 쉽게 보기 위한 시각화
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        if (boxCol == null) boxCol = GetComponent<BoxCollider2D>();
        
        Gizmos.color = boxColor;
        Vector3 center = transform.position + (Vector3)boxCol.offset;
        Gizmos.DrawCube(center, boxCol.size);
        Gizmos.DrawWireCube(center, boxCol.size);

        // 경계선(중심선) 표시 - 이 선을 기준으로 위/아래 판정
        Gizmos.color = Color.red;
        Gizmos.DrawLine(center + Vector3.left * boxCol.size.x / 2, center + Vector3.right * boxCol.size.x / 2);
    }
}