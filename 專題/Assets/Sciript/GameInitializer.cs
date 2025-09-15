using UnityEngine;
using DG.Tweening;

public class GameInitializer : MonoBehaviour
{
    private static bool initialized = false;

    void Awake()
    {
        if (!initialized)
        {
            DOTween.Init(false, true, LogBehaviour.ErrorsOnly);
            DontDestroyOnLoad(this.gameObject);
            initialized = true;
        }
        else
        {
            Destroy(gameObject); // ­Y­«½Æ¡Aª½±µ¬å±¼
        }
    }
}
