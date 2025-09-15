using UnityEngine;

public class TalkToBoss : MonoBehaviour
{
    public TextAsset inkJSONAsset; // 指定 Ink 劇本
    public InkDialogueManager dialogueManager; // 指向對話控制器

    private bool isPlayerNear = false;

    void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(KeyCode.Space))
        {
            // ✅ 加入檢查：正在對話中就不要再觸發
            if (dialogueManager != null && inkJSONAsset != null && !dialogueManager.dialogueIsPlaying)
            {
                dialogueManager.EnterDialogueMode(inkJSONAsset, "boss_talk");
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            isPlayerNear = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            isPlayerNear = false;
    }
}
