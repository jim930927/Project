using UnityEngine;

public class TalkToBoss : MonoBehaviour
{
    public TextAsset inkJSONAsset; // 指定 Ink 劇本
    public firstDialogueManager dialogueManager; // 指向對話控制器
    public Player player;

    private bool isPlayerNear = false;

    void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(KeyCode.Space))
        {
            // 🚩 檢查：不能在對話中，也不能在冷卻中
            if (dialogueManager != null && inkJSONAsset != null
                && !dialogueManager.dialogueIsPlaying)
            {
                Debug.Log("boss_talk");
                dialogueManager.EnterDialogueMode(inkJSONAsset, "boss_talk");
            }
            else
                Debug.Log("not_talk");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (player.CompareTag("Player"))
        {
            Debug.Log("Player");
            isPlayerNear = true; 
        }

    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (player.CompareTag("Player"))
            isPlayerNear = false;
    }
}
