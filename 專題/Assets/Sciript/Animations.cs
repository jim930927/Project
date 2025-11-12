using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class Animations : MonoBehaviour
{
    [Header("布幕")]
    public RectTransform leftCurtain;
    public RectTransform rightCurtain;

    [Header("動畫設定")]
    public float curtainDuration = 1.2f;

    public Action OnIntroFinished;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(PlayIntro());
    }

    IEnumerator PlayIntro()
    {
        // 播開場動畫
        leftCurtain.DOAnchorPosX(-1600f, curtainDuration).SetEase(Ease.InOutQuad);
        rightCurtain.DOAnchorPosX(1600f, curtainDuration).SetEase(Ease.InOutQuad);
        yield return new WaitForSeconds(curtainDuration + 0.2f);

        // 🚫 不再掉問題卡片，等對話呼叫 DropQuestions()
        OnIntroFinished?.Invoke();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
