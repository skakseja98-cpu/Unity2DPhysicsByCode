using UnityEngine;

public class Background_Manager : MonoBehaviour
{
    [System.Serializable]
    public struct SkyColorStage
    {
        public string name;      // 단계 이름 (예: 지상, 성층권, 우주)
        public float height;     // 해당 색상이 완벽하게 적용되는 높이 (Y좌표)
        public Color color;      // 적용할 색상
    }

    [Header("Sky Settings")]
    public SkyColorStage[] skyStages; // 인스펙터에서 설정할 단계들

    private Camera mainCam;
    private Transform player;

    void Start()
    {
        mainCam = GetComponent<Camera>();
        
        // 싱글톤으로 플레이어 찾기
        if (Player_Controller.Instance != null)
            player = Player_Controller.Instance.transform;
    }

    void Update()
    {
        if (player == null || skyStages.Length < 2) return;

        float playerY = player.position.y;
        
        // 현재 플레이어 높이에 맞는 두 단계(stage)를 찾아서 색을 섞음(Lerp)
        for (int i = 0; i < skyStages.Length - 1; i++)
        {
            SkyColorStage current = skyStages[i];
            SkyColorStage next = skyStages[i + 1];

            // 플레이어가 이 두 단계 사이에 있다면
            if (playerY >= current.height && playerY <= next.height)
            {
                // 진행률(0.0 ~ 1.0) 계산
                float t = (playerY - current.height) / (next.height - current.height);
                
                // 색상 섞기 (Lerp)
                mainCam.backgroundColor = Color.Lerp(current.color, next.color, t);
                return;
            }
        }

        // 범위 밖 처리 (가장 아래보다 낮거나, 가장 위보다 높을 때)
        if (playerY < skyStages[0].height)
            mainCam.backgroundColor = skyStages[0].color;
        else if (playerY > skyStages[skyStages.Length - 1].height)
            mainCam.backgroundColor = skyStages[skyStages.Length - 1].color;
    }
}