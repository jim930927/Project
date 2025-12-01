using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.SceneManagement;
using System.Collections;

public class FailedButton : MonoBehaviour
{
    public RectTransform leftCurtain;
    public RectTransform rightCurtain;
    public Vector2 leftClosePos = new Vector2(-425, -50);
    public Vector2 rightClosePos = new Vector2(425, -50);
    public float curtainCloseDuration = 1.2f;
    public string AgainScene;
    public string GiveUpName;

    public void Return()
    {
        StartCoroutine(WaitCurtainReturn());
    }

    public void Retry()
    {
        StartCoroutine(WaitCurtainRetry());
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    IEnumerator WaitCurtainReturn()
    {
        Sequence seq = DOTween.Sequence();
        seq.Append(leftCurtain.DOAnchorPos(leftClosePos, curtainCloseDuration));
        seq.Join(rightCurtain.DOAnchorPos(rightClosePos, curtainCloseDuration));
        yield return seq.WaitForCompletion();

        yield return new WaitForSeconds(0.3f);
        SceneManager.LoadScene(GiveUpName);
    }

    IEnumerator WaitCurtainRetry()
    {
        Sequence seq = DOTween.Sequence();
        seq.Append(leftCurtain.DOAnchorPos(leftClosePos, curtainCloseDuration));
        seq.Join(rightCurtain.DOAnchorPos(rightClosePos, curtainCloseDuration));
        yield return seq.WaitForCompletion();

        yield return new WaitForSeconds(0.3f);
        SceneManager.LoadScene(AgainScene);
    }
}
