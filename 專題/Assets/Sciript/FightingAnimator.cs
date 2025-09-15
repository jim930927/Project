using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class FightingAnimator : MonoBehaviour
{
    [Header("布幕")]
    public RectTransform leftCurtain;
    public RectTransform rightCurtain;

    [Header("問題卡片")]
    public RectTransform question1;
    public RectTransform question2;
    public RectTransform question3;

    [Header("引路人")]
    public CanvasGroup guideCharacter;

    [Header("動畫設定")]
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
        // 布幕拉開動畫
        leftCurtain.DOAnchorPosX(-1600f, curtainDuration).SetEase(Ease.InOutQuad);
        rightCurtain.DOAnchorPosX(1600f, curtainDuration).SetEase(Ease.InOutQuad);
        yield return new WaitForSeconds(curtainDuration + 0.2f);

        // 引路人淡入
        yield return guideCharacter.DOFade(1f, guideFadeInDuration).WaitForCompletion();

        // 問題卡片掉落
        question1.DOAnchorPosY(-200f, questionDropDuration).SetEase(Ease.OutBounce);
        yield return new WaitForSeconds(questionDropDelay);
        question2.DOAnchorPosY(20f, questionDropDuration).SetEase(Ease.OutBounce);
        yield return new WaitForSeconds(questionDropDelay);
        question3.DOAnchorPosY(145f, questionDropDuration).SetEase(Ease.OutBounce);
        yield return new WaitForSeconds(questionDropDuration + 0.3f);

        
    }
}
