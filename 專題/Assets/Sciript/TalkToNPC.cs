using UnityEngine;

public class TalkToNPC : MonoBehaviour
{
    public TextAsset inkJSONAsset; // 對話劇本
    public InkDialogueManager dialogueManager; // Ink 對話控制器
    public string knotName = "main_npc_talk"; // 這個 NPC 要跳轉的節點

    private bool isPlayerNear = false;
    private bool hasTalked = false;

    void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(KeyCode.Space) && !hasTalked)
        {
            if (dialogueManager != null && inkJSONAsset != null)
            {
                hasTalked = true;
                dialogueManager.EnterDialogueMode(inkJSONAsset, knotName);
            }
            else
            {
                Debug.LogWarning("⚠️ inkJSONAsset 或 dialogueManager 未設定！");
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

    public void ResetTalk()
    {
        hasTalked = false;
    }
}
