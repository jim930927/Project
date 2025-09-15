using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public MainMenuAnimator animator;  // 拖入剛剛的動畫腳本物件

    public void StartNewGame()
    {
        animator.StartNewGameTransition();  // 呼叫動畫 + 換場
    }
}
