using UnityEngine;

public class BattleStarter : MonoBehaviour
{
    public BattleDialogueManager dialogueManager;
    public TextAsset battleInkJson; // 拖入你的戰鬥 ink JSON

    void Start()
    {
        if (dialogueManager == null || battleInkJson == null)
        {
            Debug.LogWarning("⚠️ BattleStarter：請在 Inspector 指定 dialogueManager 與 battleInkJson");
            return;
        }

        dialogueManager.EnterDialogueMode(battleInkJson, "start", OnDialogueEnd);
    }

    void OnDialogueEnd()
    {
        Debug.Log("🏁 戰鬥前對話結束（在這裡接你的戰鬥流程）");
    }
}
