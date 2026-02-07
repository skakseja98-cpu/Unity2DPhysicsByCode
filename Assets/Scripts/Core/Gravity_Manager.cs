using UnityEngine;
using System.Collections.Generic;

public class GravityManager : MonoBehaviour
{
    // 어디서든 GravityManager.Instance로 접근 가능하게 함
    public static GravityManager Instance;

    [System.Serializable]
    public struct GravityStage
    {
        public string stageName;   // 예: "0_지구", "1_성층권", "2_우주"
        [Range(-2f, 2f)]
        public float gravityScale; 
    }

    [Header("Settings")]
    public Player_Controller player; 
    public List<GravityStage> gravityStages; 

    [Header("Debug Info")]
    public int currentStageIndex = 0; 

    void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        ApplyGravity();
    }

    // [추가] 외부(트리거)에서 호출하는 함수
    public void SetGravityStage(int index)
    {
        // 인덱스 유효성 검사
        if (index < 0 || index >= gravityStages.Count)
        {
            Debug.LogWarning($"[GravityManager] 잘못된 인덱스 호출됨: {index}");
            return;
        }

        // 이미 같은 단계라면 무시 (최적화)
        if (currentStageIndex == index) return;

        currentStageIndex = index;
        ApplyGravity();
    }

    void ApplyGravity()
    {
        if (player != null && gravityStages.Count > 0)
        {
            float scale = gravityStages[currentStageIndex].gravityScale;
            player.SetGravityScale(scale); 
            
            Debug.Log($"중력 변경됨: {gravityStages[currentStageIndex].stageName} (x{scale})");
        }
    }

    // 디버그 UI (선택 사항)
    void OnGUI()
    {
        if (gravityStages.Count > 0)
        {
            string status = $"Zone: {gravityStages[currentStageIndex].stageName}\n" +
                            $"Gravity: {gravityStages[currentStageIndex].gravityScale}";
            GUI.Box(new Rect(10, 10, 200, 50), status);
        }
    }
}