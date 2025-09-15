using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuAnimator : MonoBehaviour
{
    [Header("布幕 & 標題")]
    public RectTransform leftCurtain;
    public RectTransform rightCurtain;
    public RectTransform title;

    [Header("按鈕群 (依順序)")]
    public CanvasGroup[] buttons;

    [Header("動畫參數")]
    public float curtainDuration = 2.0f;           // 布幕滑入速度：放慢
    public float titleDropDuration = 1.2f;         // 標題下落
    public float buttonFadeDuration = 0.8f;        // 每顆淡入時間
    public float buttonFadeInterval = 0.3f;        // 每顆按鈕出現間隔

    public string nextSceneName = "Prologue";      // 下一個場景名稱

    void Start()
    {
        Debug.Log("MainMenuAnimator 啟動：" + gameObject.name);
        Debug.Log("布幕位置 = " + leftCurtain.anchoredPosition + " / " + rightCurtain.anchoredPosition);
        StartCoroutine(PlayOpeningSequence());
    }

    IEnumerator PlayOpeningSequence()
    {
        // 初始位置設定
        leftCurtain.anchoredPosition = new Vector2(-1500, 65);
        rightCurtain.anchoredPosition = new Vector2(1500, 65);
        title.anchoredPosition = new Vector2(0, 723);

        foreach (var btn in buttons)
        {
            btn.alpha = 0f;
            btn.interactable = false;
        }

        // 1️⃣ 布幕滑入
        leftCurtain.DOAnchorPosX(-480, curtainDuration).SetEase(Ease.OutQuad);
        yield return rightCurtain.DOAnchorPosX(480, curtainDuration).SetEase(Ease.OutQuad).WaitForCompletion();

        // 2️⃣ 標題掉下來
        yield return title.DOAnchorPosY(388, titleDropDuration).SetEase(Ease.OutBounce).WaitForCompletion();

        // 3️⃣ 按鈕依序淡入
        foreach (var btn in buttons)
        {
            btn.DOFade(1f, buttonFadeDuration);
            btn.interactable = true;
            yield return new WaitForSeconds(buttonFadeInterval);
        }
    }

    // 🎮 按下「新遊戲」後執行的離場動畫與換場
    public void StartNewGameTransition()
    {
        StartCoroutine(ExitAndLoadScene());
    }

    IEnumerator ExitAndLoadScene()
    {
        // 1️⃣ 標題升起
        title.DOAnchorPosY(800, 1.5f).SetEase(Ease.InQuad);

        // 2️⃣ 所有按鈕淡出且禁用
        foreach (var btn in buttons)
        {
            btn.DOFade(0f, 0.8f);
            btn.interactable = false;
        }

        yield return new WaitForSeconds(0.9f);  // 淡出等一等

        // 3️⃣ 布幕往兩側拉開
        leftCurtain.DOAnchorPosX(-1500, 1.8f).SetEase(Ease.InQuad);
        yield return rightCurtain.DOAnchorPosX(1500, 1.8f).SetEase(Ease.InQuad).WaitForCompletion();

        // 4️⃣ 載入下一個場景
        SceneManager.LoadScene(nextSceneName);
    }
}
