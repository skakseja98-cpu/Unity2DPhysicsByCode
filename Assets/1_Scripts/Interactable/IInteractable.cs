using UnityEngine;

public interface IInteractable
{
    // 가까이 갔을 때 (아웃라인 켜기)
    void OnFocus(); 

    // 멀어졌을 때 (아웃라인 끄기)
    void OnDefocus(); 

    // F키 눌렀을 때 (대화하기 or 팝업 띄우기)
    void OnInteract(); 
}