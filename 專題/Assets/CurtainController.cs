using UnityEngine;
using DG.Tweening; // �ݭn�w�w�� DOTween

public class CurtainController : MonoBehaviour
{
    [Header("��������")]
    public RectTransform leftCurtain;
    public RectTransform rightCurtain;

    [Header("�ʵe�Ѽ�")]
    public float closeDuration = 1.0f;
    public float openDuration = 1.0f;
    public float targetX = 0f; // �����ɥ��k�����X��m�]�q�` 0�^

    private Vector2 leftOriginPos;
    private Vector2 rightOriginPos;

    void Awake()
    {
        leftOriginPos = leftCurtain.anchoredPosition;
        rightOriginPos = rightCurtain.anchoredPosition;
    }

    public void OpenCurtain()
    {
        // �Զ}�]�h�^���^
        leftCurtain.DOAnchorPos(leftOriginPos, openDuration);
        rightCurtain.DOAnchorPos(rightOriginPos, openDuration);
    }

    public void CloseCurtain(System.Action onComplete = null)
    {
        // �����]�������X�_�ӡ^
        Sequence seq = DOTween.Sequence();
        seq.Append(leftCurtain.DOAnchorPos(new Vector2(-targetX, leftOriginPos.y), closeDuration));
        seq.Join(rightCurtain.DOAnchorPos(new Vector2(targetX, rightOriginPos.y), closeDuration));
        seq.OnComplete(() => onComplete?.Invoke());
    }
}
