using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class SceneTriggerController : MonoBehaviour
{
    public RectTransform leftCurtain;
    public RectTransform rightCurtain;
    public float curtainDuration = 1.2f;

    private bool isNearGuide = false;

    void Update()
    {
        if (isNearGuide && Input.GetKeyDown(KeyCode.Space))
        {
            TriggerSceneTransition();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Boss"))
        {
            isNearGuide = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Boss"))
        {
            isNearGuide = false;
        }
    }

    void TriggerSceneTransition()
    {
        // ✅ 撥動畫（布幕）
        leftCurtain.DOAnchorPosX(-480f, curtainDuration).SetEase(Ease.InOutQuad);
        rightCurtain.DOAnchorPosX(480f, curtainDuration).SetEase(Ease.InOutQuad);

        // ✅ 延遲載入場景
        Invoke(nameof(LoadFightingScene), curtainDuration + 0.3f);
    }

    void LoadFightingScene()
    {
        SceneManager.LoadScene("Fighting");
    }
}
