using UnityEngine;
using System.Collections.Generic;

public class GravityManager : MonoBehaviour
{
    [System.Serializable]
    public struct GravityStage
    {
        public string stageName;   // 에디터에서 알아보기 쉽게 이름 붙이기 (예: "지구", "달", "우주")
        [Range(-2f, 2f)]
        public float gravityScale; // 1이면 기본 중력, 0.5면 절반, 0이면 무중력, 음수면 역중력
    }

    [Header("Settings")]
    public Player_Controller2D player; // 플레이어 스크립트 연결
    public List<GravityStage> gravityStages; // 10단계 설정 리스트

    [Header("Debug")]
    public int currentStageIndex = 0; // 현재 단계 인덱스

    void Start()
    {
        // 시작 시 첫 번째 스테이지 중력 적용
        ApplyGravity();
    }

    void Update()
    {
        // Page Up: 중력 단계 올리기 (다음 스테이지)
        if (Input.GetKeyDown(KeyCode.PageUp))
        {
            if (currentStageIndex < gravityStages.Count - 1)
            {
                currentStageIndex++;
                ApplyGravity();
            }
        }

        // Page Down: 중력 단계 내리기 (이전 스테이지)
        if (Input.GetKeyDown(KeyCode.PageDown))
        {
            if (currentStageIndex > 0)
            {
                currentStageIndex--;
                ApplyGravity();
            }
        }
    }

    void ApplyGravity()
    {
        if (player != null && gravityStages.Count > 0)
        {
            float scale = gravityStages[currentStageIndex].gravityScale;
            player.SetGravityScale(scale); // 플레이어에게 변경된 중력 전달
            
            Debug.Log($"중력 변경됨: {gravityStages[currentStageIndex].stageName} (x{scale})");
        }
    }

    // 화면에 현재 상태 표시 (디버깅용)
    void OnGUI()
    {
        if (gravityStages.Count > 0)
        {
            string status = $"Gravity Stage: {currentStageIndex + 1} / {gravityStages.Count}\n" +
                            $"Mode: {gravityStages[currentStageIndex].stageName}\n" +
                            $"Scale: {gravityStages[currentStageIndex].gravityScale}";
            
            GUI.Box(new Rect(10, 10, 200, 60), status);
        }
    }
}