using UnityEngine;
using DG.Tweening;

public class DOTweenInitializer : MonoBehaviour
{
    private static bool initialized = false;

    void Awake()
    {
        if (!initialized)
        {
            DOTween.Init(false, true);
            DontDestroyOnLoad(gameObject); // 確保只初始化一次
            initialized = true;
            Debug.Log("✅ DOTween 初始化完成");
        }
        else
        {
            Destroy(gameObject); // 防止場景切換時重複
        }
    }
}
