using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections; // <--- 加這行才能使用 Coroutine

public class PlayIntroVideo : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public string nextSceneName = "MainMenu";
    public Image fadeImage; // UI 黑幕圖層

    public float fadeDuration = 1.5f;
    public float delayAfterFade = 0.3f; // ✅ 新增切場景延遲時間

    void Start()
    {
        // 開始時黑幕透明
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }

        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.Play();
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        if (fadeImage != null)
        {
            // 黑幕淡入 → 等待完成後延遲再切場景
            fadeImage.DOFade(1f, fadeDuration).OnComplete(() =>
            {
                StartCoroutine(DelayedLoadScene());
            });
        }
        else
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    IEnumerator DelayedLoadScene()
    {
        yield return new WaitForSeconds(delayAfterFade);
        SceneManager.LoadScene(nextSceneName);
    }
}
