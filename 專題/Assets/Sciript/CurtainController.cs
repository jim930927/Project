using UnityEngine;
using DG.Tweening; // 需要已安裝 DOTween

public class CurtainController : MonoBehaviour
{
    [Header("布幕物件")]
    public RectTransform leftCurtain;
    public RectTransform rightCurtain;

    [Header("動畫參數")]
    public float closeDuration = 1.0f;
    public float openDuration = 1.0f;
    public float targetX = 0f; // 關閉時左右的接合位置（通常 0）

    private Vector2 leftOriginPos;
    private Vector2 rightOriginPos;

    void Awake()
    {
        leftOriginPos = leftCurtain.anchoredPosition;
        rightOriginPos = rightCurtain.anchoredPosition;
    }

    public void OpenCurtain()
    {
        // 拉開（退回原位）
        leftCurtain.DOAnchorPos(leftOriginPos, openDuration);
        rightCurtain.DOAnchorPos(rightOriginPos, openDuration);
    }

    public void CloseCurtain(System.Action onComplete = null)
    {
        // 關閉（往中間合起來）
        Sequence seq = DOTween.Sequence();
        seq.Append(leftCurtain.DOAnchorPos(new Vector2(-targetX, leftOriginPos.y), closeDuration));
        seq.Join(rightCurtain.DOAnchorPos(new Vector2(targetX, rightOriginPos.y), closeDuration));
        seq.OnComplete(() => onComplete?.Invoke());
    }
}
