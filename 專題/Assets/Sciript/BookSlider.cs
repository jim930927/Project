using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class BookSlider : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("書本控制")]
    public RectTransform bookWrapper;
    public Vector2 restPos = new Vector2(510f, 14f);      // 書原本位置（關閉）
    public Vector2 slideOutPos = new Vector2(27f, 14f);   // 書滑出位置（展開）
    public float duration = 0.5f;

    private bool isOpen = false;

    [Header("標籤控制")]
    public RectTransform tagRect;
    public Vector2 tagNormalPos;
    public Vector2 tagHoverPos;
    public float tagSlideDuration = 0.2f;

    void Start()
    {
        if (bookWrapper != null)
            bookWrapper.anchoredPosition = restPos;

        if (tagRect != null)
            tagRect.anchoredPosition = tagNormalPos;
    }

    public void ToggleBook()
    {
        isOpen = !isOpen;
        Vector2 target = isOpen ? slideOutPos : restPos;
        bookWrapper.DOAnchorPos(target, duration);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tagRect != null)
            tagRect.DOAnchorPos(tagHoverPos, tagSlideDuration);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tagRect != null)
            tagRect.DOAnchorPos(tagNormalPos, tagSlideDuration);
    }
}
