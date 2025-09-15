using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class FightingAnimator : MonoBehaviour
{
    [Header("����")]
    public RectTransform leftCurtain;
    public RectTransform rightCurtain;

    [Header("���D�d��")]
    public RectTransform question1;
    public RectTransform question2;
    public RectTransform question3;

    [Header("�޸��H")]
    public CanvasGroup guideCharacter;

    [Header("�ʵe�]�w")]
    public float curtainDuration = 1.2f;
    public float questionDropDuration = 0.8f;
    public float questionDropDelay = 0.3f;
    public float guideFadeInDuration = 1.0f;

    void Start()
    {
        StartCoroutine(PlayBattleIntro());
    }

    IEnumerator PlayBattleIntro()
    {
        // �����Զ}�ʵe
        leftCurtain.DOAnchorPosX(-1600f, curtainDuration).SetEase(Ease.InOutQuad);
        rightCurtain.DOAnchorPosX(1600f, curtainDuration).SetEase(Ease.InOutQuad);
        yield return new WaitForSeconds(curtainDuration + 0.2f);

        // �޸��H�H�J
        yield return guideCharacter.DOFade(1f, guideFadeInDuration).WaitForCompletion();

        // ���D�d������
        question1.DOAnchorPosY(-200f, questionDropDuration).SetEase(Ease.OutBounce);
        yield return new WaitForSeconds(questionDropDelay);
        question2.DOAnchorPosY(20f, questionDropDuration).SetEase(Ease.OutBounce);
        yield return new WaitForSeconds(questionDropDelay);
        question3.DOAnchorPosY(145f, questionDropDuration).SetEase(Ease.OutBounce);
        yield return new WaitForSeconds(questionDropDuration + 0.3f);

        
    }
}
