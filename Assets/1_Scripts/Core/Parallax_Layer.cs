using UnityEngine;

public class Parallax_Layer : MonoBehaviour
{
    [Header("Parallax Settings")]
    [Tooltip("X축(가로) 패럴랙스 강도 (0=고정, 1=완전 따라옴)")]
    [Range(0f, 1f)] public float parallaxX = 0.5f;

    [Tooltip("Y축(세로) 패럴랙스 강도 (0=고정, 1=완전 따라옴)")]
    [Range(0f, 1f)] public float parallaxY = 0.5f;

    private Transform cam;
    private Vector3 lastCamPos;

    void Start()
    {
        if (Camera.main != null) 
            cam = Camera.main.transform;
        
        if (cam != null)
            lastCamPos = cam.position;
    }

    void LateUpdate()
    {
        if (cam == null) return;

        // 1. 카메라가 이번 프레임에 얼마나 움직였는지 계산
        Vector3 deltaMovement = cam.position - lastCamPos;

        // 2. 그 이동량에 패럴랙스 강도를 곱해서 내 위치도 살짝 이동
        transform.position += new Vector3(
            deltaMovement.x * parallaxX, 
            deltaMovement.y * parallaxY, 
            0);

        // 3. 마지막 카메라 위치 갱신
        lastCamPos = cam.position;
    }
}