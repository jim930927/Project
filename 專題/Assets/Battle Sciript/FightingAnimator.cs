using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

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

    public Action OnIntroFinished;

    private Vector2 q1StartPos, q2StartPos, q3StartPos;
    private Vector2 q1DropPos, q2DropPos, q3DropPos;

    void Start()
    {
        // 一開始記錄初始位置
        q1StartPos = question1.anchoredPosition;
        q2StartPos = question2.anchoredPosition;
        q3StartPos = question3.anchoredPosition;

        // 設定掉落後的位置（可依實際UI調整）
        q1DropPos = new Vector2(q1StartPos.x, 100f);
        q2DropPos = new Vector2(q2StartPos.x, -50f);
        q3DropPos = new Vector2(q3StartPos.x, 400f);

        StartCoroutine(PlayBattleIntro());
    }

    IEnumerator PlayBattleIntro()
    {
        // 播開場動畫
        leftCurtain.DOAnchorPosX(-1600f, curtainDuration).SetEase(Ease.InOutQuad);
        rightCurtain.DOAnchorPosX(1600f, curtainDuration).SetEase(Ease.InOutQuad);
        yield return new WaitForSeconds(curtainDuration + 0.2f);

        yield return guideCharacter.DOFade(1f, guideFadeInDuration).WaitForCompletion();

        // 🚫 不再掉問題卡片，等對話呼叫 DropQuestions()
        OnIntroFinished?.Invoke();
    }

    public IEnumerator DropQuestions(int count)
    {
        // 確保不超過 3 張
        count = Mathf.Clamp(count, 1, 3);

        if (count >= 1)
        {
            question1.DOAnchorPos(q1DropPos, questionDropDuration).SetEase(Ease.OutBounce);
            yield return new WaitForSeconds(questionDropDelay);
        }
        if (count >= 2)
        {
            question2.DOAnchorPos(q2DropPos, questionDropDuration).SetEase(Ease.OutBounce);
            yield return new WaitForSeconds(questionDropDelay);
        }
        if (count >= 3)
        {
            question3.DOAnchorPos(q3DropPos, questionDropDuration).SetEase(Ease.OutBounce);
            yield return new WaitForSeconds(questionDropDuration + 0.3f);
        }
    }


    public IEnumerator RaiseQuestions()
    {
        question1.DOAnchorPos(q1StartPos, 0.6f).SetEase(Ease.InOutQuad);
        yield return new WaitForSeconds(0.1f);
        question2.DOAnchorPos(q2StartPos, 0.6f).SetEase(Ease.InOutQuad);
        yield return new WaitForSeconds(0.1f);
        question3.DOAnchorPos(q3StartPos, 0.6f).SetEase(Ease.InOutQuad);
    }
}
