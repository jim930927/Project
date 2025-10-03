using UnityEngine;

public class BattleStarter : MonoBehaviour
{
    public BattleDialogueManager dialogueManager;
    public TextAsset battleInkJson; // 拖入你的戰鬥 ink JSON

    void Start()
    {
        // 等動畫播完才進入對話
        var animator = FindObjectOfType<FightingAnimator>();
        animator.OnIntroFinished += () =>
        {
            dialogueManager.EnterDialogueMode(battleInkJson, "start", OnDialogueEnd);
        };
    }


    void OnDialogueEnd()
    {
        Debug.Log("🏁 戰鬥前對話結束（在這裡接你的戰鬥流程）");
    }
}
