using UnityEngine;

public class Document_Object : MonoBehaviour, IInteractable
{
    private SpriteRenderer sr;
    public Sprite documentImage; // 팝업으로 보여줄 큰 이미지

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void OnFocus()
    {
        sr.color = Color.yellow; // 노랗게 빛남
    }

    public void OnDefocus()
{
    InteractionUIManager.Instance.CloseDocument();
}

    public void OnInteract() // override로 변경 (IInteractable이 인터페이스라면 그냥 둠)
{
    // 이미 열려있으면 닫기 (토글 기능)
    if (InteractionUIManager.Instance.IsDocumentOpen())
    {
        InteractionUIManager.Instance.CloseDocument();
    }
    else
    {
        InteractionUIManager.Instance.ShowDocument(documentImage);
    }
}
}